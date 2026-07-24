using System;
using System.Collections.Generic;
using UnityEngine;

public static class ConfigurationProfileSaveConverter
{
    public static SavedConfiguration Capture(
        ConfigurationProfile profile,
        string id,
        string displayName,
        string createdUtc)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));

        string now = DateTime.UtcNow.ToString("o");

        SavedConfiguration save = new SavedConfiguration
        {
            id = id,
            displayName = string.IsNullOrWhiteSpace(displayName)
                ? "Untitled Configuration"
                : displayName.Trim(),
            createdUtc = string.IsNullOrWhiteSpace(createdUtc)
                ? now
                : createdUtc,
            modifiedUtc = now
        };

        if (profile.instruments == null)
            return save;

        foreach (InstrumentConfig config in profile.instruments)
        {
            if (config == null)
                continue;

            save.instruments.Add(new SavedInstrumentConfig
            {
                instrumentId = config.instrumentId,
                pattern = config.pattern.ToString(),
                style = config.style.ToString()
            });
        }

        return save;
    }

    public static bool TryBuildRuntimeProfile(
        SavedConfiguration save,
        InstrumentStemDatabase database,
        out ConfigurationProfile runtimeProfile,
        out string error)
    {
        runtimeProfile = null;

        if (save == null)
        {
            error = "The loaded configuration is null.";
            return false;
        }

        if (database == null)
        {
            error = "InstrumentStemDatabase is not assigned.";
            return false;
        }

        List<InstrumentConfig> configs = new();

        foreach (SavedInstrumentConfig savedInstrument in save.instruments)
        {
            if (!Enum.TryParse(savedInstrument.pattern, out PatternType pattern))
            {
                error = $"Unknown pattern '{savedInstrument.pattern}'.";
                return false;
            }

            if (!Enum.TryParse(savedInstrument.style, out StyleType style))
            {
                error = $"Unknown style '{savedInstrument.style}'.";
                return false;
            }

            InstrumentStem stem = database.Find(
                savedInstrument.instrumentId,
                pattern,
                style);

            if (stem == null)
            {
                error =
                    $"No stem exists for {savedInstrument.instrumentId}, " +
                    $"{pattern}, {style}.";
                return false;
            }

            configs.Add(new InstrumentConfig
            {
                instrumentId = savedInstrument.instrumentId,
                pattern = pattern,
                style = style,
                stem = stem
            });
        }

        runtimeProfile = ScriptableObject.CreateInstance<ConfigurationProfile>();
        runtimeProfile.name = $"Runtime_{save.displayName}";
        runtimeProfile.instruments = configs.ToArray();

        error = string.Empty;
        return true;
    }
}
