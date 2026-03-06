using UnityEngine;

public class HPScript : MonoBehaviour
{
    [Tooltip("対象オブジェクトの体力")]
    int HP;


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
    }


    // ダメージ処理
    public void TakeDamage(int damage)
    {
        // ダメージをHPから引く
        HP -= damage;

        // DebugTEXT
        Debug.Log("標的は " + damage + "ダメージ食らった！");

        // 対象のHPが0未満ならオブジェクトを消す
        if (HP <= 0) Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
    }
}
