using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// ポーズ画面（全シーン共通。プレハブにして各シーンに1つ置く）
// Esc / Startボタンで開閉し、「ゲームに戻る」「設定」「タイトルに戻る」「終了」を選べる
// ・開いている間はBGM・SEを一時停止（閉じると続きから再生）
// ・ステージ（プレイヤーがいるシーン）では、時間とプレイヤーの操作も止める
public class PauseMenu : MonoBehaviour
{
    // メニューの項目でできること
    public enum MenuAction
    {
        Resume,         // ゲームに戻る
        Settings,       // 設定
        ReturnToTitle,  // タイトルに戻る（確認あり）
        Quit            // 終了（確認あり）
    }

    // メニューの項目1つ分のデータ
    [Serializable]
    public class MenuItem
    {
        [Tooltip("この項目を選んだ時の動作")]
        public MenuAction action;
        [Tooltip("項目のボタン（PauseMenuButtonを付けたもの。カーソルはこの左に合わせる）")]
        public RectTransform button;
    }

    // ポーズ画面の状態
    enum PauseState
    {
        Closed,     // 閉じている
        Menu,       // メニュー選択中
        Confirm,    // 確認ダイアログ表示中
        Settings,   // 設定画面表示中
        Closing,    // 閉じる途中（決定ボタンが離されるのを待っている）
        Loading     // タイトルへ遷移中（入力を受け付けない）
    }

    // ===== 他のスクリプトから見る状態 =====
    // ポーズ画面が開いているか（開いている間、タイトルやリザルトは入力を受け付けないようにする）
    public static bool IsOpen { get; private set; }
    // ポーズ画面を開けなくするか（シーン遷移中やリザルト確定後にtrueにする。シーンが切り替わると自動でfalseに戻る）
    public static bool IsBlocked { get; set; }

    [Header("画面")]
    [Tooltip("ポーズ画面全体（開いている間だけ表示する。画面全体を覆う半透明の背景を入れておくと、後ろのボタンを押せなくなる）")]
    [SerializeField] private GameObject pauseRoot;
    [Tooltip("メニュー（ゲームに戻る／設定／タイトルに戻る／終了）")]
    [SerializeField] private GameObject menuPanel;
    [Tooltip("確認ダイアログ")]
    [SerializeField] private GameObject confirmPanel;
    [Tooltip("設定画面")]
    [SerializeField] private GameObject settingsPanel;

    [Header("メニュー")]
    [Tooltip("メニューの項目（上から順に並べる）")]
    [SerializeField] private MenuItem[] menuItems = new MenuItem[]
    {
        new MenuItem { action = MenuAction.Resume },
        new MenuItem { action = MenuAction.Settings },
        new MenuItem { action = MenuAction.ReturnToTitle },
        new MenuItem { action = MenuAction.Quit },
    };
    [Tooltip("「タイトルに戻る」を表示するか（タイトルシーンではオフにする）")]
    [SerializeField] private bool showReturnToTitle = true;
    [Tooltip("選択中の項目の横に出すカーソル（任意）")]
    [SerializeField] private RectTransform menuCursor;

    [Header("確認ダイアログ")]
    [Tooltip("確認メッセージのテキスト")]
    [SerializeField] private TextMeshProUGUI confirmMessageText;
    [Tooltip("「タイトルに戻る」の確認メッセージ")]
    [SerializeField] private string returnToTitleMessage = "タイトルに戻りますか？";
    [Tooltip("「終了」の確認メッセージ")]
    [SerializeField] private string quitMessage = "ゲームを終了しますか？";
    [Tooltip("「はい」のボタン")]
    [SerializeField] private RectTransform yesButton;
    [Tooltip("「いいえ」のボタン")]
    [SerializeField] private RectTransform noButton;
    [Tooltip("選択中の「はい／いいえ」の横に出すカーソル（任意）")]
    [SerializeField] private RectTransform confirmCursor;

    [Header("カーソル")]
    [Tooltip("項目の左端からカーソルの右端までの距離（マイナスで左側）")]
    [SerializeField] private float cursorOffsetX = -20f;

    [Header("タイトルに戻る")]
    [Tooltip("戻り先のタイトルシーン")]
    [SerializeField] private SceneReference titleScene;
    [Tooltip("画面全体を覆う黒いパネル（任意。ポーズ画面より手前に置く）")]
    [SerializeField] private Image fadePanel;
    [Tooltip("フェードアウトにかける時間（秒）")]
    [SerializeField] private float fadeOutDuration = 0.5f;
    [Tooltip("フェードのイージング")]
    [SerializeField] private Easing.Ease fadeEase = Easing.Ease.Linear;

    [Header("SE（ポーズ中も鳴らすため、AudioManagerではなく専用のAudioSourceで鳴らす）")]
    [Tooltip("SEを鳴らすAudioSource（ポーズ中も鳴るよう、自動でIgnore Listener Pauseをオンにする）")]
    [SerializeField] private AudioSource seSource;
    [Tooltip("開いた時のSE")]
    [SerializeField] private AudioClip openSE;
    [Tooltip("カーソル移動のSE")]
    [SerializeField] private AudioClip cursorSE;
    [Tooltip("決定のSE")]
    [SerializeField] private AudioClip decideSE;
    [Tooltip("戻る・閉じるのSE")]
    [SerializeField] private AudioClip cancelSE;

    [Header("入力")]
    [Tooltip("スティックを倒したと判定するしきい値")]
    [SerializeField] private float stickThreshold = 0.5f;

    // 現在の状態
    private PauseState state = PauseState.Closed;
    // 選択中のメニュー項目の番号
    private int menuIndex;
    // 確認ダイアログで選択中のボタン（0=はい / 1=いいえ）
    private int confirmIndex;
    // 確認中の動作（タイトルに戻る／終了）
    private MenuAction confirmAction;

    // 開く前の状態（閉じた時に元に戻す）
    private float prevTimeScale = 1f;
    private CursorLockMode prevLockState;
    private bool prevCursorVisible;
    // このポーズ画面がプレイヤーの操作を止めたか
    private bool lockedPlayer;
    // このポーズ画面が開いているか（IsOpenはstaticなので、どのポーズ画面が開いたかを別に持つ）
    private bool isOwner;

    // 上下・左右の入力が押されたままか（押しっぱなしで連続移動しないように。上下と左右は別々に持つ）
    private bool isVerticalHeld;
    private bool isHorizontalHeld;


    void Awake()
    {
        // ポーズ中もSEが鳴るようにする
        if (seSource != null) seSource.ignoreListenerPause = true;

        // 「タイトルに戻る」の表示を切り替える
        foreach (MenuItem item in menuItems)
        {
            if (item.action == MenuAction.ReturnToTitle && item.button != null)
                item.button.gameObject.SetActive(showReturnToTitle);
        }

        // 最初は閉じておく
        if (pauseRoot != null) pauseRoot.SetActive(false);
    }


    void Update()
    {
        switch (state)
        {
            case PauseState.Closed:
                if (IsTogglePressed()) Open();
                break;

            case PauseState.Menu:
                UpdateMenu();
                break;

            case PauseState.Confirm:
                UpdateConfirm();
                break;

            case PauseState.Settings:
                // 設定画面の中の操作は設定画面側で行う。ここでは「戻る」だけ見る
                if (IsTogglePressed() || IsBackPressed()) CloseSettings();
                break;

            case PauseState.Closing:
            case PauseState.Loading:
                // 閉じる途中・遷移中は何もしない
                break;
        }
    }


    void OnDestroy()
    {
        // 開いたままシーンが切り替わった時（タイトルに戻る時など）に、止めたものを戻しておく
        if (isOwner) RestoreGame();
    }


    // ==================================================================================
    // 開く・閉じる
    // ==================================================================================

    // ポーズ画面を開く
    public void Open()
    {
        if (state != PauseState.Closed) return;
        if (IsOpen || IsBlocked) return;

        IsOpen  = true;
        isOwner = true;

        // 閉じた時に戻せるよう、今の状態を覚えておく
        prevTimeScale     = Time.timeScale;
        prevLockState     = Cursor.lockState;
        prevCursorVisible = Cursor.visible;

        // ステージ（プレイヤーがいるシーン）では時間とプレイヤーの操作を止める
        // （timeScaleが0でも、射撃などはUpdateで入力を読み続けるので操作も止める）
        lockedPlayer = PlayerControlLock.HasPlayer && !PlayerControlLock.IsLocked;
        if (lockedPlayer)
        {
            Time.timeScale = 0f;
            PlayerControlLock.Lock(false);
        }

        // BGM・SEを一時停止する（ポーズ画面のSEはignoreListenerPauseで鳴る）
        AudioListener.pause = true;

        // カーソルを表示する
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // メニューを表示し、一番上の項目を選ぶ
        if (pauseRoot != null) pauseRoot.SetActive(true);
        ShowPanel(PauseState.Menu);
        menuIndex = GetNextVisibleIndex(-1, 1);
        UpdateMenuCursor();

        PlaySE(openSE);
    }


    // ゲームに戻る（決定ボタンが離されてから閉じる）
    public void Resume()
    {
        if (state != PauseState.Menu) return;

        state = PauseState.Closing;
        StartCoroutine(CloseAfterRelease());
    }


    // 決定・戻るボタンが全て離されるのを待ってから閉じる
    // （押したまま操作を戻すと、そのクリックが射撃として扱われてしまうため）
    IEnumerator CloseAfterRelease()
    {
        while (IsAnyActionKeyHeld())
            yield return null;

        // 離したフレームにタイトルなどが「ボタンを離した」を拾わないよう、もう1フレーム待つ
        yield return null;

        if (pauseRoot != null) pauseRoot.SetActive(false);
        RestoreGame();
        state = PauseState.Closed;
    }


    // 止めたものを元に戻す
    void RestoreGame()
    {
        if (lockedPlayer)
        {
            PlayerControlLock.Unlock();
            Time.timeScale = prevTimeScale;
            lockedPlayer = false;
        }

        AudioListener.pause = false;

        Cursor.lockState = prevLockState;
        Cursor.visible   = prevCursorVisible;

        IsOpen  = false;
        isOwner = false;
    }


    // ==================================================================================
    // メニュー
    // ==================================================================================

    // メニュー選択中の処理
    void UpdateMenu()
    {
        // 戻る（Esc / Start / B）でゲームに戻る
        if (IsTogglePressed() || IsBackPressed())
        {
            PlaySE(cancelSE);
            Resume();
            return;
        }

        // カーソル移動
        int dir = GetVerticalInput();
        if (dir != 0) SelectMenu(GetNextVisibleIndex(menuIndex, dir));

        // 決定
        if (IsDecidePressed()) DecideMenu(menuIndex);
    }


    // メニューの項目を選択する
    void SelectMenu(int index)
    {
        if (index < 0 || index == menuIndex) return;

        menuIndex = index;
        UpdateMenuCursor();
        PlaySE(cursorSE);
    }


    // メニューの項目を決定する
    void DecideMenu(int index)
    {
        if (index < 0 || index >= menuItems.Length) return;
        if (!IsVisible(index)) return;

        menuIndex = index;

        switch (menuItems[index].action)
        {
            case MenuAction.Resume:
                PlaySE(decideSE);
                Resume();
                break;

            case MenuAction.Settings:
                PlaySE(decideSE);
                ShowPanel(PauseState.Settings);
                break;

            case MenuAction.ReturnToTitle:
            case MenuAction.Quit:
                PlaySE(decideSE);
                OpenConfirm(menuItems[index].action);
                break;
        }
    }


    // 設定画面を閉じてメニューに戻る（設定画面の「戻る」ボタンからも呼べる）
    public void CloseSettings()
    {
        if (state != PauseState.Settings) return;

        PlaySE(cancelSE);
        ShowPanel(PauseState.Menu);
        UpdateMenuCursor();
    }


    // ==================================================================================
    // 確認ダイアログ
    // ==================================================================================

    // 確認ダイアログを開く（誤操作を防ぐため「いいえ」を選んだ状態で開く）
    void OpenConfirm(MenuAction action)
    {
        confirmAction = action;

        if (confirmMessageText != null)
            confirmMessageText.text = action == MenuAction.Quit ? quitMessage : returnToTitleMessage;

        ShowPanel(PauseState.Confirm);
        confirmIndex = 1;
        UpdateConfirmCursor();
    }


    // 確認ダイアログ表示中の処理
    void UpdateConfirm()
    {
        // 戻る（Esc / Start / B）は「いいえ」と同じ
        if (IsTogglePressed() || IsBackPressed())
        {
            CancelConfirm();
            return;
        }

        // 「はい／いいえ」の切り替え（上下でも左右でも動かせる）
        int dir = GetVerticalInput();
        if (dir == 0) dir = GetHorizontalInput();
        if (dir != 0) SelectConfirm(confirmIndex == 0 ? 1 : 0);

        // 決定
        if (IsDecidePressed()) DecideConfirm(confirmIndex);
    }


    // 「はい／いいえ」を選択する
    void SelectConfirm(int index)
    {
        if (index < 0 || index > 1 || index == confirmIndex) return;

        confirmIndex = index;
        UpdateConfirmCursor();
        PlaySE(cursorSE);
    }


    // 「はい／いいえ」を決定する
    void DecideConfirm(int index)
    {
        // いいえ
        if (index != 0)
        {
            CancelConfirm();
            return;
        }

        // はい
        PlaySE(decideSE);
        if (confirmAction == MenuAction.Quit) QuitGame();
        else ReturnToTitle();
    }


    // 確認をやめてメニューに戻る
    void CancelConfirm()
    {
        PlaySE(cancelSE);
        ShowPanel(PauseState.Menu);
        UpdateMenuCursor();
    }


    // ==================================================================================
    // タイトルに戻る・終了
    // ==================================================================================

    // タイトルに戻る（画面が真っ暗になってからシーンを読み込む）
    void ReturnToTitle()
    {
        if (titleScene == null)
        {
            Debug.LogError("titleSceneにSceneReferenceが設定されていません。");
            CancelConfirm();
            return;
        }

        // 二重に遷移しないよう入力を止める
        state = PauseState.Loading;
        IsBlocked = true;

        StartCoroutine(FadeOutSequence(() =>
        {
            // リザルトから戻る時のために、結果を消しておく
            GameResult.Clear();

            // 止めた時間・音・操作はOnDestroyで元に戻る
            if (GameManager.instance != null)
                GameManager.instance.LoadScene(titleScene);
            else
                SceneManager.LoadScene(titleScene.sceneName);
        }));
    }


    // ゲームを終了する（エディタでは再生を止める）
    void QuitGame()
    {
        state = PauseState.Loading;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


    // FadePanelを真っ黒にする（timeScaleが0でも動くよう、unscaledDeltaTimeを使う）
    IEnumerator FadeOutSequence(Action onComplete)
    {
        if (fadePanel != null)
        {
            Func<float, float> ease = Easing.GetEasingMethod(fadeEase);
            Color color = fadePanel.color;

            fadePanel.gameObject.SetActive(true);
            fadePanel.raycastTarget = true;

            float time = 0f;
            while (time < fadeOutDuration)
            {
                time += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(time / fadeOutDuration);
                color.a = Mathf.Lerp(0f, 1f, ease(t));
                fadePanel.color = color;

                yield return null;
            }

            color.a = 1f;
            fadePanel.color = color;
        }

        onComplete?.Invoke();
    }


    // ==================================================================================
    // マウス（PauseMenuButtonから呼ばれる）
    // ==================================================================================

    // ボタンにマウスが乗った時
    public void OnButtonHover(PauseMenuButton.ButtonGroup group, int index)
    {
        if (group == PauseMenuButton.ButtonGroup.Menu && state == PauseState.Menu)
            SelectMenu(index);
        else if (group == PauseMenuButton.ButtonGroup.Confirm && state == PauseState.Confirm)
            SelectConfirm(index);
    }


    // ボタンがクリックされた時
    public void OnButtonClick(PauseMenuButton.ButtonGroup group, int index)
    {
        if (group == PauseMenuButton.ButtonGroup.Menu && state == PauseState.Menu)
            DecideMenu(index);
        else if (group == PauseMenuButton.ButtonGroup.Confirm && state == PauseState.Confirm)
            DecideConfirm(index);
    }


    // ==================================================================================
    // 表示
    // ==================================================================================

    // 状態を切り替えて、その状態のパネルだけを表示する
    void ShowPanel(PauseState next)
    {
        state = next;

        if (menuPanel     != null) menuPanel.SetActive(state == PauseState.Menu);
        if (confirmPanel  != null) confirmPanel.SetActive(state == PauseState.Confirm);
        if (settingsPanel != null) settingsPanel.SetActive(state == PauseState.Settings);
    }


    // メニューのカーソルを選択中の項目の横へ動かす
    void UpdateMenuCursor()
    {
        if (menuIndex < 0 || menuIndex >= menuItems.Length) return;
        MoveCursor(menuCursor, menuItems[menuIndex].button);
    }


    // 確認ダイアログのカーソルを選択中のボタンの横へ動かす
    void UpdateConfirmCursor()
    {
        MoveCursor(confirmCursor, confirmIndex == 0 ? yesButton : noButton);
    }


    // カーソルの右端がtargetの左端（縦は中央）に来るように動かす
    // （アンカーやピボットの設定、親が違っても位置がずれないようにワールド座標で計算する）
    void MoveCursor(RectTransform cursor, RectTransform target)
    {
        if (cursor == null || target == null) return;

        // 表示した直後はレイアウトが計算されていないことがあるので、先に計算させる
        Canvas.ForceUpdateCanvases();

        RectTransform cursorParent = cursor.parent as RectTransform;
        Vector3[] corners = new Vector3[4];   // 0:左下 1:左上 2:右上 3:右下

        // 対象の左端の中央（カーソルの親から見た座標）
        target.GetWorldCorners(corners);
        Vector3 targetLeft = cursorParent.InverseTransformPoint((corners[0] + corners[1]) * 0.5f);

        // カーソルの右端の中央（カーソルの親から見た座標）
        cursor.GetWorldCorners(corners);
        Vector3 cursorRight = cursorParent.InverseTransformPoint((corners[2] + corners[3]) * 0.5f);

        // 目標地点との差分だけ、今のanchoredPositionをずらす
        Vector3 diff = targetLeft + new Vector3(cursorOffsetX, 0f, 0f) - cursorRight;
        cursor.anchoredPosition += new Vector2(diff.x, diff.y);
    }


    // 項目が表示されているか（「タイトルに戻る」を隠している時などに飛ばすため）
    bool IsVisible(int index)
    {
        if (index < 0 || index >= menuItems.Length) return false;

        RectTransform button = menuItems[index].button;
        return button != null && button.gameObject.activeSelf;
    }


    // fromからdir方向に見て、次に表示されている項目の番号を返す（端まで行ったら反対側へ）
    int GetNextVisibleIndex(int from, int dir)
    {
        int count = menuItems.Length;
        for (int i = 1; i <= count; i++)
        {
            int index = ((from + dir * i) % count + count) % count;
            if (IsVisible(index)) return index;
        }
        return -1;
    }


    // ==================================================================================
    // 入力
    // ==================================================================================

    // ポーズの開閉（Esc / Startボタン）
    bool IsTogglePressed()
    {
        return Input.GetKeyDown(KeyCode.Escape)
            || Input.GetKeyDown(KeyCode.Joystick1Button7);
    }


    // 決定（Space / Enter / Aボタン）※左クリックはPauseMenuButtonで扱う
    bool IsDecidePressed()
    {
        return Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.Joystick1Button0);
    }


    // 戻る（右クリック / Bボタン）※Escは開閉と同じ扱い
    bool IsBackPressed()
    {
        return Input.GetMouseButtonDown(1)
            || Input.GetKeyDown(KeyCode.Joystick1Button1);
    }


    // ゲーム側で使われるボタンが押されたままか（閉じる前に離されるのを待つため）
    bool IsAnyActionKeyHeld()
    {
        return Input.GetMouseButton(0)
            || Input.GetMouseButton(1)
            || Input.GetKey(KeyCode.Space)
            || Input.GetKey(KeyCode.Return)
            || Input.GetKey(KeyCode.Escape)
            || Input.GetKey(KeyCode.Joystick1Button0)
            || Input.GetKey(KeyCode.Joystick1Button1)
            || Input.GetKey(KeyCode.Joystick1Button7);
    }


    // 上下の入力（上:-1 / 下:+1 / なし:0）
    int GetVerticalInput()
    {
        // キーボード
        // （矢印キー・W/Sは"Vertical"軸にも割り当てられているので、押した時点で「押したまま」扱いにして
        //   次のフレームに軸の値でもう一度動いてしまうのを防ぐ）
        if (Input.GetKeyDown(KeyCode.UpArrow)   || Input.GetKeyDown(KeyCode.W)) { isVerticalHeld = true; return -1; }
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) { isVerticalHeld = true; return  1; }

        // マウスホイール（上に回すと上の項目へ）
        float wheel = Input.mouseScrollDelta.y;
        if (wheel > 0) return -1;
        if (wheel < 0) return  1;

        // スティック（一度戻すまで次の入力を受け付けない）
        return GetHeldAxisInput(-Input.GetAxisRaw("Vertical"), ref isVerticalHeld);
    }


    // 左右の入力（左:-1 / 右:+1 / なし:0）
    int GetHorizontalInput()
    {
        // キーボード（上下と同じく、"Horizontal"軸での二重入力を防ぐ）
        if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A)) { isHorizontalHeld = true; return -1; }
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) { isHorizontalHeld = true; return  1; }

        return GetHeldAxisInput(Input.GetAxisRaw("Horizontal"), ref isHorizontalHeld);
    }


    // 軸の入力を1回ずつに変換する（一度離す（しきい値未満に戻す）まで次の入力を受け付けない）
    int GetHeldAxisInput(float value, ref bool isHeld)
    {
        if (Mathf.Abs(value) < stickThreshold)
        {
            isHeld = false;
            return 0;
        }
        if (isHeld) return 0;

        isHeld = true;
        return value > 0 ? 1 : -1;
    }


    // SEを鳴らす
    void PlaySE(AudioClip clip)
    {
        if (seSource != null && clip != null)
            seSource.PlayOneShot(clip);
    }


    // ==================================================================================
    // static変数の初期化
    // ==================================================================================

    // 再生開始時に初期化し、シーンが切り替わるたびにブロックを解除するよう登録する
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Initialize()
    {
        IsOpen    = false;
        IsBlocked = false;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;

        // 新しいシーンでは開ける状態から始める
        IsBlocked = false;

        // 念のため、開いたまま切り替わっていたら時間と音を戻す（通常はOnDestroyで戻っている）
        if (IsOpen)
        {
            IsOpen = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}
