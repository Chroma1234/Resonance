using System;

[Serializable]
public class PlayerStatisticsData
{
    public int totalSessions;
    public float totalPlayTimeSeconds;

    public int configurationsCreated;
    public int configurationsLoaded;
    public int configurationsOverwritten;
    public int configurationsDeleted;

    public int instrumentsActivated;
    public int duetsTriggered;

    public string lastPlayedUtc;

    public PlayerStatisticsData()
    {
        totalSessions = 0;
        totalPlayTimeSeconds = 0f;

        configurationsCreated = 0;
        configurationsLoaded = 0;
        configurationsOverwritten = 0;
        configurationsDeleted = 0;

        instrumentsActivated = 0;
        duetsTriggered = 0;

        lastPlayedUtc = DateTime.UtcNow.ToString("O");
    }
}