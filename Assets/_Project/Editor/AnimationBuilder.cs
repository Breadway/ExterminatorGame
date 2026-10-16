#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Slices Art/Player/PlayerSheet.png into 32x32 sprites and builds the player's animation clips
/// and AnimatorController from the frame ranges documented in tools/make_player_art.py.
/// Safe to rerun; clips and the controller are updated in place. The original Player.controller and
/// Player.png are not touched.
/// </summary>
public static class AnimationBuilder
{
    private const string Sheet = "Assets/_Project/Art/Player/PlayerSheet.png";
    private const string ClipDir = "Assets/_Project/Art/Player/Animations";
    private const string ControllerPath = "Assets/_Project/Art/Player/PlayerAnimator.controller";
    private const int Cell = 32;
    private const int Cols = 8;

    private struct ClipSpec
    {
        public string name;
        public int[] frames;
        public float fps;
        public bool loop;
    }

    private static readonly ClipSpec[] Clips =
    {
        new ClipSpec { name = "Idle", frames = new[] { 0, 0, 1, 1, 2, 2, 3, 3 }, fps = 6f, loop = true },
        new ClipSpec { name = "Walk", frames = new[] { 8, 9, 10, 11, 12, 13 }, fps = 12f, loop = true },
        new ClipSpec { name = "Dash", frames = new[] { 4, 5, 5 }, fps = 18f, loop = false },
        new ClipSpec { name = "Hurt", frames = new[] { 6, 7, 7 }, fps = 14f, loop = false },
        new ClipSpec { name = "Death", frames = new[] { 16, 17, 18, 19, 20 }, fps = 9f, loop = false },
    };

    private const string FlameStrip = "Assets/_Project/Art/Environment/FlameStrip.png";
    private const string SmokePath = "Assets/_Project/Art/Environment/Smoke.png";
    private const string FirePrefab = "Assets/_Project/Data/StatusEffects/Fire.prefab";

    /// <summary>Slice the 4-frame flame strip and give the Fire status prefab an animated flame.</summary>
    public static void BuildFlame()
    {
        AssetDatabase.ImportAsset(FlameStrip, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(FlameStrip);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 16f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var rects = new List<SpriteRect>();
        for (int i = 0; i < 4; i++)
            rects.Add(new SpriteRect { name = $"Flame_{i}", rect = new Rect(i * 16, 0, 16, 20), alignment = SpriteAlignment.BottomCenter, pivot = new Vector2(0.5f, 0f), spriteID = GUID.Generate() });
        provider.SetSpriteRects(rects.ToArray());
        provider.Apply();
        importer.SaveAndReimport();

        var smokeImporter = (TextureImporter)AssetImporter.GetAtPath(SmokePath);
        smokeImporter.textureType = TextureImporterType.Sprite;
        smokeImporter.spritePixelsPerUnit = 16f;
        smokeImporter.filterMode = FilterMode.Point;
        smokeImporter.SaveAndReimport();

        var frames = new List<Sprite>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(FlameStrip))
            if (asset is Sprite s) frames.Add(s);
        frames.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        if (frames.Count == 0) { Debug.LogError("Flame frames missing."); return; }

        var root = PrefabUtility.LoadPrefabContents(FirePrefab);
        try
        {
            var sr = root.GetComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            sr.color = Color.white;
            sr.sortingOrder = 6;
            root.transform.localScale = Vector3.one * 1.4f;
            var book = root.GetComponent<SpriteFlipbook>();
            if (book == null) book = root.AddComponent<SpriteFlipbook>();
            var so = new SerializedObject(book);
            var arr = so.FindProperty("frames");
            arr.arraySize = frames.Count;
            for (int i = 0; i < frames.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, FirePrefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        Debug.Log("Flame built.");
    }

    [MenuItem("Exterminator/Build Animations")]
    public static void Build()
    {
        BuildFlame();
        AssetDatabase.ImportAsset(Sheet, ImportAssetOptions.ForceUpdate);
        SliceSheet();
        var sprites = LoadSprites();
        if (sprites.Count == 0)
        {
            Debug.LogError("PlayerSheet sprites not found after slicing.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(ClipDir)) AssetDatabase.CreateFolder("Assets/_Project/Art/Player", "Animations");
        var clips = new Dictionary<string, AnimationClip>();
        foreach (var spec in Clips) clips[spec.name] = BuildClip(spec, sprites);
        BuildController(clips);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Animations built: {clips.Count} clips.");
    }

    private static void SliceSheet()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Sheet);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        // Match the original Player.png (100 PPU, centre pivot) so the 7x scale and collider still fit.
        importer.spritePixelsPerUnit = 100f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Sheet);
        int rows = tex.height / Cell;
        var rects = new List<SpriteRect>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                int index = r * Cols + c;
                rects.Add(new SpriteRect
                {
                    name = $"Player_{index:00}",
                    rect = new Rect(c * Cell, tex.height - (r + 1) * Cell, Cell, Cell),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = GUID.Generate(),
                });
            }
        }
        provider.SetSpriteRects(rects.ToArray());
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static Dictionary<int, Sprite> LoadSprites()
    {
        var map = new Dictionary<int, Sprite>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(Sheet))
        {
            if (asset is Sprite s && s.name.StartsWith("Player_") && int.TryParse(s.name.Substring(7), out int index))
                map[index] = s;
        }
        return map;
    }

    private static AnimationClip BuildClip(ClipSpec spec, Dictionary<int, Sprite> sprites)
    {
        string path = $"{ClipDir}/{spec.name}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.frameRate = spec.fps;
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[spec.frames.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = i / spec.fps, value = sprites.ContainsKey(spec.frames[i]) ? sprites[spec.frames[i]] : null };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = spec.loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void BuildController(Dictionary<string, AnimationClip> clips)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null) AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        foreach (var f in new[] { "MoveX", "MoveY", "AimX", "AimY" }) controller.AddParameter(f, AnimatorControllerParameterType.Float);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dash", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;
        var idle = sm.AddState("Idle"); idle.motion = clips["Idle"];
        var walk = sm.AddState("Walk"); walk.motion = clips["Walk"];
        var dash = sm.AddState("Dash"); dash.motion = clips["Dash"];
        var hurt = sm.AddState("Hurt"); hurt.motion = clips["Hurt"];
        var death = sm.AddState("Death"); death.motion = clips["Death"];
        sm.defaultState = idle;

        Transition(idle, walk, "IsMoving", true);
        Transition(walk, idle, "IsMoving", false);
        foreach (var from in new[] { idle, walk })
        {
            TriggerTransition(from, dash, "Dash");
            TriggerTransition(from, hurt, "Hurt");
        }
        // Dash and hurt are short one-shots that return to idle when their clip ends
        foreach (var oneShot in new[] { dash, hurt })
        {
            var t = oneShot.AddTransition(idle);
            t.hasExitTime = true;
            t.exitTime = 1f;
            t.duration = 0f;
        }
        var any = sm.AddAnyStateTransition(death);
        any.AddCondition(AnimatorConditionMode.If, 0f, "Death");
        any.duration = 0f;
        any.canTransitionToSelf = false;
        EditorUtility.SetDirty(controller);
    }

    private static void Transition(AnimatorState from, AnimatorState to, string param, bool value)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0.05f;
        t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, param);
    }

    private static void TriggerTransition(AnimatorState from, AnimatorState to, string trigger)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0f;
        t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }
}
#endif
