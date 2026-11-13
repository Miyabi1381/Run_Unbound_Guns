// ======================================================================================
// File: ScreenShatter.cs
//
// Brief: 画面がガラスのように割れて崩れ落ちる演出クラス
//        1. 今の画面をキャプチャする
//        2. 撃たれた位置を中心に、放射状のひびで画面を破片に分割する
//        3. 破片を飛び散らせて落とす(破片の向こうに次の画面が見える)
//        Canvas内の、画面いっぱいにストレッチした空オブジェクトにアタッチして使う
//
// Author: Nagaya Haruta
// ======================================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class ScreenShatter : MonoBehaviour
{
    // 破片1枚分のデータ
    class Shard
    {
        public RectTransform rect;      // 破片のRectTransform
        public ShardGraphic graphic;    // 破片の描画
        public Vector2 startPos;        // 落下開始位置
        public Vector2 velocity;        // 飛び散る速度
        public float spin;              // 回転速度(度/秒)
        public float delay;             // 落ち始めるまでの待ち時間
    }


    [Header("割れ方")]
    [Tooltip("放射状のひびの本数(画面の四隅へ向かう4本に追加する数)")]
    [SerializeField] int extraRayCount = 12;
    [Tooltip("同心円状のひびを入れる位置(着弾点から画面端までを1とした割合。小さい順に並べる)")]
    [SerializeField] float[] ringRates = { 0.12f, 0.35f, 0.65f };
    [Tooltip("ひびの位置のばらつき")]
    [SerializeField, Range(0f, 0.25f)] float jitter = 0.2f;

    [Header("動き")]
    [Tooltip("ひびが入ってから崩れ始めるまでの時間(秒)")]
    [SerializeField] float crackHoldTime = 0.15f;
    [Tooltip("ひびの隙間の幅")]
    [SerializeField] float crackGap = 6f;
    [Tooltip("破片が落ちきるまでの時間(秒)")]
    [SerializeField] float fallTime = 1.0f;
    [Tooltip("着弾点から遠い破片ほど遅れて落ち始める、その最大の遅れ(秒)")]
    [SerializeField] float maxDelay = 0.2f;
    [Tooltip("破片が飛び散る速さ")]
    [SerializeField] float burstSpeed = 700f;
    [Tooltip("重力の強さ")]
    [SerializeField] float gravity = 3000f;
    [Tooltip("破片の回転速度の最大値(度/秒)")]
    [SerializeField] float maxSpin = 240f;


    // 画面を割る演出を再生する
    //   screenPoint : 割れる中心(スクリーン座標。Input.mousePositionをそのまま渡せる)
    //   uiCamera    : Canvasが使っているカメラ(Screen Space - Overlayならnull)
    //   onCaptured  : 画面をキャプチャした直後に呼ばれる処理(ここで裏の画面を切り替える)
    public IEnumerator Play(Vector2 screenPoint, Camera uiCamera, Action onCaptured)
    {
        // このフレームの描画が終わるまで待ってから、画面をテクスチャに写し取る
        yield return new WaitForEndOfFrame();

        Texture2D screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenTexture.Apply();

        // 裏の画面を切り替えてもらう(この後すぐ破片で覆うので、切り替わった瞬間は見えない)
        if (onCaptured != null) onCaptured();

        // 着弾点を、このオブジェクト基準の座標に変換する
        RectTransform root = (RectTransform)transform;
        Rect area = root.rect;

        Vector2 impact;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPoint, uiCamera, out impact);
        impact.x = Mathf.Clamp(impact.x, area.xMin + 1f, area.xMax - 1f);
        impact.y = Mathf.Clamp(impact.y, area.yMin + 1f, area.yMax - 1f);

        // 破片を生成する(生成した時点で少し隙間が空き、ひびに見える)
        List<Shard> shards = CreateShards(screenTexture, area, impact);

        // ひびが入った状態で一瞬止める
        float time = 0f;
        while (time < crackHoldTime)
        {
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        // 破片を飛び散らせて落とす
        time = 0f;
        float totalTime = fallTime + maxDelay;

        while (time < totalTime)
        {
            time += Time.unscaledDeltaTime;

            foreach (Shard shard in shards)
            {
                // 破片ごとの経過時間(まだ待ち時間中なら動かさない)
                float t = time - shard.delay;
                if (t <= 0f) continue;

                // 位置 = 開始位置 + 速度×時間 + 重力による落下(1/2・g・t²)
                Vector2 pos = shard.startPos + shard.velocity * t + Vector2.down * (0.5f * gravity * t * t);
                shard.rect.localPosition = pos;
                shard.rect.localRotation = Quaternion.Euler(0f, 0f, shard.spin * t);

                // 後半で少しずつ透明にする
                float rate = Mathf.Clamp01(t / fallTime);
                Color color = shard.graphic.color;
                color.a = 1f - Mathf.Clamp01((rate - 0.5f) / 0.5f);
                shard.graphic.color = color;
            }

            yield return null;
        }

        // 後片付け(破片とキャプチャしたテクスチャを消す)
        foreach (Shard shard in shards)
        {
            Destroy(shard.rect.gameObject);
        }
        Destroy(screenTexture);
    }


    // 着弾点を中心に、画面全体を破片に分割して生成する
    List<Shard> CreateShards(Texture texture, Rect area, Vector2 impact)
    {
        // --- 放射状のひびの角度を決める ---
        List<float> angles = new List<float>();

        // 画面の四隅へ向かうひびは必ず入れる(入れないと、隅が破片に含まれず欠けてしまう)
        Vector2[] corners =
        {
            new Vector2(area.xMin, area.yMin),
            new Vector2(area.xMin, area.yMax),
            new Vector2(area.xMax, area.yMax),
            new Vector2(area.xMax, area.yMin),
        };
        foreach (Vector2 corner in corners)
        {
            angles.Add(Mathf.Atan2(corner.y - impact.y, corner.x - impact.x));
        }

        // 残りは、1周を等分した範囲の中でランダムにずらして決める
        for (int i = 0; i < extraRayCount; i++)
        {
            float rate = (i + UnityEngine.Random.value) / extraRayCount;
            angles.Add(rate * Mathf.PI * 2f - Mathf.PI);
        }

        angles.Sort();

        // --- ひび同士の交点(破片の頂点)を計算する ---
        int rayCount  = angles.Count;
        int ringCount = ringRates.Length + 1;   // 最後の1つは画面端
        Vector2[,] points = new Vector2[rayCount, ringCount];

        for (int i = 0; i < rayCount; i++)
        {
            Vector2 dir  = new Vector2(Mathf.Cos(angles[i]), Mathf.Sin(angles[i]));
            float length = DistanceToBorder(impact, dir, area);

            for (int j = 0; j < ringCount; j++)
            {
                // 一番外側は画面端ぴったり、それ以外は少しばらつかせてギザギザにする
                float rate = 1f;
                if (j < ringCount - 1)
                {
                    rate = ringRates[j] * (1f + UnityEngine.Random.Range(-jitter, jitter));
                    rate = Mathf.Clamp(rate, 0.02f, 0.95f);
                }

                points[i, j] = impact + dir * (length * rate);
            }
        }

        // --- 隣り合うひびに囲まれた部分を、破片として生成する ---
        List<Shard> shards = new List<Shard>();
        float maxDistance = new Vector2(area.width, area.height).magnitude;

        for (int i = 0; i < rayCount; i++)
        {
            int next = (i + 1) % rayCount;

            // 着弾点に接する、一番内側の三角形
            shards.Add(CreateShard(texture, area, impact, maxDistance,
                new Vector2[] { impact, points[i, 0], points[next, 0] }));

            // その外側の四角形
            for (int j = 0; j < ringCount - 1; j++)
            {
                shards.Add(CreateShard(texture, area, impact, maxDistance,
                    new Vector2[] { points[i, j], points[i, j + 1], points[next, j + 1], points[next, j] }));
            }
        }

        return shards;
    }


    // 破片を1枚生成する
    Shard CreateShard(Texture texture, Rect area, Vector2 impact, float maxDistance, Vector2[] polygon)
    {
        // 破片の中心(頂点の平均)
        Vector2 center = Vector2.zero;
        foreach (Vector2 point in polygon) center += point;
        center /= polygon.Length;

        // 頂点を「中心からの相対位置」に直し、キャプチャ画像のどこを貼るか(UV)も計算する
        Vector2[] vertices = new Vector2[polygon.Length];
        Vector2[] uvs      = new Vector2[polygon.Length];

        for (int i = 0; i < polygon.Length; i++)
        {
            vertices[i] = polygon[i] - center;
            uvs[i] = new Vector2(
                (polygon[i].x - area.xMin) / area.width,
                (polygon[i].y - area.yMin) / area.height);
        }

        // オブジェクトを生成する
        GameObject obj = new GameObject("Shard", typeof(RectTransform), typeof(CanvasRenderer));
        obj.layer = gameObject.layer;
        RectTransform rect = (RectTransform)obj.transform;
        rect.SetParent(transform, false);

        // RectTransformを、破片全体が収まる大きさにする
        // (中心を軸に回転させたいので、中心から一番遠い頂点までの距離の2倍を1辺とする)
        float radius = 0f;
        foreach (Vector2 vertex in vertices) radius = Mathf.Max(radius, vertex.magnitude);
        rect.sizeDelta = Vector2.one * (radius * 2f);

        ShardGraphic graphic = obj.AddComponent<ShardGraphic>();
        graphic.raycastTarget = false;
        graphic.Setup(texture, vertices, uvs);

        // 着弾点から外側へ向かう方向と距離
        Vector2 offset   = center - impact;
        float distance   = offset.magnitude;
        Vector2 outward  = (distance > 0.001f) ? offset / distance : Vector2.up;

        Shard shard = new Shard();
        shard.rect     = rect;
        shard.graphic  = graphic;
        // 少し外側にずらして置くことで、破片同士に隙間ができてひびに見える
        shard.startPos = center + outward * crackGap * UnityEngine.Random.Range(0.5f, 1.5f);
        shard.velocity = outward * burstSpeed * UnityEngine.Random.Range(0.4f, 1.2f)
                       + Vector2.up * UnityEngine.Random.Range(0f, 300f);
        shard.spin     = UnityEngine.Random.Range(-maxSpin, maxSpin);
        // 着弾点に近い破片から順に崩れる
        shard.delay    = (distance / maxDistance) * maxDelay;

        rect.localPosition = shard.startPos;

        return shard;
    }


    // 点pからdir方向に進んだ時、画面端にぶつかるまでの距離を求める
    float DistanceToBorder(Vector2 p, Vector2 dir, Rect area)
    {
        float distX = float.MaxValue;
        float distY = float.MaxValue;

        if (dir.x > 0.0001f)       distX = (area.xMax - p.x) / dir.x;
        else if (dir.x < -0.0001f) distX = (area.xMin - p.x) / dir.x;

        if (dir.y > 0.0001f)       distY = (area.yMax - p.y) / dir.y;
        else if (dir.y < -0.0001f) distY = (area.yMin - p.y) / dir.y;

        return Mathf.Min(distX, distY);
    }
}
