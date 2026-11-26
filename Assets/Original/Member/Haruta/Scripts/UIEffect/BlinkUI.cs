using System;
using UnityEngine;

// UIに付けると点滅させる汎用コンポーネント
// CanvasGroupの透明度を変えるので、Text・Image・子オブジェクトをまとめて点滅できる
[RequireComponent(typeof(CanvasGroup))]
public class BlinkUI : MonoBehaviour
{
    [Header("透明度")]
    [Tooltip("一番薄い時の透明度")]
    [Range(0f, 1f)] [SerializeField] private float minAlpha = 0f;
    [Tooltip("一番濃い時の透明度")]
    [Range(0f, 1f)] [SerializeField] private float maxAlpha = 1f;

    [Header("タイミング")]
    [Tooltip("点滅1回（濃い→薄い→濃い）にかける時間（秒）")]
    [SerializeField] private float blinkDuration = 1.0f;
    [Tooltip("表示されてから点滅を始めるまでの待ち時間（秒）")]
    [SerializeField] private float startDelay = 0f;
    [Tooltip("フェードのイージング")]
    [SerializeField] private Easing.Ease ease = Easing.Ease.InOutSine;
    [Tooltip("Time.timeScaleの影響を受けない（ポーズ中も点滅させたい時にオン）")]
    [SerializeField] private bool useUnscaledTime = false;

    // 透明度を変えるCanvasGroup
    private CanvasGroup canvasGroup;
    // イージング関数
    private Func<float, float> easeMethod;
    // 点滅を始めてからの経過時間
    private float time;


    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }


    // 表示されるたびに濃い状態から点滅をやり直す
    void OnEnable()
    {
        easeMethod = Easing.GetEasingMethod(ease);
        time = -startDelay;
        canvasGroup.alpha = maxAlpha;
    }


    // 非表示・コンポーネント無効化の時は濃い状態に戻しておく
    void OnDisable()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = maxAlpha;
    }


    void Update()
    {
        time += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // 待ち時間中は濃いまま
        if (time < 0f) return;

        // 0→1→0を繰り返す値を作り、イージングにかけて透明度にする
        // （t=0で濃い、t=1で薄い）
        float t = Mathf.PingPong(time * 2f / blinkDuration, 1f);
        canvasGroup.alpha = Mathf.LerpUnclamped(maxAlpha, minAlpha, easeMethod(t));
    }


    // インスペクターで値を変えた時に不正な値を防ぐ
    void OnValidate()
    {
        blinkDuration = Mathf.Max(0.01f, blinkDuration);
        startDelay    = Mathf.Max(0f, startDelay);

        // プレイ中にイージングを変えても反映されるようにする
        easeMethod = Easing.GetEasingMethod(ease);
    }
}
