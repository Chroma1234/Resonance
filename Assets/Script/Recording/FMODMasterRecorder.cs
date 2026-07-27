using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using FMOD;
using FMODUnity;
using UnityEngine;
using TMPro;
using System.Collections;

using Debug = UnityEngine.Debug;
using Thread = System.Threading.Thread;


// This script records the FMOD Master Bus
// and exports it as a WAV file.
//
// Responsibilities:
// - Capture audio from FMOD.
// - Convert the audio into stereo.
// - Save the recording as a WAV file.
// - Display recording status in the UI.
// - Clean up resources when recording ends.

public class FMODMasterRecorder : MonoBehaviour
{
    [Header("Recording File")]

    // Base name of the exported recording file.
    // A timestamp is added automatically
    // to prevent duplicate filenames.
    [SerializeField]
    private string fileName = "Resonance";

    [Header("Recording Buffer")]

    // Controls the size of the temporary
    // audio buffer used while recording.
    //
    // A larger buffer reduces the chance
    // of losing audio samples.
    [SerializeField]
    [Range(2, 30)]
    private int ringBufferSeconds = 10;

    // The exported WAV file always uses
    // stereo audio with 16-bit PCM.
    private const int RecordingChannels = 2;
    private const short BitsPerSample = 16;

    // The FMOD Master Bus that all game audio passes through.
    private ChannelGroup masterChannelGroup;

    // The custom DSP used to capture audio.
    private DSP recordingDSP;

    // Callback that FMOD calls every time
    // new audio is generated.
    private DSP_READ_CALLBACK recordingCallback;

    // Stores the currently active recorder.
    //
    // The FMOD callback is static,
    // so it needs a reference back
    // to this recorder instance.
    private static FMODMasterRecorder activeRecorder;

    // True while recording is in progress.
    private volatile bool isRecording;

    // True once the recorder has
    // been successfully initialized.
    private bool isReady;

    // Stores FMOD's sample rate.
    // Default is 48 kHz.
    private int sampleRate = 48000;

    // Temporary buffer that stores
    // stereo audio samples before
    // they are written to the WAV file.
    private float[] ringBuffer;

    // Current write position.
    private int writePosition;

    // Current read position.
    private int readPosition;

    /*
     * Temporary audio buffers used
     * inside the FMOD callback.
     *
     * callbackInputBuffer stores
     * the original FMOD audio.
     *
     * callbackStereoBuffer stores
     * the converted stereo audio.
     */
    private readonly float[] callbackInputBuffer =
        new float[65536];

    private readonly float[] callbackStereoBuffer =
        new float[65536];


    // Separate thread that writes
    // recorded audio into the WAV file.
    //
    // Using another thread prevents
    // file writing from freezing the game.
    private Thread writerThread;

    // Controls whether the writer thread
    // should continue running.
    private volatile bool writerShouldRun;

    // File used to save the recording.
    private FileStream outputStream;

    // Writes binary WAV data.
    private BinaryWriter outputWriter;

    // Total number of audio bytes written.
    private long writtenDataBytes;

    // Full path of the saved recording.
    private string currentFilePath;

    // Stores any errors from the writer thread.
    private string writerError;

    // Counts audio samples that
    // could not fit inside the ring buffer.
    private int droppedSamples;


    [Header("UI Feedback")]

    // Text that displays the
    // current recording status.
    [SerializeField]
    private TMP_Text recordingStatusText;


    // Messages shown in the UI.
    [SerializeField] private string readyMessage = "Ready to record";
    [SerializeField] private string recordingMessage = "? Recording...";
    [SerializeField] private string savedMessage = "Recording saved!";


    // How long the "Recording saved"
    // message stays on screen.
    [SerializeField] private float savedMessageDuration = 2f;


    // Reference to the UI coroutine.
    private Coroutine statusCoroutine;

    // Read-only property that allows
    // other scripts to check whether
    // recording is currently active.
    public bool IsRecording
    {
        get { return isRecording; }
    }

    private void Start()
    {
        // Prepare the recorder when
        // the scene starts.
        SetupRecorder();

        // Display the default UI message.
        UpdateStatusText(readyMessage);
    }

    // Initializes the FMOD recorder.
    //
    // This method only runs once
    // before recording starts.
    private void SetupRecorder()
    {
        // Prevent initializing twice.
        if (isReady)
        {
            return;
        }

        activeRecorder = this;

        // Store this recorder so the
        // static FMOD callback can access it.
        recordingCallback = CaptureAudio;

        RESULT result =
            RuntimeManager.CoreSystem.getMasterChannelGroup(
                out masterChannelGroup
            );

        if (result != RESULT.OK)
        {
            Debug.LogError(
                "Could not get FMOD Master Channel Group: " +
                result
            );

            return;
        }

        result =
            RuntimeManager.CoreSystem.getSoftwareFormat(
                out sampleRate,
                out SPEAKERMODE speakerMode,
                out int rawSpeakerCount
            );

        if (result != RESULT.OK)
        {
            Debug.LogWarning(
                "Could not read FMOD sample rate. " +
                "Using 48000 Hz."
            );

            sampleRate = 48000;
        }

        /*
         * The recording buffer only stores stereo samples,
         * regardless of FMOD's speaker/channel configuration.
         */
        int ringBufferSize =
            sampleRate *
            RecordingChannels *
            Mathf.Max(2, ringBufferSeconds);

        ringBuffer =
            new float[ringBufferSize];

        DSP_DESCRIPTION description =
            new DSP_DESCRIPTION();

        description.pluginsdkversion =
            FMOD.VERSION.number;

        description.numinputbuffers = 1;
        description.numoutputbuffers = 1;
        description.read = recordingCallback;

        description.name =
            new byte[32];

        byte[] dspName =
            Encoding.ASCII.GetBytes(
                "Stereo Master WAV Recorder"
            );

        Array.Copy(
            dspName,
            description.name,
            Mathf.Min(
                dspName.Length,
                description.name.Length - 1
            )
        );

        result =
            RuntimeManager.CoreSystem.createDSP(
                ref description,
                out recordingDSP
            );

        if (result != RESULT.OK)
        {
            Debug.LogError(
                "Could not create recording DSP: " +
                result
            );

            return;
        }

        result =
            masterChannelGroup.addDSP(
                CHANNELCONTROL_DSP_INDEX.TAIL,
                recordingDSP
            );

        if (result != RESULT.OK)
        {
            Debug.LogError(
                "Could not attach recording DSP: " +
                result
            );

            recordingDSP.release();
            return;
        }

        isReady = true;

        Debug.Log(
            "FMOD Master Recorder is ready."
        );
    }

    // Connect this to the Record button.
    //
    // Starts recording by:
    // 1. Checking that the recorder is ready.
    // 2. Creating a new WAV file.
    // 3. Writing an empty WAV header.
    // 4. Starting the writer thread.
    public void StartRecording()
    {
        // If the recorder has not been initialized,
        // set it up before recording.
        if (!isReady)
        {
            SetupRecorder();
        }

        // Stop if the recorder
        // still could not be initialized.
        if (!isReady)
        {
            Debug.LogError(
                "FMOD Master Recorder is not ready."
            );

            return;
        }

        // Prevent multiple recordings
        // from running at the same time.
        if (isRecording)
        {
            Debug.LogWarning(
                "Recording is already running."
            );

            return;
        }

        // Reset all recording variables
        // before starting a new recording.
        ResetRecordingState();

        // Create a unique filename by
        // appending the current date and time.
        string finalFileName =
            fileName + "_" +
            DateTime.Now.ToString("yyyyMMdd_HHmmss") +
            ".wav";

        // Save the recording inside
        // Unity's persistent data folder.
        currentFilePath =
            Path.Combine(
                Application.persistentDataPath,
                finalFileName
            );

        try
        {
            // Create the WAV file.
            outputStream =
                new FileStream(
                    currentFilePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read
                );

            // BinaryWriter is used to
            // write binary audio data.
            outputWriter =
                new BinaryWriter(outputStream);

            // Write an empty WAV header first.
            // The header will be updated
            // after recording finishes.
            WriteEmptyWavHeader();

            // Allow the writer thread
            // to begin writing audio.
            writerShouldRun = true;

            // Create the background thread
            // that writes audio to disk.
            writerThread =
                new Thread(WriterThreadLoop);

            // Give the thread a readable name.
            writerThread.Name =
                "FMOD WAV Writer";

            // Run the thread in the background.
            writerThread.IsBackground = true;
            // Start the background thread.
            // It continuously writes
            // recorded audio into the WAV file.
            writerThread.Start();

            // Recording has now started.
            isRecording = true;

            // Update the UI so the user knows
            // recording is currently active.
            UpdateStatusText(recordingMessage);

            Debug.Log(
                "FMOD Master Bus recording started."
            );
        }
        catch (Exception exception)
        {
            // Close the file if something
            // went wrong during setup.
            CloseOutputFile();

            Debug.LogError(
                "Could not start recording:\n" +
                exception.Message
            );
        }
    }

    // Connect this to the Stop Recording button.
    //
    // Stops recording, waits for the writer thread
    // to finish, updates the WAV header,
    // and saves the completed recording.
    public void StopRecordingAndSave()
    {
        // Make sure a recording is
        // currently in progress.
        if (!isRecording)
        {
            Debug.LogWarning(
                "There is no recording to stop."
            );

            return;
        }

        // Stop accepting new audio samples.
        isRecording = false;

        // Tell the writer thread to
        // finish writing any remaining audio.
        writerShouldRun = false;

        // Wait until the writer thread
        // finishes before closing the file.
        if (writerThread != null &&
            writerThread.IsAlive)
        {
            writerThread.Join();
        }

        // Remove the thread reference.
        writerThread = null;

        try
        {
            // Update the WAV header with the
            // correct file size and data size.
            if (outputWriter != null &&
                outputStream != null)
            {
                CorrectWavHeader();
            }
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Could not finish the WAV header:\n" +
                exception.Message
            );
        }
        finally
        {
            // Always close the file,
            // even if an error occurs.
            CloseOutputFile();
        }

        // Check whether the writer thread
        // encountered any errors.
        if (!string.IsNullOrEmpty(writerError))
        {
            Debug.LogError(
                "Recording writer error:\n" +
                writerError
            );

            return;
        }

        // If no audio data was written,
        // notify the user.
        if (writtenDataBytes == 0)
        {
            Debug.LogWarning(
                "The WAV was created, but no FMOD " +
                "audio was captured."
            );

            UpdateStatusText("No audio was recorded.");

            return;
        }

        // Recording completed successfully.
        Debug.Log(
            "Stereo recording saved successfully:\n" +
            currentFilePath
        );

        // Display a success message
        // in the UI.
        ShowSavedFeedback();

        // Warn if the recording buffer
        // became full and audio samples
        // had to be dropped.
        if (droppedSamples > 0)
        {
            Debug.LogWarning(
                droppedSamples +
                " stereo samples were dropped because " +
                "the recording buffer became full. " +
                "Increase Ring Buffer Seconds."
            );
        }
    }

    // Reset all recording variables
    // before starting a new recording.
    private void ResetRecordingState()
    {
        // Reset the ring buffer positions.
        writePosition = 0;
        readPosition = 0;

        // Reset recording statistics.
        writtenDataBytes = 0;
        droppedSamples = 0;

        // Clear previous errors and file path.
        writerError = null;
        currentFilePath = null;
    }

    /*
     * FMOD automatically calls this method
     * every time new audio is generated.
     *
     * The original audio continues playing
     * normally, while a stereo copy is made
     * for the WAV recording.
     */
    [AOT.MonoPInvokeCallback(
        typeof(DSP_READ_CALLBACK)
    )]
    private static RESULT CaptureAudio(
        ref DSP_STATE dspState,
        IntPtr inputBuffer,
        IntPtr outputBuffer,
        uint length,
        int inputChannels,
        ref int outputChannels
    )
    {
        // Get the active recorder instance.
        FMODMasterRecorder recorder =
            activeRecorder;

        // Make sure everything is valid
        // before processing audio.
        if (recorder == null ||
            inputBuffer == IntPtr.Zero ||
            outputBuffer == IntPtr.Zero ||
            inputChannels <= 0 ||
            length == 0)
        {
            return RESULT.OK;
        }

        // Keep the original number of
        // output channels unchanged.
        outputChannels = inputChannels;

        // Total number of audio frames
        // received from FMOD.
        int totalFrames =
            (int)length;

        int processedFrames = 0;

        // Process the audio in smaller chunks.
        // This prevents the callback buffers
        // from overflowing.
        while (processedFrames < totalFrames)
        {
            /*
             * Calculate how many audio frames
             * can safely fit inside the reusable buffers.
             */
            int maximumInputFrames =
                recorder.callbackInputBuffer.Length /
                inputChannels;

            int maximumStereoFrames =
                recorder.callbackStereoBuffer.Length /
                RecordingChannels;

            int framesInChunk =
                Math.Min(
                    totalFrames - processedFrames,
                    Math.Min(
                        maximumInputFrames,
                        maximumStereoFrames
                    )
                );

            // Stop if something went wrong.
            if (framesInChunk <= 0)
            {
                return RESULT.ERR_INTERNAL;
            }

            // Number of audio samples
            // inside this chunk.
            int inputSampleCount =
                framesInChunk *
                inputChannels;

            // Calculate the current position
            // inside FMOD's audio buffer.
            int inputByteOffset =
                processedFrames *
                inputChannels *
                sizeof(float);

            IntPtr inputPosition =
                IntPtr.Add(
                    inputBuffer,
                    inputByteOffset
                );

            IntPtr outputPosition =
                IntPtr.Add(
                    outputBuffer,
                    inputByteOffset
                );

            // Copy FMOD's audio into
            // a temporary buffer.
            Marshal.Copy(
                inputPosition,
                recorder.callbackInputBuffer,
                0,
                inputSampleCount
            );

            /*
             * Copy the original audio back
             * to FMOD so the game audio
             * continues playing normally.
             */
            Marshal.Copy(
                recorder.callbackInputBuffer,
                0,
                outputPosition,
                inputSampleCount
            );

            // Only save audio while recording.
            if (recorder.isRecording)
            {
                // Convert the FMOD audio
                // into stereo.
                recorder.ConvertToStereo(
                    recorder.callbackInputBuffer,
                    recorder.callbackStereoBuffer,
                    framesInChunk,
                    inputChannels
                );

                // Store the stereo samples
                // inside the ring buffer.
                recorder.WriteToRingBuffer(
                    recorder.callbackStereoBuffer,
                    framesInChunk *
                    RecordingChannels
                );
            }

            // Continue processing
            // the remaining audio frames.
            processedFrames +=
                framesInChunk;
        }

        // Tell FMOD the callback
        // completed successfully.
        return RESULT.OK;
    }
    // Converts FMOD's audio into
    // a stereo (left and right) format
    // before saving it to the WAV file.
    private void ConvertToStereo(
        float[] inputSamples,
        float[] stereoSamples,
        int frameCount,
        int inputChannels
    )
    {
        // Process every audio frame.
        for (
            int frame = 0;
            frame < frameCount;
            frame++
        )
        {
            // Position of this frame
            // inside the original audio.
            int inputPosition =
                frame * inputChannels;

            // Position inside the stereo buffer.
            int stereoPosition =
                frame * RecordingChannels;

            float left;
            float right;

            // Mono audio is copied
            // into both left and right channels.
            if (inputChannels == 1)
            {
                left =
                    inputSamples[inputPosition];

                right = left;
            }
            else
            {
                // Use FMOD's first two channels
                // as the stereo output.
                left =
                    inputSamples[inputPosition];

                right =
                    inputSamples[inputPosition + 1];

                /*
                 * If surround sound channels exist,
                 * gently mix them into the stereo output
                 * so no audio is lost.
                 */
                if (inputChannels > 2)
                {
                    float extraTotal = 0f;

                    for (
                        int channel = 2;
                        channel < inputChannels;
                        channel++
                    )
                    {
                        extraTotal +=
                            inputSamples[
                                inputPosition + channel
                            ];
                    }

                    float extraAverage =
                        extraTotal /
                        (inputChannels - 2);

                    left +=
                        extraAverage * 0.35f;

                    right +=
                        extraAverage * 0.35f;
                }
            }

            // Clamp the audio values
            // so they stay within
            // the valid audio range.
            stereoSamples[stereoPosition] =
                Mathf.Clamp(
                    left,
                    -1f,
                    1f
                );

            stereoSamples[stereoPosition + 1] =
                Mathf.Clamp(
                    right,
                    -1f,
                    1f
                );
        }
    }

    // Writes stereo audio samples
    // into the ring buffer.
    //
    // The writer thread will later
    // read these samples and save them
    // into the WAV file.
    private void WriteToRingBuffer(
        float[] source,
        int sampleCount
    )
    {
        // Current write position.
        int currentWrite =
            writePosition;

        // Current read position.
        int currentRead =
            Volatile.Read(ref readPosition);

        int freeSpace;

        // Calculate the available space
        // remaining inside the ring buffer.
        if (currentWrite >= currentRead)
        {
            freeSpace =
                ringBuffer.Length -
                (currentWrite - currentRead) -
                1;
        }
        else
        {
            freeSpace =
                currentRead -
                currentWrite -
                1;
        }

        // Only write as many samples
        // as the buffer can hold.
        int samplesToWrite =
            Math.Min(
                sampleCount,
                freeSpace
            );

        // Keep stereo samples together.
        samplesToWrite -=
            samplesToWrite %
            RecordingChannels;

        // Buffer is full.
        if (samplesToWrite <= 0)
        {
            // Count the dropped samples.
            Interlocked.Add(
                ref droppedSamples,
                sampleCount
            );

            return;
        }

        // Write the first section.
        int firstPart =
            Math.Min(
                samplesToWrite,
                ringBuffer.Length - currentWrite
            );

        // Prevent splitting
        // a stereo sample pair.
        firstPart -=
            firstPart %
            RecordingChannels;

        Array.Copy(
            source,
            0,
            ringBuffer,
            currentWrite,
            firstPart
        );

        // If necessary,
        // wrap around to
        // the beginning of the buffer.
        int secondPart =
            samplesToWrite - firstPart;

        if (secondPart > 0)
        {
            Array.Copy(
                source,
                firstPart,
                ringBuffer,
                0,
                secondPart
            );
        }

        // Update the write position.
        int newWritePosition =
            (currentWrite + samplesToWrite) %
            ringBuffer.Length;

        Volatile.Write(
            ref writePosition,
            newWritePosition
        );

        // Count any samples
        // that could not fit.
        if (samplesToWrite < sampleCount)
        {
            Interlocked.Add(
                ref droppedSamples,
                sampleCount - samplesToWrite
            );
        }
    }

    // Runs on a separate background thread.
    //
    // Continuously reads audio from the ring buffer
    // and writes it into the WAV file.
    //
    // Using another thread prevents file writing
    // from slowing down or freezing the game.
    private void WriterThreadLoop()
    {
        // Temporary buffer for floating-point audio samples.
        // The block size is kept divisible by two
        // because stereo audio uses left and right channels.
        float[] floatBlock =
            new float[8192];

        // Temporary buffer for 16-bit PCM data
        // before writing it to the WAV file.
        byte[] byteBlock =
            new byte[floatBlock.Length * 2];

        try
        {
            // Continue writing while recording
            // is active or there are still
            // samples waiting inside the ring buffer.
            while (
                writerShouldRun ||
                HasSamplesWaiting()
            )
            {
                // Read the next block of samples
                // from the ring buffer.
                int samplesRead =
                    ReadFromRingBuffer(
                        floatBlock,
                        floatBlock.Length
                    );

                // No audio available yet.
                // Wait briefly before checking again.
                if (samplesRead == 0)
                {
                    Thread.Sleep(1);
                    continue;
                }

                // Convert floating-point audio
                // into 16-bit PCM format.
                int byteCount =
                    ConvertFloatToPcm16(
                        floatBlock,
                        samplesRead,
                        byteBlock
                    );

                // Write the converted audio
                // into the WAV file.
                outputStream.Write(
                    byteBlock,
                    0,
                    byteCount
                );

                // Keep track of the total
                // number of bytes written.
                writtenDataBytes +=
                    byteCount;
            }

            // Ensure all remaining data
            // is written to disk.
            outputStream.Flush();
        }
        catch (Exception exception)
        {
            // Store the error so it can be
            // reported after recording stops.
            writerError =
                exception.Message;
        }
    }

    // Returns true if there are still
    // audio samples waiting inside
    // the ring buffer.
    private bool HasSamplesWaiting()
    {
        return
            Volatile.Read(ref readPosition) !=
            Volatile.Read(ref writePosition);
    }

    // Reads audio samples from
    // the ring buffer.
    private int ReadFromRingBuffer(
        float[] destination,
        int maximumSamples
    )
    {
        // Current read position.
        int currentRead =
            readPosition;

        // Current write position.
        int currentWrite =
            Volatile.Read(ref writePosition);

        int availableSamples;

        // Calculate how many samples
        // are currently available.
        if (currentWrite >= currentRead)
        {
            availableSamples =
                currentWrite -
                currentRead;
        }
        else
        {
            availableSamples =
                ringBuffer.Length -
                currentRead +
                currentWrite;
        }

        // Read only the available samples.
        int samplesToRead =
            Math.Min(
                maximumSamples,
                availableSamples
            );

        // Keep stereo samples together.
        samplesToRead -=
            samplesToRead %
            RecordingChannels;

        if (samplesToRead <= 0)
        {
            return 0;
        }

        // Read the first section.
        int firstPart =
            Math.Min(
                samplesToRead,
                ringBuffer.Length - currentRead
            );

        firstPart -=
            firstPart %
            RecordingChannels;

        Array.Copy(
            ringBuffer,
            currentRead,
            destination,
            0,
            firstPart
        );

        // If necessary,
        // continue reading from
        // the beginning of the buffer.
        int secondPart =
            samplesToRead -
            firstPart;

        if (secondPart > 0)
        {
            Array.Copy(
                ringBuffer,
                0,
                destination,
                firstPart,
                secondPart
            );
        }

        // Update the read position.
        int newReadPosition =
            (currentRead + samplesToRead) %
            ringBuffer.Length;

        Volatile.Write(
            ref readPosition,
            newReadPosition
        );

        return samplesToRead;
    }

    // Converts floating-point audio
    // into 16-bit PCM format,
    // which is required by WAV files.
    private int ConvertFloatToPcm16(
        float[] floatSamples,
        int sampleCount,
        byte[] byteDestination
    )
    {
        int bytePosition = 0;

        for (
            int sampleIndex = 0;
            sampleIndex < sampleCount;
            sampleIndex++
        )
        {
            // Limit the audio value
            // to the valid range.
            float limitedSample =
                Math.Max(
                    -1f,
                    Math.Min(
                        1f,
                        floatSamples[sampleIndex]
                    )
                );

            // Convert the floating-point
            // sample into a 16-bit integer.
            short pcmSample =
                (short)(
                    limitedSample *
                    short.MaxValue
                );

            // Store the sample as
            // two bytes (little-endian).
            byteDestination[bytePosition++] =
                (byte)(
                    pcmSample & 0xFF
                );

            byteDestination[bytePosition++] =
                (byte)(
                    (pcmSample >> 8) & 0xFF
                );
        }

        // Return the total number
        // of bytes generated.
        return bytePosition;
    }

    // Writes an empty WAV header.
    //
    // The correct file size and audio length
    // are unknown at the beginning of recording,
    // so placeholder values are written first.
    private void WriteEmptyWavHeader()
    {
        // Number of bytes per audio sample.
        int bytesPerSample =
            BitsPerSample / 8;

        // WAV file starts with the RIFF identifier.
        outputWriter.Write(
            Encoding.ASCII.GetBytes("RIFF")
        );

        // Placeholder for the final RIFF size.
        outputWriter.Write(0);

        // Specify this as a WAV file.
        outputWriter.Write(
            Encoding.ASCII.GetBytes("WAVE")
        );

        // Write the audio format section.
        outputWriter.Write(
            Encoding.ASCII.GetBytes("fmt ")
        );

        // PCM format chunk size.
        outputWriter.Write(16);

        // PCM audio format.
        outputWriter.Write((short)1);

        // Number of audio channels.
        outputWriter.Write(
            (short)RecordingChannels
        );

        // Audio sample rate.
        outputWriter.Write(sampleRate);

        // Calculate bytes per second.
        int byteRate =
            sampleRate *
            RecordingChannels *
            bytesPerSample;

        outputWriter.Write(byteRate);

        // Size of one audio frame.
        short blockAlignment =
            (short)(
                RecordingChannels *
                bytesPerSample
            );

        outputWriter.Write(
            blockAlignment
        );

        // Bits used for each sample.
        outputWriter.Write(
            BitsPerSample
        );

        // Beginning of the audio data section.
        outputWriter.Write(
            Encoding.ASCII.GetBytes("data")
        );

        // Placeholder for the final data size.
        outputWriter.Write(0);
    }

    // Updates the placeholder values
    // inside the WAV header after
    // recording has finished.
    private void CorrectWavHeader()
    {
        int bytesPerSample =
            BitsPerSample / 8;

        // Make sure all data
        // has been written first.
        outputWriter.Flush();

        // Update the RIFF chunk size.
        outputStream.Seek(
            4,
            SeekOrigin.Begin
        );

        outputWriter.Write(
            (int)(
                36 +
                writtenDataBytes
            )
        );

        // Update the number of channels.
        outputStream.Seek(
            22,
            SeekOrigin.Begin
        );

        outputWriter.Write(
            (short)RecordingChannels
        );

        // Update the byte rate.
        outputStream.Seek(
            28,
            SeekOrigin.Begin
        );

        outputWriter.Write(
            sampleRate *
            RecordingChannels *
            bytesPerSample
        );

        // Update the block alignment.
        outputStream.Seek(
            32,
            SeekOrigin.Begin
        );

        outputWriter.Write(
            (short)(
                RecordingChannels *
                bytesPerSample
            )
        );

        // Update the final audio data size.
        outputStream.Seek(
            40,
            SeekOrigin.Begin
        );

        outputWriter.Write(
            (int)writtenDataBytes
        );

        outputWriter.Flush();
    }

    // Closes the recording file
    // and releases file resources.
    private void CloseOutputFile()
    {
        try
        {
            // Close the BinaryWriter.
            if (outputWriter != null)
            {
                outputWriter.Flush();
                outputWriter.Dispose();
                outputWriter = null;
            }

            // Close the FileStream.
            if (outputStream != null)
            {
                outputStream.Dispose();
                outputStream = null;
            }
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Failed to close the recording file:\n" +
                exception.Message
            );
        }
    }

    // Updates the recording status
    // shown on the UI.
    private void UpdateStatusText(string message)
    {
        if (recordingStatusText != null)
        {
            recordingStatusText.text = message;
        }
    }

    // Displays the "Recording Saved"
    // message using a coroutine.
    private void ShowSavedFeedback()
    {
        if (statusCoroutine != null)
        {
            StopCoroutine(statusCoroutine);
        }

        statusCoroutine =
            StartCoroutine(SavedFeedbackCoroutine());
    }

    // Shows the saved message
    // for a short period before hiding it.
    private IEnumerator SavedFeedbackCoroutine()
    {
        UpdateStatusText(savedMessage);

        yield return new WaitForSecondsRealtime(
            savedMessageDuration
        );

        // Clear the status text.
        UpdateStatusText("");

        statusCoroutine = null;
    }

    // Automatically called when
    // this object is destroyed.
    //
    // Cleans up all recording resources.
    private void OnDestroy()
    {
        // Stop recording if it
        // is still running.
        if (isRecording)
        {
            StopRecordingAndSave();
        }

        // Stop the writer thread.
        writerShouldRun = false;

        if (writerThread != null &&
            writerThread.IsAlive)
        {
            writerThread.Join();
        }

        // Close the recording file.
        CloseOutputFile();

        // Remove the recording DSP
        // from FMOD's Master Bus.
        if (
            masterChannelGroup.hasHandle() &&
            recordingDSP.hasHandle()
        )
        {
            masterChannelGroup.removeDSP(
                recordingDSP
            );
        }

        // Release the DSP resource.
        if (recordingDSP.hasHandle())
        {
            recordingDSP.release();
        }

        // Clear the active recorder reference.
        if (activeRecorder == this)
        {
            activeRecorder = null;
        }

        // Mark the recorder
        // as no longer initialized.
        isReady = false;
    }
}