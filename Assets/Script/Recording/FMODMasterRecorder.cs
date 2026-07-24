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

public class FMODMasterRecorder : MonoBehaviour
{
    [Header("Recording File")]
    [SerializeField]
    private string fileName = "Resonance";

    [Header("Recording Buffer")]
    [SerializeField]
    [Range(2, 30)]
    private int ringBufferSeconds = 10;

    // The exported WAV is always stereo.
    private const int RecordingChannels = 2;
    private const short BitsPerSample = 16;

    private ChannelGroup masterChannelGroup;
    private DSP recordingDSP;
    private DSP_READ_CALLBACK recordingCallback;

    private static FMODMasterRecorder activeRecorder;

    private volatile bool isRecording;
    private bool isReady;

    private int sampleRate = 48000;

    // Temporary stereo ring buffer.
    private float[] ringBuffer;
    private int writePosition;
    private int readPosition;

    /*
     * FMOD audio callback buffers.
     *
     * callbackInputBuffer stores the original FMOD channels.
     * callbackStereoBuffer stores the converted stereo audio.
     */
    private readonly float[] callbackInputBuffer =
        new float[65536];

    private readonly float[] callbackStereoBuffer =
        new float[65536];

    private Thread writerThread;
    private volatile bool writerShouldRun;

    private FileStream outputStream;
    private BinaryWriter outputWriter;

    private long writtenDataBytes;
    private string currentFilePath;
    private string writerError;

    private int droppedSamples;


    [Header("UI Feedback")]
    [SerializeField] private TMP_Text recordingStatusText;

    [SerializeField] private string readyMessage = "Ready to record";
    [SerializeField] private string recordingMessage = "? Recording...";
    [SerializeField] private string savedMessage = "Recording saved!";

    [SerializeField] private float savedMessageDuration = 2f;

    private Coroutine statusCoroutine;


    public bool IsRecording
    {
        get { return isRecording; }
    }

    private void Start()
    {
        SetupRecorder();
        UpdateStatusText(readyMessage);
    }

    private void SetupRecorder()
    {
        if (isReady)
        {
            return;
        }

        activeRecorder = this;
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
    public void StartRecording()
    {
        if (!isReady)
        {
            SetupRecorder();
        }

        if (!isReady)
        {
            Debug.LogError(
                "FMOD Master Recorder is not ready."
            );

            return;
        }

        if (isRecording)
        {
            Debug.LogWarning(
                "Recording is already running."
            );

            return;
        }

        ResetRecordingState();

        string finalFileName =
            fileName + "_" +
            DateTime.Now.ToString("yyyyMMdd_HHmmss") +
            ".wav";

        currentFilePath =
            Path.Combine(
                Application.persistentDataPath,
                finalFileName
            );

        try
        {
            outputStream =
                new FileStream(
                    currentFilePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read
                );

            outputWriter =
                new BinaryWriter(outputStream);

            WriteEmptyWavHeader();

            writerShouldRun = true;

            writerThread =
                new Thread(WriterThreadLoop);

            writerThread.Name =
                "FMOD WAV Writer";

            writerThread.IsBackground = true;
            writerThread.Start();

            isRecording = true;

            UpdateStatusText(recordingMessage);

            Debug.Log(
                "FMOD Master Bus recording started."
            );
        }
        catch (Exception exception)
        {
            CloseOutputFile();

            Debug.LogError(
                "Could not start recording:\n" +
                exception.Message
            );
        }
    }

    // Connect this to the Stop Recording button.
    public void StopRecordingAndSave()
    {
        if (!isRecording)
        {
            Debug.LogWarning(
                "There is no recording to stop."
            );

            return;
        }

        // Stop accepting new samples.
        isRecording = false;

        // Allow the writer thread to finish queued samples.
        writerShouldRun = false;

        if (writerThread != null &&
            writerThread.IsAlive)
        {
            writerThread.Join();
        }

        writerThread = null;

        try
        {
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
            CloseOutputFile();
        }

        if (!string.IsNullOrEmpty(writerError))
        {
            Debug.LogError(
                "Recording writer error:\n" +
                writerError
            );

            return;
        }

        if (writtenDataBytes == 0)
        {
            Debug.LogWarning(
                "The WAV was created, but no FMOD " +
                "audio was captured."
            );

            UpdateStatusText("No audio was recorded.");

            return;
        }

        Debug.Log(
            "Stereo recording saved successfully:\n" +
            currentFilePath
        );

        ShowSavedFeedback();

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

    private void ResetRecordingState()
    {
        writePosition = 0;
        readPosition = 0;

        writtenDataBytes = 0;
        droppedSamples = 0;

        writerError = null;
        currentFilePath = null;
    }

    /*
     * FMOD calls this method on its mixer thread.
     *
     * The original FMOD output is passed through unchanged.
     * A separate stereo copy is created for the WAV.
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
        FMODMasterRecorder recorder =
            activeRecorder;

        if (recorder == null ||
            inputBuffer == IntPtr.Zero ||
            outputBuffer == IntPtr.Zero ||
            inputChannels <= 0 ||
            length == 0)
        {
            return RESULT.OK;
        }

        outputChannels = inputChannels;

        int totalFrames =
            (int)length;

        int processedFrames = 0;

        while (processedFrames < totalFrames)
        {
            /*
             * Work out how many complete audio frames fit
             * inside both reusable callback arrays.
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

            if (framesInChunk <= 0)
            {
                return RESULT.ERR_INTERNAL;
            }

            int inputSampleCount =
                framesInChunk *
                inputChannels;

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

            Marshal.Copy(
                inputPosition,
                recorder.callbackInputBuffer,
                0,
                inputSampleCount
            );

            /*
             * Pass the original FMOD audio to the DSP output,
             * so adding the recorder does not mute the game.
             */
            Marshal.Copy(
                recorder.callbackInputBuffer,
                0,
                outputPosition,
                inputSampleCount
            );

            if (recorder.isRecording)
            {
                recorder.ConvertToStereo(
                    recorder.callbackInputBuffer,
                    recorder.callbackStereoBuffer,
                    framesInChunk,
                    inputChannels
                );

                recorder.WriteToRingBuffer(
                    recorder.callbackStereoBuffer,
                    framesInChunk *
                    RecordingChannels
                );
            }

            processedFrames +=
                framesInChunk;
        }

        return RESULT.OK;
    }

    private void ConvertToStereo(
        float[] inputSamples,
        float[] stereoSamples,
        int frameCount,
        int inputChannels
    )
    {
        for (
            int frame = 0;
            frame < frameCount;
            frame++
        )
        {
            int inputPosition =
                frame * inputChannels;

            int stereoPosition =
                frame * RecordingChannels;

            float left;
            float right;

            if (inputChannels == 1)
            {
                // Mono becomes stereo.
                left =
                    inputSamples[inputPosition];

                right = left;
            }
            else
            {
                // FMOD's first two channels become left and right.
                left =
                    inputSamples[inputPosition];

                right =
                    inputSamples[inputPosition + 1];

                /*
                 * Quietly mix any additional surround channels
                 * into the stereo output.
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

    private void WriteToRingBuffer(
        float[] source,
        int sampleCount
    )
    {
        int currentWrite =
            writePosition;

        int currentRead =
            Volatile.Read(ref readPosition);

        int freeSpace;

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

        int samplesToWrite =
            Math.Min(
                sampleCount,
                freeSpace
            );

        /*
         * Keep stereo sample pairs together.
         */
        samplesToWrite -=
            samplesToWrite %
            RecordingChannels;

        if (samplesToWrite <= 0)
        {
            Interlocked.Add(
                ref droppedSamples,
                sampleCount
            );

            return;
        }

        int firstPart =
            Math.Min(
                samplesToWrite,
                ringBuffer.Length - currentWrite
            );

        /*
         * Prevent the first section from splitting
         * a stereo sample pair.
         */
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

        int newWritePosition =
            (currentWrite + samplesToWrite) %
            ringBuffer.Length;

        Volatile.Write(
            ref writePosition,
            newWritePosition
        );

        if (samplesToWrite < sampleCount)
        {
            Interlocked.Add(
                ref droppedSamples,
                sampleCount - samplesToWrite
            );
        }
    }

    private void WriterThreadLoop()
    {
        // Keep the block size divisible by two.
        float[] floatBlock =
            new float[8192];

        byte[] byteBlock =
            new byte[floatBlock.Length * 2];

        try
        {
            while (
                writerShouldRun ||
                HasSamplesWaiting()
            )
            {
                int samplesRead =
                    ReadFromRingBuffer(
                        floatBlock,
                        floatBlock.Length
                    );

                if (samplesRead == 0)
                {
                    Thread.Sleep(1);
                    continue;
                }

                int byteCount =
                    ConvertFloatToPcm16(
                        floatBlock,
                        samplesRead,
                        byteBlock
                    );

                outputStream.Write(
                    byteBlock,
                    0,
                    byteCount
                );

                writtenDataBytes +=
                    byteCount;
            }

            outputStream.Flush();
        }
        catch (Exception exception)
        {
            writerError =
                exception.Message;
        }
    }

    private bool HasSamplesWaiting()
    {
        return
            Volatile.Read(ref readPosition) !=
            Volatile.Read(ref writePosition);
    }

    private int ReadFromRingBuffer(
        float[] destination,
        int maximumSamples
    )
    {
        int currentRead =
            readPosition;

        int currentWrite =
            Volatile.Read(ref writePosition);

        int availableSamples;

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

        int samplesToRead =
            Math.Min(
                maximumSamples,
                availableSamples
            );

        // Keep stereo sample pairs together.
        samplesToRead -=
            samplesToRead %
            RecordingChannels;

        if (samplesToRead <= 0)
        {
            return 0;
        }

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

        int newReadPosition =
            (currentRead + samplesToRead) %
            ringBuffer.Length;

        Volatile.Write(
            ref readPosition,
            newReadPosition
        );

        return samplesToRead;
    }

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
            float limitedSample =
                Math.Max(
                    -1f,
                    Math.Min(
                        1f,
                        floatSamples[sampleIndex]
                    )
                );

            short pcmSample =
                (short)(
                    limitedSample *
                    short.MaxValue
                );

            byteDestination[bytePosition++] =
                (byte)(
                    pcmSample & 0xFF
                );

            byteDestination[bytePosition++] =
                (byte)(
                    (pcmSample >> 8) & 0xFF
                );
        }

        return bytePosition;
    }

    private void WriteEmptyWavHeader()
    {
        int bytesPerSample =
            BitsPerSample / 8;

        outputWriter.Write(
            Encoding.ASCII.GetBytes("RIFF")
        );

        // Placeholder RIFF size.
        outputWriter.Write(0);

        outputWriter.Write(
            Encoding.ASCII.GetBytes("WAVE")
        );

        outputWriter.Write(
            Encoding.ASCII.GetBytes("fmt ")
        );

        // PCM format section size.
        outputWriter.Write(16);

        // PCM audio format.
        outputWriter.Write((short)1);

        outputWriter.Write(
            (short)RecordingChannels
        );

        outputWriter.Write(sampleRate);

        int byteRate =
            sampleRate *
            RecordingChannels *
            bytesPerSample;

        outputWriter.Write(byteRate);

        short blockAlignment =
            (short)(
                RecordingChannels *
                bytesPerSample
            );

        outputWriter.Write(
            blockAlignment
        );

        outputWriter.Write(
            BitsPerSample
        );

        outputWriter.Write(
            Encoding.ASCII.GetBytes("data")
        );

        // Placeholder audio-data size.
        outputWriter.Write(0);
    }

    private void CorrectWavHeader()
    {
        int bytesPerSample =
            BitsPerSample / 8;

        outputWriter.Flush();

        // RIFF chunk size.
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

        // Number of channels.
        outputStream.Seek(
            22,
            SeekOrigin.Begin
        );

        outputWriter.Write(
            (short)RecordingChannels
        );

        // Byte rate.
        outputStream.Seek(
            28,
            SeekOrigin.Begin
        );

        outputWriter.Write(
            sampleRate *
            RecordingChannels *
            bytesPerSample
        );

        // Block alignment.
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

        // Audio-data size.
        outputStream.Seek(
            40,
            SeekOrigin.Begin
        );

        outputWriter.Write(
            (int)writtenDataBytes
        );

        outputWriter.Flush();
    }

    private void CloseOutputFile()
    {
        try
        {
            if (outputWriter != null)
            {
                outputWriter.Flush();
                outputWriter.Dispose();
                outputWriter = null;
            }

            /*
             * Disposing BinaryWriter normally also closes its
             * FileStream, but this ensures the stream reference
             * is cleared as well.
             */
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
    private void UpdateStatusText(string message)
    {
        if (recordingStatusText != null)
        {
            recordingStatusText.text = message;
        }
    }

    private void ShowSavedFeedback()
    {
        if (statusCoroutine != null)
        {
            StopCoroutine(statusCoroutine);
        }

        statusCoroutine =
            StartCoroutine(SavedFeedbackCoroutine());
    }

    private IEnumerator SavedFeedbackCoroutine()
    {
        UpdateStatusText(savedMessage);

        yield return new WaitForSecondsRealtime(
            savedMessageDuration
        );

        // Hide the text after a short delay.
        UpdateStatusText("");

        statusCoroutine = null;
    }
    private void OnDestroy()
    {
        if (isRecording)
        {
            StopRecordingAndSave();
        }

        writerShouldRun = false;

        if (writerThread != null &&
            writerThread.IsAlive)
        {
            writerThread.Join();
        }

        CloseOutputFile();

        if (
            masterChannelGroup.hasHandle() &&
            recordingDSP.hasHandle()
        )
        {
            masterChannelGroup.removeDSP(
                recordingDSP
            );
        }

        if (recordingDSP.hasHandle())
        {
            recordingDSP.release();
        }

        if (activeRecorder == this)
        {
            activeRecorder = null;
        }

        isReady = false;
    }
}