using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using UnityEngine.Video;

public static class BuildWizardGame
{
    private const string AssetsFolder = "Assets/wizard-assets";
    private const string ScenesFolder = "Assets/Scenes";

    private const string TtfPath = AssetsFolder + "/OvercameDemoRegular.ttf";
    private const string FontAssetPath = AssetsFolder + "/OvercameDemoRegular SDF.asset";
    private const string RedRtPath = AssetsFolder + "/Red Texture.renderTexture";
    private const string BlueRtPath = AssetsFolder + "/Blue Texture.renderTexture";

    private const string RedStancePath = AssetsFolder + "/red_wizard_stance.mp4";
    private const string RedAttackPath = AssetsFolder + "/red_wizard_attack.mp4";
    private const string BlueStancePath = AssetsFolder + "/blue_wizard_stance.mp4";
    private const string BlueAttackPath = AssetsFolder + "/blue_wizard_attack.mp4";

    private const string Scene1Path = ScenesFolder + "/Scene 1.unity";
    private const string Scene2Path = ScenesFolder + "/Scene 2.unity";
    private const string Scene3Path = ScenesFolder + "/Scene 3.unity";
    private const string Scene4Path = ScenesFolder + "/Scene 4.unity";

    [MenuItem("Tools/Build Wizard Game")]
    public static void BuildAll()
    {
        EnsureFolder("Assets", "Scenes");
        EnsureTMPEssentialResources();

        TMP_FontAsset fontAsset = CreateOrReplaceFontAsset();
        RenderTexture redRT = CreateOrReplaceRenderTexture(RedRtPath, "Red Texture");
        RenderTexture blueRT = CreateOrReplaceRenderTexture(BlueRtPath, "Blue Texture");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Reload persisted assets so scene references point to disk-backed objects
        fontAsset = LoadRequired<TMP_FontAsset>(FontAssetPath);
        redRT = LoadRequired<RenderTexture>(RedRtPath);
        blueRT = LoadRequired<RenderTexture>(BlueRtPath);

        BuildScene1(fontAsset);
        BuildScene2(fontAsset);
        BuildScene3(fontAsset, redRT, blueRT);
        BuildScene4(fontAsset);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(Scene1Path, true),
            new EditorBuildSettingsScene(Scene2Path, true),
            new EditorBuildSettingsScene(Scene3Path, true),
            new EditorBuildSettingsScene(Scene4Path, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(Scene1Path, OpenSceneMode.Single);

        Debug.Log("[BuildWizardGame] Successfully built font asset, 2 RenderTextures, Scenes 1-4, and Build Settings (0..3). Opened Scene 1.");
    }

    private static void EnsureTMPEssentialResources()
    {
        bool hasShaders = Shader.Find("TextMeshPro/Distance Field") != null
                          || Shader.Find("TextMeshPro/Mobile/Distance Field") != null;
        bool hasSettings = AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset") != null;

        if (hasShaders && hasSettings)
        {
            return;
        }

        string[] candidatePaths =
        {
            Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage"),
            Path.GetFullPath("Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage")
        };

        foreach (string pkgPath in candidatePaths)
        {
            if (File.Exists(pkgPath))
            {
                AssetDatabase.ImportPackage(pkgPath, false);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("[BuildWizardGame] Imported TMP Essential Resources from: " + pkgPath);
                return;
            }
        }

        Debug.LogWarning("[BuildWizardGame] TMP Essential Resources package not found at standard package paths; proceeding with existing shaders.");
    }

    private static TMP_FontAsset CreateOrReplaceFontAsset()
    {
        Font sourceFont = LoadRequired<Font>(TtfPath);

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) != null)
        {
            AssetDatabase.DeleteAsset(FontAssetPath);
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024
        );

        if (fontAsset == null)
        {
            throw new InvalidOperationException("TMP_FontAsset.CreateFontAsset returned null for " + TtfPath);
        }

        fontAsset.name = "OvercameDemoRegular SDF";

        // Ensure shader & material are valid even if TMP Essential Resources are not imported
        Shader sdfShader = Shader.Find("TextMeshPro/Distance Field")
                           ?? Shader.Find("TextMeshPro/Mobile/Distance Field");

        if (fontAsset.material == null)
        {
            if (sdfShader == null)
            {
                throw new InvalidOperationException("Could not find a TextMeshPro Distance Field shader.");
            }
            fontAsset.material = new Material(sdfShader);
        }
        else if (fontAsset.material.shader == null && sdfShader != null)
        {
            fontAsset.material.shader = sdfShader;
        }

        if (fontAsset.atlasTexture != null)
        {
            fontAsset.material.SetTexture(ShaderUtilities.ID_MainTex, fontAsset.atlasTexture);
        }

        // Pre-populate ASCII glyphs so text renders immediately in Edit & Play mode
        fontAsset.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .!:-_/\n");

        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

        if (fontAsset.atlasTextures != null)
        {
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                Texture2D tex = fontAsset.atlasTextures[i];
                if (tex != null && !AssetDatabase.Contains(tex))
                {
                    tex.name = "OvercameDemoRegular SDF Atlas " + i;
                    AssetDatabase.AddObjectToAsset(tex, fontAsset);
                }
            }
        }

        if (fontAsset.atlasTexture != null && !AssetDatabase.Contains(fontAsset.atlasTexture))
        {
            fontAsset.atlasTexture.name = "OvercameDemoRegular SDF Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        }

        if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material))
        {
            fontAsset.material.name = "OvercameDemoRegular SDF Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        EditorUtility.SetDirty(fontAsset);
        return fontAsset;
    }

    private static RenderTexture CreateOrReplaceRenderTexture(string path, string rtName)
    {
        if (AssetDatabase.LoadAssetAtPath<RenderTexture>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        RenderTexture rt = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32)
        {
            name = rtName
        };

        AssetDatabase.CreateAsset(rt, path);
        EditorUtility.SetDirty(rt);
        return rt;
    }

    private static void BuildScene1(TMP_FontAsset fontAsset)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateMainCamera(Color.black);
        Transform canvasTransform = CreateCanvas();
        CreateEventSystem();

        GameObject scriptGo = new GameObject("ScriptGO");
        Scene1 script = scriptGo.AddComponent<Scene1>();

        CreateTMPText(
            canvasTransform,
            "Title",
            "A SAMPLE GAME",
            fontAsset,
            64f,
            Color.white,
            new Vector2(0f, 80f),
            new Vector2(960f, 120f),
            TextAlignmentOptions.Center
        );

        Button startBtn = CreateButton(
            canvasTransform,
            "Button",
            "START",
            fontAsset,
            26f,
            HexColor("#F2DF1D"),
            HexColor("#1A1A1A"),
            new Vector2(0f, -40f),
            new Vector2(180f, 44f)
        );
        UnityEventTools.AddPersistentListener(startBtn.onClick, script.NextScene);
        EditorUtility.SetDirty(startBtn);
        EditorUtility.SetDirty(script);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, Scene1Path);
    }

    private static void BuildScene2(TMP_FontAsset fontAsset)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateMainCamera(HexColor("#314D79"));
        Transform canvasTransform = CreateCanvas();
        CreateEventSystem();

        GameObject scriptGo = new GameObject("ScriptGO");
        Scene2 script = scriptGo.AddComponent<Scene2>();

        TMP_InputField redInput = CreateTMPInputField(
            canvasTransform,
            "Red",
            "ENTER NAME...",
            LoadDefaultTMPFont(fontAsset),
            new Vector2(-340f, 30f),
            new Vector2(240f, 44f)
        );

        TMP_InputField blueInput = CreateTMPInputField(
            canvasTransform,
            "Blue",
            "ENTER NAME...",
            LoadDefaultTMPFont(fontAsset),
            new Vector2(340f, 30f),
            new Vector2(240f, 44f)
        );

        Button playBtn = CreateButton(
            canvasTransform,
            "Button",
            "PLAY",
            fontAsset,
            24f,
            HexColor("#19C819"),
            Color.white,
            new Vector2(0f, -50f),
            new Vector2(180f, 48f)
        );
        UnityEventTools.AddPersistentListener(playBtn.onClick, script.NextScene);

        script.redInputName = redInput;
        script.blueInputName = blueInput;

        EditorUtility.SetDirty(playBtn);
        EditorUtility.SetDirty(script);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, Scene2Path);
    }

    private static void BuildScene3(TMP_FontAsset fontAsset, RenderTexture redRT, RenderTexture blueRT)
    {
        VideoClip redStance = LoadRequired<VideoClip>(RedStancePath);
        VideoClip redAttack = LoadRequired<VideoClip>(RedAttackPath);
        VideoClip blueStance = LoadRequired<VideoClip>(BlueStancePath);
        VideoClip blueAttack = LoadRequired<VideoClip>(BlueAttackPath);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateMainCamera(HexColor("#314D79"));
        Transform canvasTransform = CreateCanvas();
        CreateEventSystem();

        GameObject scriptGo = new GameObject("ScriptGO");
        Scene3 script = scriptGo.AddComponent<Scene3>();

        VideoPlayer redVP = CreateVideoPlayer("Red VP", redStance, redRT);
        VideoPlayer blueVP = CreateVideoPlayer("Blue VP", blueStance, blueRT);

        // Top HUD
        TMP_Text redNameTxt = CreateTMPText(
            canvasTransform,
            "Red",
            "RED",
            fontAsset,
            42f,
            Color.white,
            new Vector2(-460f, 300f),
            new Vector2(260f, 64f),
            TextAlignmentOptions.Center
        );

        TMP_FontAsset hpFont = LoadDefaultTMPFont(fontAsset);

        TMP_Text redHpTxt = CreateTMPText(
            canvasTransform,
            "Red HP",
            "50",
            hpFont,
            44f,
            Color.white,
            new Vector2(-220f, 300f),
            new Vector2(140f, 64f),
            TextAlignmentOptions.Center
        );

        TMP_Text blueHpTxt = CreateTMPText(
            canvasTransform,
            "Blue HP",
            "50",
            hpFont,
            44f,
            Color.white,
            new Vector2(220f, 300f),
            new Vector2(140f, 64f),
            TextAlignmentOptions.Center
        );

        TMP_Text blueNameTxt = CreateTMPText(
            canvasTransform,
            "BBlue",
            "BLUE",
            fontAsset,
            42f,
            Color.white,
            new Vector2(460f, 300f),
            new Vector2(260f, 64f),
            TextAlignmentOptions.Center
        );

        // Center Wizard RawImages
        CreateRawImage(canvasTransform, "Red Raw", redRT, new Vector2(-320f, 0f), new Vector2(420f, 340f));
        CreateRawImage(canvasTransform, "Blue Raw", blueRT, new Vector2(320f, 0f), new Vector2(420f, 340f));

        // Bottom Attack Buttons
        Button redAttackBtn = CreateButton(
            canvasTransform,
            "Red Attack",
            "ATTACK",
            fontAsset,
            22f,
            HexColor("#E0665E"),
            Color.white,
            new Vector2(-320f, -220f),
            new Vector2(185f, 48f)
        );
        UnityEventTools.AddPersistentListener(redAttackBtn.onClick, script.AttackRed);

        Button blueAttackBtn = CreateButton(
            canvasTransform,
            "Blue Attack",
            "ATTACK",
            fontAsset,
            22f,
            HexColor("#5B9BD5"),
            Color.white,
            new Vector2(320f, -220f),
            new Vector2(185f, 48f)
        );
        UnityEventTools.AddPersistentListener(blueAttackBtn.onClick, script.AttackBlue);

        // Wire all Scene3 serialized fields
        script.blueAttack = blueAttack;
        script.redAttack = redAttack;
        script.blueStance = blueStance;
        script.redStance = redStance;
        script.blueVidPlayer = blueVP;
        script.redVidPlayer = redVP;
        script.redGameName = redNameTxt;
        script.blueGameName = blueNameTxt;
        script.blueHPText = blueHpTxt;
        script.redHPText = redHpTxt;
        script.blueHP = 50;
        script.redHP = 50;

        EditorUtility.SetDirty(redAttackBtn);
        EditorUtility.SetDirty(blueAttackBtn);
        EditorUtility.SetDirty(script);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, Scene3Path);
    }

    private static void BuildScene4(TMP_FontAsset fontAsset)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateMainCamera(HexColor("#314D79"));
        Transform canvasTransform = CreateCanvas();
        CreateEventSystem();

        CreateTMPText(
            canvasTransform,
            "Game Over",
            "GAME\nOVER",
            fontAsset,
            72f,
            Color.white,
            Vector2.zero,
            new Vector2(700f, 240f),
            TextAlignmentOptions.Center
        );

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, Scene4Path);
    }

    private static TMP_FontAsset LoadDefaultTMPFont(TMP_FontAsset fallback)
    {
        TMP_FontAsset libSans = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (libSans != null) return libSans;
        if (TMP_Settings.defaultFontAsset != null) return TMP_Settings.defaultFontAsset;
        return fallback;
    }

    private static void CreateMainCamera(Color backgroundColor)
    {
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = backgroundColor;

        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<UniversalAdditionalCameraData>();
    }

    private static Transform CreateCanvas()
    {
        GameObject canvasGo = new GameObject("Canvas");
        canvasGo.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1366f, 768f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        return canvasGo.transform;
    }

    private static void CreateEventSystem()
    {
        GameObject esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();
    }

    private static VideoPlayer CreateVideoPlayer(string name, VideoClip clip, RenderTexture targetRT)
    {
        GameObject vpGo = new GameObject(name);
        VideoPlayer vp = vpGo.AddComponent<VideoPlayer>();
        vp.source = VideoSource.VideoClip;
        vp.clip = clip;
        vp.playOnAwake = true;
        vp.waitForFirstFrame = true;
        vp.isLooping = true;
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = targetRT;
        vp.aspectRatio = VideoAspectRatio.FitHorizontally;
        return vp;
    }

    private static TMP_Text CreateTMPText(
        Transform parent,
        string name,
        string content,
        TMP_FontAsset fontAsset,
        float fontSize,
        Color color,
        Vector2 anchoredPos,
        Vector2 sizeDelta,
        TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.font = fontAsset;
        if (fontAsset.material != null)
        {
            tmp.fontSharedMaterial = fontAsset.material;
        }
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        TMP_FontAsset fontAsset,
        float fontSize,
        Color bgColor,
        Color textColor,
        Vector2 anchoredPos,
        Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Image img = go.AddComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        GameObject textGo = new GameObject("Text (TMP)", typeof(RectTransform));
        textGo.layer = LayerMask.NameToLayer("UI");
        textGo.transform.SetParent(go.transform, false);

        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.font = fontAsset;
        if (fontAsset.material != null)
        {
            tmp.fontSharedMaterial = fontAsset.material;
        }
        tmp.fontSize = fontSize;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return btn;
    }

    private static TMP_InputField CreateTMPInputField(
        Transform parent,
        string name,
        string placeholderText,
        TMP_FontAsset fontAsset,
        Vector2 anchoredPos,
        Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Image bg = go.AddComponent<Image>();
        bg.color = Color.white;
        bg.raycastTarget = true;

        TMP_InputField inputField = go.AddComponent<TMP_InputField>();
        inputField.targetGraphic = bg;

        // Text Area viewport
        GameObject textAreaGo = new GameObject("Text Area", typeof(RectTransform));
        textAreaGo.layer = LayerMask.NameToLayer("UI");
        textAreaGo.transform.SetParent(go.transform, false);

        RectTransform textAreaRt = textAreaGo.GetComponent<RectTransform>();
        textAreaRt.anchorMin = Vector2.zero;
        textAreaRt.anchorMax = Vector2.one;
        textAreaRt.offsetMin = new Vector2(10f, 6f);
        textAreaRt.offsetMax = new Vector2(-10f, -7f);

        RectMask2D mask = textAreaGo.AddComponent<RectMask2D>();
        mask.padding = new Vector4(-8f, -5f, -8f, -5f);

        // Placeholder
        GameObject placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
        placeholderGo.layer = LayerMask.NameToLayer("UI");
        placeholderGo.transform.SetParent(textAreaGo.transform, false);

        RectTransform placeholderRt = placeholderGo.GetComponent<RectTransform>();
        placeholderRt.anchorMin = Vector2.zero;
        placeholderRt.anchorMax = Vector2.one;
        placeholderRt.offsetMin = Vector2.zero;
        placeholderRt.offsetMax = Vector2.zero;

        TextMeshProUGUI placeholderTmp = placeholderGo.AddComponent<TextMeshProUGUI>();
        placeholderTmp.text = placeholderText;
        placeholderTmp.font = fontAsset;
        if (fontAsset.material != null)
        {
            placeholderTmp.fontSharedMaterial = fontAsset.material;
        }
        placeholderTmp.fontSize = 20f;
        placeholderTmp.fontStyle = FontStyles.Italic;
        placeholderTmp.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        placeholderTmp.alignment = TextAlignmentOptions.MidlineLeft;
        placeholderTmp.raycastTarget = false;

        // Input text
        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.layer = LayerMask.NameToLayer("UI");
        textGo.transform.SetParent(textAreaGo.transform, false);

        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI textTmp = textGo.AddComponent<TextMeshProUGUI>();
        textTmp.text = string.Empty;
        textTmp.font = fontAsset;
        if (fontAsset.material != null)
        {
            textTmp.fontSharedMaterial = fontAsset.material;
        }
        textTmp.fontSize = 20f;
        textTmp.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        textTmp.alignment = TextAlignmentOptions.MidlineLeft;
        textTmp.raycastTarget = false;

        inputField.textViewport = textAreaRt;
        inputField.textComponent = textTmp;
        inputField.placeholder = placeholderTmp;
        inputField.fontAsset = fontAsset;
        inputField.pointSize = 20f;
        inputField.caretColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        inputField.customCaretColor = true;

        return inputField;
    }

    private static RawImage CreateRawImage(
        Transform parent,
        string name,
        Texture texture,
        Vector2 anchoredPos,
        Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        RawImage raw = go.AddComponent<RawImage>();
        raw.texture = texture;
        raw.color = Color.white;
        return raw;
    }

    private static Color HexColor(string hex)
    {
        if (!ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            throw new ArgumentException("Invalid hex color: " + hex);
        }
        return color;
    }

    private static T LoadRequired<T>(string assetPath) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset == null)
        {
            throw new InvalidOperationException($"Required asset of type {typeof(T).Name} not found at '{assetPath}'.");
        }
        return asset;
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        string full = parent + "/" + folderName;
        if (!AssetDatabase.IsValidFolder(full))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
