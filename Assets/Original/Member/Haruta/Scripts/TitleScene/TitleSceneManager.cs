using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TitleSceneManager : MonoBehaviour
{
    // ステージ1つ分のデータ
    [Serializable]
    public class StageData
    {
        [Tooltip("UIに表示するステージ名")]
        public string displayName;
        [Tooltip("遷移先のシーン")]
        public SceneReference scene;
        [Tooltip("プレビュー用のプレハブ")]
        public GameObject previewPrefab;
        [Tooltip("ステージ名のテキスト（選択カーソルをこの横に合わせる）")]
        public RectTransform labelText;
    }

    // タイトルの状態
    enum TitleState
    {
        Intro,          // シーン開始時のフェードイン中（入力を受け付けない）
        PressStart,     // Press Start待ち
        StageSelect,    // ステージ選択中
        Loading         // フェードアウト～シーン遷移中（入力を受け付けない）
    }

    [Header("ステージ")]
    [Tooltip("選択できるステージ（上から順に並ぶ）")]
    [SerializeField] private StageData[] stages = new StageData[3];
    [Tooltip("プレビューを生成する親オブジェクト")]
    [SerializeField] private Transform previewRoot;

    [Header("UI")]
    [Tooltip("Press Start表示")]
    [SerializeField] private GameObject pressStartUI;
    [Tooltip("ステージ選択UI")]
    [SerializeField] private GameObject stageSelectUI;
    [Tooltip("選択中のステージ名を表示するテキスト（任意）")]
    [SerializeField] private TextMeshProUGUI stageNameText;

    [Header("フェード")]
    [Tooltip("画面全体を覆う黒いパネル（FadePanel）")]
    [SerializeField] private Image fadePanel;
    [Tooltip("シーン開始時のフェードインにかける時間（秒）")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [Tooltip("シーン遷移前のフェードアウトにかける時間（秒）")]
    [SerializeField] private float fadeOutDuration = 0.5f;
    [Tooltip("フェードのイージング")]
    [SerializeField] private Easing.Ease fadeEase = Easing.Ease.Linear;

    [Header("タイトルロゴ演出")]
    [Tooltip("上から降りてくるタイトルロゴ")]
    [SerializeField] private RectTransform titleText;
    [Tooltip("ロゴが止まってから表示する「Press Start」のテキスト（StartText）")]
    [SerializeField] private GameObject startPrompt;
    [Tooltip("降り始める位置（anchoredPosition.y）")]
    [SerializeField] private float titleStartY = 1080f;
    [Tooltip("止まる位置（anchoredPosition.y）")]
    [SerializeField] private float titleEndY = 240f;
    [Tooltip("降りてくるのにかける時間（秒）")]
    [SerializeField] private float titleDropDuration = 1.0f;
    [Tooltip("降りてくる時のイージング")]
    [SerializeField] private Easing.Ease titleDropEase = Easing.Ease.OutCubic;
    [Tooltip("元の位置へ戻るのにかける時間（秒）")]
    [SerializeField] private float titleRiseDuration = 0.5f;
    [Tooltip("元の位置へ戻る時のイージング")]
    [SerializeField] private Easing.Ease titleRiseEase = Easing.Ease.InCubic;

    [Header("ステージ選択演出")]
    [Tooltip("横からスライドしてくるステージ選択の親オブジェクト（StageSelectState）")]
    [SerializeField] private RectTransform stageSelectPanel;
    [Tooltip("画面外の位置（anchoredPosition.x）")]
    [SerializeField] private float stageSelectHiddenX = -1920f;
    [Tooltip("表示位置（anchoredPosition.x）")]
    [SerializeField] private float stageSelectShownX = 0f;
    [Tooltip("入ってくるのにかける時間（秒）")]
    [SerializeField] private float stageSelectInDuration = 0.5f;
    [Tooltip("入ってくる時のイージング")]
    [SerializeField] private Easing.Ease stageSelectInEase = Easing.Ease.OutCubic;
    [Tooltip("出ていくのにかける時間（秒）")]
    [SerializeField] private float stageSelectOutDuration = 0.5f;
    [Tooltip("出ていく時のイージング")]
    [SerializeField] private Easing.Ease stageSelectOutEase = Easing.Ease.InCubic;

    [Header("選択カーソル")]
    [Tooltip("選択中のステージ名の横に出すカーソル（SelectCursor）")]
    [SerializeField] private RectTransform selectCursor;
    [Tooltip("ステージ名の左端からカーソルの右端までの距離（マイナスで左側）")]
    [SerializeField] private float cursorOffsetX = 20f;
    [Tooltip("カーソルが移動するのにかける時間（秒）。0なら瞬間移動")]
    [SerializeField] private float cursorMoveDuration = 0.1f;
    [Tooltip("カーソル移動のイージング")]
    [SerializeField] private Easing.Ease cursorMoveEase = Easing.Ease.OutCubic;

    [Header("入力")]
    [Tooltip("スティックを倒したと判定するしきい値")]
    [SerializeField] private float stickThreshold = 0.5f;

    // 現在の状態
    private TitleState state;
    // 選択中のステージ番号
    private int selectIndex;
    // 生成したプレビュー
    private GameObject[] previews;
    // スティック・キーが倒れた（押された）ままか（倒しっぱなしで連続移動しないように）
    private bool isStickHeld;
    // タイトルロゴが動いている最中か（この間は入力を受け付けない）
    private bool isTitleMoving;
    // 実行中のタイトルロゴ演出
    private Coroutine titleMoveCoroutine;
    // ステージ選択UIが動いている最中か（この間は入力を受け付けない）
    private bool isStageSelectMoving;
    // 実行中のステージ選択UI演出
    private Coroutine stageSelectMoveCoroutine;
    // 実行中のカーソル移動演出
    private Coroutine cursorMoveCoroutine;
    // 実行中のフェード演出
    private Coroutine fadeCoroutine;


    void Start()
    {
        // タイトルではマウスで操作するのでカーソルを表示する（ステージ側のMouseLookScriptで再びロックされる）
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // プレビューを全て生成して非表示にしておく（切り替えのたびに生成しない）
        previews = new GameObject[stages.Length];
        for (int i = 0; i < stages.Length; i++)
        {
            if (stages[i].previewPrefab == null) continue;

            Transform parent = previewRoot != null ? previewRoot : transform;
            previews[i] = Instantiate(stages[i].previewPrefab, parent);
            previews[i].SetActive(false);
        }

        // ロゴを画面外に置いておく（フェードイン中にエディタ上の配置位置で見えないように）
        if (titleText != null)
            titleText.anchoredPosition = new Vector2(titleText.anchoredPosition.x, titleStartY);

        // フェードインが終わってからPress Start（ロゴが降りてくる）へ進む
        selectIndex = 0;
        ChangeState(TitleState.Intro);
        Fade(1f, 0f, fadeInDuration, () => ChangeState(TitleState.PressStart));

        // タイトルBGMを再生
        if (AudioManager.instance != null)
            AudioManager.instance.PlayBGM("Opening BGM");
    }


    void Update()
    {
        // ポーズ画面が開いている間は入力を受け付けない
        if (PauseMenu.IsOpen) return;

        switch (state)
        {
            case TitleState.PressStart:
                UpdatePressStart();
                break;

            case TitleState.StageSelect:
                UpdateStageSelect();
                break;

            case TitleState.Intro:
            case TitleState.Loading:
                // フェード中・遷移中は何もしない
                break;
        }
    }


    // Press Start中の処理
    void UpdatePressStart()
    {
        // タイトルロゴが止まるまでは入力を受け付けない
        if (isTitleMoving) return;

        // 左クリックは「離した時」に進める
        // （押した瞬間に進むと、そのまま離した位置のステージボタンがクリック扱いになるため）
        if (IsDecidePressed() || Input.GetMouseButtonUp(0))
        {
            PlaySE("Decision SE");

            // ロゴが動き出したら「Press Start」は消す
            if (startPrompt != null) startPrompt.SetActive(false);

            // タイトルロゴが元の位置へ戻り切ってからステージ選択へ切り替える
            MoveTitle(titleEndY, titleStartY, titleRiseDuration, titleRiseEase,
                () => ChangeState(TitleState.StageSelect));
        }
    }


    // ステージ選択中の処理
    void UpdateStageSelect()
    {
        // ステージ選択UIが止まるまでは入力を受け付けない
        if (isStageSelectMoving) return;

        // カーソル移動（キー・スティック・ホイール）
        int dir = GetMoveInput();
        if (dir != 0)
        {
            SelectStage((selectIndex + dir + stages.Length) % stages.Length);
        }

        // 決定（キー・コントローラー。マウスはStageSelectButtonから呼ばれる）
        if (IsDecidePressed())
        {
            DecideStage(selectIndex);
            return;
        }

        // 戻る
        if (IsCancelPressed())
        {
            PlaySE("Cancel SE");

            // ステージ選択UIが画面外へ出切ってからPress Startへ戻る
            MoveStageSelect(stageSelectShownX, stageSelectHiddenX, stageSelectOutDuration, stageSelectOutEase,
                () => ChangeState(TitleState.PressStart));
        }
    }


    // ステージを選択する（ボタンにマウスが乗った時にも呼ばれる）
    public void SelectStage(int index)
    {
        if (PauseMenu.IsOpen) return;
        if (state != TitleState.StageSelect) return;
        if (isStageSelectMoving) return;
        if (index < 0 || index >= stages.Length) return;
        if (index == selectIndex) return;

        PlaySE("Cursor SE");
        selectIndex = index;
        UpdatePreview(false);
    }


    // ステージを決定する（ボタンをクリックした時にも呼ばれる）
    public void DecideStage(int index)
    {
        if (PauseMenu.IsOpen) return;
        if (state != TitleState.StageSelect) return;
        if (isStageSelectMoving) return;
        if (index < 0 || index >= stages.Length) return;

        selectIndex = index;

        // 遷移できない設定なら、UIを出さずにここで止める
        if (!CanLoadSelectedStage()) return;

        // ステージへ遷移し始めたら、ポーズ画面を開けないようにする
        PauseMenu.IsBlocked = true;

        PlaySE("Decision SE");

        // ステージ選択UIが画面外へ出切ってからステージへ遷移する
        MoveStageSelect(stageSelectShownX, stageSelectHiddenX, stageSelectOutDuration, stageSelectOutEase,
            LoadSelectedStage);
    }


    // 状態を切り替える
    void ChangeState(TitleState next)
    {
        state = next;

        // フェードイン中もPress Start画面（ロゴは画面外）を後ろに出しておく
        if (pressStartUI  != null) pressStartUI.SetActive(state == TitleState.PressStart || state == TitleState.Intro);
        if (stageSelectUI != null) stageSelectUI.SetActive(state == TitleState.StageSelect);

        // Press Start中も背景として選択中のプレビューを出しておく
        // （ステート切り替え時はカーソルを瞬間移動させる）
        UpdatePreview(true);

        // 「Press Start」はロゴが止まるまで隠しておく
        if (startPrompt != null) startPrompt.SetActive(false);

        // Press Startに入ったらタイトルロゴを降らせ、止まったら「Press Start」を出す
        if (state == TitleState.PressStart)
            MoveTitle(titleStartY, titleEndY, titleDropDuration, titleDropEase,
                () => { if (startPrompt != null) startPrompt.SetActive(true); });

        // ステージ選択に入ったらステージ選択UIを横から入れる
        if (state == TitleState.StageSelect)
            MoveStageSelect(stageSelectHiddenX, stageSelectShownX, stageSelectInDuration, stageSelectInEase, null);
    }


    // タイトルロゴの移動演出を開始する（Y方向だけ動かす。終わったらonCompleteを呼ぶ）
    void MoveTitle(float fromY, float toY, float duration, Easing.Ease easeType, Action onComplete)
    {
        // ロゴが未設定なら演出を飛ばしてすぐ次へ進む
        if (titleText == null)
        {
            onComplete?.Invoke();
            return;
        }

        // 前の演出が残っていれば止めてからやり直す
        if (titleMoveCoroutine != null)
            StopCoroutine(titleMoveCoroutine);

        float x = titleText.anchoredPosition.x;
        isTitleMoving = true;
        titleMoveCoroutine = StartCoroutine(MoveRectSequence(
            titleText, new Vector2(x, fromY), new Vector2(x, toY), duration, easeType,
            () =>
            {
                isTitleMoving      = false;
                titleMoveCoroutine = null;
                onComplete?.Invoke();
            }));
    }


    // ステージ選択UIの移動演出を開始する（X方向だけ動かす。終わったらonCompleteを呼ぶ）
    void MoveStageSelect(float fromX, float toX, float duration, Easing.Ease easeType, Action onComplete)
    {
        // パネルが未設定なら演出を飛ばしてすぐ次へ進む
        if (stageSelectPanel == null)
        {
            onComplete?.Invoke();
            return;
        }

        // 前の演出が残っていれば止めてからやり直す
        if (stageSelectMoveCoroutine != null)
            StopCoroutine(stageSelectMoveCoroutine);

        float y = stageSelectPanel.anchoredPosition.y;
        isStageSelectMoving = true;
        stageSelectMoveCoroutine = StartCoroutine(MoveRectSequence(
            stageSelectPanel, new Vector2(fromX, y), new Vector2(toX, y), duration, easeType,
            () =>
            {
                isStageSelectMoving      = false;
                stageSelectMoveCoroutine = null;
                onComplete?.Invoke();
            }));
    }


    // フェードを開始する（fromAlpha→toAlpha。終わったらonCompleteを呼ぶ）
    void Fade(float fromAlpha, float toAlpha, float duration, Action onComplete)
    {
        // パネルが未設定なら演出を飛ばしてすぐ次へ進む
        if (fadePanel == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeSequence(fromAlpha, toAlpha, duration, onComplete));
    }


    // FadePanelの透明度をイージングで変える
    IEnumerator FadeSequence(float fromAlpha, float toAlpha, float duration, Action onComplete)
    {
        Func<float, float> ease = Easing.GetEasingMethod(fadeEase);
        Color color = fadePanel.color;

        // フェード中はパネルでマウス操作をさえぎる
        fadePanel.gameObject.SetActive(true);
        fadePanel.raycastTarget = true;

        color.a = fromAlpha;
        fadePanel.color = color;

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);
            color.a = Mathf.Lerp(fromAlpha, toAlpha, ease(t));
            fadePanel.color = color;

            yield return null;
        }

        color.a = toAlpha;
        fadePanel.color = color;

        // 透明になったら、ボタンへのマウス操作をさえぎらないようにする
        // （透明でもRaycast Targetがオンだと、下のボタンが反応しなくなるため）
        if (toAlpha <= 0f)
            fadePanel.raycastTarget = false;

        fadeCoroutine = null;
        onComplete?.Invoke();
    }


    // RectTransformをfromからtoへイージングで動かす（タイトルロゴ・ステージ選択UI共通）
    IEnumerator MoveRectSequence(RectTransform target, Vector2 from, Vector2 to,
                                 float duration, Easing.Ease easeType, Action onComplete)
    {
        Func<float, float> ease = Easing.GetEasingMethod(easeType);

        // 開始位置に置く
        target.anchoredPosition = from;

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;

            // 経過割合(0～1)をイージングにかけて位置を補間する
            float t = Mathf.Clamp01(time / duration);
            target.anchoredPosition = Vector2.LerpUnclamped(from, to, ease(t));

            yield return null;
        }

        // 誤差が残らないよう最終位置にぴったり合わせる
        target.anchoredPosition = to;

        // 演出後の処理（フラグを戻す・ステート切り替えなど）
        onComplete?.Invoke();
    }


    // 選択中のステージのプレビューだけを表示する（instantCursor: カーソルを瞬間移動させるか）
    void UpdatePreview(bool instantCursor)
    {
        for (int i = 0; i < previews.Length; i++)
        {
            if (previews[i] != null)
                previews[i].SetActive(i == selectIndex);
        }

        if (stageNameText != null)
            stageNameText.text = stages[selectIndex].displayName;

        UpdateCursor(instantCursor);
    }


    // 選択カーソルを選択中のステージ名の横へ動かす
    void UpdateCursor(bool instant)
    {
        if (selectCursor == null) return;

        RectTransform label = stages[selectIndex].labelText;
        if (label == null) return;

        // 移動中のカーソル演出があれば止める
        if (cursorMoveCoroutine != null)
        {
            StopCoroutine(cursorMoveCoroutine);
            cursorMoveCoroutine = null;
        }

        Vector2 target = GetCursorTargetPos(label);

        // 瞬間移動
        if (instant || cursorMoveDuration <= 0f)
        {
            selectCursor.anchoredPosition = target;
            return;
        }

        // イージングで移動
        cursorMoveCoroutine = StartCoroutine(MoveRectSequence(
            selectCursor, selectCursor.anchoredPosition, target, cursorMoveDuration, cursorMoveEase,
            () => cursorMoveCoroutine = null));
    }


    // カーソルの右端がステージ名の左端（縦は中央）に来る時のanchoredPositionを求める
    // （アンカーやピボットの設定、親が違っても位置がずれないようにワールド座標で計算する）
    Vector2 GetCursorTargetPos(RectTransform label)
    {
        RectTransform cursorParent = selectCursor.parent as RectTransform;
        Vector3[] corners = new Vector3[4];   // 0:左下 1:左上 2:右上 3:右下

        // ステージ名の左端の中央（カーソルの親から見た座標）
        label.GetWorldCorners(corners);
        Vector3 labelLeft = cursorParent.InverseTransformPoint((corners[0] + corners[1]) * 0.5f);

        // カーソルの右端の中央（カーソルの親から見た座標）
        selectCursor.GetWorldCorners(corners);
        Vector3 cursorRight = cursorParent.InverseTransformPoint((corners[2] + corners[3]) * 0.5f);

        // 目標地点との差分だけ、今のanchoredPositionをずらす
        Vector3 diff = labelLeft + new Vector3(cursorOffsetX, 0f, 0f) - cursorRight;
        return selectCursor.anchoredPosition + new Vector2(diff.x, diff.y);
    }


    // 選択中のステージへ遷移できるか確認する
    bool CanLoadSelectedStage()
    {
        if (GameManager.instance == null)
        {
            Debug.LogError("GameManagerが存在しません。");
            return false;
        }
        if (stages[selectIndex].scene == null)
        {
            Debug.LogError(selectIndex + "番目のステージにSceneReferenceが設定されていません。");
            return false;
        }
        return true;
    }


    // 選択中のステージへ遷移する（画面が真っ暗になってからシーンを読み込む）
    void LoadSelectedStage()
    {
        if (!CanLoadSelectedStage()) return;

        // 二重に遷移しないよう入力を止める
        ChangeState(TitleState.Loading);

        SceneReference scene = stages[selectIndex].scene;
        Fade(0f, 1f, fadeOutDuration, () => GameManager.instance.LoadScene(scene));
    }


    // 上下の入力を取得する（上:-1 / 下:+1 / なし:0）
    int GetMoveInput()
    {
        // キーボード
        // （矢印キー・W/Sは"Vertical"軸にも割り当てられているので、押した時点で「押したまま」扱いにして
        //   次のフレームに軸の値でもう一度動いてしまうのを防ぐ）
        if (Input.GetKeyDown(KeyCode.UpArrow)   || Input.GetKeyDown(KeyCode.W)) { isStickHeld = true; return -1; }
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) { isStickHeld = true; return  1; }

        // マウスホイール（上に回すと上のステージへ）
        float wheel = Input.mouseScrollDelta.y;
        if (wheel > 0) return -1;
        if (wheel < 0) return  1;

        // スティック（一度戻すまで次の入力を受け付けない）
        float v = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(v) < stickThreshold)
        {
            isStickHeld = false;
            return 0;
        }
        if (isStickHeld) return 0;

        isStickHeld = true;
        return v > 0 ? -1 : 1;
    }


    // 決定ボタン（Space / Enter / Aボタン）※左クリックは別で扱う
    bool IsDecidePressed()
    {
        return Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.Joystick1Button0);
    }


    // 戻るボタン（右クリック / Bボタン）※Escは全シーン共通でポーズ画面の開閉に使う
    bool IsCancelPressed()
    {
        return Input.GetMouseButtonDown(1)
            || Input.GetKeyDown(KeyCode.Joystick1Button1);
    }


    // SEを再生する（AudioManagerがなくても止まらないように）
    void PlaySE(string name)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySE(name);
    }
}
