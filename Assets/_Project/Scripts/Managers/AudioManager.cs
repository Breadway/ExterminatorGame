using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays every sound in the game. It owns a pool of voices, two crossfading music sources and a
/// set of held loops (flame, foam, beam charge). Gameplay code never references it: it raises
/// GameEvents (or GameEvents.PlaySound) and this listens. Clips come from Resources/Audio, built by
/// tools/make_audio.py. Missing clips are skipped quietly so a bare scene still runs.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Volumes (0-1)")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.6f;

    [Header("Voices")]
    [SerializeField] private int voiceCount = 20;
    [SerializeField] private float musicFadeSeconds = 1.4f;
    [SerializeField] private float duckedMusicFraction = 0.35f;

    // Base loudness and pitch spread per sound. Anything not listed plays at 1.0 with a little jitter.
    private struct Tuning { public float volume, pitchJitter, cooldown; }
    private static readonly Dictionary<string, Tuning> tuning = new Dictionary<string, Tuning>
    {
        { "hit_0", new Tuning { volume = 0.45f, pitchJitter = 0.12f, cooldown = 0.05f } },
        { "hit_1", new Tuning { volume = 0.45f, pitchJitter = 0.12f, cooldown = 0.05f } },
        { "hit_2", new Tuning { volume = 0.45f, pitchJitter = 0.12f, cooldown = 0.05f } },
        { "die_0", new Tuning { volume = 0.7f, pitchJitter = 0.15f, cooldown = 0.04f } },
        { "die_1", new Tuning { volume = 0.7f, pitchJitter = 0.15f, cooldown = 0.04f } },
        { "die_2", new Tuning { volume = 0.7f, pitchJitter = 0.15f, cooldown = 0.04f } },
        { "step_0", new Tuning { volume = 0.5f, pitchJitter = 0.1f, cooldown = 0.08f } },
        { "step_1", new Tuning { volume = 0.5f, pitchJitter = 0.1f, cooldown = 0.08f } },
        { "scrap", new Tuning { volume = 0.35f, pitchJitter = 0.08f, cooldown = 0.06f } },
        { "zap", new Tuning { volume = 0.6f, pitchJitter = 0.1f, cooldown = 0.05f } },
        { "spit", new Tuning { volume = 0.4f, pitchJitter = 0.1f, cooldown = 0.08f } },
        { "spit_hit", new Tuning { volume = 0.4f, pitchJitter = 0.1f, cooldown = 0.06f } },
        { "spawn", new Tuning { volume = 0.25f, pitchJitter = 0.2f, cooldown = 0.07f } },
        { "mop_hit", new Tuning { volume = 0.8f, pitchJitter = 0.08f, cooldown = 0.05f } },
        { "player_hurt", new Tuning { volume = 0.9f, pitchJitter = 0.05f, cooldown = 0.1f } },
        { "ui_hover", new Tuning { volume = 0.4f, pitchJitter = 0.03f, cooldown = 0.03f } },
        { "boss_roar", new Tuning { volume = 1f, pitchJitter = 0f, cooldown = 1f } },
        { "explosion", new Tuning { volume = 0.9f, pitchJitter = 0.05f, cooldown = 0.08f } },
    };

    // Which loop clip plays while a weapon is held, and whether it fades when released
    private static readonly Dictionary<string, string> weaponLoops = new Dictionary<string, string>
    {
        { "flamethrower", "flame_loop" },
        { "foam", "foam_loop" },
        { "beam", "beam_charge" },
    };

    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
    private readonly Dictionary<string, AudioSource> loops = new Dictionary<string, AudioSource>();
    private AudioSource[] voices;
    private int nextVoice;
    private AudioSource musicA, musicB, musicLive;
    private string currentTrack = "";
    private float musicTarget = 1f;
    private float duck = 1f;
    private bool musicOneShot;

    public float MasterVolume { get => masterVolume; set { masterVolume = Mathf.Clamp01(value); Persist(); } }
    public float SfxVolume { get => sfxVolume; set { sfxVolume = Mathf.Clamp01(value); Persist(); } }
    public float MusicVolume { get => musicVolume; set { musicVolume = Mathf.Clamp01(value); Persist(); } }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        var data = SaveSystem.Data;
        masterVolume = data.masterVolume;
        sfxVolume = data.sfxVolume;
        musicVolume = data.musicVolume;

        foreach (var clip in Resources.LoadAll<AudioClip>("Audio")) clips[clip.name] = clip;

        voices = new AudioSource[Mathf.Max(4, voiceCount)];
        for (int i = 0; i < voices.Length; i++) voices[i] = NewSource("Voice " + i, false);
        musicA = NewSource("Music A", true);
        musicB = NewSource("Music B", true);
        musicLive = musicA;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private AudioSource NewSource(string name, bool loop)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0f;
        s.ignoreListenerPause = true;
        return s;
    }

    private void Persist()
    {
        var data = SaveSystem.Data;
        data.masterVolume = masterVolume;
        data.sfxVolume = sfxVolume;
        data.musicVolume = musicVolume;
    }

    // ========================================
    // EVENTS
    // ========================================

    void OnEnable()
    {
        GameEvents.OnPlaySound += Play;
        GameEvents.OnPlayMusic += SetMusic;
        GameEvents.OnEnemyHit += HandleEnemyHit;
        GameEvents.OnEnemyKilled += HandleEnemyKilled;
        GameEvents.OnPlayerDamaged += HandlePlayerDamaged;
        GameEvents.OnPlayerDied += HandlePlayerDied;
        GameEvents.OnLevelUp += HandleLevelUp;
        GameEvents.OnPlayerUpgradeApplied += HandleUpgrade;
        GameEvents.OnRoomStarted += HandleRoomStarted;
        GameEvents.OnRoomCleared += HandleRoomCleared;
        GameEvents.OnBossSpawned += HandleBossSpawned;
        GameEvents.OnPlayerDash += HandleDash;
        GameEvents.OnPlayerHealed += HandleHealed;
        GameEvents.OnRunEnded += HandleRunEnded;
        GameEvents.OnRunStarted += HandleRunStarted;
        GameEvents.OnGameStateChanged += HandleStateChanged;
        GameEvents.OnWeaponHeld += HandleWeaponHeld;
        GameEvents.OnWeaponFired += HandleWeaponFired;
        GameEvents.OnScrapChanged += HandleScrap;
    }

    void OnDisable()
    {
        GameEvents.OnPlaySound -= Play;
        GameEvents.OnPlayMusic -= SetMusic;
        GameEvents.OnEnemyHit -= HandleEnemyHit;
        GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        GameEvents.OnPlayerDamaged -= HandlePlayerDamaged;
        GameEvents.OnPlayerDied -= HandlePlayerDied;
        GameEvents.OnLevelUp -= HandleLevelUp;
        GameEvents.OnPlayerUpgradeApplied -= HandleUpgrade;
        GameEvents.OnRoomStarted -= HandleRoomStarted;
        GameEvents.OnRoomCleared -= HandleRoomCleared;
        GameEvents.OnBossSpawned -= HandleBossSpawned;
        GameEvents.OnPlayerDash -= HandleDash;
        GameEvents.OnPlayerHealed -= HandleHealed;
        GameEvents.OnRunEnded -= HandleRunEnded;
        GameEvents.OnRunStarted -= HandleRunStarted;
        GameEvents.OnGameStateChanged -= HandleStateChanged;
        GameEvents.OnWeaponHeld -= HandleWeaponHeld;
        GameEvents.OnWeaponFired -= HandleWeaponFired;
        GameEvents.OnScrapChanged -= HandleScrap;
    }

    void Start()
    {
        // The lobby is shown at start, so its music plays until the first room begins
        SetMusic("music_lobby");
    }

    private void HandleEnemyHit(Enemy enemy, float amount)
    {
        Play("hit_" + Random.Range(0, 3), enemy != null ? (Vector2)enemy.transform.position : default, Mathf.Clamp(0.5f + amount * 0.02f, 0.5f, 1f));
    }

    private void HandleEnemyKilled(Enemy enemy)
    {
        Vector2 pos = enemy != null ? (Vector2)enemy.transform.position : default;
        if (enemy != null && enemy.IsBoss) { Play("explosion", pos); return; }
        Play("die_" + Random.Range(0, 3), pos);
    }

    private void HandlePlayerDamaged(float amount, float hp, float max) { Play("player_hurt"); }
    private void HandlePlayerDied() { Play("player_death"); StopAllLoops(); }
    private void HandleLevelUp(int level) { Play("level_up"); }
    private void HandleUpgrade(UpgradeData upgrade) { Play("upgrade_pick"); }
    private void HandleBossSpawned(Enemy boss) { Play("boss_roar"); }
    private void HandleDash(Vector2 dir) { Play("dash"); }
    private void HandleHealed(float amount) { Play("heal"); }
    private void HandleRoomCleared(int index) { Play("room_clear"); }
    private void HandleScrap(int total, int delta) { Play("scrap", default, 1f); }

    private void HandleRoomStarted(int index, RoomDefinition room)
    {
        Play("room_start");
        if (room == null) return;
        if (room.type == RoomType.Boss) { Play("boss_warning"); SetMusic("music_boss"); }
        else if (room.type == RoomType.Shop) SetMusic("music_lobby");
        else SetMusic("music_combat");
    }

    private void HandleRunStarted() { SetMusic("music_combat"); }

    private void HandleRunEnded(bool victory)
    {
        StopAllLoops();
        SetMusic(victory ? "music_victory" : "music_gameover", false);
    }

    private void HandleStateChanged(GameState state)
    {
        // Pull the music down behind menus so the cards and shop are easy to hear and read
        duck = (state == GameState.UpgradeChoice || state == GameState.Shop || state == GameState.Paused) ? duckedMusicFraction : 1f;
        if (state != GameState.Playing) PauseLoops();
    }

    private void HandleWeaponHeld(string weaponId, bool held)
    {
        if (!weaponLoops.TryGetValue(weaponId, out string clip)) return;
        if (held) StartLoop(clip, weaponId == "beam" ? 0.7f : 0.55f, weaponId == "beam");
        else StopLoop(clip);
    }

    private void HandleWeaponFired(string weaponId, Vector2 origin, Vector2 dir, float power)
    {
        switch (weaponId)
        {
            case "mop": Play("mop_swing", origin); break;
            case "cables": Play("zap", origin); break;
            case "beam": Play(power >= 0.99f ? "beam_fire" : "beam_tap", origin); break;
        }
    }

    // ========================================
    // ONE-SHOTS
    // ========================================

    /// <summary>One-shot with no position (UI, player sounds).</summary>
    public void Play(string name) { Play(name, default, 1f); }

    public void Play(string name, Vector2 position) { Play(name, position, 1f); }

    public void Play(string name, Vector2 position, float gain)
    {
        if (!clips.TryGetValue(name, out var clip)) return;
        tuning.TryGetValue(name, out var t);
        if (t.volume <= 0f) t.volume = 1f;

        if (t.cooldown > 0f)
        {
            if (lastPlayed.TryGetValue(name, out float last) && Time.unscaledTime - last < t.cooldown) return;
            lastPlayed[name] = Time.unscaledTime;
        }

        var source = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Length;
        source.Stop();
        source.clip = clip;
        source.volume = Mathf.Clamp01(masterVolume * sfxVolume * t.volume * gain);
        source.pitch = 1f + Random.Range(-t.pitchJitter, t.pitchJitter);
        source.panStereo = PanFor(position);
        source.Play();
    }

    private float PanFor(Vector2 position)
    {
        if (position == default || Camera.main == null) return 0f;
        return Mathf.Clamp((position.x - Camera.main.transform.position.x) / 14f, -0.6f, 0.6f);
    }

    // ========================================
    // LOOPS
    // ========================================

    private void StartLoop(string name, float volume, bool pitchRises)
    {
        if (!clips.TryGetValue(name, out var clip)) return;
        if (!loops.TryGetValue(name, out var source))
        {
            source = NewSource("Loop " + name, !pitchRises);
            loops[name] = source;
        }
        source.clip = clip;
        source.volume = 0f;
        source.pitch = 1f;
        source.loop = !pitchRises;
        loopTargets[name] = volume;
        loopRise[name] = pitchRises;
        if (!source.isPlaying || pitchRises) { source.time = 0f; source.Play(); }
    }

    private readonly Dictionary<string, float> loopTargets = new Dictionary<string, float>();
    private readonly Dictionary<string, bool> loopRise = new Dictionary<string, bool>();

    private void StopLoop(string name) { loopTargets[name] = 0f; }
    private void StopAllLoops() { foreach (var key in new List<string>(loopTargets.Keys)) loopTargets[key] = 0f; }

    private void PauseLoops()
    {
        // Held weapons release when a menu opens
        StopAllLoops();
    }

    // ========================================
    // MUSIC
    // ========================================

    public void SetMusic(string name) { SetMusic(name, true); }

    public void SetMusic(string name, bool loop)
    {
        if (name == currentTrack) return;
        if (!clips.TryGetValue(name, out var clip)) return;
        currentTrack = name;
        musicOneShot = !loop;
        var incoming = musicLive == musicA ? musicB : musicA;
        incoming.clip = clip;
        incoming.loop = loop;
        incoming.volume = 0f;
        incoming.time = 0f;
        incoming.Play();
        musicLive = incoming;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Music crossfade and ducking
        float musicGain = masterVolume * musicVolume * duck;
        float step = dt / Mathf.Max(0.05f, musicFadeSeconds);
        foreach (var source in new[] { musicA, musicB })
        {
            bool live = source == musicLive;
            float target = live ? musicGain : 0f;
            source.volume = Mathf.MoveTowards(source.volume, target, step * Mathf.Max(0.2f, masterVolume * musicVolume));
            if (!live && source.volume <= 0.001f && source.isPlaying) source.Stop();
        }

        // Loops fade in and out; a charge sound rises in pitch while held
        foreach (var pair in new List<KeyValuePair<string, float>>(loopTargets))
        {
            if (!loops.TryGetValue(pair.Key, out var s)) continue;
            float target = pair.Value * masterVolume * sfxVolume;
            float speed = target > s.volume ? 10f : 6f;
            s.volume = Mathf.MoveTowards(s.volume, target, speed * dt);
            if (loopRise.TryGetValue(pair.Key, out bool rises) && rises && pair.Value > 0f)
                s.pitch = Mathf.Min(1.6f, s.pitch + dt * 0.4f);
            if (pair.Value <= 0f && s.volume <= 0.001f && s.isPlaying) { s.Stop(); s.pitch = 1f; }
        }
    }
}
