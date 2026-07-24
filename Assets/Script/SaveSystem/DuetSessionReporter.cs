using UnityEngine;

public class DuetSessionReporter : MonoBehaviour
{
    [SerializeField] private ResonanceSessionTracker sessionTracker;

    private bool wasActive;

    public void ReportDuetState(bool isActive)
    {
        if (isActive && !wasActive)
            sessionTracker.RecordDuetActivated();

        wasActive = isActive;
    }
}
