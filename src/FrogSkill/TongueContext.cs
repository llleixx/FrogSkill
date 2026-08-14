using UnityEngine;

namespace FrogSkill;

internal sealed class TongueContext
{
    private const float MissDistanceRatio = 0.35f;
    private static readonly Vector3 FallbackMouthLocalOffset = new(0f, 0.82f, 0.16f);

    private readonly Character _caster;
    private readonly Renderer? _mouthRenderer;

    internal TongueContext(Character caster)
    {
        _caster = caster;
        AnimatedMouth? animatedMouth = caster.GetComponent<AnimatedMouth>();
        _mouthRenderer = animatedMouth != null ? animatedMouth.mouthRenderer : null;
        if (_mouthRenderer == null && caster.refs?.customization?.refs != null)
            _mouthRenderer = caster.refs.customization.refs.mouthRenderer;
    }

    internal bool TryGetAim(
        float maxDistance,
        out Character? target,
        out Vector3 endpoint)
    {
        target = null;
        endpoint = default;
        Camera camera = MainCamera.instance != null
            ? MainCamera.instance.GetComponent<Camera>()
            : Camera.main;
        if (camera == null)
            return false;

        Ray aimRay = camera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
        float missDistance = maxDistance * MissDistanceRatio;
        endpoint = aimRay.GetPoint(missDistance);
        RaycastHit[] hits = Physics.RaycastAll(
            aimRay,
            maxDistance,
            HelperFunctions.GetMask(HelperFunctions.LayerType.AllPhysicalExceptDefault));
        RaycastHit nearestHit = default;
        float nearestDistance = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.distance >= nearestDistance)
                continue;

            CharacterRagdoll.TryGetCharacterFromCollider(hit.collider, out Character hitCharacter);
            if (hitCharacter == _caster)
                continue;

            nearestDistance = hit.distance;
            nearestHit = hit;
        }

        if (nearestHit.collider != null && nearestHit.distance < missDistance)
            endpoint = nearestHit.point;

        if (nearestHit.collider == null ||
            !CharacterRagdoll.TryGetCharacterFromCollider(nearestHit.collider, out Character hitTarget) ||
            !IsPermittedTarget(hitTarget))
            return true;

        target = hitTarget;
        return true;
    }

    internal bool IsPermittedTarget(Character? target)
    {
        if (target == null || target == _caster)
            return false;

        bool supportedType = target.isZombie || (!target.isBot && !target.isScoutmaster);
        return supportedType && target.data != null && !target.data.dead &&
               !target.data.fullyPassedOut && target.refs?.ragdoll != null &&
               target.refs.ragdoll.partDict.ContainsKey(BodypartType.Torso);
    }

    internal bool CanCasterAttack()
    {
        return _caster.data != null && !_caster.data.dead &&
               !_caster.data.fullyPassedOut && !_caster.warping;
    }

    internal Vector3 GetMouthPosition()
    {
        if (_mouthRenderer != null)
            return _mouthRenderer.bounds.center;

        Transform head = _caster.refs.head.transform;
        return head.TransformPoint(FallbackMouthLocalOffset);
    }
}
