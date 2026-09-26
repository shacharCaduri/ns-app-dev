using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class ArenaSurface : MonoBehaviour
{
    public bool walkable = true;
    public bool jumpThrough = true;
    [Range(0.1f, 3f)] public float speedMultiplier = 1f;
    [Min(0f)] public float jumpMultiplier = 1f;
    public static readonly List<ArenaSurface> Active = new List<ArenaSurface>();
    public Bounds Bounds => GetComponent<BoxCollider2D>().bounds;
    private void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
    private void OnDisable() { Active.Remove(this); }

    public static ArenaSurface Support(Vector3 feet)
    {
        foreach (ArenaSurface surface in Active)
        {
            if (!surface.walkable) continue;
            Bounds b = surface.Bounds;
            if (feet.x >= b.min.x - 0.15f && feet.x <= b.max.x + 0.15f && Mathf.Abs(feet.y - b.max.y) < 0.04f)
                return surface;
        }
        return null;
    }

    // Swept feet prevent landing through a platform even at low frame rates.
    public static Vector3 Move(Vector3 from, Vector3 to, ref float verticalSpeed, bool canStep)
    {
        const float halfWidth = 0.22f;
        const float bodyHeight = 1.45f;
        foreach (ArenaSurface surface in Active)
        {
            if (!surface.walkable || surface.jumpThrough) continue;
            Bounds b = surface.Bounds;
            if (to.y + bodyHeight <= b.min.y || from.y >= b.max.y - 0.02f) continue;
            if (canStep && b.max.y - from.y <= 0.45f && b.max.y >= from.y && to.x >= b.min.x && to.x <= b.max.x)
            {
                to.y = Mathf.Max(to.y, b.max.y);
                verticalSpeed = 0f;
                continue;
            }
            if (from.x + halfWidth <= b.min.x && to.x + halfWidth > b.min.x) to.x = b.min.x - halfWidth;
            if (from.x - halfWidth >= b.max.x && to.x - halfWidth < b.max.x) to.x = b.max.x + halfWidth;
            if (verticalSpeed > 0f && from.y + bodyHeight <= b.min.y && to.y + bodyHeight >= b.min.y && to.x > b.min.x && to.x < b.max.x)
            {
                to.y = b.min.y - bodyHeight;
                verticalSpeed = 0f;
            }
        }
        float landing = float.NegativeInfinity;
        if (verticalSpeed <= 0f)
        {
            foreach (ArenaSurface surface in Active)
            {
                if (!surface.walkable) continue;
                Bounds b = surface.Bounds;
                if (to.x < b.min.x - 0.15f || to.x > b.max.x + 0.15f) continue;
                if (from.y >= b.max.y - 0.04f && to.y <= b.max.y)
                    landing = Mathf.Max(landing, b.max.y);
            }
        }
        if (!float.IsNegativeInfinity(landing)) { to.y = landing; verticalSpeed = 0f; }
        return to;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = jumpThrough ? Color.cyan : Color.green;
        Gizmos.DrawWireCube(Bounds.center, Bounds.size);
    }
}
