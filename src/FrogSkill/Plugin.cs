using BepInEx;
using HarmonyLib;

namespace FrogSkill;

[BepInPlugin(PluginGuid, PluginName, BuildInfo.Version)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.github.lllei.FrogSkill";
    public const string PluginName = "FrogSkill";

    internal static Plugin? Instance { get; private set; }
    internal ModConfig Settings { get; private set; } = null!;
    internal BepInEx.Logging.ManualLogSource ModLogger => Logger;

    private Harmony? _harmony;

    private void Awake()
    {
        Instance = this;
        Settings = new ModConfig(Config);
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(CharacterStartPatch));
        Logger.LogInfo($"{PluginName} {BuildInfo.Version} loaded. All players must install the mod for multiplayer use.");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        Instance = null;
    }
}

[HarmonyPatch(typeof(Character), "Start")]
internal static class CharacterStartPatch
{
    private static void Postfix(Character __instance)
    {
        if (__instance.GetComponent<ScoutTongue>() == null)
            __instance.gameObject.AddComponent<ScoutTongue>();

        // PUN caches RPC MonoBehaviours, so refresh after adding ours at runtime.
        __instance.photonView.RefreshRpcMonoBehaviourCache();
    }
}
