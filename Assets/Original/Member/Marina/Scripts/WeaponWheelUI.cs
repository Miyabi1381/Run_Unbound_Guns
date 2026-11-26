using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

// 武器アイコンの配置用のスクリプトファイル

public class WeaponWheelUI : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("アイコンを並べる親(円の中心になるRectTransform)")]
    [SerializeField] private RectTransform center;
    [Tooltip("アイコン一枚分のImageプレハブ")]
    [SerializeField] private Image iconPrefab;

    [Header("配置")]
    [SerializeField] private float radiusX = 120.0f;  // 横半径
    [SerializeField] private float radiusY = 40f;     // 縦半径
    [SerializeField] private float rotateTime = 0.25f; // 公転にかかる時間
    [SerializeField] private float selectedScale = 1.2f; // 選択時の大きさ
    [SerializeField] private float unselectedScale = 0.7f; // 非選択時の大きさ
    [SerializeField][Range(0f, 1f)] private float unselectedAlpha = 0.5f; // 非選択時のアルファ

    private readonly List<Image> icons = new List<Image>(); //　アイコンのリスト
    private int count;
    private int selectedIndex;
    private float currentAngle;
    private float targetAngle;
    private float angleVelocity;

    /// <summary>GunInventory から初期化時に呼ぶ</summary>
    public void Setup(Texture[] textures)
    {
        // 既存を破棄
        foreach (var i in icons) if (i) Destroy(i.gameObject);
        icons.Clear();

        count = textures.Length;
        for (int i = 0; i < count; i++)
        {
            Image img = Instantiate(iconPrefab, center);
            img.gameObject.SetActive(true);
            // TextureをSpriteに変換
            Texture2D tex = textures[i] as Texture2D;
            if (tex == null) Debug.LogWarning("icons[" + i + "] が Texture2D ではありません: " + textures[i]);
            if (tex != null)
            {
                img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            img.raycastTarget = false;
            icons.Add(img);
        }
        SelectImmediate(0);
    }

    /// <summary>持ち替え時に呼ぶ。公転して選択を切り替える</summary>
    public void Select(int index)
    {
        if (count == 0) return;
        selectedIndex = index;
        float step = 360f / count;

        // 現在角から見て最短の回転方向になるよう調整
        float desired = -step * index;
        targetAngle = currentAngle + Mathf.DeltaAngle(currentAngle, desired);
    }

    public void SelectImmediate(int index)
    {
        selectedIndex = index;
        currentAngle = targetAngle = -(360f / Mathf.Max(1, count)) * index;
        Layout();
    }

    // Update is called once per frame
    void Update()
    {
        currentAngle = Mathf.SmoothDamp(currentAngle, targetAngle, ref angleVelocity, rotateTime);
        Layout();
    }

    private void Layout()
    {
        if (count == 0) return;
        float step = 360f / count;

        for(int i = 0; i < count; i++)
        {
            // 0度 = 真下(手前)。i番目の角度に全体の回転を足す
            float deg = step * i + currentAngle;
            float rad = deg * Mathf.Deg2Rad;

            float x = Mathf.Sin(rad) * radiusX;
            float y = Mathf.Cos(rad) * radiusY;

            RectTransform rt = icons[i].rectTransform;
            rt.anchoredPosition = new Vector2(x, y);

            // 手前(0度に近い)ほど大きく濃く
            float closeness = (Mathf.Cos(rad) + 1f) * 0.5f;   // 手前=1, 奥=0
            float scale = Mathf.Lerp(unselectedScale, selectedScale, closeness);
            rt.localScale = Vector3.one * scale;

            Color c = icons[i].color;
            c.a = Mathf.Lerp(unselectedAlpha, 1f, closeness);
            icons[i].color = c;

            // 手前のアイコンを前面に描く
            rt.SetSiblingIndex(Mathf.RoundToInt(closeness * (count - 1)));
        }

    }
}
