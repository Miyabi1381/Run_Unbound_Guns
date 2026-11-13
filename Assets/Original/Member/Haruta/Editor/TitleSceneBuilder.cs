// ======================================================================================
// File: TitleSceneBuilder.cs
//
// Brief: タイトルシーンのUI階層を自動で組み立てるエディタ拡張
//        メニューの Tools > Haruta > タイトルシーンをセットアップ から実行する
//        (何度実行しても同じ状態になる。Ctrl+Zで元に戻せる)
//
// Author: Nagaya Haruta
// ======================================================================================
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TitleSceneBuilder
{
    // 各アセットの場所
    const string ScenePath       = "Assets/Original/Scenes/TitleScene.unity";
    const string SceneRefFolder  = "Assets/Original/Scenes/SceneReference/";
    const string LogoSourcePath  = "Assets/Original/Resources/Images/Materials/Title.png";
    const string LogoWhiteFolder = "Assets/Original/Member/Haruta/Textures";
    const string LogoWhitePath   = LogoWhiteFolder + "/Title_White.png";
    const string StencilFontPath = "Assets/_Tanks/Art/Fonts/Big_Shoulders_Stencil/BigShouldersStencil SDF.asset";
    const string ShotClipPath    = "Assets/Easy FPS/Sounds/shotSound.wav";
    const string ShotSEName      = "Shot SE";

    // このスクリプトが生成するオブジェクトの名前(再実行時に作り直す)
    static readonly string[] GeneratedNames = { "BackgroundCover", "StageSelect", "ShatterRoot", "Crosshair", "Fader" };
    // 使わなくなる旧メニュー
    static readonly string[] OldMenuNames = { "MainMenu", "ModeMenu", "StageMenu" };


    [MenuItem("Tools/Haruta/タイトルシーンをセットアップ")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("タイトルシーンのセットアップ", "再生を止めてから実行してください。", "OK");
            return;
        }

        // タイトルシーンを開く
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath);
        }

        // 必要なオブジェクトを探す
        GameObject canvasObj  = FindRoot(scene, "Canvas");
        GameObject managerObj = FindRoot(scene, "TitleManager");
        Transform titleTr     = (canvasObj != null) ? canvasObj.transform.Find("PressSpaceKey") : null;
        Transform startTr     = (titleTr   != null) ? titleTr.Find("PressSpaceKeyTXT") : null;

        if (canvasObj == null || managerObj == null || titleTr == null || startTr == null)
        {
            EditorUtility.DisplayDialog("タイトルシーンのセットアップ",
                "Canvas / TitleManager / PressSpaceKey / PressSpaceKeyTXT のどれかが見つかりませんでした。", "OK");
            return;
        }

        Undo.SetCurrentGroupName("タイトルシーンをセットアップ");
        int undoGroup = Undo.GetCurrentGroup();

        Transform canvas = canvasObj.transform;

        // ------------------------------------------------------------------------------
        // 古いものを片付ける
        // ------------------------------------------------------------------------------
        foreach (string name in OldMenuNames)   DestroyChild(canvas, name);
        foreach (string name in GeneratedNames) DestroyChild(canvas, name);
        DestroyChild(titleTr, "ShootGuideTXT");

        GameObject randomPicker = FindRoot(scene, "RandomPicker");
        if (randomPicker != null) Undo.DestroyObjectImmediate(randomPicker);

        TitleManager oldManager = managerObj.GetComponent<TitleManager>();
        if (oldManager != null) Undo.DestroyObjectImmediate(oldManager);

        // ------------------------------------------------------------------------------
        // フォント
        // ------------------------------------------------------------------------------
        TMP_Text startText = startTr.GetComponent<TMP_Text>();
        TMP_FontAsset baseFont  = (startText != null) ? startText.font : null;
        TMP_FontAsset titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(StencilFontPath);
        if (titleFont == null) titleFont = baseFont;

        // ------------------------------------------------------------------------------
        // 背景を暗くするカバー(一番奥)
        // ------------------------------------------------------------------------------
        Image cover = CreateImage("BackgroundCover", canvas, new Color(0f, 0f, 0f, 0.35f));
        Stretch(cover.rectTransform);

        // ------------------------------------------------------------------------------
        // タイトル画面(既存のPressSpaceKeyを流用する)
        // ------------------------------------------------------------------------------
        Undo.RegisterFullObjectHierarchyUndo(titleTr.gameObject, "タイトル画面を変更");
        titleTr.gameObject.SetActive(true);
        CanvasGroup titleGroup = GetOrAdd<CanvasGroup>(titleTr.gameObject);
        titleGroup.alpha = 1f;

        // ロゴ(元画像が黒文字で暗い背景だと見えないので、白文字版を作って差し替える)
        Transform logoTr = titleTr.Find("Title");
        if (logoTr != null)
        {
            Image logo = logoTr.GetComponent<Image>();
            Sprite whiteLogo = GetWhiteLogoSprite();
            if (logo != null)
            {
                if (whiteLogo != null) logo.sprite = whiteLogo;
                logo.color          = Color.white;
                logo.preserveAspect = true;
                logo.raycastTarget  = false;
            }
        }

        // START(撃ち抜く的)
        RectTransform startRect = (RectTransform)startTr;
        SetRect(startRect, new Vector2(0f, -260f), new Vector2(480f, 170f));
        startRect.localScale = Vector3.one;
        if (startText != null)
        {
            startText.text             = "START";
            startText.enableAutoSizing = false;
            startText.fontSize         = 120f;
            startText.alignment        = TextAlignmentOptions.Center;
            startText.color            = Color.white;
            startText.raycastTarget    = false;
            if (titleFont != null) startText.font = titleFont;
        }

        // 操作ガイド(点滅)
        TextMeshProUGUI shootGuide = CreateText("ShootGuideTXT", titleTr, "- SHOOT TO START -", 34f,
            new Vector2(0f, -390f), new Vector2(800f, 60f), baseFont);
        Undo.AddComponent<BlinkText>(shootGuide.gameObject);

        // ------------------------------------------------------------------------------
        // ステージ選択
        // ------------------------------------------------------------------------------
        GameObject selectObj = CreateUI("StageSelect", canvas);
        Stretch((RectTransform)selectObj.transform);
        CanvasGroup selectGroup = selectObj.AddComponent<CanvasGroup>();

        CreateText("HeaderTXT", selectObj.transform, "STAGE SELECT", 64f,
            new Vector2(0f, 400f), new Vector2(1000f, 100f), titleFont);

        // ステージ名 + 説明文(撃つと決定)
        GameObject infoObj = CreateUI("StageInfo", selectObj.transform);
        SetRect((RectTransform)infoObj.transform, new Vector2(0f, 20f), new Vector2(1000f, 340f));
        CanvasGroup infoGroup = infoObj.AddComponent<CanvasGroup>();

        TextMeshProUGUI stageName = CreateText("StageNameTXT", infoObj.transform, "DESERT", 190f,
            new Vector2(0f, 30f), new Vector2(1000f, 230f), titleFont);
        TextMeshProUGUI stageDescription = CreateText("StageDescriptionTXT", infoObj.transform, "STAGE 1", 40f,
            new Vector2(0f, -120f), new Vector2(1000f, 60f), baseFont);

        // 左右の矢印(撃つと切り替え)
        TextMeshProUGUI leftArrow = CreateText("LeftArrow", selectObj.transform, "<", 170f,
            new Vector2(-720f, 20f), new Vector2(200f, 240f), baseFont);
        TextMeshProUGUI rightArrow = CreateText("RightArrow", selectObj.transform, ">", 170f,
            new Vector2(720f, 20f), new Vector2(200f, 240f), baseFont);

        // 今何番目かを示す丸印
        GameObject dotsObj = CreateUI("Dots", selectObj.transform);
        SetRect((RectTransform)dotsObj.transform, new Vector2(0f, -230f), new Vector2(200f, 40f));

        Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        Image[] dots = new Image[3];
        for (int i = 0; i < dots.Length; i++)
        {
            dots[i] = CreateImage("Dot" + (i + 1), dotsObj.transform, new Color(1f, 1f, 1f, 0.4f));
            dots[i].sprite = knob;
            SetRect(dots[i].rectTransform, new Vector2((i - 1) * 60f, 0f), new Vector2(26f, 26f));
        }

        CreateText("GuideTXT", selectObj.transform,
            "[A] [D] / SHOOT ARROWS : SELECT        [SPACE] / SHOOT STAGE NAME : START        [ESC] : BACK", 26f,
            new Vector2(0f, -460f), new Vector2(1800f, 50f), baseFont);

        // エディタ上ではタイトル画面だけ見えるようにしておく(実行時もスクリプトが同じ状態にする)
        selectGroup.alpha = 0f;
        selectObj.SetActive(false);

        // ------------------------------------------------------------------------------
        // 画面が割れる演出の置き場所
        // ------------------------------------------------------------------------------
        GameObject shatterObj = CreateUI("ShatterRoot", canvas);
        Stretch((RectTransform)shatterObj.transform);
        ScreenShatter shatter = shatterObj.AddComponent<ScreenShatter>();

        // ------------------------------------------------------------------------------
        // 照準(十字 + 中心の点)
        // ------------------------------------------------------------------------------
        GameObject crosshairObj = CreateUI("Crosshair", canvas);
        SetRect((RectTransform)crosshairObj.transform, Vector2.zero, Vector2.zero);

        SetRect(CreateImage("Top",    crosshairObj.transform, Color.white).rectTransform, new Vector2(0f,  20f), new Vector2(4f, 18f));
        SetRect(CreateImage("Bottom", crosshairObj.transform, Color.white).rectTransform, new Vector2(0f, -20f), new Vector2(4f, 18f));
        SetRect(CreateImage("Left",   crosshairObj.transform, Color.white).rectTransform, new Vector2(-20f, 0f), new Vector2(18f, 4f));
        SetRect(CreateImage("Right",  crosshairObj.transform, Color.white).rectTransform, new Vector2( 20f, 0f), new Vector2(18f, 4f));
        SetRect(CreateImage("Center", crosshairObj.transform, Color.white).rectTransform, Vector2.zero,          new Vector2(4f, 4f));

        // ------------------------------------------------------------------------------
        // 画面全体のフェード(一番手前)
        // ------------------------------------------------------------------------------
        Image fader = CreateImage("Fader", canvas, new Color(0f, 0f, 0f, 0f));
        Stretch(fader.rectTransform);
        ScreenFader screenFader = fader.gameObject.AddComponent<ScreenFader>();

        // 並び順(上が奥、下が手前)
        cover.transform.SetSiblingIndex(0);
        titleTr.SetSiblingIndex(1);
        selectObj.transform.SetSiblingIndex(2);
        shatterObj.transform.SetSiblingIndex(3);
        crosshairObj.transform.SetSiblingIndex(4);
        fader.transform.SetAsLastSibling();

        // ------------------------------------------------------------------------------
        // 射撃SEをAudioManagerに登録する
        // ------------------------------------------------------------------------------
        bool hasShotSE = RegisterShotSE(scene);

        // ------------------------------------------------------------------------------
        // TitleSceneControllerを付けて、参照を登録する
        // ------------------------------------------------------------------------------
        TitleSceneController controller = GetOrAdd<TitleSceneController>(managerObj);
        SerializedObject so = new SerializedObject(controller);

        string[] stageNames   = { "DESERT", "JUNGLE", "MOON" };
        string[] sceneRefs    = { "DesertStage Reference.asset", "JungleStage Reference.asset", "MoonStage Reference.asset" };
        string[] backgrounds  = { "LevelDesert", "LevelJungle", "LevelMoon" };
        Color[]  themeColors  =
        {
            new Color(1.00f, 0.74f, 0.30f),     // 砂漠：オレンジ
            new Color(0.45f, 0.92f, 0.45f),     // ジャングル：緑
            new Color(0.62f, 0.78f, 1.00f),     // 月：青白
        };

        SerializedProperty stagesProp = so.FindProperty("stages");
        stagesProp.arraySize = stageNames.Length;

        for (int i = 0; i < stageNames.Length; i++)
        {
            SerializedProperty stage = stagesProp.GetArrayElementAtIndex(i);
            stage.FindPropertyRelative("displayName").stringValue = stageNames[i];
            stage.FindPropertyRelative("description").stringValue = "STAGE " + (i + 1);
            stage.FindPropertyRelative("scene").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<SceneReference>(SceneRefFolder + sceneRefs[i]);
            stage.FindPropertyRelative("background").objectReferenceValue = FindRoot(scene, backgrounds[i]);
            stage.FindPropertyRelative("themeColor").colorValue = themeColors[i];
        }

        so.FindProperty("titleGroup").objectReferenceValue           = titleGroup;
        so.FindProperty("startTarget").objectReferenceValue          = startRect;
        so.FindProperty("crosshair").objectReferenceValue            = crosshairObj.transform;
        so.FindProperty("screenShatter").objectReferenceValue        = shatter;
        so.FindProperty("stageSelectGroup").objectReferenceValue     = selectGroup;
        so.FindProperty("stageInfoGroup").objectReferenceValue       = infoGroup;
        so.FindProperty("stageNameText").objectReferenceValue        = stageName;
        so.FindProperty("stageDescriptionText").objectReferenceValue = stageDescription;
        so.FindProperty("leftArrow").objectReferenceValue            = leftArrow.rectTransform;
        so.FindProperty("rightArrow").objectReferenceValue           = rightArrow.rectTransform;
        so.FindProperty("backgroundCover").objectReferenceValue      = cover;
        so.FindProperty("screenFader").objectReferenceValue          = screenFader;
        so.FindProperty("bgmSource").objectReferenceValue            = managerObj.GetComponent<AudioSource>();
        if (hasShotSE) so.FindProperty("shotSE").stringValue = ShotSEName;

        SerializedProperty dotsProp = so.FindProperty("stageDots");
        dotsProp.arraySize = dots.Length;
        for (int i = 0; i < dots.Length; i++)
        {
            dotsProp.GetArrayElementAtIndex(i).objectReferenceValue = dots[i];
        }

        // ステージ名はテーマカラーで色を変える
        SerializedProperty themeProp = so.FindProperty("themeGraphics");
        themeProp.arraySize = 1;
        themeProp.GetArrayElementAtIndex(0).objectReferenceValue = stageName;

        so.ApplyModifiedProperties();

        // ------------------------------------------------------------------------------
        // 仕上げ
        // ------------------------------------------------------------------------------
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = managerObj;

        Debug.Log("TitleSceneBuilder: タイトルシーンのセットアップが完了しました。");
        EditorUtility.DisplayDialog("タイトルシーンのセットアップ",
            "完了しました。\nPlayで動作を確認して、問題なければ Ctrl+S でシーンを保存してください。\n(元に戻す場合は Ctrl+Z)", "OK");
    }


    // ----------------------------------------------------------------------------------
    // 補助関数
    // ----------------------------------------------------------------------------------

    // シーン直下のオブジェクトを名前で探す(非アクティブでも見つかる)
    static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
        }
        return null;
    }


    // 指定した名前の子オブジェクトがあれば削除する
    static void DestroyChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) Undo.DestroyObjectImmediate(child.gameObject);
    }


    // コンポーネントがなければ追加して取得する
    static T GetOrAdd<T>(GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        if (component == null) component = Undo.AddComponent<T>(obj);
        return component;
    }


    // 空のUIオブジェクトを生成する
    static GameObject CreateUI(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        obj.layer = parent.gameObject.layer;
        obj.transform.SetParent(parent, false);
        return obj;
    }


    // Imageを生成する
    static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject obj = CreateUI(name, parent);
        Image image = obj.AddComponent<Image>();
        image.color         = color;
        image.raycastTarget = false;
        return image;
    }


    // TextMeshProのテキストを生成する
    static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize,
                                      Vector2 position, Vector2 size, TMP_FontAsset font)
    {
        GameObject obj = CreateUI(name, parent);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text          = text;
        tmp.fontSize      = fontSize;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.color         = Color.white;
        tmp.raycastTarget = false;
        SetRect(tmp.rectTransform, position, size);
        return tmp;
    }


    // 親いっぱいに広げる
    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }


    // 親の中央を基準に、位置と大きさを設定する
    static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin        = new Vector2(0.5f, 0.5f);
        rect.anchorMax        = new Vector2(0.5f, 0.5f);
        rect.pivot            = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta        = size;
    }


    // ロゴ画像の白文字版を用意する(なければ元画像から作る)
    static Sprite GetWhiteLogoSprite()
    {
        if (!File.Exists(LogoWhitePath))
        {
            if (!File.Exists(LogoSourcePath)) return null;

            // 元画像を読み込み、透明度はそのままで色だけ白にする
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(LogoSourcePath));

            Color32[] pixels = texture.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, pixels[i].a);
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            Directory.CreateDirectory(LogoWhiteFolder);
            File.WriteAllBytes(LogoWhitePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            // スプライトとして読み込む設定にする
            AssetDatabase.ImportAsset(LogoWhitePath);
            TextureImporter importer = AssetImporter.GetAtPath(LogoWhitePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType         = TextureImporterType.Sprite;
                importer.spriteImportMode    = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize      = 4096;
                importer.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(LogoWhitePath);
    }


    // AudioManagerのSEリストに射撃SEを登録する(登録できたらtrue)
    static bool RegisterShotSE(Scene scene)
    {
        GameObject audioObj = FindRoot(scene, "AudioManager");
        AudioManager audioManager = (audioObj != null) ? audioObj.GetComponent<AudioManager>() : null;
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ShotClipPath);
        if (audioManager == null || clip == null) return false;

        SerializedObject so = new SerializedObject(audioManager);
        SerializedProperty seDatas = so.FindProperty("seDatas");

        // すでに登録済みなら何もしない
        for (int i = 0; i < seDatas.arraySize; i++)
        {
            if (seDatas.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == ShotSEName)
                return true;
        }

        int index = seDatas.arraySize;
        seDatas.arraySize = index + 1;
        SerializedProperty data = seDatas.GetArrayElementAtIndex(index);
        data.FindPropertyRelative("name").stringValue = ShotSEName;
        data.FindPropertyRelative("audioClip").objectReferenceValue = clip;
        so.ApplyModifiedProperties();

        return true;
    }
}
