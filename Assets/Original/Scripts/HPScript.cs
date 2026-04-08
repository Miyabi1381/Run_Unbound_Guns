using System;
using UnityEngine;

public class HPScript : MonoBehaviour
{
    [Tooltip("対象オブジェクトの体力")]
    int HP;
    [Tooltip("死んだときに呼び出す関数を入れる変数（ラムダ式）")]
    public Action onDeath;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // ダミータグがついたオブジェクトの場合
        if(gameObject.CompareTag("Dummie"))
        {
            HP = 200;
        }

        // ターゲットタグがついたオブジェクトの場合
        if (gameObject.CompareTag("Target"))
        {
            HP = 1;
        }

        // 敵タグがついたオブジェクトの場合
        if (gameObject.CompareTag("Enemy"))
        {
            HP = 50;
        }

        // プレイヤータグがついたオブジェクトの場合
        if (gameObject.CompareTag("Player"))
        {
            HP = 1000;
        }

    }


    // ダメージ処理
    public void TakeDamage(int damage)
    {
        // ダメージをHPから引く
        HP -= damage;

        // DebugTEXT
        Debug.Log("標的は " + damage + "ダメージ食らった！");

        // 対象のHPが0未満ならオブジェクトを消す&死亡通知する
        if (HP <= 0)
        {
            Dead();
        }

    }


    // 死亡処理
    public void Dead()
    {
        onDeath?.Invoke();
        Debug.Log("標的は破壊された！");
        Destroy(gameObject);

    }

    // Update is called once per frame
    void Update()
    {
    }
}
