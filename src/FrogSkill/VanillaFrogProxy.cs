using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace FrogSkill;

internal static class VanillaFrogProxy
{
    private const int Marker = 0x46524F47;
    private static readonly HashSet<int> PendingDestroy = new();

    internal static object[] InstantiationData { get; } = { Marker };

    internal static bool IsProxy(PhotonView? view)
    {
        object[]? data = view?.InstantiationData;
        return data != null && data.Length == 1 && data[0] is int marker && marker == Marker;
    }

    internal static void DestroyAfterRelease(PhotonView? view)
    {
        if (view == null || !view.IsMine || !IsProxy(view) ||
            !PendingDestroy.Add(view.ViewID) || Plugin.Instance == null)
            return;

        Plugin.Instance.StartCoroutine(DestroyNextFrame(view, view.ViewID));
    }

    private static IEnumerator DestroyNextFrame(PhotonView view, int viewId)
    {
        yield return null;
        PendingDestroy.Remove(viewId);

        if (view == null || !view.IsMine || !IsProxy(view))
            yield break;

        if (PhotonNetwork.InRoom)
            PhotonNetwork.Destroy(view.gameObject);
        else
            Object.Destroy(view.gameObject);
    }
}
