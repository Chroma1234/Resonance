using UnityEngine;

public class DuetSessionReporter : MonoBehaviour
{
    [Tooltip("Optional. Found automatically if left empty.")]
    [SerializeField] private ResonanceSessionTracker sessionTracker;

    private bool wasActive;

    private void Awake()
    {
        if (sessionTracker == null)
        {
            sessionTracker = GetComponent<ResonanceSessionTracker>();
        }

        if (sessionTracker == null)
        {
            sessionTracker = ResonanceSessionTracker.Instance;
        }
    }

    public void ReportDuetState(bool isActive)
    {
        if (sessionTracker == null)
        {
            sessionTracker = ResonanceSessionTracker.Instance;
            if (sessionTracker == null)
            {
                return;
            }
        }

        if (isActive && !wasActive)
        {
            sessionTracker.RecordDuetActivated();
        }

        wasActive = isActive;
    }
}
