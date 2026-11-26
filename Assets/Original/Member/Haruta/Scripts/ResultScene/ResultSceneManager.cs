using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// ResultSceneに置く
// GameResultを読んでクリア／ゲームオーバーの表示を切り替え、決定でTitleSceneへ戻る
public class ResultSceneManager : MonoBehaviour
{
    // リザルトの状態
    enum ResultState
    {
        Intro,      // フェードイン中（入力を受け付けない）
        Wait,       // 決定待ち
        Loading     // フェードアウト～シーン遷移中（入力を受け付けない）
    }

    [Header("遷移先")]
    [Tooltip("戻り先のタイトルシーン")]
    [SerializeField] private SceneReference titleScene;

    [Header("UI")]
    [Tooltip("クリア時に表示するUI")]
    [SerializeField] private GameObject clearUI;
    [Tooltip("ゲームオーバー時に表示するUI")]
    [SerializeField] private GameObject gameOverUI;
    [Tooltip("ステージ名を表示するテキスト（任意）")]
    [SerializeField] private TextMeshProUGUI stageNameText;
    [Tooltip("「クリックでタイトルへ」などの案内（任意。入力を受け付けるようになってから表示）")]
    [SerializeField] private GameObject continuePrompt;

    [Header("戦績（どれも任意。空の欄は表示しない）")]
    [Tooltip("撃破数を表示するテキスト")]
    [SerializeField] private TextMeshProUGUI killCountText;
    [Tooltip("撃破数の書式（{0}に数値が入る）")]
    [SerializeField] private string killCountFormat = "{0}";
    [Tooltip("到達WAVEを表示するテキスト")]
    [SerializeField] private TextMeshProUGUI reachedWaveText;
    [Tooltip("到達WAVEの書式（{0}に数値が入る）")]
    [SerializeField] private string reachedWaveFormat = "Wave {0}";
    [Tooltip("プレイ時間を表示するテキスト")]
    [SerializeField] private TextMeshProUGUI playTimeText;
    [Tooltip("プレイ時間の書式（{0}に分、{1}に秒が入る。{1:00}で2桁表示）")]
    [SerializeField] private string playTimeFormat = "{0}:{1:00}";

    [Header("フェード")]
    [Tooltip("画面全体を覆う黒いパネル（FadePanel）")]
    [SerializeField] private Image fadePanel;
    [Tooltip("シーン開始時のフェードインにかける時間（秒）")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [Tooltip("シーン遷移前のフェードアウトにかける時間（秒）")]
    [SerializeField] private float fadeOutDuration = 0.5f;
    [Tooltip("フェードのイージング")]
    [SerializeField] private Easing.Ease fadeEase = Easing.Ease.Linear;

    [Header("入力")]
    [Tooltip("フェードイン後、入力を受け付けるまでの待ち時間（秒）\nステージで撃ち続けていたクリックで即スキップしないように")]
    [SerializeField] private float inputDelay = 1.0f;

    [Header("デバッグ")]
    [Tooltip("ResultSceneを単体で再生した時（結果が無い時）に表示する結果")]
    [SerializeField] private ResultType debugResultType = ResultType.Clear;
    [Tooltip("単体で再生した時に表示する撃破数（レイアウト確認用）")]
    [SerializeField] private int debugKillCount = 123;
    [Tooltip("単体で再生した時に表示する到達WAVE（レイアウト確認用）")]
    [SerializeField] private int debugReachedWave = 5;
    [Tooltip("単体で再生した時に表示するプレイ時間（秒）（レイアウト確認用）")]
    [SerializeField] private float debugPlayTime = 754f;

    // 現在の状態
    private ResultState state;
    // 表示する結果
    private ResultType resultType;
    // 実行中のフェード演出
    private Coroutine fadeCoroutine;


    void Start()
    {
        // リザルトはマウスで操作するのでカーソルを表示する
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // 結果を読み込む（無ければデバッグ用の結果を使う）
        resultType = GameResult.HasResult ? GameResult.Type : debugResultType;
        if (!GameResult.HasResult)
            Debug.LogWarning("GameResultに結果がありません。デバッグ用の結果（" + debugResultType + "）を表示します。");

        // 表示を切り替える
        ShowResult();
        ShowStats();
        if (continuePrompt != null) continuePrompt.SetActive(false);

        // フェードインが終わり、少し待ってから入力を受け付ける
        state = ResultState.Intro;
        Fade(1f, 0f, fadeInDuration, () => StartCoroutine(WaitInputDelay()));
    }


    void Update()
    {
        if (state != ResultState.Wait) return;

        if (IsDecidePressed())
        {
            PlaySE("Decision SE");
            LoadTitle();
        }
    }


    // 結果に合わせてクリア／ゲームオーバーのUIを切り替える
    void ShowResult()
    {
        if (clearUI    != null) clearUI.SetActive(resultType == ResultType.Clear);
        if (gameOverUI != null) gameOverUI.SetActive(resultType == ResultType.GameOver);

        if (stageNameText != null)
            stageNameText.text = GameResult.StageName;
    }


    // 戦績を表示する
    void ShowStats()
    {
        // 結果が無ければ（単体で再生した時）デバッグ用の値を使う
        bool hasResult = GameResult.HasResult;
        int   killCount   = hasResult ? GameResult.KillCount   : debugKillCount;
        int   reachedWave = hasResult ? GameResult.ReachedWave : debugReachedWave;
        float playTime    = hasResult ? GameResult.PlayTime    : debugPlayTime;

        // 撃破数
        if (killCountText != null)
            killCountText.text = string.Format(killCountFormat, killCount);

        // 到達WAVE
        if (reachedWaveText != null)
            reachedWaveText.text = string.Format(reachedWaveFormat, reachedWave);

        // プレイ時間（秒を分と秒に分ける。小数は切り捨て）
        if (playTimeText != null)
        {
            int totalSeconds = Mathf.FloorToInt(playTime);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            playTimeText.text = string.Format(playTimeFormat, minutes, seconds);
        }
    }


    // 入力受付までの待ち時間
    IEnumerator WaitInputDelay()
    {
        yield return new WaitForSeconds(inputDelay);

        state = ResultState.Wait;
        if (continuePrompt != null) continuePrompt.SetActive(true);
    }


    // タイトルへ戻る（画面が真っ暗になってからシーンを読み込む）
    void LoadTitle()
    {
        if (titleScene == null)
        {
            Debug.LogError("titleSceneにSceneReferenceが設定されていません。");
            return;
        }

        // 二重に遷移しないよう入力を止める
        state = ResultState.Loading;

        Fade(0f, 1f, fadeOutDuration, () =>
        {
            // 結果は使い終わったので消しておく
            GameResult.Clear();

            // GameManagerがあればそれを使い、無ければ直接読み込む（単体テスト用）
            if (GameManager.instance != null)
                GameManager.instance.LoadScene(titleScene);
            else
                SceneManager.LoadScene(titleScene.sceneName);
        });
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

        // 透明になったら、下のUIへのマウス操作をさえぎらないようにする
        if (toAlpha <= 0f)
            fadePanel.raycastTarget = false;

        fadeCoroutine = null;
        onComplete?.Invoke();
    }


    // 決定ボタン（左クリック / Space / Enter / Aボタン）
    bool IsDecidePressed()
    {
        return Input.GetMouseButtonDown(0)
            || Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.Joystick1Button0);
    }


    // SEを再生する（AudioManagerがなくても止まらないように）
    void PlaySE(string name)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySE(name);
    }
}
