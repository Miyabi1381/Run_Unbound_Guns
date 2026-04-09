using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HPGaugeColorChange : MonoBehaviour
{
    [Tooltip("バーの2色")]
    public Color color_1, color_2;
    [Tooltip("HPバー画像(白推奨)")]
    private Image image_HPgauge;
    [Tooltip("HPスクリプト")]
    [SerializeField] HPScript hpScript;

    // デバッグ
    [Range(0, 100)] public float hp;


    // Start is called before the first frame update
    void Start()
    {
        image_HPgauge = gameObject.GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        // スライシングを防ぐためfloat型にする
        float hp_ratio = (float)hpScript.GetCurrentHP() / hpScript.GetMaxHP();

        // ゲージを制御
        image_HPgauge.color = Color.Lerp(color_2, color_1, hp_ratio);
        image_HPgauge.fillAmount = hp_ratio;
    }
}
