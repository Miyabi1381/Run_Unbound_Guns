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

    private SakuraHPScript hpScript;

    private void Awake()
    {
        hpScript = GetComponentInParent<SakuraHPScript>();
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
