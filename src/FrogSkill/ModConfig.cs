using BepInEx.Configuration;
using UnityEngine;

namespace FrogSkill;

internal sealed class ModConfig
{
    public ModConfig(ConfigFile config)
    {
        Enabled = config.Bind("General", "Enabled", true, "Enable the Scout tongue skill.");
        ActivationKey = config.Bind(
            "Controls",
            "ActivationKey",
            KeyCode.G,
            "Fire the tongue, or re-grab while pulling once cooldown has elapsed.");
        ModeSwitchKey = config.Bind(
            "Controls",
            "ModeSwitchKey",
            KeyCode.C,
            "Switch between Custom and Vanilla tongue modes for the next shot.");

        MaxDistance = BindRange(
            config, "Tongue", "MaxDistance", 40f, 5f, 60f,
            "Maximum target distance in meters. Vanilla FrogTongue default: 40.");
        AimForgivenessDegrees = BindRange(
            config, "Tongue", "AimForgivenessDegrees", 3f, 0f, 10f,
            "Aim-assist cone half-angle in degrees. Direct crosshair hits always take priority; set to 0 for strict targeting.");
        PullForce = BindRange(
            config, "Tongue", "PullForce", 450f, 0f, 2000f,
            "Directional pull acceleration before the vanilla distance curve. Current Frog prefab default: 450.");
        LiftForce = BindRange(
            config, "Tongue", "LiftForce", 30f, 0f, 500f,
            "Additional upward acceleration per vertical meter. FrogSkill default: 30; set to 0 for no supplemental lift.");
        MaxHookDuration = BindRange(
            config, "Tongue", "MaxHookDuration", 1f, 0.5f, 10f,
            "Vanilla-style maximum hook time in seconds, including the 0.5 second target-turn phase. Current Frog prefab default: 1.");
        StopDistance = BindRange(
            config, "Tongue", "StopDistance", 5f, 1f, 10f,
            "Release when the target reaches this distance. Vanilla FrogTongue default: 5.");
        ExtraDragOther = BindRange(
            config, "Tongue", "ExtraDragOther", 0.95f, 0f, 1f,
            "Velocity retention applied each physics frame while pulling. Vanilla FrogTongue default: 0.95.");
        ExtraDragLetGo = BindRange(
            config, "Tongue", "ExtraDragLetGo", 0.1f, 0f, 1f,
            "Velocity retention applied once when releasing. Vanilla FrogTongue default: 0.1.");
        Cooldown = BindRange(config, "Tongue", "Cooldown", 0.5f, 0f, 60f, "Cooldown after successfully grabbing a target. Misses do not trigger it.");
    }

    public ConfigEntry<bool> Enabled { get; }
    public ConfigEntry<KeyCode> ActivationKey { get; }
    public ConfigEntry<KeyCode> ModeSwitchKey { get; }
    public ConfigEntry<float> MaxDistance { get; }
    public ConfigEntry<float> AimForgivenessDegrees { get; }
    public ConfigEntry<float> PullForce { get; }
    public ConfigEntry<float> LiftForce { get; }
    public ConfigEntry<float> MaxHookDuration { get; }
    public ConfigEntry<float> StopDistance { get; }
    public ConfigEntry<float> ExtraDragOther { get; }
    public ConfigEntry<float> ExtraDragLetGo { get; }
    public ConfigEntry<float> Cooldown { get; }

    private static ConfigEntry<float> BindRange(
        ConfigFile config,
        string section,
        string key,
        float value,
        float min,
        float max,
        string description)
    {
        return config.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueRange<float>(min, max)));
    }
}
