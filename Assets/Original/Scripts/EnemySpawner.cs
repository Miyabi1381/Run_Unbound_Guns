using System;
using UnityEditor;
using UnityEngine;
using TMPro;

public class EnemySpawner : MonoBehaviour
{
    // プレイヤー
    [Tooltip("プレイヤーの情報")]
    Transform player;

    // 敵
    [Tooltip("敵プレファブを登録")]
    public EnemyController enemyPrefab;

    // スポナーのカウント
    [Tooltip("ステージクリア条件である1ステージのスポーン上限")]
    public int spawnUpperLimit;
    [Tooltip("一度にスポーンできる上限")]
    public int spawnLimit;
    [Tooltip("これまでにスポーンした総数")]
    [HideInInspector] public int spawnCount;
    [Tooltip("これまでにキルした総数")]
    [HideInInspector] public int killCount;
    [Tooltip("現在スポーンしている数")]
    [HideInInspector] public int currentSpawnCount;
    [Tooltip("スポーンする間隔")]
    public float spawnInterval = 1;
    [Tooltip("スポーン間隔を測るタイマー")]
    [HideInInspector] public float timer;

    // スポナーの範囲
    [Tooltip("スポーンできる最短距離")]
    public float nearSpawnDistance = 20;
    [Tooltip("スポーンできる最長距離")]
    public float farSpawnDistance  = 40;
    [Tooltip("ステージ上でスポーンすることのできる範囲(最小X)")]
    public float stageMinX = -50;
    [Tooltip("ステージ上でスポーンすることのできる範囲(最大X)")]
    public float stageMaxX =  50;
    [Tooltip("ステージ上でスポーンすることのできる範囲(最小Z)")]
    public float stageMinZ = -50;
    [Tooltip("ステージ上でスポーンすることのできる範囲(最大Z)")]
    public float stageMaxZ =  50;

    // UI
    [Tooltip("UI（敵の数）のテキストオブジェクト")]
    GameObject enemyTxt;
    [Tooltip("UI（ウェーブ数）のテキストオブジェクト")]
    GameObject waveTxt;
    [Tooltip("GameClearのテキストオブジェクト")]
    GameObject clearTxt;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // プレイヤーのキャッシュを一度だけ取得
        player = PlayerMovementScript.instance.transform;

        // 変数とテキストを結び付けてクリア以外画面に表示する+テキストを更新
        enemyTxt = GameObject.Find("EnemyCounts");
        waveTxt  = GameObject.Find("WaveCounts");
        clearTxt = GameObject.Find("GameClear");
        enemyTxt.SetActive(true);
        waveTxt.SetActive(true);
        clearTxt.SetActive(false);
        UIUpdate();

        // 敵をスポーンさせる
        InstantiateEnemy();
    }


    // Update is called once per frame
    void Update()
    {
        // タイマーがスポーン間隔を満たしたらスポーンさせる
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            // タイマーをリセットする
            timer = 0;

            // スポーン上限に達したならクリア表示をする
            if (killCount >= spawnUpperLimit)
            {
                clearTxt.SetActive(true);
            }

            // スポーン上限に満たしていないならスポーンさせる
            if ((spawnCount < spawnUpperLimit)
                && (currentSpawnCount < spawnLimit))
            {
                InstantiateEnemy();
            }

        }
    }


    // 敵を生成する
    private void InstantiateEnemy()
    {
        // スポーンする位置を取得
        Vector3 spawnPos = GetSpawnPos();

        // 敵をスポーンさせる
        EnemyController enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        // アタッチされたHPスクリプトを取得する
        HPScript hp = enemy.GetComponent<HPScript>();

        // 敵が倒されたらカウント+UIの更新をする関数（ラムダ式）
        if (hp != null)
        {
            hp.onDeath += () =>
            {
                currentSpawnCount--;
                killCount++;
                UIUpdate();
            };

        }

        // スポーンカウントを足す
        currentSpawnCount++;
        spawnCount++;
    }


    // スポーンさせる位置をランダムに取得
    public Vector3 GetSpawnPos()
    {
        // スポーン可能なX/Zの範囲をステージ内で制限 (memo: MaxとMinが逆なのは、ステージの端と生成円の端のより内側にあるもの(大小)を優先するから。)
        float minX = Mathf.Max(stageMinX, player.position.x - farSpawnDistance);
        float maxX = Mathf.Min(stageMaxX, player.position.x + farSpawnDistance);
        float minZ = Mathf.Max(stageMinZ, player.position.z - farSpawnDistance);
        float maxZ = Mathf.Min(stageMaxZ, player.position.z + farSpawnDistance);

        // プレイヤーを中心にスポーンする情報を取得
        float angle    = UnityEngine.Random.Range(0f, 360f);                              ///< 角度
        float distance = UnityEngine.Random.Range(nearSpawnDistance, farSpawnDistance);   ///< 距離

        // 情報を[角度]と[距離]から[座標]に変換する
        float x = player.position.x + Mathf.Cos(angle * Mathf.Deg2Rad) * distance;
        float z = player.position.z + Mathf.Sin(angle * Mathf.Deg2Rad) * distance;

        // 割り出した座標をステージ内に収める
        x = Mathf.Clamp(x, minX, maxX);
        z = Mathf.Clamp(z, minZ, maxZ);

        return new Vector3(
            x,
            enemyPrefab.transform.position.y,
            z
            );
    }


    // スポーンできる範囲かどうかを判断する(ステージ上かどうか)
    public bool IsSpawn()
    {
        return true;
    }


    // UIを更新する
    private void UIUpdate()
    {
        enemyTxt.GetComponent<TextMeshProUGUI>().text = (spawnUpperLimit - killCount) + "/" + spawnUpperLimit;
        waveTxt.GetComponent<TextMeshProUGUI>().text  = "Wave " + "1";
    }
}
