using UnityEngine;

namespace FrogSkill;

internal static class TongueVisual
{
    private const float MissExtendDuration = 0.12f;
    private const float MissRetractDuration = 0.12f;
    private static readonly Color Color = new(0.85f, 0.18f, 0.28f, 1f);

    internal static LineRenderer CreateLine(
        Transform parent,
        string objectName,
        out Material? material)
    {
        GameObject visual = new(objectName);
        visual.transform.SetParent(parent, false);
        LineRenderer line = visual.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = 0.075f;
        line.endWidth = 0.035f;
        line.numCapVertices = 4;

        Shader shader = Shader.Find("Sprites/Default");
        material = shader == null ? null : new Material(shader);
        if (material != null)
            line.material = material;

        line.startColor = Color;
        line.endColor = Color;
        line.enabled = false;
        return line;
    }

    internal static bool UpdateMiss(
        LineRenderer line,
        float startedAt,
        Vector3 origin,
        Vector3 endpoint)
    {
        float elapsed = Time.time - startedAt;
        float extension = Mathf.Clamp01(elapsed / MissExtendDuration);
        float retraction = Mathf.Clamp01((elapsed - MissExtendDuration) / MissRetractDuration);
        line.SetPosition(0, origin);
        line.SetPosition(1, Vector3.Lerp(origin, endpoint, extension * (1f - retraction)));
        return retraction >= 1f;
    }
}
