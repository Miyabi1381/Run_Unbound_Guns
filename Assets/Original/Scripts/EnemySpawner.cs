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
            if (killCount == spawnUpperLimit)
            {
                clearTxt.SetActive(true);
            }

            // スポーン上限に満たしていないならスポーンさせる
            if ((killCount < spawnUpperLimit)
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

        // 敵が倒されたらスポーンカウントを減らす関数（ラムダ式）
        if (hp != null)
        {
            hp.onDeath += () =>
            {
                currentSpawnCount--;
                killCount++;
                UIUpdate();
            };

        }

        // スポーンカウントを足す+テキストを更新
        currentSpawnCount++;
    }


    // スポーンさせる位置をランダムに取得
    public Vector3 GetSpawnPos()
    {
        float angle = UnityEngine.Random.Range(0f, 360f);                                   // プレイヤーを中心にスポーンする方向(角度)
        float distance   = UnityEngine.Random.Range(nearSpawnDistance, farSpawnDistance);   // プレイヤーを中心にスポーンする範囲(距離)

        // 情報を[角度]と[距離]から[座標]に変換する
        float x = Mathf.Cos(angle * Mathf.Deg2Rad) * distance;
        float z = Mathf.Sin(angle * Mathf.Deg2Rad) * distance;
        return new Vector3(
            player.position.x + x,
            enemyPrefab.transform.position.y,
            player.position.z + z
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
