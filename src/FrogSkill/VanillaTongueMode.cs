using Photon.Pun;
using UnityEngine;

namespace FrogSkill;

internal sealed class VanillaTongueMode : MonoBehaviour, ITongueMode
{
    private const string FrogPrefabName = "0_Items/Frog";
    private const string FrogActionRpc = "RPCA_FrogAction";
    private const string SetKinematicRpc = "SetKinematicRPC";
    private const float ProxySyncInterval = 0.08f;
    private const float ProxyCleanupDelay = 0.2f;

    private Character _caster = null!;
    private TongueContext _context = null!;
    private GameObject? _proxy;
    private FrogTongue? _proxyTongue;
    private PhotonView? _proxyView;
    private LineRenderer? _missLine;
    private Material? _missLineMaterial;
    private float _nextProxySyncTime;
    private float _cleanupAt = float.PositiveInfinity;
    private float _missStartedAt;
    private Vector3 _missEndpoint;
    private bool _missActive;
    private bool _releaseSent;

    private ModConfig Config => Plugin.Instance!.Settings;
    bool ITongueMode.CanRelease => _proxy != null && !_releaseSent;
    bool ITongueMode.IsBusy => _proxy != null || _missActive;

    private void Awake()
    {
        _caster = GetComponent<Character>();
        _context = new TongueContext(_caster);

        CreateMissVisual();
    }

    private void Update()
    {
        UpdateMissVisual();
        if (!_caster.IsLocal || Plugin.Instance == null)
            return;

        if (_proxy != null)
        {
            FollowProxy();
            if (Time.time >= _cleanupAt)
                DestroyProxy();
        }
    }

    bool ITongueMode.TryFire()
    {
        if (!_caster.IsLocal || Plugin.Instance == null || !Config.Enabled.Value ||
            _proxy != null || _missActive || !_context.CanCasterAttack() ||
            !_context.TryGetAim(
                Config.MaxDistance.Value,
                Config.AimForgivenessDegrees.Value,
                out Character? target,
                out Vector3 endpoint))
            return false;

        if (target != null)
            return FireProxy(target);

        StartMiss(endpoint);
        return true;
    }

    void ITongueMode.Release()
    {
        if (_caster.IsLocal)
            ReleaseProxy();
    }

    private bool FireProxy(Character target)
    {
        if (!PhotonNetwork.InRoom)
        {
            Plugin.Instance!.ModLogger.LogWarning("A Photon room is required to create the vanilla frog proxy.");
            return false;
        }

        Vector3 mouth = _context.GetMouthPosition();
        Quaternion rotation = GetProxyRotation(target.Center - mouth);
        GameObject proxy;
        try
        {
            proxy = PhotonNetwork.Instantiate(
                FrogPrefabName,
                mouth,
                rotation,
                0,
                VanillaFrogProxy.InstantiationData);
        }
        catch (System.Exception exception)
        {
            Plugin.Instance!.ModLogger.LogError($"Failed to instantiate the vanilla frog proxy: {exception}");
            return false;
        }

        _proxy = proxy;
        _proxyTongue = proxy.GetComponent<FrogTongue>();
        _proxyView = proxy.GetComponent<PhotonView>();
        Item? proxyItem = proxy.GetComponent<Item>();
        if (_proxyTongue == null || _proxyView == null || proxyItem == null)
        {
            Plugin.Instance!.ModLogger.LogError("The vanilla Frog prefab is missing FrogTongue, Item, or PhotonView.");
            DestroyProxy();
            return false;
        }

        ConfigureOwnedProxy(proxyItem, _proxyTongue);
        MoveProxy(mouth, rotation);

        _proxyView.RPC(SetKinematicRpc, RpcTarget.All, true, proxy.transform.position, proxy.transform.rotation);
        _proxyView.RPC(
            FrogActionRpc,
            RpcTarget.All,
            target.photonView,
            FrogTongue.FrogActionType.Attack,
            Vector3.zero);

        _nextProxySyncTime = Time.time + ProxySyncInterval;
        _cleanupAt = Time.time + _proxyTongue.maxScoutHookTime + 1.25f;
        _releaseSent = false;
        return true;
    }

    private void ConfigureOwnedProxy(Item proxyItem, FrogTongue tongue)
    {
        proxyItem.blockInteraction = true;
        proxyItem.rig.useGravity = false;
        proxyItem.rig.detectCollisions = false;
        proxyItem.rig.linearVelocity = Vector3.zero;
        proxyItem.rig.angularVelocity = Vector3.zero;

        // Mob.Update exits while sleeping; FrogTongue's overrides still animate and pull.
        tongue.sleeping = true;
        tongue.dragForce = Config.PullForce.Value;
        tongue.liftDragForce = Config.LiftForce.Value;
        tongue.maxScoutHookTime = Config.MaxHookDuration.Value;
        tongue.stopPullFriendDistance = Config.StopDistance.Value;
    }

    private void FollowProxy()
    {
        if (_proxy == null || _proxyView == null || _proxyTongue == null)
            return;

        Vector3 mouth = _context.GetMouthPosition();
        Vector3 targetDirection = _proxyTongue.tongueEnd.position - mouth;
        Quaternion rotation = GetProxyRotation(targetDirection);
        MoveProxy(mouth, rotation);

        if (Time.time < _nextProxySyncTime)
            return;

        _nextProxySyncTime = Time.time + ProxySyncInterval;
        _proxyView.RPC(SetKinematicRpc, RpcTarget.Others, true, _proxy.transform.position, _proxy.transform.rotation);
    }

    private void MoveProxy(Vector3 mouth, Quaternion rotation)
    {
        if (_proxy == null || _proxyTongue == null)
            return;

        Transform proxyTransform = _proxy.transform;
        proxyTransform.SetPositionAndRotation(mouth, rotation);
        Vector3 dragPointOffset = _proxyTongue.dragPoint.position - proxyTransform.position;
        proxyTransform.position -= dragPointOffset;

        Rigidbody? rig = _proxy.GetComponent<Rigidbody>();
        if (rig == null)
            return;

        rig.position = proxyTransform.position;
        rig.rotation = proxyTransform.rotation;
        rig.linearVelocity = Vector3.zero;
        rig.angularVelocity = Vector3.zero;
    }

    private void ReleaseProxy()
    {
        if (_proxyView == null || _releaseSent)
            return;

        _releaseSent = true;
        _proxyView.RPC(
            FrogActionRpc,
            RpcTarget.All,
            _proxyView,
            FrogTongue.FrogActionType.LetGo,
            Vector3.zero);
        _cleanupAt = Time.time + ProxyCleanupDelay;
    }

    private void DestroyProxy()
    {
        GameObject? proxy = _proxy;
        _proxy = null;
        _proxyTongue = null;
        _proxyView = null;
        _cleanupAt = float.PositiveInfinity;
        _releaseSent = false;

        if (proxy == null)
            return;

        PhotonView? view = proxy.GetComponent<PhotonView>();
        if (PhotonNetwork.InRoom && view != null && view.IsMine)
            PhotonNetwork.Destroy(proxy);
        else
            Destroy(proxy);
    }

    private static Quaternion GetProxyRotation(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : Quaternion.identity;
    }

    private void CreateMissVisual()
    {
        _missLine = TongueVisual.CreateLine(
            transform,
            "FrogSkill Vanilla Miss Tongue",
            out _missLineMaterial);
    }

    private void StartMiss(Vector3 endpoint)
    {
        if (_missLine == null)
            return;

        _missEndpoint = endpoint;
        _missStartedAt = Time.time;
        _missActive = true;
        _missLine.enabled = true;
    }

    private void UpdateMissVisual()
    {
        if (!_missActive || _missLine == null)
            return;

        Vector3 mouth = _context.GetMouthPosition();
        if (!TongueVisual.UpdateMiss(_missLine, _missStartedAt, mouth, _missEndpoint))
            return;

        _missActive = false;
        _missLine.enabled = false;
    }

    private void OnDestroy()
    {
        DestroyProxy();
        if (_missLineMaterial != null)
            Destroy(_missLineMaterial);
    }
}
