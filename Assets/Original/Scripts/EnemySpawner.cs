using UnityEditor;
using UnityEngine;
using TMPro;

public class EnemySpawner : MonoBehaviour
{
    // エネミースポナー
    [Tooltip("ステージクリア条件である1ステージのスポーン上限")]
    public int spawnUpperLimit;
    [Tooltip("一度にスポーンできる上限")]
    public int spawnLimit;
    [Tooltip("これまでにスポーンした総数")]
    [HideInInspector] public int spawnedCount;
    [Tooltip("現在スポーンしている数")]
    [HideInInspector] public int currentSpawnCount;
    [Tooltip("スポーンする間隔")]
    public float spawnInterval = 1;
    [Tooltip("スポーン間隔を測るタイマー")]
    [HideInInspector] public float timer;
    [Tooltip("敵プレファブを登録")]
    public EnemyController enemyPrefab;

    // UI
    [Tooltip("UI（敵の数）のオブジェクト")]
    GameObject enemyTxt;
    [Tooltip("UI（ウェーブ数）のオブジェクト")]
    GameObject waveTxt;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 変数とテキストを結び付けて画面に表示する+テキストを更新
        enemyTxt = GameObject.Find("EnemyCounts");
        waveTxt  = GameObject.Find("WaveCounts");
        enemyTxt.SetActive(true);
        waveTxt.SetActive(true);
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

            // スポーン上限に満たしていないならスポーンさせる
            if ((spawnedCount < spawnUpperLimit)
                && (currentSpawnCount < spawnLimit))
            {
                InstantiateEnemy();
            }

        }
    }


    // 敵を生成する
    private void InstantiateEnemy()
    {
        // 敵をスポーンさせる
        EnemyController enemy = Instantiate(enemyPrefab);

        // アタッチされたHPスクリプトを取得する
        HPScript hp = enemy.GetComponent<HPScript>();

        // 敵が倒されたらスポーンカウントを減らす関数（ラムダ式）
        if (hp != null)
        {
            hp.onDeath += () =>
            {
                currentSpawnCount--;
            };

        }

        // スポーンカウントを足す+テキストを更新
        currentSpawnCount++;
        spawnedCount++;
        UIUpdate();
    }


    // UIを更新する
    private void UIUpdate()
    {
        enemyTxt.GetComponent<TextMeshProUGUI>().text = (spawnUpperLimit - spawnedCount + 1) + "/" + spawnUpperLimit;
        waveTxt.GetComponent<TextMeshProUGUI>().text = "1";
    }
}
