using System.Collections;
using TMPro;
using UnityEngine;

namespace FrogSkill;

internal sealed class TongueModeController : MonoBehaviour
{
    private enum TongueMode
    {
        Custom,
        Vanilla
    }

    private ITongueMode _customTongue = null!;
    private ITongueMode _vanillaTongue = null!;
    private Character _caster = null!;
    private TongueMode _mode;
    private float _nextFireTime;
    private TextMeshProUGUI? _modeLabel;
    private Material? _modeLabelMaterial;
    private CanvasGroup? _modeLabelGroup;
    private Coroutine? _modeLabelRoutine;

    private ModConfig Config => Plugin.Instance!.Settings;

    private void Awake()
    {
        _caster = GetComponent<Character>();
        _customTongue = GetComponent<CustomTongueMode>();
        _vanillaTongue = GetComponent<VanillaTongueMode>();
        _mode = TongueMode.Custom;
    }

    private void Update()
    {
        if (Plugin.Instance == null || !CanReadInput())
            return;

        bool keysConflict = Config.ActivationKey.Value == Config.ModeSwitchKey.Value;
        bool switchPressed = !keysConflict && Input.GetKeyDown(Config.ModeSwitchKey.Value);
        bool actionPressed = Input.GetKeyDown(Config.ActivationKey.Value);

        if (switchPressed)
            SwitchMode();

        // Fire/release wins if both actions are configured to the same key.
        if (!actionPressed)
            return;

        if (_customTongue.CanRelease)
        {
            _customTongue.Release();
            return;
        }

        if (_vanillaTongue.CanRelease)
        {
            _vanillaTongue.Release();
            return;
        }

        if (!Config.Enabled.Value || _customTongue.IsBusy || _vanillaTongue.IsBusy ||
            Time.time < _nextFireTime)
            return;

        bool fired = _mode == TongueMode.Custom
            ? _customTongue.TryFire()
            : _vanillaTongue.TryFire();
        if (fired)
            _nextFireTime = Time.time + Config.Cooldown.Value;
    }

    private void SwitchMode()
    {
        _mode = _mode == TongueMode.Custom
            ? TongueMode.Vanilla
            : TongueMode.Custom;

        string modeName = _mode == TongueMode.Vanilla ? "Vanilla" : "Custom";
        ShowModeNotification(modeName);
        Plugin.Instance?.ModLogger.LogInfo($"Tongue mode switched to {modeName}.");
    }

    private void ShowModeNotification(string modeName)
    {
        if (!EnsureModeLabel() || _modeLabel == null || _modeLabelGroup == null)
            return;

        Color scoutColor = _caster.refs.customization.PlayerColor;
        scoutColor.a = 1f;
        _modeLabel.color = scoutColor;
        _modeLabel.text = $"Tongue mode: {modeName}";
        _modeLabel.transform.SetAsLastSibling();
        if (_modeLabelRoutine != null)
            StopCoroutine(_modeLabelRoutine);
        _modeLabelRoutine = StartCoroutine(ShowModeLabelRoutine());
    }

    private bool EnsureModeLabel()
    {
        if (_modeLabel != null && _modeLabelGroup != null)
            return true;
        if (GUIManager.instance == null || GUIManager.instance.hudCanvas == null)
            return false;

        GameObject labelObject = new(
            "FrogSkill Mode Notification",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(GUIManager.instance.hudCanvas.transform, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -96f);
        rect.sizeDelta = new Vector2(520f, 52f);

        _modeLabelGroup = labelObject.GetComponent<CanvasGroup>();
        _modeLabelGroup.alpha = 0f;
        _modeLabelGroup.blocksRaycasts = false;
        _modeLabelGroup.interactable = false;

        _modeLabel = labelObject.GetComponent<TextMeshProUGUI>();
        _modeLabel.alignment = TextAlignmentOptions.Center;
        _modeLabel.fontSize = 30f;
        TextMeshProUGUI? fontSource = GUIManager.instance.interactNameText ??
                                          GUIManager.instance.itemPromptMain;
        if (fontSource != null && fontSource.font != null)
        {
            _modeLabel.font = fontSource.font;
            _modeLabel.fontStyle = fontSource.fontStyle;
            if (fontSource.fontSharedMaterial != null)
            {
                _modeLabelMaterial = new Material(fontSource.fontSharedMaterial);
                _modeLabel.fontSharedMaterial = _modeLabelMaterial;
            }
        }
        _modeLabel.faceColor = Color.white;
        _modeLabel.outlineColor = Color.black;
        _modeLabel.outlineWidth = 0.08f;
        _modeLabel.raycastTarget = false;
        return true;
    }

    private IEnumerator ShowModeLabelRoutine()
    {
        if (_modeLabelGroup == null)
            yield break;

        _modeLabelGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(1.25f);

        const float fadeDuration = 0.25f;
        float elapsed = 0f;
        while (elapsed < fadeDuration && _modeLabelGroup != null)
        {
            elapsed += Time.unscaledDeltaTime;
            _modeLabelGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        if (_modeLabelGroup != null)
            _modeLabelGroup.alpha = 0f;
        _modeLabelRoutine = null;
    }

    private static bool CanReadInput()
    {
        return Time.timeScale > 0f && GUIManager.instance != null &&
               !GUIManager.instance.windowBlockingInput && !GUIManager.instance.wheelActive;
    }

    private void OnDestroy()
    {
        if (_modeLabel != null)
            Destroy(_modeLabel.gameObject);
        if (_modeLabelMaterial != null)
            Destroy(_modeLabelMaterial);
    }
}
