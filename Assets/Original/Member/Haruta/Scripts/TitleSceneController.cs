// ======================================================================================
// File: TitleSceneController.cs
//
// Brief: タイトルシーン全体の管理クラス
//        「タイトル画面」→「ステージ選択」の2段階で進行する
//        タイトル画面：マウスで照準を動かし、左クリックでSTARTを撃ち抜くと画面が割れる
//        ステージ選択：左右キー(または矢印を撃つ)で、背景の3Dステージごと切り替える
//
// Author: Nagaya Haruta
// ======================================================================================
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleSceneController : MonoBehaviour
{
    // ステージ1つ分のデータ
    [Serializable]
    public class StageEntry
    {
        [Tooltip("画面に大きく表示するステージ名")]
        public string displayName = "DESERT";
        [Tooltip("ステージ名の下に表示する説明文(空でもよい)")]
        public string description = "";
        [Tooltip("遷移先のシーンリファレンス")]
        public SceneReference scene;
        [Tooltip("背景として表示する3Dステージ(LevelDesertなど)")]
        public GameObject background;
        [Tooltip("ステージのテーマカラー")]
        public Color themeColor = Color.white;
    }

    // 画面の状態
    enum Phase
    {
        Title,          // タイトル画面(STARTを撃つ)
        StageSelect,    // ステージ選択
    }


    [Header("ステージ設定")]
    [Tooltip("選択できるステージ(並べた順に左右キーで切り替わる)")]
    [SerializeField] StageEntry[] stages;
    [Tooltip("最初に表示するステージをランダムにするか")]
    [SerializeField] bool randomFirstStage = true;

    [Header("タイトル画面UI")]
    [Tooltip("タイトルロゴとSTARTをまとめた親")]
    [SerializeField] CanvasGroup titleGroup;
    [Tooltip("撃ち抜く的になるSTART(この四角形の範囲が当たり判定になる)")]
    [SerializeField] RectTransform startTarget;

    [Header("照準")]
    [Tooltip("マウスに追従する照準のUI")]
    [SerializeField] RectTransform crosshair;
    [Tooltip("通常時の照準の色")]
    [SerializeField] Color crosshairNormalColor = Color.white;
    [Tooltip("撃てるものを狙っている時の照準の色")]
    [SerializeField] Color crosshairHoverColor = new Color(1f, 0.25f, 0.2f, 1f);
    [Tooltip("画面が割れる演出")]
    [SerializeField] ScreenShatter screenShatter;

    [Header("ステージ選択UI")]
    [Tooltip("ステージ選択画面のUIをまとめた親")]
    [SerializeField] CanvasGroup stageSelectGroup;
    [Tooltip("ステージ名と説明文をまとめた親(切り替え時にスライドする。撃つと決定になる)")]
    [SerializeField] CanvasGroup stageInfoGroup;
    [Tooltip("ステージ名のテキスト")]
    [SerializeField] TMP_Text stageNameText;
    [Tooltip("説明文のテキスト(なくてもよい)")]
    [SerializeField] TMP_Text stageDescriptionText;
    [Tooltip("左矢印(なくてもよい。撃つと前のステージへ)")]
    [SerializeField] RectTransform leftArrow;
    [Tooltip("右矢印(なくてもよい。撃つと次のステージへ)")]
    [SerializeField] RectTransform rightArrow;
    [Tooltip("今何番目のステージかを示す丸印(なくてもよい。ステージと同じ数だけ登録する)")]
    [SerializeField] Image[] stageDots;
    [Tooltip("テーマカラーで色を変えたいUI(なくてもよい)")]
    [SerializeField] Graphic[] themeGraphics;

    [Header("フェード")]
    [Tooltip("背景とUIの間に置く黒いImage(背景を少し暗くする + 背景切り替えを隠す)")]
    [SerializeField] Image backgroundCover;
    [Tooltip("画面全体のフェード")]
    [SerializeField] ScreenFader screenFader;
    [Tooltip("通常時に背景をどれくらい暗くするか")]
    [SerializeField, Range(0f, 1f)] float dimAlpha = 0.35f;

    [Header("演出時間(秒)")]
    [Tooltip("シーン開始時のフェードイン")]
    [SerializeField] float openingFadeTime = 1.0f;
    [Tooltip("ステージ選択 → タイトル画面に戻る時の切り替え")]
    [SerializeField] float panelFadeTime = 0.4f;
    [Tooltip("ステージの切り替え")]
    [SerializeField] float stageSwitchTime = 0.4f;
    [Tooltip("ステージ決定後のフェードアウト")]
    [SerializeField] float sceneFadeTime = 1.0f;
    [Tooltip("ステージ名がスライドする距離")]
    [SerializeField] float slideDistance = 300f;
    [Tooltip("矢印が動く距離")]
    [SerializeField] float arrowNudgeDistance = 25f;

    [Header("サウンド")]
    [Tooltip("AudioManagerに登録したBGM名")]
    [SerializeField] string bgmName = "Opening BGM";
    [Tooltip("AudioManagerに登録した射撃SE名(左クリックのたびに鳴る)")]
    [SerializeField] string shotSE = "Decision SE";
    [Tooltip("AudioManagerに登録したガラスが割れるSE名(空なら鳴らさない)")]
    [SerializeField] string glassSE = "";
    [Tooltip("AudioManagerに登録した決定SE名")]
    [SerializeField] string decisionSE = "Decision SE";
    [Tooltip("AudioManagerに登録したカーソルSE名")]
    [SerializeField] string cursorSE = "Cursor SE";
    [Tooltip("AudioManagerに登録したキャンセルSE名")]
    [SerializeField] string cancelSE = "Cancel SE";
    [Tooltip("BGM用のAudioSource(登録するとステージ決定時にBGMもフェードアウトする。なくてもよい)")]
    [SerializeField] AudioSource bgmSource;


    // 現在の画面の状態
    Phase phase = Phase.Title;
    // 選択中のステージ番号
    int currentIndex;
    // 演出中かどうか(演出中は入力を受け付けない)
    bool isBusy;

    // Canvasが使っているカメラ(Screen Space - Overlayならnull)
    Camera uiCamera;
    // 照準を構成しているUI(色を変えるために使う)
    Graphic[] crosshairGraphics;

    // 元の位置情報
    RectTransform stageInfoRect;
    Vector2 stageInfoDefaultPos;
    Vector2 leftArrowDefaultPos;
    Vector2 rightArrowDefaultPos;


    void Start()
    {
        // ステージが登録されていなければ動かせないので止める
        if (stages == null || stages.Length == 0)
        {
            Debug.LogError("TitleSceneController: ステージが1つも登録されていません！");
            enabled = false;
            return;
        }

        // マウス座標とUIの当たり判定に使うカメラを取得する
        Canvas canvas = titleGroup.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas = canvas.rootCanvas;
            uiCamera = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;
        }

        // 照準があれば、本来のマウスカーソルは隠す
        if (crosshair != null)
        {
            crosshairGraphics = crosshair.GetComponentsInChildren<Graphic>(true);
            Cursor.visible = false;
        }

        // 元の位置を保存する
        stageInfoRect       = stageInfoGroup.GetComponent<RectTransform>();
        stageInfoDefaultPos = stageInfoRect.anchoredPosition;
        if (leftArrow  != null) leftArrowDefaultPos  = leftArrow.anchoredPosition;
        if (rightArrow != null) rightArrowDefaultPos = rightArrow.anchoredPosition;

        // 最初に表示するステージを決める
        currentIndex = randomFirstStage ? UnityEngine.Random.Range(0, stages.Length) : 0;
        ApplyStage(currentIndex);

        // 初期状態(タイトル画面だけ表示)
        titleGroup.gameObject.SetActive(true);
        titleGroup.alpha = 1f;
        stageSelectGroup.gameObject.SetActive(false);
        stageSelectGroup.alpha = 0f;
        SetCoverAlpha(dimAlpha);

        // タイトルBGMを再生する
        if (AudioManager.instance != null)
            AudioManager.instance.PlayBGM(bgmName);

        // フェードインで開始する
        StartCoroutine(OpeningSequence());
    }


    void Update()
    {
        // 照準は演出中でも常にマウスに追従させる
        UpdateCrosshairPosition();

        // 演出中は入力を受け付けない
        if (isBusy) return;

        switch (phase)
        {
            case Phase.Title:
                HandleTitleInput();
                break;

            case Phase.StageSelect:
                HandleStageSelectInput();
                break;
        }
    }


    void OnDestroy()
    {
        // 隠したマウスカーソルを元に戻す
        Cursor.visible = true;
    }


    // ----------------------------------------------------------------------------------
    // 入力処理
    // ----------------------------------------------------------------------------------

    // タイトル画面の入力処理
    void HandleTitleInput()
    {
        // STARTを狙っているか
        bool isAiming = IsMouseOver(startTarget);
        SetCrosshairHover(isAiming);

        // 狙っている間はSTARTを少し大きくする
        if (startTarget != null)
        {
            Vector3 targetScale = Vector3.one * (isAiming ? 1.15f : 1f);
            startTarget.localScale = Vector3.Lerp(startTarget.localScale, targetScale, Time.unscaledDeltaTime * 15f);
        }

        // 左クリックで射撃
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();

            // STARTに当たったら画面を割ってステージ選択へ
            if (isAiming)
                StartCoroutine(ShatterSequence());
        }
    }


    // ステージ選択の入力処理
    void HandleStageSelectInput()
    {
        // 照準がどこを狙っているか
        bool isAimingLeft  = IsMouseOver(leftArrow);
        bool isAimingRight = IsMouseOver(rightArrow);
        bool isAimingStage = IsMouseOver(stageInfoRect);
        SetCrosshairHover(isAimingLeft || isAimingRight || isAimingStage);

        // 左クリックで射撃
        bool isShot = Input.GetMouseButtonDown(0);
        if (isShot) Shoot();

        // 左右キー(または矢印を撃つ)でステージ切り替え
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || (isShot && isAimingLeft))
        {
            StartCoroutine(ChangeStage(-1));
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || (isShot && isAimingRight))
        {
            StartCoroutine(ChangeStage(1));
        }
        // 決定キー(またはステージ名を撃つ)でステージへ遷移
        else if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || (isShot && isAimingStage))
        {
            StartCoroutine(DecideSequence());
        }
        // 戻るキーでタイトル画面へ
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            PlaySE(cancelSE);
            StartCoroutine(BackToTitle());
        }
    }


    // 射撃(SEを鳴らして照準を反動で動かす)
    void Shoot()
    {
        PlaySE(shotSE);
        StartCoroutine(RecoilCrosshair());
    }


    // ----------------------------------------------------------------------------------
    // 照準
    // ----------------------------------------------------------------------------------

    // 照準をマウスの位置に移動させる
    void UpdateCrosshairPosition()
    {
        if (crosshair == null) return;

        RectTransform parent = crosshair.parent as RectTransform;
        if (parent == null) return;

        // マウスのスクリーン座標を、Canvas上のワールド座標に変換する
        Vector3 worldPos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, Input.mousePosition, uiCamera, out worldPos))
            crosshair.position = worldPos;
    }


    // マウス(照準)が指定したUIの上にあるか
    bool IsMouseOver(RectTransform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;

        return RectTransformUtility.RectangleContainsScreenPoint(target, Input.mousePosition, uiCamera);
    }


    // 照準の色を切り替える(撃てるものを狙っている時は色を変える)
    void SetCrosshairHover(bool isHover)
    {
        if (crosshairGraphics == null) return;

        Color color = isHover ? crosshairHoverColor : crosshairNormalColor;
        foreach (Graphic graphic in crosshairGraphics)
        {
            graphic.color = color;
        }
    }


    // 射撃の反動(照準を一瞬大きくして戻す)
    IEnumerator RecoilCrosshair()
    {
        if (crosshair == null) yield break;

        yield return Tween(0.15f, t =>
        {
            // 一気に広がって、ゆっくり戻る
            crosshair.localScale = Vector3.one * (1f + 0.6f * (1f - Easing.EaseOutQuad(t)));
        });
    }


    // ----------------------------------------------------------------------------------
    // 演出(コルーチン)
    // ----------------------------------------------------------------------------------

    // シーン開始時のフェードイン
    IEnumerator OpeningSequence()
    {
        isBusy = true;

        if (screenFader != null)
        {
            screenFader.SetAlpha(1f);
            yield return screenFader.FadeIn(openingFadeTime);
        }

        isBusy = false;
    }


    // STARTを撃ち抜いた → 画面が割れる → ステージ選択
    IEnumerator ShatterSequence()
    {
        isBusy = true;

        // 撃った位置(ここを中心に画面が割れる)
        Vector2 hitPoint = Input.mousePosition;
        PlaySE(glassSE);

        if (screenShatter != null)
        {
            // 割れる画面に照準が写り込まないように、キャプチャが終わるまで隠す
            if (crosshair != null) crosshair.gameObject.SetActive(false);

            yield return screenShatter.Play(hitPoint, uiCamera, () =>
            {
                // 破片の裏でステージ選択に切り替えておく
                ShowStageSelect();
                if (crosshair != null) crosshair.gameObject.SetActive(true);
            });
        }
        else
        {
            // 割れる演出がなければ、すぐに切り替える
            ShowStageSelect();
        }

        isBusy = false;
    }


    // タイトル画面を消して、ステージ選択を表示する(演出なし)
    void ShowStageSelect()
    {
        titleGroup.alpha = 0f;
        titleGroup.gameObject.SetActive(false);
        if (startTarget != null) startTarget.localScale = Vector3.one;

        stageSelectGroup.gameObject.SetActive(true);
        stageSelectGroup.alpha = 1f;

        SetCrosshairHover(false);
        phase = Phase.StageSelect;
    }


    // ステージ選択 → タイトル画面に、フェードしながら戻る
    IEnumerator BackToTitle()
    {
        isBusy = true;
        SetCrosshairHover(false);

        titleGroup.gameObject.SetActive(true);
        titleGroup.alpha = 0f;

        yield return Tween(panelFadeTime, t =>
        {
            stageSelectGroup.alpha = 1f - t;
            titleGroup.alpha       = Easing.EaseOutQuad(t);
        });

        stageSelectGroup.gameObject.SetActive(false);

        phase  = Phase.Title;
        isBusy = false;
    }


    // ステージを切り替える(dir: -1で左、1で右)
    IEnumerator ChangeStage(int dir)
    {
        isBusy = true;
        PlaySE(cursorSE);

        // 押した方向の矢印を動かす
        if (dir < 0) StartCoroutine(NudgeArrow(leftArrow,  leftArrowDefaultPos,  -1));
        else         StartCoroutine(NudgeArrow(rightArrow, rightArrowDefaultPos,  1));

        // 次のステージ番号(端まで行ったら反対側に回る)
        int nextIndex = (currentIndex + dir + stages.Length) % stages.Length;
        float halfTime = stageSwitchTime * 0.5f;

        // 前半：ステージ名が押した方向と逆へ流れて消え、背景が暗くなる
        yield return Tween(halfTime, t =>
        {
            float e = Easing.EaseInQuad(t);
            stageInfoRect.anchoredPosition = stageInfoDefaultPos + Vector2.left * dir * slideDistance * e;
            stageInfoGroup.alpha = 1f - e;
            SetCoverAlpha(Mathf.Lerp(dimAlpha, 1f, e));
        });

        // 真っ暗な間に背景とテキストを入れ替える
        currentIndex = nextIndex;
        ApplyStage(currentIndex);

        // 後半：新しいステージ名が押した方向から入ってきて、背景が明るくなる
        yield return Tween(halfTime, t =>
        {
            float e = Easing.EaseOutQuart(t);
            stageInfoRect.anchoredPosition = stageInfoDefaultPos + Vector2.right * dir * slideDistance * (1f - e);
            stageInfoGroup.alpha = e;
            SetCoverAlpha(Mathf.Lerp(1f, dimAlpha, e));
        });

        isBusy = false;
    }


    // ステージ決定 → フェードアウト → シーン遷移
    IEnumerator DecideSequence()
    {
        isBusy = true;
        SetCrosshairHover(false);

        StageEntry stage = stages[currentIndex];

        // シーンが登録されていなければ遷移できないので中断する
        if (stage.scene == null || string.IsNullOrEmpty(stage.scene.sceneName))
        {
            Debug.LogError("TitleSceneController: " + stage.displayName + " のシーンリファレンスが設定されていません！");
            isBusy = false;
            yield break;
        }

        PlaySE(decisionSE);

        // ステージ名を一瞬大きくして「決定した」ことを伝える
        yield return Tween(0.25f, t =>
        {
            stageInfoRect.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(t * Mathf.PI));
        });

        // 画面とBGMを一緒にフェードアウトさせる
        float startVolume = (bgmSource != null) ? bgmSource.volume : 0f;

        yield return Tween(sceneFadeTime, t =>
        {
            if (screenFader != null) screenFader.SetAlpha(t);
            if (bgmSource   != null) bgmSource.volume = Mathf.Lerp(startVolume, 0f, t);
        });

        // シーンをロードする(GameManagerがなければ直接ロードする)
        if (GameManager.instance != null)
            GameManager.instance.LoadScene(stage.scene);
        else
            SceneManager.LoadScene(stage.scene.sceneName);
    }


    // 矢印を外側に動かして戻す
    IEnumerator NudgeArrow(RectTransform arrow, Vector2 defaultPos, int dir)
    {
        if (arrow == null) yield break;

        yield return Tween(0.2f, t =>
        {
            // Sinで 0 → 1 → 0 と変化させて、行って戻る動きにする
            float e = Mathf.Sin(t * Mathf.PI);
            arrow.anchoredPosition = defaultPos + Vector2.right * dir * arrowNudgeDistance * e;
        });
    }


    // 指定秒かけて0→1に変化する値を、毎フレーム渡された処理に送る
    IEnumerator Tween(float duration, Action<float> onUpdate)
    {
        float time = 0f;

        while (time < duration)
        {
            // timeScaleが0でも動くようにunscaledDeltaTimeを使う
            time += Time.unscaledDeltaTime;
            onUpdate(Mathf.Clamp01(time / duration));
            yield return null;
        }

        onUpdate(1f);
    }


    // ----------------------------------------------------------------------------------
    // 表示の更新
    // ----------------------------------------------------------------------------------

    // 指定したステージの内容を、背景とUIに反映する
    void ApplyStage(int index)
    {
        StageEntry stage = stages[index];

        // 選択中のステージの背景だけ表示する
        for (int i = 0; i < stages.Length; i++)
        {
            if (stages[i].background != null)
                stages[i].background.SetActive(i == index);
        }

        // テキスト
        if (stageNameText        != null) stageNameText.text        = stage.displayName;
        if (stageDescriptionText != null) stageDescriptionText.text = stage.description;

        // テーマカラー(アルファ値は元のままにする)
        if (themeGraphics != null)
        {
            foreach (Graphic graphic in themeGraphics)
            {
                if (graphic == null) continue;

                Color color = stage.themeColor;
                color.a = graphic.color.a;
                graphic.color = color;
            }
        }

        // 丸印(選択中だけテーマカラーで大きく、それ以外は半透明の白)
        if (stageDots != null)
        {
            for (int i = 0; i < stageDots.Length; i++)
            {
                if (stageDots[i] == null) continue;

                bool isSelected = (i == index);
                stageDots[i].color = isSelected ? stage.themeColor : new Color(1f, 1f, 1f, 0.4f);
                stageDots[i].transform.localScale = Vector3.one * (isSelected ? 1.4f : 1f);
            }
        }
    }


    // 背景を覆う黒いImageのアルファ値を設定する
    void SetCoverAlpha(float alpha)
    {
        if (backgroundCover == null) return;

        Color color = backgroundCover.color;
        color.a = alpha;
        backgroundCover.color = color;
    }


    // SEを再生する(AudioManagerがなければ何もしない)
    void PlaySE(string seName)
    {
        if (AudioManager.instance != null && !string.IsNullOrEmpty(seName))
            AudioManager.instance.PlaySE(seName);
    }
}
