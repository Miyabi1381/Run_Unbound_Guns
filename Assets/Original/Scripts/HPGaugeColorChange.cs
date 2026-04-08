using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HPGaugeColorChange : MonoBehaviour
{
    [Tooltip("バーの2色")]
    public Color color_1, color_2;
    [Tooltip("たぶん、使用したい画像をアタッチするんだと思う")]
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
        float hp_ratio = hpScript.GetCurrentHP() / hpScript.GetMaxHP();

        image_HPgauge.color = Color.Lerp(color_2, color_1, hp_ratio);
        image_HPgauge.fillAmount = hp_ratio;
    }
}
