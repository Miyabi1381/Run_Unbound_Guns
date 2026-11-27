using UnityEngine;
using UnityEngine.SceneManagement;

// 各ステージに1つ置く
// クリア・ゲームオーバーの通知を受け取ったら、結果をGameResultに記録してゲームを停止する
// （ResultSceneへの遷移そのものはGameSceneManagerが行う）
public class StageResultReporter : MonoBehaviour
{
    [Header("通知元（空ならシーンから自動で探す）")]
    [Tooltip("クリア通知を出す敵スポナー")]
    [SerializeField] private EnemySpawner enemySpawner;
    [Tooltip("ゲームオーバー通知を出すプレイヤーのHPスクリプト")]
    [SerializeField] private HPScript playerHP;

    [Header("停止処理")]
    [Tooltip("敵を探す時のタグ")]
    [SerializeField] private string enemyTag = "Enemy";

    // 既に結果が出たか（クリア後に死亡した時などに二重で処理しないように）
    private bool isFinished;


    void Start()
    {
        // 未設定なら自動で探す
        // （PlayerMovementScript.instanceはAwakeで入るので、OnEnableではなくStartで探す）
        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<EnemySpawner>();

        if (playerHP == null && PlayerMovementScript.instance != null)
            playerHP = PlayerMovementScript.instance.GetComponent<HPScript>();

        // 通知を受け取れるようにする
        if (enemySpawner != null)
            enemySpawner.onAllEnemiesKilled += HandleClear;
        else
            Debug.LogError("EnemySpawnerが見つかりません。クリア判定を受け取れません。");

        if (playerHP != null)
            playerHP.onDeadPlayer += HandleGameOver;
        else
            Debug.LogError("プレイヤーのHPScriptが見つかりません。ゲームオーバー判定を受け取れません。");
    }


    void OnDestroy()
    {
        // シーンを抜ける時に通知の受け取りを解除する
        if (enemySpawner != null) enemySpawner.onAllEnemiesKilled -= HandleClear;
        if (playerHP     != null) playerHP.onDeadPlayer       -= HandleGameOver;
    }


    // クリア通知を受け取った時
    void HandleClear()
    {
        Finish(ResultType.Clear);
    }


    // ゲームオーバー通知を受け取った時
    void HandleGameOver()
    {
        Finish(ResultType.GameOver);
    }


    // 結果を記録してゲームを止める
    void Finish(ResultType type)
    {
        // 最初に出た結果だけを採用する
        if (isFinished) return;
        isFinished = true;

        // 結果を記録
        GameResult.Set(type, SceneManager.GetActiveScene().name);
        // ★スコアを追加する時は、ここで撃破数などを集めてGameResultに渡す

        // リザルトへ遷移するまでポーズ画面を開けないようにする
        // （開いて閉じると、止めたプレイヤーの操作が元に戻ってしまうため）
        PauseMenu.IsBlocked = true;

        // ゲームを止める
        // プレイヤー：timeScaleは止めないので、慣性で滑ったり落ちたりしないよう物理も止める
        PlayerControlLock.Lock(true);
        StopEnemies();
        ReleaseCursor();
    }


    // スポーン・WAVE進行・敵AIを止める
    void StopEnemies()
    {
        // スポーンとWAVE進行（どちらもEnemySpawnerのUpdateで動いている）
        if (enemySpawner != null) enemySpawner.enabled = false;

        // 場に残っている敵
        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag(enemyTag))
        {
            // AIのスクリプト（EnemyController・EnemyBase系など）を全て止める
            // （敵の種類が増えても対応できるよう、型を指定せずにMonoBehaviourをまとめて止める）
            foreach (MonoBehaviour script in enemy.GetComponentsInChildren<MonoBehaviour>())
                script.enabled = false;

            // アニメーションをその場で止める
            foreach (Animator animator in enemy.GetComponentsInChildren<Animator>())
                animator.speed = 0f;

            // 攻撃用の当たり判定を切る
            // （OnTriggerEnterはスクリプトを止めても呼ばれるため、止めた後に攻撃が当たらないようにする）
            foreach (Collider col in enemy.GetComponentsInChildren<Collider>())
            {
                if (col.isTrigger) col.enabled = false;
            }
        }
    }


    // マウスカーソルを解放する
    void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }
}
