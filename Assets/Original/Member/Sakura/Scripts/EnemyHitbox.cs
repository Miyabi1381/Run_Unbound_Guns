using UnityEngine;

public enum HitboxType
{
    Body,
    Head
}

public class EnemyHitbox : MonoBehaviour
{
    public bool isHead;
    public float headshotMultiplier = 2f;

    private HPScript hpScript;

    private void Awake()
    {
        hpScript = GetComponentInParent<HPScript>();
    }

    public void TakeDamage(int damage)
    {
        if (hpScript == null)
        {
            Debug.LogError("親のSakuraHPScriptが見つかりません", gameObject);
            return;
        }

        if (isHead)
        {
            damage = hpScript.GetCurrentHP();
        }

        hpScript.TakeDamage(damage);
    }
}
