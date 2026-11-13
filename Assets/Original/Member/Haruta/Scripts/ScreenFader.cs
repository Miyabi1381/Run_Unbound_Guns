// ======================================================================================
// File: ScreenFader.cs
//
// Brief: 画面全体を覆うImageのアルファ値を変えてフェードイン/アウトさせるクラス
//        Canvasの一番下(最前面)に置いた、画面いっぱいの黒いImageにアタッチして使う
//
// Author: Nagaya Haruta
// ======================================================================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ScreenFader : MonoBehaviour
{
    // フェードに使うImage
    Image fadeImage;

    // Imageを取得する(Awakeより先に呼ばれても動くように、使う時に取得する)
    Image FadeImage
    {
        get
        {
            if (fadeImage == null) fadeImage = GetComponent<Image>();
            return fadeImage;
        }
    }


    // アルファ値を直接設定する(0: 透明 ～ 1: 真っ暗)
    public void SetAlpha(float alpha)
    {
        Color color = FadeImage.color;
        color.a = Mathf.Clamp01(alpha);
        FadeImage.color = color;
    }


    // フェードイン処理(真っ暗 → 画面が見える)
    public IEnumerator FadeIn(float duration)
    {
        yield return Fade(1f, 0f, duration);
    }


    // フェードアウト処理(画面が見える → 真っ暗)
    public IEnumerator FadeOut(float duration)
    {
        yield return Fade(0f, 1f, duration);
    }


    // 指定秒かけてアルファ値を変化させる
    IEnumerator Fade(float from, float to, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            // timeScaleが0でも動くようにunscaledDeltaTimeを使う
            time += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, time / duration));
            yield return null;
        }

        SetAlpha(to);
    }
}
