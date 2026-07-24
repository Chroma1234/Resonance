using FMODUnity;
using FMOD.Studio;
using UnityEngine;

public class MusicLandmark : MonoBehaviour
{
    [HideInInspector] public InstrumentData instrumentData;

    private EventInstance instance;
    private Transform player;

    private float currentIntensity;
    private float currentDuet;

    private bool duetEnabled;

    public float DistanceToPlayer { get; private set; }

    public bool PlayerInDuetRange => DistanceToPlayer <= instrumentData.duetRadius;

    [SerializeField] public GameObject model;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        Mood mood = MoodManager.Instance.GetMood(instrumentData);

        Debug.Log($"{instrumentData.instrumentName} will play mood {mood}");

        EventReference selectedEvent = instrumentData.GetEvent(mood);

        instance = RuntimeManager.CreateInstance(selectedEvent);

        RuntimeManager.AttachInstanceToGameObject(instance, gameObject);

        instance.start();

        SetModel();
    }

    private void Update()
    {
        DistanceToPlayer = Vector3.Distance(player.position, transform.position);

        float targetIntensity = DistanceToPlayer <= instrumentData.intenseDistance ? 1f : 0f;
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * instrumentData.smoothing);

        float targetDuet = duetEnabled ? 1f : 0f;
        currentDuet = Mathf.Lerp(currentDuet, targetDuet, Time.deltaTime * instrumentData.smoothing);

        instance.setParameterByName("Intensity", currentIntensity);
        instance.setParameterByName("Duet", currentDuet);
    }

    public void SetModel()
    {
        if (instrumentData.modelPrefab != null)
        {
            if (model != null)
                Destroy(model);

            if (instrumentData.modelPrefab != null)
            {
                model = Instantiate(instrumentData.modelPrefab, transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
            }
        }
    }

    public void SetDuet(bool enabled)
    {
        duetEnabled = enabled;
    }

    private void OnDestroy()
    {
        instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        instance.release();
    }

    private void OnDrawGizmos()
    {
        if (instrumentData == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, instrumentData.intenseDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, instrumentData.maxDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, instrumentData.duetRadius);
    }
}