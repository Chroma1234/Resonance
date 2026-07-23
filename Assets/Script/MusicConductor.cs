using UnityEngine;

public class MusicConductor : MonoBehaviour
{
    [SerializeField] private float secondsPerBar = 2.45f;

    private float timer;

    private void Update()
    {
        if (SoundManager.Instance == null) return;

        timer += Time.deltaTime;
        if (timer >= secondsPerBar)
        {
            timer -= secondsPerBar;
            SoundManager.Instance.OnLoopBarBoundary();
        }
    }
}
