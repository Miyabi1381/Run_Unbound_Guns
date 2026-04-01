using UnityEditor;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Tooltip("ステージクリア条件である1ステージのスポーン上限")]
    public int spawnUpperLimit;
    [Tooltip("未設定")]
    public int a;
    [Tooltip("敵プレファブ")]
    public EnemyController enemyPrefab;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Instantiate(enemyPrefab);
    }
}
