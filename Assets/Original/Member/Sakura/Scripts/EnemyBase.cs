using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    // プレイヤー
    [Tooltip("プレイヤーの情報")]
    protected Transform player;

    // 座標
    [Tooltip("Y軸(高さ)を固定するための変数")]
    [HideInInspector] protected float fixedY;

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
    [HideInInspector] protected float timer;
    [Tooltip("攻撃時に与えるダメージ")]
    public int damage = 5;
    [Tooltip("攻撃用の当たり判定")]
    public Collider attackCol;
    [Tooltip("攻撃ヒットフラグ")]
    protected bool hasHit;

    // 敵AI(アニメーション)
    [Tooltip("アニメーター")]
    protected Animator animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        // キャッシュを一度だけ取得
        player = PlayerMovementScript.instance.transform;
        // 敵の初期位置を取得
        fixedY = this.transform.position.y;
        // 攻撃ヒットフラグを初期化
        hasHit = false;
        // アニメーターを取得
        animator = GetComponent<Animator>();
    }

    // プレイヤーとの方向を取得
    protected Vector3 GetPlayerDirection()
    {
        if (player == null) return Vector3.zero;

        // 敵からプレイヤーへの方向を取得(最適化)し、高さを固定
        Vector3 dir = new Vector3(
            player.position.x - this.transform.position.x,
            0f,
            player.position.z - this.transform.position.z
            );

        return dir;
    }

    // プレイヤーの方向を向く
    protected void FacePlayer(Vector3 dir)
    {
        // 方向が0でないなら、プレイヤーの方向に正面を向ける
        if (dir != Vector3.zero)
        {
            transform.forward = dir;
        }
    }

    // プレイヤーに向かって移動
    protected void MoveTowardsPlayer(Vector3 dir)
    {
        Vector3 pos = transform.position;
        pos += dir.normalized * speed * Time.deltaTime;

        // 高さを固定
        pos.y = fixedY;
        transform.position = pos;
    }

    // 移動アニメーションの設定
    protected void SetMoveAnimation(float moveSpeed)
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", moveSpeed);
        }
    }

    // 攻撃間隔の管理
    protected void TryAttack()
    {
        timer += Time.deltaTime;
        if (timer >= fightInterval)
        {
            // 値をリセット
            timer = 0;          // インターバルタイマー
            hasHit = false;     // 攻撃ヒットフラグ

            // 攻撃動作
            Attack();
        }
    }

    // 攻撃処理(モーション/エフェクト)
    protected virtual void Attack()
    {
        // 攻撃アニメーションを再生
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // 攻撃時にのみONの当たり判定をON
        attackCol.enabled = true;

        // 指定秒後
        Invoke(nameof(EndAttack), 0.2f);
    }


    // 攻撃終了処理
    protected virtual void EndAttack()
    {
        // 攻撃時にのみONの当たり判定をOFF
        attackCol.enabled = false;
    }


    // 攻撃処理(当たり判定)
    protected virtual void OnTriggerEnter(Collider other)
    {
        // 既にヒットしていたら抜ける
        if (hasHit) return;

        // 対象にプレイヤータグが付いていたら攻撃を通す
        if (other.CompareTag("Player"))
        {
            // ヒットフラグをオンにする
            hasHit = true;

            // コンポーネントが確認されたらHPの減少処理を行う
            if (other.TryGetComponent<HPScript>(out HPScript hpScript))
            {
                // HPの減少処理
                hpScript.TakeDamage(damage);
            }
        }

    }
}
