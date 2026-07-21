using FMODUnity;
using FMOD.Studio;
using UnityEngine;

public class MusicLandmark : MonoBehaviour
{
    [SerializeField] public InstrumentData instrumentData;

    private EventInstance instance;
    private Transform player;

    private float currentIntensity;
    private float currentDuet;

    private bool duetEnabled;

    public float DistanceToPlayer { get; private set; }

    public bool PlayerInDuetRange => DistanceToPlayer <= instrumentData.duetRadius;

    public bool debug;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        instance = RuntimeManager.CreateInstance(instrumentData.eventReference);

        RuntimeManager.AttachInstanceToGameObject(instance, gameObject);

        instance.start();
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

        if (debug)
        {
            Debug.Log(currentIntensity);
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