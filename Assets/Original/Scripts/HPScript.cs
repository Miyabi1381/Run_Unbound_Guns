using System;
using UnityEngine;

public class HPScript : MonoBehaviour
{
    [Tooltip("対象オブジェクトの体力")]
    private int HP;
    [Tooltip("対象オブジェクトの現在の体力")]
    [HideInInspector] public int currentHP;
    [Tooltip("対象オブジェクトの最大体力")]
    [HideInInspector] public int maxHP;
    [Tooltip("死んだときに呼び出す関数を入れる変数（ラムダ式）")]
    public Action onDeath;
    [Tooltip("ゲームオーバーかどうか、またその後のアクション通知")]
    [HideInInspector] public event Action onDeadPlayer;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // ダミータグがついたオブジェクトの場合
        if(gameObject.CompareTag("Dummie"))
        {
            HP = 200;
            maxHP = HP;
            currentHP = HP;
        }

        // ターゲットタグがついたオブジェクトの場合
        if (gameObject.CompareTag("Target"))
        {
            HP = 1;
            maxHP = HP;
            currentHP = HP;
        }

        // 敵タグがついたオブジェクトの場合
        if (gameObject.CompareTag("Enemy"))
        {
            HP = 50;
            maxHP = HP;
            currentHP = HP;
        }

        // プレイヤータグがついたオブジェクトの場合
        if (gameObject.CompareTag("Player"))
        {
            HP = 1000;
            maxHP = HP;
            currentHP = HP;
        }

    }


    // ダメージ処理
    public void TakeDamage(int damage)
    {
        // ダメージをHPから引く
        HP -= damage;
        // 現在のHPを更新する
        currentHP = HP;

        // DebugTEXT
        Debug.Log("標的は " + damage + "ダメージ食らった！");

        // 対象のHPが0未満ならオブジェクトを消す、または死亡通知する
        if (HP <= 0)
        {
            // 敵の場合オブジェクトを消す
            if (gameObject.CompareTag("Enemy"))
            {
                Dead();
            }

            // プレイヤーの場合死亡通知を出す
            if (gameObject.CompareTag("Player"))
            {
                PlayerDead();
            }

        }

    }


    // 死亡処理
    public void Dead()
    {
        onDeath?.Invoke();
        Debug.Log("標的は破壊された！");
        Destroy(gameObject);
    }


    // プレイヤーの死亡処理
    public void PlayerDead()
    {
        // ToDo: プレイヤーが死んだときの処理。今後変更あり（resultへ）
        onDeadPlayer?.Invoke();
    }


    // 現在の体力を取得する関数
    public int GetCurrentHP() { return currentHP; }


    // 対象オブジェクトの体力を取得する関数
    public int GetMaxHP() { return maxHP; }


    // Update is called once per frame
    void Update()
    {
    }
}
