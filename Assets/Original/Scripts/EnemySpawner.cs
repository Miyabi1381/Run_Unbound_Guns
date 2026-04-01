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
    [Tooltip("敵プレファブを登録")]
    public EnemyController enemyPrefab;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // スポーン上限に満たしていないならスポーンさせる
        if (spawnedCount < spawnUpperLimit)
        {
            if (currentSpawnCount < spawnLimit)
            {
                InstantiateEnemy();
            }

        }
    }

    private void InstantiateEnemy()
    {
        // 敵をスポーンさせて、スポーンカウントする
        Instantiate(enemyPrefab);
        currentSpawnCount++;
        spawnedCount++;
    }

}
