using System.Collections.Generic;
using UnityEngine;

public enum LoopType
{
    Normal,
    Active,
    Solo
}

public class LandmarkAudioState
{
    public int LandmarkId;
    public int CurrentPriority;
    public LoopType CurrentLoop = LoopType.Normal;
    public LoopType TargetLoop = LoopType.Normal;
    public bool TransitionPending = false;
}

public class StemSet
{
    public string NormalEventPath;
    public string ActiveEventPath;
    public string SoloEventPath;
}

public struct LandmarkMixInput
{
    public int LandmarkId;
    public float Distance;
    public float InfluenceRadius;
}

public interface IAudioBackend
{
    void CrossfadeLoop(
        int landmarkId,
        LoopType fromLoop,
        LoopType toLoop,
        float durationSeconds);

    void SetMixParameters(
        int landmarkId,
        float presence,
        float clarity,
        float reverb);
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private Dictionary<int, LandmarkAudioState> _landmarks
        = new Dictionary<int, LandmarkAudioState>();

    private Dictionary<int, StemSet> _stemSets
        = new Dictionary<int, StemSet>();

    private int? _duetAId;
    private int? _duetBId;
    private bool _duetActive;

    [SerializeField] private MonoBehaviour _audioBackendBehaviour;
    private IAudioBackend _backend;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _backend = _audioBackendBehaviour as IAudioBackend;
        if (_backend == null && _audioBackendBehaviour != null)
        {
            Debug.LogWarning($"{name}: Assigned backend does not implement IAudioBackend.");
        }
    }

    public void InitializeSession(ConfigurationProfile profile)
    {
        _landmarks.Clear();
        _stemSets.Clear();
        _duetAId = null;
        _duetBId = null;
        _duetActive = false;

        if (profile == null || profile.instruments == null)
        {
            Debug.LogWarning("SoundManager: InitializeSession called with null profile or instruments.");
            return;
        }

        foreach (var config in profile.instruments)
        {
            if (config == null || config.stem == null)
            {
                Debug.LogWarning("SoundManager: InstrumentConfig or stem is null in profile.");
                continue;
            }

            int landmarkId = ResolveLandmarkIdFromInstrumentId(config.instrumentId);
            if (landmarkId < 0)
            {
                Debug.LogWarning($"SoundManager: Unknown InstrumentId'{config.instrumentId}'.");
                continue;
            }

            RegisterLandmark(landmarkId);

            _stemSets[landmarkId] = new StemSet
            {
                NormalEventPath = config.stem.normalEventPath,
                ActiveEventPath = config.stem.activeEventPath,
                SoloEventPath = config.stem.soloEventPath
            };
        }
    }

    public void EndSession()
    {
        _landmarks.Clear();
        _stemSets.Clear();
        _duetAId = null;
        _duetBId = null;
        _duetActive = false;
    }

    public void RegisterLandmark(int landmarkId)
    {
        if (_landmarks.ContainsKey(landmarkId)) return;

        _landmarks[landmarkId] = new LandmarkAudioState
        {
            LandmarkId = landmarkId,
            CurrentPriority = 0,
            CurrentLoop = LoopType.Normal,
            TargetLoop = LoopType.Normal,
            TransitionPending = false
        };
    }

    public void UpdateLandmarkPriority(int landmarkId, int newPriority)
    {
        if (!_landmarks.TryGetValue(landmarkId, out var state)) return;

        if (state.CurrentPriority == newPriority) return;

        state.CurrentPriority = newPriority;

        bool isInDuet = _duetActive && (landmarkId == _duetAId || landmarkId == _duetBId);

        if (newPriority <= 1)
        {
            state.TargetLoop = LoopType.Normal;
        }
        else
        {
            state.TargetLoop = isInDuet ? LoopType.Solo : LoopType.Active;
        }

        state.TransitionPending = true;
    }

    public void SetDuetPair(int landmarkAId, int landmarkBId)
    {
        _duetAId = landmarkAId;
        _duetBId = landmarkBId;
        _duetActive = true;

        foreach (var kvp in _landmarks)
        {
            var state = kvp.Value;
            bool isDuetMember = state.LandmarkId == landmarkAId || state.LandmarkId == landmarkBId;

            if (state.CurrentPriority == 2)
            {
                state.TargetLoop = isDuetMember ? LoopType.Solo : LoopType.Active;
                state.TransitionPending = true;
            }
        }
    }

    public void ClearDuet()
    {
        _duetAId = null;
        _duetBId = null;
        _duetActive = false;

        foreach (var state in _landmarks.Values)
        {
            if (state.CurrentPriority <= 1)
            {
                state.TargetLoop = LoopType.Normal;
            }
            else
            {
                state.TargetLoop = LoopType.Active;
            }
            state.TransitionPending = true;
        }
    }

    public void OnLoopBarBoundary()
    {
        if (_backend == null) return;

        foreach (var state in _landmarks.Values)
        {
            if (!state.TransitionPending) continue;
            if (state.CurrentLoop == state.TargetLoop)
            {
                state.TransitionPending = false;
                continue;
            }

            _backend?.CrossfadeLoop(
                state.LandmarkId,
                state.CurrentLoop,
                state.TargetLoop,
                1.0f);

            state.CurrentLoop = state.TargetLoop;
            state.TransitionPending = false;
        }
    }

    public void UpdateMixing(LandmarkMixInput[] inputs)
    {
        if (_backend == null) return;

        foreach (var input in inputs)
        {
            if (!_landmarks.TryGetValue(input.LandmarkId, out var state)) continue;

            float normalized = Mathf.Clamp01(
                1f - (input.Distance / input.InfluenceRadius)
            );

            float presence = normalized;
            float clarity = normalized;
            float reverb = 1f - normalized;

            _backend.SetMixParameters(input.LandmarkId, presence, clarity, reverb);
        }
    }

    private int ResolveLandmarkIdFromInstrumentId(string instrumentId)
    {
        switch (instrumentId)
        {
            case "Piano": return 0;
            case "Cello": return 1;
            case "Saxophone": return 2;
            default: return -1;
        }
    }
}
