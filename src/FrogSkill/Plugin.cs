using BepInEx;
using HarmonyLib;

namespace FrogSkill;

[BepInPlugin(PluginGuid, PluginName, BuildInfo.Version)]
[BepInIncompatibility(VanillaPluginGuid)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.github.lllei.FrogSkill";
    public const string PluginName = "FrogSkill";
    private const string VanillaPluginGuid = "com.github.lllei.FrogSkillVanilla";

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
        _harmony.PatchAll(typeof(FrogProxyAiPatch));
        _harmony.PatchAll(typeof(FrogProxyCollisionPatch));
        _harmony.PatchAll(typeof(FrogProxyReleasePatch));
        Logger.LogInfo($"{PluginName} {BuildInfo.Version} loaded with Custom and Vanilla tongue modes.");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        Instance = null;
    }
}

[HarmonyPatch(typeof(FrogTongue), nameof(FrogTongue.RPCA_FrogAction))]
internal static class FrogProxyReleasePatch
{
    private static void Postfix(
        FrogTongue __instance,
        FrogTongue.FrogActionType frogActionType)
    {
        if (frogActionType == FrogTongue.FrogActionType.LetGo)
            VanillaFrogProxy.DestroyAfterRelease(__instance.photonView);
    }
}

[HarmonyPatch(typeof(FrogTongue), nameof(FrogTongue.OnCollisionEnter))]
internal static class FrogProxyCollisionPatch
{
    private static bool Prefix(FrogTongue __instance)
    {
        return !VanillaFrogProxy.IsProxy(__instance.photonView);
    }
}

[HarmonyPatch(typeof(FrogTongue), nameof(FrogTongue.CheckAllCharacters))]
internal static class FrogProxyAiPatch
{
    private static bool Prefix(FrogTongue __instance)
    {
        return !VanillaFrogProxy.IsProxy(__instance.photonView);
    }
}

[HarmonyPatch(typeof(Character), "Start")]
internal static class CharacterStartPatch
{
    private static void Postfix(Character __instance)
    {
        if (__instance.GetComponent<CustomTongueMode>() == null)
            __instance.gameObject.AddComponent<CustomTongueMode>();

        // PUN caches RPC MonoBehaviours, so refresh after adding ours at runtime.
        __instance.photonView.RefreshRpcMonoBehaviourCache();

        if (!__instance.IsLocal)
            return;

        if (__instance.GetComponent<VanillaTongueMode>() == null)
            __instance.gameObject.AddComponent<VanillaTongueMode>();
        if (__instance.GetComponent<TongueModeController>() == null)
            __instance.gameObject.AddComponent<TongueModeController>();
    }
}
