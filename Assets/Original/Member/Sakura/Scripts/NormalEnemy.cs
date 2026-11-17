using UnityEngine;

public class NormalEnemy : EnemyBase
{
    void Update()
    {
        if (player == null) return;

        // プレイヤーとの方向を取得
        Vector3 dir = GetPlayerDirection();

        // プレイヤーのほうを向く
        FacePlayer(dir);

        // プレイヤーとの距離(移動・攻撃を切り替えるだけなので、sqrMagnitude を使う)
        float distance = dir.sqrMagnitude;

        // 追従範囲内なら移動
        if (distance > nearStopDistance * nearStopDistance && distance < farStopDistance * farStopDistance)
        {
            MoveTowardsPlayer(dir);

            SetMoveAnimation(speed);
        }
        // 攻撃範囲内なら攻撃
        else if (distance < nearStopDistance * nearStopDistance)
        {
            SetMoveAnimation(0f);

            TryAttack();
        }
        // 追従範囲外なら待機
        else
        {
            SetMoveAnimation(0f);
        }

    }
}
