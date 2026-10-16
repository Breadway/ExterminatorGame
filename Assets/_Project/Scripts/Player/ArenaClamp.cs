using UnityEngine;

/// <summary>
/// Keeps a physics body inside the arena rectangle. Walls are the normal barrier, but a crowd of
/// kinematic enemies pressing a dynamic player into a static wall can squeeze it through, so this
/// is the guarantee. It corrects the body before every physics step, so any overshoot is at most
/// one step of travel.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ArenaClamp : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D body;
    private Rect bounds;
    private bool active;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<Collider2D>();
    }

    public void SetBounds(Rect arena)
    {
        bounds = arena;
        active = true;
    }

    public void Clear()
    {
        active = false;
    }

    void FixedUpdate()
    {
        if (!active || rb == null) return;

        Vector2 extents = body != null ? (Vector2)body.bounds.extents : Vector2.one * 0.4f;
        Vector2 p = rb.position;
        Vector2 clamped = new Vector2(
            Mathf.Clamp(p.x, bounds.xMin + extents.x, bounds.xMax - extents.x),
            Mathf.Clamp(p.y, bounds.yMin + extents.y, bounds.yMax - extents.y));
        if ((clamped - p).sqrMagnitude < 1e-6f) return;

        rb.position = clamped;
        // Remove the velocity component that was driving the body out of bounds
        Vector2 v = rb.linearVelocity;
        if (!Mathf.Approximately(clamped.x, p.x)) v.x = 0f;
        if (!Mathf.Approximately(clamped.y, p.y)) v.y = 0f;
        rb.linearVelocity = v;
    }
}
