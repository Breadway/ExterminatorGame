using UnityEngine;

/// <summary>
/// Debug bot for automated playtests: aims at the nearest enemy, fires, strafes around and picks
/// upgrades. Disabled by default and never added to a build scene; tests and the playtest tooling
/// switch it on with AutoPlayer.Enable(). It drives the same public APIs a human's input reaches.
/// </summary>
public class AutoPlayer : MonoBehaviour
{
    public static AutoPlayer Instance { get; private set; }

    public bool pickUpgrades = true;
    public float strafeSpeed = 0.6f;
    public float retreatBelowRange = 2.0f;

    private PlayerController player;
    private Transform firePoint;
    private PlayerAttack attack;
    private Rigidbody2D rb;
    private IWeaponAttack weapon;
    private float nextPick;
    private bool firing;

    public static AutoPlayer Enable()
    {
        var p = PlayerController.Instance;
        if (p == null) return null;
        var bot = p.GetComponent<AutoPlayer>() ?? p.gameObject.AddComponent<AutoPlayer>();
        bot.enabled = true;
        Instance = bot;
        return bot;
    }

    public static void Disable()
    {
        if (Instance != null) Instance.enabled = false;
    }

    void OnEnable()
    {
        player = GetComponent<PlayerController>();
        attack = GetComponent<PlayerAttack>();
        rb = GetComponent<Rigidbody2D>();
        firePoint = transform.Find("FirePoint");
        foreach (var c in GetComponentsInChildren<MonoBehaviour>())
            if (c is IWeaponAttack w && c.enabled) weapon = w;
    }

    void OnDisable()
    {
        if (firing) weapon?.StopAttack();
        firing = false;
    }

    // PlayerAiming rewrites the fire point from the mouse in its own Update, so the bot aims in
    // LateUpdate, after it, and disables mouse aiming while it is in control.
    private PlayerAiming mouseAiming;
    private Vector2 aimDirection = Vector2.up;
    private bool hasAim;

    void LateUpdate()
    {
        if (hasAim && firePoint != null)
            firePoint.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg - 90f);
    }

    public bool allowDash = true;

    void Update()
    {
        if (Time.timeScale <= 0f)
        {
            TryPickUpgrade();
            return;
        }
        if (firePoint == null || weapon == null || player == null) return;

        Enemy target = Nearest(out float distance);
        if (target == null)
        {
            if (firing) { weapon.StopAttack(); firing = false; }
            return;
        }

        Vector2 to = (Vector2)target.transform.position - (Vector2)firePoint.position;
        aimDirection = to.normalized;
        hasAim = true;

        if (weapon is IHoldWeapon hold)
        {
            // Charge to full on a long cadence, then release
            if (!firing) { hold.BeginHold(); firing = true; nextPick = Time.time + 1.7f; }
            else if (Time.time >= nextPick) { hold.ReleaseHold(); firing = false; }
        }
        else if (!firing)
        {
            weapon.Attack();
            firing = true;
        }
        else if (!(weapon is FlamethrowerAttack))
        {
            weapon.Attack(); // interval weapons ignore extra calls until ready
        }

        // Kite: back away from anything close, otherwise circle (dash disabled in unit probes so it
        // never changes the player's invulnerability state; real play still dashes)
        if (!allowDash && rb != null && !player.IsStunned)
        {
            Vector2 dir = to.normalized;
            Vector2 move = distance < retreatBelowRange ? -dir : new Vector2(-dir.y, dir.x) * strafeSpeed;
            rb.linearVelocity = move * 6f;
            return;
        }
        // Kite: back away from anything close, otherwise circle
        if (rb != null && !player.IsStunned)
        {
            Vector2 dir = to.normalized;
            Vector2 move = distance < retreatBelowRange ? -dir : new Vector2(-dir.y, dir.x) * strafeSpeed;
            rb.linearVelocity = move * 6f;
        }
    }

    private Enemy Nearest(out float best)
    {
        best = float.MaxValue;
        Enemy result = null;
        foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (e.Health == null || !e.Health.IsAlive) continue;
            float d = Vector2.Distance(e.transform.position, transform.position);
            if (d < best) { best = d; result = e; }
        }
        return result;
    }

    private void TryPickUpgrade()
    {
        if (!pickUpgrades || Time.unscaledTime < nextPick) return;
        var um = UpgradeManager.Instance;
        if (um == null || !um.IsChoosing) return;
        nextPick = Time.unscaledTime + 0.2f;
        um.ChooseUpgrade(Random.Range(0, um.CurrentChoices.Count));
    }
}
