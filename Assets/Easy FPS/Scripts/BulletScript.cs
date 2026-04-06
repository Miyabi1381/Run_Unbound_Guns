using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour {

	[Tooltip("Furthest distance bullet will look for target")]
	public float maxDistance = 1000000;
	RaycastHit hit;
	[Tooltip("Prefab of wall damange hit. The object needs 'LevelPart' tag to create decal on it.")]
	public GameObject decalHitWall;
	[Tooltip("Decal will need to be sligtly infront of the wall so it doesnt cause rendeing problems so for best feel put from 0.01-0.1.")]
	public float floatInfrontOfWall;
	[Tooltip("Blood prefab particle this bullet will create upoon hitting enemy")]
	public GameObject bloodEffect;
	[Tooltip("Put Weapon layer and Player layer to ignore bullet raycast.")]
	public LayerMask ignoreLayer;
    [Tooltip("ターゲットに着弾したときに出すエフェクト")]
    public GameObject targetEffect;
    [Tooltip("弾のダメージ")]
    private int damage;
    [Tooltip("一度にスポーンできる上限")]
    public int spawnLimit;
    [Tooltip("現在スポーンしている数")]
    [HideInInspector] public int currentSpawnCount;

    // 弾痕
    [Tooltip("弾痕のエフェクト")]
    private static Queue<GameObject> decalMarks = new Queue<GameObject>();
    [Tooltip("弾痕エフェクトの表示上限")]
    public static int maxDecalMarks = 5;

    public void Initialize(int damageVal)
    {
        damage = damageVal;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
	{
	}


    /*
    * Uppon bullet creation with this script attatched,
    * bullet creates a raycast which searches for corresponding tags.
    * If raycast finds somethig it will create a decal of corresponding tag.
    */
    void Update()
    {

        if (Physics.Raycast(transform.position, transform.forward, out hit, maxDistance, ~ignoreLayer))
        {
            if (decalHitWall)
            {
                if (hit.transform.tag == "LevelPart")
                {
                    // 弾痕を生成
                    GameObject mark = Instantiate(decalHitWall, hit.point + hit.normal * floatInfrontOfWall, Quaternion.LookRotation(hit.normal));
                    decalMarks.Enqueue(mark);

                    // 上限を超えたら古い弾痕を即削除
                    if (decalMarks.Count > maxDecalMarks)
                    {
                        Destroy(decalMarks.Dequeue());
                    }

                }

                if (hit.transform.tag == "Enemy")
                {
                    if (hit.collider.TryGetComponent<HPScript>(out HPScript hpScript))
                    {
                        hpScript.TakeDamage(damage);
                    }
                    Instantiate(bloodEffect, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(gameObject);

                }

                if (hit.transform.tag == "Dummie")
                {
                    if (hit.collider.TryGetComponent<HPScript>(out HPScript hpScript))
                    {
                        hpScript.TakeDamage(damage);
                    }

                    Instantiate(bloodEffect, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(gameObject);
                }

                if (hit.transform.tag == "Target")
                {
                    if (hit.collider.TryGetComponent<HPScript>(out HPScript hpScript))
                    {
                        hpScript.TakeDamage(damage);
                    }

                    Instantiate(targetEffect, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(gameObject);
                }
            }

            Destroy(gameObject);
        }
        Destroy(gameObject, 0.1f);
    }

}
