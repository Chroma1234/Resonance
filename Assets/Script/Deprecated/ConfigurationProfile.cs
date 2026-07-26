using UnityEngine;

[System.Serializable]
public class InstrumentConfig
{
    public string instrumentId;

    public PatternType pattern;
    public StyleType style;

    public InstrumentStem stem;
}

public enum PatternType { A, B }
public enum StyleType { A, B }

[CreateAssetMenu(
    fileName = "ConfigurationProfile",
    menuName = "Resonance/Configuration Profile")]
public class ConfigurationProfile : ScriptableObject
{
    public InstrumentConfig[] instruments;
}
