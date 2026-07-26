using UnityEngine;

[System.Serializable]
public class InstrumentConfig
{
    public string instrumentId;

    public Mood style;

    public InstrumentStem stem;
}

[CreateAssetMenu(
    fileName = "ConfigurationProfile",
    menuName = "Resonance/Configuration Profile")]
public class ConfigurationProfile : ScriptableObject
{
    public InstrumentConfig[] instruments;
}
