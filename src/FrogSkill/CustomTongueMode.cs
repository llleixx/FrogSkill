using Photon.Pun;
using UnityEngine;

namespace FrogSkill;

public sealed class CustomTongueMode : MonoBehaviour, ITongueMode
{
    // These wire names are shared with FrogSkill 1.0 clients and must remain stable.
    private const string FireRpc = nameof(RPCA_FrogSkillFire);
    private const string MissRpc = nameof(RPCA_FrogSkillMiss);
    private const string ReleaseRpc = nameof(RPCA_FrogSkillRelease);
    private const float TongueTravelDuration = 0.25f;
    private const float FrogTargetTurnDuration = 0.5f;
    private const float NetworkMaxDistance = 65f;

    private Character _caster = null!;
    private TongueContext _context = null!;
    private Character? _target;
    private Rigidbody? _targetRig;
    private LineRenderer? _line;
    private Material? _lineMaterial;
    private float _firedAt;
    private float _pullStartedAt;
    private Vector3 _missEndpoint;
    private bool _missActive;
    private bool _pulling;

    private ModConfig Config => Plugin.Instance!.Settings;
    bool ITongueMode.CanRelease => _target != null;
    bool ITongueMode.IsBusy => _target != null || _missActive;

    private void Awake()
    {
        _caster = GetComponent<Character>();
        _context = new TongueContext(_caster);
        CreateTongueVisual();
    }

    private void Update()
    {
        if (_target != null && !IsValidActiveTarget())
            ReleaseOrClear();

        UpdateTongueVisual();
    }

    bool ITongueMode.TryFire()
    {
        if (!_caster.IsLocal || Plugin.Instance == null || !Config.Enabled.Value ||
            _target != null || _missActive || !_context.CanCasterAttack() ||
            !_context.TryGetAim(
                Config.MaxDistance.Value,
                Config.AimForgivenessDegrees.Value,
                out Character? target,
                out Vector3 endpoint))
            return false;

        if (target != null)
        {
            if (!FrogPullProfile.TryGet(Plugin.Instance.ModLogger, out _))
                return false;

            _caster.photonView.RPC(FireRpc, RpcTarget.All, target.photonView.ViewID);
        }
        else
        {
            _caster.photonView.RPC(MissRpc, RpcTarget.All, endpoint);
        }

        return true;
    }

    void ITongueMode.Release()
    {
        if (_caster.IsLocal && _target != null)
            SendRelease();
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

        Vector3 anchor = _context.GetMouthPosition();
        Vector3 delta = anchor - _target.Center;
        float distance = delta.magnitude;
        if (!FrogPullProfile.TryGet(Plugin.Instance.ModLogger, out FrogPullProfile pullProfile))
        {
            ReleaseOrClear();
            return;
        }

        Vector3 direction = distance > 0.001f ? delta / distance : Vector3.zero;
        float curveMultiplier = pullProfile.PullStrengthCurve.Evaluate(distance);
        Vector3 pull = direction * Config.PullForce.Value * curveMultiplier;
        Vector3 lift = Vector3.up * Mathf.Clamp(delta.y, 0f, pullProfile.MaxLiftDistance) *
                       Config.LiftForce.Value;

        _target.refs.movement.ApplyExtraDrag(Config.ExtraDragOther.Value, ignoreRagdoll: true);
        _target.data.sinceGrounded = Mathf.Clamp(_target.data.sinceGrounded, 0f, 1f);

        if (!_caster.photonView.IsMine)
            return;

        _target.AddForceToBodyPart(_targetRig, (pull + lift) * 0.2f, pull + lift);

        // Vanilla applies the current physics frame's force before testing whether to let go.
        float effectivePullDuration = Mathf.Max(
            Time.fixedDeltaTime,
            Config.MaxHookDuration.Value - FrogTargetTurnDuration);
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
        if (!_context.IsPermittedTarget(target) || Plugin.Instance == null ||
            !FrogPullProfile.TryGet(Plugin.Instance.ModLogger, out _))
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

    private bool IsValidActiveTarget()
    {
        return _context.IsPermittedTarget(_target) && _targetRig != null && !_caster.data.dead;
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

    private void CreateTongueVisual()
    {
        _line = TongueVisual.CreateLine(transform, "FrogSkill Custom Tongue", out _lineMaterial);
    }

    private void UpdateTongueVisual()
    {
        if (_line == null || !_line.enabled)
            return;

        Vector3 mouth = _context.GetMouthPosition();
        if (_missActive)
        {
            if (TongueVisual.UpdateMiss(_line, _firedAt, mouth, _missEndpoint))
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
        Vector3 towardCaster = _context.GetMouthPosition() - _target.Center;
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
