using Photon.Pun;
using UnityEngine;

namespace FrogSkill;

public sealed class ScoutTongue : MonoBehaviour
{
    private const string FireRpc = nameof(RPCA_FrogSkillFire);
    private const string MissRpc = nameof(RPCA_FrogSkillMiss);
    private const string ReleaseRpc = nameof(RPCA_FrogSkillRelease);
    private const float TongueTravelDuration = 0.25f;
    private const float MissExtendDuration = 0.12f;
    private const float MissRetractDuration = 0.12f;
    private const float MissDistanceRatio = 0.35f;
    private const float VanillaTargetTurnDuration = 0.5f;
    private const float NetworkMaxDistance = 65f;
    private static readonly Vector3 FallbackMouthLocalOffset = new(0f, 0.82f, 0.16f);
    private static readonly Color TongueColor = new(0.85f, 0.18f, 0.28f, 1f);

    private Character _caster = null!;
    private Renderer? _mouthRenderer;
    private Character? _target;
    private Rigidbody? _targetRig;
    private LineRenderer? _line;
    private Material? _lineMaterial;
    private float _firedAt;
    private float _pullStartedAt;
    private float _nextFireTime;
    private Vector3 _missEndpoint;
    private bool _missActive;
    private bool _pulling;

    private ModConfig Config => Plugin.Instance!.Settings;

    private void Awake()
    {
        _caster = GetComponent<Character>();
        AnimatedMouth? animatedMouth = GetComponent<AnimatedMouth>();
        _mouthRenderer = animatedMouth != null ? animatedMouth.mouthRenderer : null;
        if (_mouthRenderer == null && _caster.refs?.customization?.refs != null)
            _mouthRenderer = _caster.refs.customization.refs.mouthRenderer;
        CreateTongueVisual();
    }

    private void Update()
    {
        if (_target != null && !IsValidActiveTarget())
            ReleaseOrClear();

        UpdateTongueVisual();
        if (!_caster.IsLocal || Plugin.Instance == null || !Config.Enabled.Value || !CanReadInput())
            return;

        if (!Input.GetKeyDown(Config.ActivationKey.Value))
            return;

        if (_target != null)
        {
            SendRelease();
            return;
        }

        if (Time.time < _nextFireTime || !CanCasterAttack() ||
            !TryGetAim(out Character? target, out Vector3 endpoint))
            return;

        if (target != null)
        {
            if (!VanillaFrogDragProfile.TryGet(Plugin.Instance.ModLogger, out _))
                return;

            _nextFireTime = Time.time + Config.Cooldown.Value;
            _caster.photonView.RPC(FireRpc, RpcTarget.All, target.photonView.ViewID);
        }
        else
        {
            _nextFireTime = Time.time + Config.Cooldown.Value;
            _caster.photonView.RPC(MissRpc, RpcTarget.All, endpoint);
        }
    }

    private void FixedUpdate()
    {
        if (!_pulling || _target == null || _targetRig == null)
            return;

        if (Plugin.Instance == null || !Config.Enabled.Value || !IsValidActiveTarget())
        {
            ReleaseOrClear();
            return;
        }

        Vector3 anchor = GetMouthPosition();
        Vector3 delta = anchor - _target.Center;
        float distance = delta.magnitude;
        if (!VanillaFrogDragProfile.TryGet(Plugin.Instance.ModLogger, out VanillaFrogDragProfile vanilla))
        {
            ReleaseOrClear();
            return;
        }

        Vector3 direction = distance > 0.001f ? delta / distance : Vector3.zero;
        float curveMultiplier = vanilla.PullStrengthCurve.Evaluate(distance);
        Vector3 pull = direction * Config.PullForce.Value * curveMultiplier;
        Vector3 lift = Vector3.up * Mathf.Clamp(delta.y, 0f, vanilla.MaxLiftDistance) *
                       Config.LiftForce.Value;

        _target.refs.movement.ApplyExtraDrag(Config.ExtraDragOther.Value, ignoreRagdoll: true);
        _target.data.sinceGrounded = Mathf.Clamp(_target.data.sinceGrounded, 0f, 1f);

        if (!_caster.photonView.IsMine)
            return;

        _target.AddForceToBodyPart(_targetRig, (pull + lift) * 0.2f, pull + lift);

        // Vanilla applies the current physics frame's force before testing whether to let go.
        float effectivePullDuration = Mathf.Max(
            Time.fixedDeltaTime,
            Config.MaxHookDuration.Value - VanillaTargetTurnDuration);
        if (Vector3.Distance(anchor, _targetRig.position) < Config.StopDistance.Value ||
            Time.time - _pullStartedAt > effectivePullDuration)
        {
            SendRelease();
        }
    }

    [PunRPC]
    public void RPCA_FrogSkillFire(int targetViewId, PhotonMessageInfo info)
    {
        if (!IsOwnerMessage(info.Sender) || _target != null)
            return;

        PhotonView targetView = PhotonView.Find(targetViewId);
        Character? target = targetView == null ? null : targetView.GetComponent<Character>();
        if (!IsPermittedTarget(target) || Plugin.Instance == null ||
            !VanillaFrogDragProfile.TryGet(Plugin.Instance.ModLogger, out _))
            return;

        if (Vector3.Distance(_caster.Center, target!.Center) > NetworkMaxDistance)
            return;

        ClearMiss();
        _target = target;
        _targetRig = target.refs.ragdoll.partDict[BodypartType.Torso].Rig;
        _firedAt = Time.time;
        _pulling = false;
        _line!.enabled = true;
    }

    [PunRPC]
    public void RPCA_FrogSkillMiss(Vector3 endpoint, PhotonMessageInfo info)
    {
        if (!IsOwnerMessage(info.Sender) || _target != null ||
            Vector3.Distance(_caster.Center, endpoint) > NetworkMaxDistance)
            return;

        _missEndpoint = endpoint;
        _firedAt = Time.time;
        _missActive = true;
        _line!.enabled = true;
    }

    [PunRPC]
    public void RPCA_FrogSkillRelease(PhotonMessageInfo info)
    {
        if (IsOwnerMessage(info.Sender))
            ClearTarget();
    }

    private bool TryGetAim(out Character? target, out Vector3 endpoint)
    {
        target = null;
        endpoint = default;
        Camera camera = MainCamera.instance != null
            ? MainCamera.instance.GetComponent<Camera>()
            : Camera.main;
        if (camera == null)
            return false;

        Ray aimRay = camera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
        float missDistance = Config.MaxDistance.Value * MissDistanceRatio;
        endpoint = aimRay.GetPoint(missDistance);
        RaycastHit[] hits = Physics.RaycastAll(
            aimRay,
            Config.MaxDistance.Value,
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

    private bool IsValidActiveTarget()
    {
        return IsPermittedTarget(_target) && _targetRig != null && !_caster.data.dead;
    }

    private bool IsPermittedTarget(Character? target)
    {
        if (target == null || target == _caster)
            return false;

        bool supportedType = target.isZombie || (!target.isBot && !target.isScoutmaster);
        return supportedType && target.data != null && !target.data.dead &&
               !target.data.fullyPassedOut && target.refs?.ragdoll != null &&
               target.refs.ragdoll.partDict.ContainsKey(BodypartType.Torso);
    }

    private bool CanCasterAttack()
    {
        return _caster.data != null && !_caster.data.dead && !_caster.data.fullyPassedOut && !_caster.warping;
    }

    private static bool CanReadInput()
    {
        return Time.timeScale > 0f && GUIManager.instance != null &&
               !GUIManager.instance.windowBlockingInput && !GUIManager.instance.wheelActive;
    }

    private bool IsOwnerMessage(Photon.Realtime.Player sender)
    {
        return sender != null && _caster.photonView.Owner != null && sender.ActorNumber == _caster.photonView.Owner.ActorNumber;
    }

    private void SendRelease()
    {
        if (_caster.photonView != null)
            _caster.photonView.RPC(ReleaseRpc, RpcTarget.All);
    }

    private void ReleaseOrClear()
    {
        if (_caster.photonView.IsMine)
            SendRelease();
        else
            ClearTarget();
    }

    private void ClearTarget()
    {
        if (_pulling && _target != null && _target.refs?.movement != null)
        {
            float releaseDrag = Plugin.Instance == null ? 0.1f : Config.ExtraDragLetGo.Value;
            _target.refs.movement.ApplyExtraDrag(releaseDrag, ignoreRagdoll: true);
            _target.data.sinceGrounded = 0f;
        }

        _pulling = false;
        _targetRig = null;
        _target = null;
        if (_line != null && !_missActive)
            _line.enabled = false;
    }

    private void ClearMiss()
    {
        _missActive = false;
        if (_line != null && _target == null)
            _line.enabled = false;
    }

    private Vector3 GetMouthPosition()
    {
        if (_mouthRenderer != null)
            return _mouthRenderer.bounds.center;

        Transform head = _caster.refs.head.transform;
        return head.TransformPoint(FallbackMouthLocalOffset);
    }

    private void CreateTongueVisual()
    {
        GameObject visual = new("FrogSkill Tongue");
        visual.transform.SetParent(transform, false);
        _line = visual.AddComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.positionCount = 2;
        _line.startWidth = 0.075f;
        _line.endWidth = 0.035f;
        _line.numCapVertices = 4;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            _lineMaterial = new Material(shader);
            _line.material = _lineMaterial;
        }
        _line.startColor = TongueColor;
        _line.endColor = TongueColor;
        _line.enabled = false;
    }

    private void UpdateTongueVisual()
    {
        if (_line == null || !_line.enabled)
            return;

        Vector3 mouth = GetMouthPosition();
        if (_missActive)
        {
            float elapsed = Time.time - _firedAt;
            float missExtension = Mathf.Clamp01(elapsed / MissExtendDuration);
            float retraction = Mathf.Clamp01((elapsed - MissExtendDuration) / MissRetractDuration);
            _line.SetPosition(0, mouth);
            _line.SetPosition(1, Vector3.Lerp(mouth, _missEndpoint, missExtension * (1f - retraction)));

            if (retraction >= 1f)
                ClearMiss();
            return;
        }

        if (_target == null)
            return;

        float extension = Mathf.Clamp01((Time.time - _firedAt) / TongueTravelDuration);
        _line.SetPosition(0, mouth);
        _line.SetPosition(1, Vector3.Lerp(mouth, _target.Center, extension));

        if (extension >= 1f && !_pulling)
            BeginPull();
    }

    private void BeginPull()
    {
        if (_target == null)
            return;

        _pulling = true;
        _pullStartedAt = Time.time;
        Vector3 towardCaster = GetMouthPosition() - _target.Center;
        towardCaster.y = 0f;
        if (towardCaster.sqrMagnitude > 0.0001f)
            _target.data.lookValues = DirectionToLook(towardCaster.normalized);

        if (_target.photonView.IsMine)
        {
            _target.refs.climbing.StopAnyClimbing();
            GamefeelHandler.instance?.AddPerlinShake(5f, 0.2f);
        }

        // Fire is already an RpcTarget.All event, so each receiver applies the state locally.
        _target.RPCA_Fall(1f, 0f);
    }

    private static Vector3 DirectionToLook(Vector3 direction)
    {
        Vector3 euler = Quaternion.LookRotation(direction, Vector3.up).eulerAngles;
        while (euler.x > 180f)
            euler.x -= 360f;
        return new Vector3(euler.y, -euler.x, 0f);
    }

    private void OnDestroy()
    {
        ClearTarget();
        ClearMiss();
        if (_lineMaterial != null)
            Destroy(_lineMaterial);
    }
}
