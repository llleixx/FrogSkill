using UnityEngine;

namespace FrogSkill;

internal sealed class TongueContext
{
    private const float MissDistanceRatio = 0.35f;
    private const float VisibilityRayExtraDistance = 0.05f;
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
        float aimForgivenessDegrees,
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
        int physicalMask = HelperFunctions.GetMask(HelperFunctions.LayerType.AllPhysicalExceptDefault);
        TryGetNearestPhysicalHit(aimRay, maxDistance, physicalMask, out RaycastHit nearestHit);

        if (nearestHit.collider != null && nearestHit.distance < missDistance)
            endpoint = nearestHit.point;

        if (nearestHit.collider != null &&
            CharacterRagdoll.TryGetCharacterFromCollider(nearestHit.collider, out Character hitTarget) &&
            IsPermittedTarget(hitTarget))
        {
            target = hitTarget;
            return true;
        }

        if (aimForgivenessDegrees > 0f)
            target = FindAssistedTarget(aimRay, maxDistance, aimForgivenessDegrees, physicalMask);
        return true;
    }

    private Character? FindAssistedTarget(
        Ray aimRay,
        float maxDistance,
        float aimForgivenessDegrees,
        int physicalMask)
    {
        float maxSlope = Mathf.Tan(aimForgivenessDegrees * Mathf.Deg2Rad);
        float bestSlope = float.MaxValue;
        float bestDistance = float.MaxValue;
        Character? bestTarget = null;
        Collider[] colliders = Physics.OverlapSphere(aimRay.origin, maxDistance, physicalMask);

        foreach (Collider collider in colliders)
        {
            if (collider == null ||
                !CharacterRagdoll.TryGetCharacterFromCollider(collider, out Character candidate) ||
                !IsPermittedTarget(candidate))
                continue;

            float centerProjection = Vector3.Dot(collider.bounds.center - aimRay.origin, aimRay.direction);
            Vector3 pointOnRay = aimRay.GetPoint(Mathf.Clamp(centerProjection, 0f, maxDistance));
            Vector3 candidatePoint = collider.ClosestPoint(pointOnRay);
            Vector3 toCandidate = candidatePoint - aimRay.origin;
            float forwardDistance = Vector3.Dot(toCandidate, aimRay.direction);
            float candidateDistance = toCandidate.magnitude;
            if (forwardDistance <= 0f || candidateDistance <= 0f || candidateDistance > maxDistance)
                continue;

            float lateralDistance = Vector3.Cross(aimRay.direction, toCandidate).magnitude;
            float allowedRadius = forwardDistance * maxSlope;
            if (lateralDistance > allowedRadius)
                continue;

            float slope = lateralDistance / forwardDistance;
            if (slope > bestSlope || (Mathf.Approximately(slope, bestSlope) && candidateDistance >= bestDistance))
                continue;

            Ray visibilityRay = new(aimRay.origin, toCandidate / candidateDistance);
            if (!TryGetNearestPhysicalHit(
                    visibilityRay,
                    candidateDistance + VisibilityRayExtraDistance,
                    physicalMask,
                    out RaycastHit visibilityHit) ||
                !CharacterRagdoll.TryGetCharacterFromCollider(
                    visibilityHit.collider,
                    out Character visibleCharacter) ||
                visibleCharacter != candidate)
                continue;

            bestSlope = slope;
            bestDistance = candidateDistance;
            bestTarget = candidate;
        }

        return bestTarget;
    }

    private bool TryGetNearestPhysicalHit(
        Ray ray,
        float maxDistance,
        int physicalMask,
        out RaycastHit nearestHit)
    {
        nearestHit = default;
        float nearestDistance = float.MaxValue;
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, physicalMask);

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

        return nearestHit.collider != null;
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
