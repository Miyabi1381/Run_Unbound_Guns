using UnityEditor;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
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


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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

        // スポーンカウントを足す
        currentSpawnCount++;
        spawnedCount++;
    }

}
