using UnityEngine;

public class MusicLandmark : MonoBehaviour
{

    [SerializeField] private InstrumentData instrumentData;

    // [HideInInspector] public InstrumentData instrumentData;

    private Transform player;

    public float DistanceToPlayer { get; private set; }

    public bool PlayerInDuetRange => DistanceToPlayer <= instrumentData.duetRadius;

    [SerializeField] public GameObject model;

    [SerializeField] private string instrumentId;

    public int LandmarkId
    {
        get
        {
            if (SoundManager.Instance == null) return -1;
            return ResolveLandmarkIdFromInstrumentId(instrumentId);
        }
    }

    public float InfluenceRadius => instrumentData.maxDistance;
    public float DuetRadius => instrumentData.duetRadius;

    public Vector3 Position => transform.position;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        SetModel();
    }

    private void Update()
    {
        if (player == null || instrumentData == null) return;

        DistanceToPlayer = Vector3.Distance(player.position, transform.position);
    }

    public void SetModel()
    {
        if (instrumentData == null || instrumentData.modelPrefab == null)
            return;

        if (model != null)
            Destroy(model);

        model = Instantiate(instrumentData.modelPrefab, transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;
    }

    public void SetDuet(bool enabled)
    {
        // Visual or gameplay feedback for duet state here if needed
    }

    private int ResolveLandmarkIdFromInstrumentId(string id)
    {
        switch (id)
        {
            case "Piano": return 0;
            case "Cello": return 1;
            case "Saxophone": return 2;
            default: return -1;
        }
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