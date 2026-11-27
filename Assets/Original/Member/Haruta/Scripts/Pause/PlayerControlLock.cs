using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// プレイヤーの操作（移動・視点・射撃・武器切り替え）を止める／元に戻す共通処理
// ポーズ画面（止めて戻す）とリザルト（止めたまま）の両方から使う
public static class PlayerControlLock
{
    // 止めているか
    public static bool IsLocked { get; private set; }

    // シーンに操作できるプレイヤーがいるか（タイトル・リザルトではfalse）
    public static bool HasPlayer => PlayerMovementScript.instance != null;

    // 止めたコンポーネント（元々有効だったものだけを覚えておき、Unlockでそれだけを戻す）
    private static readonly List<Behaviour> disabledBehaviours = new List<Behaviour>();
    // 物理を止めたRigidbodyと、止める前のisKinematic
    private static Rigidbody frozenBody;
    private static bool wasKinematic;


    // プレイヤーの操作を止める
    // freezeBody: Rigidbodyも止めるか（timeScaleを0にしない時に、慣性で滑ったり落ちたりしないように）
    public static void Lock(bool freezeBody)
    {
        PlayerMovementScript player = PlayerMovementScript.instance;
        if (player == null) return;

        // 既に止めている時は、物理の停止だけ追加で受け付ける
        if (IsLocked)
        {
            if (freezeBody) FreezeBody(player);
            return;
        }

        IsLocked = true;
        disabledBehaviours.Clear();

        // 移動
        Disable(player);

        // 視点（MouseLookScriptはLキーでカーソルを再ロックするので止めておく）
        Disable(player.GetComponent<MouseLookScript>());

        // 武器切り替えと、今持っている銃の射撃
        // （銃はプレイヤーの子ではなく別オブジェクトとして生成されるので、GunInventoryから取得する）
        GunInventory inventory = player.GetComponent<GunInventory>();
        Disable(inventory);
        if (inventory != null && inventory.currentGun != null)
            Disable(inventory.currentGun.GetComponent<GunScript>());

        if (freezeBody) FreezeBody(player);
    }


    // 止めた操作を元に戻す
    public static void Unlock()
    {
        if (!IsLocked) return;

        // 止めたものだけを戻す（シーン遷移などで消えていたら飛ばす）
        foreach (Behaviour behaviour in disabledBehaviours)
        {
            if (behaviour != null) behaviour.enabled = true;
        }
        disabledBehaviours.Clear();

        // 物理を戻す
        if (frozenBody != null) frozenBody.isKinematic = wasKinematic;
        frozenBody = null;

        IsLocked = false;
    }


    // 有効なコンポーネントだけを止めて覚えておく
    static void Disable(Behaviour behaviour)
    {
        if (behaviour == null || !behaviour.enabled) return;

        behaviour.enabled = false;
        disabledBehaviours.Add(behaviour);
    }


    // プレイヤーの物理を止める
    static void FreezeBody(PlayerMovementScript player)
    {
        if (frozenBody != null) return;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null) return;

        frozenBody   = rb;
        wasKinematic = rb.isKinematic;

        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic     = true;
    }


    // 記録を消す（コンポーネントは戻さない。シーンが切り替わって対象が無くなった時用）
    static void ResetState()
    {
        disabledBehaviours.Clear();
        frozenBody = null;
        IsLocked   = false;
    }


    // 再生開始時に初期化し、シーンが切り替わるたびに記録を消すよう登録する
    // （リザルトで止めたままステージを抜けても、次のステージで正しく止められるように）
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Initialize()
    {
        ResetState();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) ResetState();
    }
}
