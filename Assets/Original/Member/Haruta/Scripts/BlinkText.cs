// ======================================================================================
// File: BlinkText.cs
//
// Brief: テキストや画像をゆっくり点滅させるクラス(「Press Space Key」用)
//        TextMeshProのテキストにもImageにもアタッチできる
//
// Author: Nagaya Haruta
// ======================================================================================
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class BlinkText : MonoBehaviour
{
    [Tooltip("点滅の速さ")]
    [SerializeField] float speed = 3f;
    [Tooltip("一番薄い時のアルファ値")]
    [SerializeField, Range(0f, 1f)] float minAlpha = 0.15f;
    [Tooltip("一番濃い時のアルファ値")]
    [SerializeField, Range(0f, 1f)] float maxAlpha = 1f;

    // 点滅させる対象(TMPのテキストもImageもGraphicを継承している)
    Graphic graphic;
    // 経過時間
    float timer;


    void Awake()
    {
        graphic = GetComponent<Graphic>();
    }


    // 表示されるたびに、一番濃い状態から点滅を始める
    void OnEnable()
    {
        timer = 0f;
    }


    void Update()
    {
        timer += Time.unscaledDeltaTime * speed;

        // Cosの値(-1～1)を0～1に変換して、アルファ値を滑らかに往復させる
        float rate = (Mathf.Cos(timer) + 1f) * 0.5f;

        Color color = graphic.color;
        color.a = Mathf.Lerp(minAlpha, maxAlpha, rate);
        graphic.color = color;
    }
}
