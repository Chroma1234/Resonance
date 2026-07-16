using FMODUnity;
using FMOD.Studio;
using UnityEngine;

public class MusicLandmark : MonoBehaviour
{
    [SerializeField] private InstrumentData instrumentData;

    private EventInstance instance;
    private Transform player;

    private float currentIntensity;
    private float duetIntensity;
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
        instance.setParameterByName("Duet", 1);
    }

    private void Update()
    {
        DistanceToPlayer = Vector3.Distance(player.position, transform.position);

        float target = Mathf.InverseLerp(instrumentData.maxDistance, instrumentData.intenseDistance, DistanceToPlayer);

        currentIntensity = Mathf.Lerp(currentIntensity, target, Time.deltaTime * instrumentData.smoothing);

        if (duetEnabled)
        {
            //instance.setParameterByName("Duet", 1);
        }
        else
        {
            //instance.setParameterByName("Intensity", currentIntensity);
        }

        if (debug)
        {
            Debug.Log(currentIntensity);
        }
    }

    public void SetDuet(bool enabled)
    {
        if (duetEnabled == enabled)
        {
            return;
        }

        duetEnabled = enabled;

        //instance.setParameterByName("Intensity", 0);
        //instance.setParameterByName("Duet", enabled ? 1 : 0);
    }

    private void OnDestroy()
    {
        instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        instance.release();
    }
}