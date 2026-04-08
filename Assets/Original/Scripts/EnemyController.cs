using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // プレイヤー
    [Tooltip("プレイヤーの情報")]
    Transform player;

    // HP
    [Tooltip("HPスクリプト")]
    HPScript hp;

    // 座標
    [Tooltip("Y軸(高さ)を固定するための変数")]
    [HideInInspector] private float fixedY;

    // 敵AI(移動)
    [Tooltip("歩行速度")]
    public float speed = 1;
    [Tooltip("プレイヤーに近づいたときに移動をやめる距離")]
    public float nearStopDistance = 1;
    [Tooltip("プレイヤーから離れたときに移動(追従)をやめる距離")]
    public float farStopDistance = 10;
    
    // 敵AI(攻撃)
    [Tooltip("攻撃する間隔")]
    public float fightInterval = 1;
    [Tooltip("攻撃間隔を測るタイマー")]
    [HideInInspector] public float timer;
    [Tooltip("攻撃時に与えるダメージ")]
    public int damage = 5;
    [Tooltip("攻撃用の当たり判定")]
    public Collider attackCol;
    [Tooltip("攻撃ヒットフラグ")]
    private bool hasHit;

    [Tooltip("デバッグ用、攻撃インターバルの確認用カウンター")]
    [HideInInspector] public int intervalCount;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // キャッシュを一度だけ取得
        player = PlayerMovementScript.instance.transform;
        // 敵の初期位置を取得
        fixedY = this.transform.position.y;
        // 攻撃ヒットフラグを初期化
        hasHit = false;
    }

    // Update is called once per frame
    void Update()
    {
        // 敵からプレイヤーへの方向を取得(最適化)し、高さを固定
        Vector3 dir = new Vector3(
            player.position.x - this.transform.position.x,
            0f,
            player.position.z - this.transform.position.z
            );

        // 方向が0でないなら、プレイヤーの方向に正面を向ける
        if (dir != Vector3.zero)
        {
            transform.forward = dir;
        }

        // 今いる位置がプレイヤーを追従する範囲内であれば移動処理を行う (memo: .sqrMagnitudeは平方根²の計算)
        if (dir.sqrMagnitude > nearStopDistance * nearStopDistance
            && dir.sqrMagnitude < farStopDistance * farStopDistance)
        {
            // 敵を移動させる（軽量化のため直接座標を設定）
            Vector3 pos = this.transform.position;
            pos += dir.normalized * speed * Time.deltaTime;
            pos.y = fixedY;

            this.transform.position = pos;
        }

        // プレイヤーとの距離が一定値以内であれば追従せずとどまる（追加要素：攻撃する）
        else if (dir.sqrMagnitude < nearStopDistance * nearStopDistance)
        {
            // インターバル付き攻撃動作
            timer += Time.deltaTime;
            if (timer >= fightInterval)
            {
                // 値をリセット
                timer = 0;          // インターバルタイマー
                intervalCount++;    // Debug: インターバル確認用カウント
                hasHit = false;     // 攻撃ヒットフラグ

                // 攻撃動作
                Attack();
            }

        }

    }


    // 攻撃処理(モーション/エフェクト)
    void Attack()
    {
        // 攻撃当たり判定ON
        attackCol.enabled = true;

        // 指定秒後
        Invoke(nameof(EndAttack), 0.2f);
    }


    // 攻撃終了処理
    void EndAttack()
    {
        attackCol.enabled = false;
    }


    // 攻撃処理(当たり判定)
    private void OnTriggerEnter(Collider other)
    {
        // 既にヒットしていたら抜ける
        if (hasHit) return;

        // 対象にプレイヤータグが付いていたら攻撃を通す
        if (other.CompareTag("Player"))
        {
            Debug.Log("あたったよ！5ダメな！！");
            hasHit = true;

            hp.TakeDamage(damage);
        }

    }
}
