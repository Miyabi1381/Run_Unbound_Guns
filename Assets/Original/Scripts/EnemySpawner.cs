using System;
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
    private int spawnUpperLimit;
    [Tooltip("一度にスポーンできる上限")]
    [SerializeField] private int spawnLimit;
    [Tooltip("これまでにスポーンした総数")]  // カウントのタイミングが違うので以下2種を用意
    private int spawnCount;
    [Tooltip("これまでにキルした総数")]
    private int killCount;
    [Tooltip("現在スポーンしている数")]
    private int currentSpawnCount;
    [Tooltip("スポーンする間隔")]
    private float spawnInterval = 1;
    [Tooltip("スポーン間隔を測るタイマー")]
    private float spawnTimer;

    // ウェーブ
    [Tooltip("ウェーブごとの出現上限")]
    [SerializeField] private int waveSpawnUpperLimit;
    [Tooltip("ウェーブごとの出現カウント")]
    private int waveSpawnCount;
    [Tooltip("ウェーブのカウンター")]
    private int waveCounter;
    [Tooltip("ウェーブ内でキルした数")]
    private int wavekillCount;
    [Tooltip("ウェーブ上限")]
    [SerializeField] private int waveUpperCounter;
    [Tooltip("ウェーブごとに追加する敵の数を指定")]
    [SerializeField] private int enemyIncreasePerWave;
    [Tooltip("次のウェーブの開始時間")]
    [SerializeField] private float startToNextWaveTime;
    [Tooltip("次のウェーブまでの待機時間")]
    private float nextWaveTimer;
    [Tooltip("ウェーブのレベルを跳ね上げるタイミング")]
    [SerializeField] private int levelUpWave;


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

    // クリア判定
    [Tooltip("ゲームクリアしたかどうか、またその後のアクション通知")]
    [HideInInspector] public event Action onAllEnemiesKilled;
    [Tooltip("ゲームクリア判定のフラグ")]
    [HideInInspector] private bool isGameClear;
    [Tooltip("ウェーブクリア判定のフラグ")]
    [HideInInspector] private bool isWaveClear;


    // UI
    [Tooltip("UI（敵の数）のテキストオブジェクト")]
    GameObject enemyTxt;
    [Tooltip("UI（ウェーブ数）のテキストオブジェクト")]
    GameObject waveTxt;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // プレイヤーのキャッシュを一度だけ取得
        player = PlayerMovementScript.instance.transform;

        // フラグを初期化
        isGameClear = false;
        isWaveClear = false;

        // 変数とテキストを結び付けてクリア以外画面に表示する
        enemyTxt = GameObject.Find("EnemyCounts");
        waveTxt  = GameObject.Find("WaveCounts");
        enemyTxt.SetActive(true);
        waveTxt.SetActive(true);

        // 各数値を初期化してテキストを更新
        spawnUpperLimit = waveSpawnUpperLimit;  // ウェーブのリミットを全体の上限値に代入
        waveCounter++;                          // ウェーブカウンターを1にセット
        UIUpdate();

        // 敵をスポーンさせる
        InstantiateEnemy();
    }


    // Update is called once per frame
    void Update()
    {
        // タイマーがスポーン間隔を満たしたらスポーンさせる
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            // タイマーをリセットする
            spawnTimer -= spawnInterval;

            // ウェーブ数が指定数値を満たし、スポーン上限に達したならクリア表示をする
            if ((!isGameClear)                          // 既にクリアしたか
                && (waveCounter >= waveUpperCounter)    // 最終ウェーブに到達したか
                && (killCount >= spawnUpperLimit))      // キルカウントがスポーン上限に達したか
            {
                isGameClear = true;

                Debug.Log("クリア通知を送信した");
                // GameSceneManagerにクリア通達を送る
                onAllEnemiesKilled?.Invoke();
            }

            // スポーン上限に満たしていないならスポーンさせる
            if ((spawnCount < spawnUpperLimit)              // スポーンカウントがスポーン上限に達したか
                && (currentSpawnCount < spawnLimit)         // 現在のスポーンカウントが同時スポーン上限に達したか
                && (waveSpawnCount < waveSpawnUpperLimit))  // ウェーブごとのスポーン上限に到達したか
            {
                InstantiateEnemy();
            }

        }
            // ウェーブ処理をする
            Wave();
    }


    // ウェーブ処理
    void Wave()
    {
        // 敵を倒し終わった時に、ウェーブが最終値に到達していなければウェーブクリアとする。
        // ウェーブをクリアしたときに、上がり幅分敵を追加して次のウェーブに移行する
        if (wavekillCount >= waveSpawnUpperLimit)
        {
            // ウェーブクリアフラグチェック
            if (!isWaveClear)
            {
                // ウェーブクリアの表示をする
                Debug.Log("ウェーブ " + waveCounter + " をクリア！次に備えましょう！");
                isWaveClear = true;

                // クリアウェーブが最後なら抜ける
                if (waveCounter == waveUpperCounter) return;
            }

            // ウェーブ間の待機時間が過ぎたら次のウェーブに移行する
            nextWaveTimer += Time.deltaTime;
            if (nextWaveTimer >= startToNextWaveTime)
            {
                // 前ウェーブの情報をリセット
                waveSpawnCount = 0;                     // ウェーブごとのスポーンカウンター
                isWaveClear = false;                    // ウェーブのクリア判定
                wavekillCount = 0;                      // キルカウントのリセット
                nextWaveTimer -= startToNextWaveTime;   // ウェーブ間の待機タイマー
                
                // ウェーブを加算
                waveCounter++;

                // ウェーブごとの敵数の加算率を計算し、次のウェーブの値に足す
                waveSpawnUpperLimit += CalculateWavesAddEnemyCount();

                // ゲーム全体のクリア条件値も加算する
                spawnUpperLimit += waveSpawnUpperLimit;

                // UIを更新
                UIUpdate();

                Debug.Log("ウェーブ " + waveCounter + " が開始しました！");
                
            }

        }
    }


    // ウェーブの加算値を計算する関数
    public int CalculateWavesAddEnemyCount()
    {
        // ウェーブ数で徐々に敵を増加
        return waveCounter * enemyIncreasePerWave + (waveCounter / levelUpWave);
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
                wavekillCount++;
                UIUpdate();
            };

        }

        // スポーンカウントを足す
        currentSpawnCount++;
        spawnCount++;
        waveSpawnCount++;
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


    // UIを更新する
    private void UIUpdate()
    {
        // ウェーブごとのスポーン上限と、現在のウェーブを表示
        enemyTxt.GetComponent<TextMeshProUGUI>().text = (waveSpawnUpperLimit - wavekillCount) + "/" + waveSpawnUpperLimit;
        waveTxt.GetComponent<TextMeshProUGUI>().text  = "Wave " + waveCounter;
    }
}
