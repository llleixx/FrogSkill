using BepInEx.Logging;
using UnityEngine;

namespace FrogSkill;

internal sealed class FrogPullProfile
{
    private const float RetryInterval = 5f;

    private static FrogPullProfile? _cached;
    private static float _nextSearchTime;
    private static bool _missingLogged;

    private FrogPullProfile(FrogTongue source)
    {
        PullStrengthCurve = CloneCurve(source.pullStrengthCurve);
        MaxLiftDistance = source.maxLiftDistance;
    }

    public AnimationCurve PullStrengthCurve { get; }
    public float MaxLiftDistance { get; }

    public static bool TryGet(ManualLogSource logger, out FrogPullProfile profile)
    {
        if (_cached != null)
        {
            profile = _cached;
            return true;
        }

        if (Time.unscaledTime < _nextSearchTime)
        {
            profile = null!;
            return false;
        }

        _nextSearchTime = Time.unscaledTime + RetryInterval;
        foreach (FrogTongue frog in Resources.FindObjectsOfTypeAll<FrogTongue>())
        {
            if (frog == null || frog.pullStrengthCurve == null || frog.pullStrengthCurve.length == 0)
                continue;

            _cached = new FrogPullProfile(frog);
            _missingLogged = false;
            logger.LogInfo(
                $"Captured vanilla FrogTongue drag profile from {frog.gameObject.name}: " +
                $"curve keys={_cached.PullStrengthCurve.length}, max lift distance={_cached.MaxLiftDistance}.");
            profile = _cached;
            return true;
        }

        if (!_missingLogged)
        {
            _missingLogged = true;
            logger.LogWarning("A loaded FrogTongue prefab was not found yet; tongue firing is unavailable until the vanilla pull curve can be captured.");
        }

        profile = null!;
        return false;
    }

    private static AnimationCurve CloneCurve(AnimationCurve source)
    {
        AnimationCurve clone = new(source.keys)
        {
            preWrapMode = source.preWrapMode,
            postWrapMode = source.postWrapMode
        };
        return clone;
    }
}
