using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using FMOD;
using FMODUnity;
using UnityEngine;

using Debug = UnityEngine.Debug;
using Thread = System.Threading.Thread;

public class FMODMasterRecorder : MonoBehaviour
{
    [Header("Recording File")]
    [SerializeField]
    private string fileName = "GameRecording";

    [Header("Recording Buffer")]
    [Tooltip("Temporary audio buffer size in seconds.")]
    [SerializeField]
    [Range(2, 30)]
    private int ringBufferSeconds = 10;

    // FMOD objects.
    private ChannelGroup masterChannelGroup;
    private DSP recordingDSP;
    private DSP_READ_CALLBACK recordingCallback;

    // Static reference used by FMOD's static callback.
    private static FMODMasterRecorder activeRecorder;

    // Recording state.
    private volatile bool isRecording;
    private bool isReady;

    // Audio information.
    private int sampleRate = 48000;
    private volatile int channelCount = 2;

    /*
     * The ring buffer temporarily holds audio between:
     *
     * FMOD audio thread -> ring buffer -> WAV writer thread
     *
     * This avoids writing files or growing Lists inside
     * FMOD's real-time mixer callback.
     */
    private float[] ringBuffer;

    private int writePosition;
    private int readPosition;

    // Reusable buffer used inside the FMOD callback.
    // It is created once, not every callback.
    private readonly float[] callbackBuffer =
        new float[65536];

    // Background WAV-writing thread.
    private Thread writerThread;
    private volatile bool writerShouldRun;

    private FileStream outputStream;
    private BinaryWriter outputWriter;

    // WAV statistics.
    private long writtenDataBytes;
    private string currentFilePath;
    private string writerError;

    // Number of samples dropped if the temporary buffer fills.
    private int droppedSamples;

    public bool IsRecording
    {
        get { return isRecording; }
    }

    private void Start()
    {
        SetupRecorder();
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
                "Could not read FMOD's audio format. " +
                "Using 48000 Hz stereo."
            );

            sampleRate = 48000;
            channelCount = 2;
        }
        else
        {
            channelCount = Mathf.Max(1, rawSpeakerCount);
        }

        /*
         * Allocate the temporary ring buffer once.
         *
         * A minimum of two channels is used so stereo
         * recording has enough temporary space.
         */
        int bufferChannels =
            Mathf.Max(channelCount, 2);

        int ringBufferSize =
            sampleRate *
            bufferChannels *
            Mathf.Max(2, ringBufferSeconds);

        ringBuffer = new float[ringBufferSize];

        DSP_DESCRIPTION description =
            new DSP_DESCRIPTION();

        description.pluginsdkversion =
            FMOD.VERSION.number;

        description.numinputbuffers = 1;
        description.numoutputbuffers = 1;
        description.read = recordingCallback;

        description.name = new byte[32];

        byte[] dspName =
            Encoding.ASCII.GetBytes(
                "Master WAV Recorder"
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

        /*
         * Place the recording DSP at the end of the
         * FMOD Master Channel Group.
         */
        result = masterChannelGroup.addDSP(
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

        currentFilePath = Path.Combine(
            Application.persistentDataPath,
            finalFileName
        );

        try
        {
            outputStream = new FileStream(
                currentFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read
            );

            outputWriter =
                new BinaryWriter(outputStream);

            // Write an empty WAV header for now.
            // Its sizes are corrected when recording stops.
            WriteEmptyWavHeader();

            writerShouldRun = true;

            writerThread = new Thread(
                WriterThreadLoop
            );

            writerThread.Name =
                "FMOD WAV Writer";

            writerThread.IsBackground = true;
            writerThread.Start();

            // Start capturing after the file is ready.
            isRecording = true;

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

        /*
         * Stop FMOD from putting more samples into
         * the temporary buffer.
         */
        isRecording = false;

        /*
         * Tell the writer thread to finish everything
         * still waiting inside the ring buffer.
         */
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

            return;
        }

        Debug.Log(
            "Recording saved successfully:\n" +
            currentFilePath
        );

        if (droppedSamples > 0)
        {
            Debug.LogWarning(
                droppedSamples +
                " audio samples were dropped because " +
                "the temporary recording buffer became full. " +
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
     * FMOD calls this on its real-time mixer thread.
     *
     * Keep this method light:
     * - no List.AddRange
     * - no file writing
     * - no Debug.Log
     * - no new arrays every callback
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

        recorder.channelCount = inputChannels;
        outputChannels = inputChannels;

        int totalSamples =
            (int)length * inputChannels;

        int processedSamples = 0;

        /*
         * Process in chunks so no temporary array
         * needs to be created inside the callback.
         */
        while (processedSamples < totalSamples)
        {
            int samplesInChunk = Math.Min(
                recorder.callbackBuffer.Length,
                totalSamples - processedSamples
            );

            IntPtr inputPosition = IntPtr.Add(
                inputBuffer,
                processedSamples * sizeof(float)
            );

            IntPtr outputPosition = IntPtr.Add(
                outputBuffer,
                processedSamples * sizeof(float)
            );

            Marshal.Copy(
                inputPosition,
                recorder.callbackBuffer,
                0,
                samplesInChunk
            );

            /*
             * Copy the same audio to the DSP output.
             * This keeps FMOD audible while recording.
             */
            Marshal.Copy(
                recorder.callbackBuffer,
                0,
                outputPosition,
                samplesInChunk
            );

            if (recorder.isRecording)
            {
                recorder.WriteToRingBuffer(
                    recorder.callbackBuffer,
                    samplesInChunk
                );
            }

            processedSamples += samplesInChunk;
        }

        return RESULT.OK;
    }

    /*
     * Called only by FMOD's audio thread.
     *
     * It copies samples into already allocated memory.
     */
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
            Math.Min(sampleCount, freeSpace);

        if (samplesToWrite <= 0)
        {
            Interlocked.Add(
                ref droppedSamples,
                sampleCount
            );

            return;
        }

        int firstPart = Math.Min(
            samplesToWrite,
            ringBuffer.Length - currentWrite
        );

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

    /*
     * Runs on a normal background thread.
     *
     * This thread performs the slower jobs:
     * - reading the ring buffer
     * - converting float audio to PCM16
     * - writing data to the WAV file
     */
    private void WriterThreadLoop()
    {
        float[] floatBlock =
            new float[8192];

        byte[] byteBlock =
            new byte[floatBlock.Length * 2];

        try
        {
            while (writerShouldRun ||
                   HasSamplesWaiting())
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

                writtenDataBytes += byteCount;
            }

            outputStream.Flush();
        }
        catch (Exception exception)
        {
            writerError = exception.Message;
        }
    }

    private bool HasSamplesWaiting()
    {
        return Volatile.Read(ref readPosition) !=
               Volatile.Read(ref writePosition);
    }

    /*
     * Called only by the WAV writer thread.
     */
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
                currentWrite - currentRead;
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

        if (samplesToRead <= 0)
        {
            return 0;
        }

        int firstPart = Math.Min(
            samplesToRead,
            ringBuffer.Length - currentRead
        );

        Array.Copy(
            ringBuffer,
            currentRead,
            destination,
            0,
            firstPart
        );

        int secondPart =
            samplesToRead - firstPart;

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

        for (int i = 0; i < sampleCount; i++)
        {
            float limitedSample =
                Math.Max(
                    -1f,
                    Math.Min(1f, floatSamples[i])
                );

            short pcmSample =
                (short)(
                    limitedSample *
                    short.MaxValue
                );

            byteDestination[bytePosition++] =
                (byte)(pcmSample & 0xFF);

            byteDestination[bytePosition++] =
                (byte)((pcmSample >> 8) & 0xFF);
        }

        return bytePosition;
    }

    private void WriteEmptyWavHeader()
    {
        const short bitsPerSample = 16;
        const short pcmFormat = 1;

        int channels =
            Mathf.Max(1, channelCount);

        int bytesPerSample =
            bitsPerSample / 8;

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

        outputWriter.Write(16);
        outputWriter.Write(pcmFormat);
        outputWriter.Write((short)channels);
        outputWriter.Write(sampleRate);

        int byteRate =
            sampleRate *
            channels *
            bytesPerSample;

        outputWriter.Write(byteRate);

        short blockAlignment =
            (short)(
                channels * bytesPerSample
            );

        outputWriter.Write(blockAlignment);
        outputWriter.Write(bitsPerSample);

        outputWriter.Write(
            Encoding.ASCII.GetBytes("data")
        );

        // Placeholder data size.
        outputWriter.Write(0);
    }

    private void CorrectWavHeader()
    {
        int channels =
            Mathf.Max(1, channelCount);

        const short bitsPerSample = 16;

        int bytesPerSample =
            bitsPerSample / 8;

        outputWriter.Flush();

        // Correct RIFF chunk size.
        outputStream.Seek(
            4,
            SeekOrigin.Begin
        );

        outputWriter.Write(
            (int)(36 + writtenDataBytes)
        );

        // Correct channel count.
        outputStream.Seek(
            22,
            SeekOrigin.Begin
        );

        outputWriter.Write(
            (short)channels
        );

        // Correct byte rate.
        outputStream.Seek(
            28,
            SeekOrigin.Begin
        );

        outputWriter.Write(
            sampleRate *
            channels *
            bytesPerSample
        );

        // Correct block alignment.
        outputStream.Seek(
            32,
            SeekOrigin.Begin
        );

        outputWriter.Write(
            (short)(
                channels * bytesPerSample
            )
        );

        // Correct audio-data size.
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
        if (outputWriter != null)
        {
            outputWriter.Dispose();
            outputWriter = null;
        }

        if (outputStream != null)
        {
            outputStream.Dispose();
            outputStream = null;
        }
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

        if (masterChannelGroup.hasHandle() &&
            recordingDSP.hasHandle())
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