// ======================================================================================
// File: EnemyAvatarChange.cs
//
// Brief: 敵のモデルをステージに合わせて自動変更する
//
// Author: Banno Miyabi
// Date: 2026/11/11
// ======================================================================================
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyAvatarChange : MonoBehaviour
{
    // メンバ変数 -------------------------------------------------------------------
    [Tooltip("データ群をセットする枠")]
    [SerializeField] private EnemyData enemyData;

    // ライフサイクル関数 -----------------------------------------------------------
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 大元のアニメーション
        Animator rootAnimator = GetComponent<Animator>();
        if (enemyData == null || rootAnimator == null) return;

        // 現在のシーン名を取得
        string currentScene = SceneManager.GetActiveScene().name;
        // シーン名と一致するデータを検索
        EnemyData.EnemySetData currentSet = default;
        bool isFound = false;
        foreach (var set in enemyData.sceneEnemySetting)
        {
            // 指定した文字列と取得したシーン名が同一なら処理を抜ける
            if (set.sceneName == currentScene)
            {
                currentSet = set;
                isFound    = true;
                break;
            }
        }

        // 見つけたらPrefabにアタッチしてアニメーションを実行
        if (isFound && currentSet.modelPrefab != null)
        {
            // モデルを生成して子要素にする
            GameObject spawnedModel = Instantiate(currentSet.modelPrefab, transform);
            spawnedModel.transform.localPosition = Vector3.zero;
            spawnedModel.transform.localRotation = Quaternion.identity;

            // モデルの高さをコライダーと合わせる
            SetModelBottomToCapsuleColBottom(spawnedModel);

            // 動的にアニメーション群を増減できるように
            AnimatorOverrideController dynamicOverride = new AnimatorOverrideController(rootAnimator.runtimeAnimatorController);

            // アバターを適用
            Animator internalAnimator = spawnedModel.GetComponent<Animator>();
            if (internalAnimator != null)
            {
                rootAnimator.avatar = internalAnimator.avatar;
                internalAnimator.enabled = false;
            }

            // スクリプタブルオブジェクトに登録された数だけループしてアニメーションを上書きする
            foreach (var animGroup in currentSet.animations)
            {
                if (!string.IsNullOrEmpty(animGroup.animationName) && animGroup.clip != null)
                {
                    // ダミーのアニメーション名を見つけたら、本番用に置き換える
                    string dummyKey = "Dummy" + animGroup.animationName;
                    dynamicOverride[dummyKey] = animGroup.clip;
                }
            }

            // 完成したコントローラーを親に適用
            rootAnimator.runtimeAnimatorController = dynamicOverride;
        }
        else
        {
            Debug.LogWarning($"Scene「{currentScene}」に対応する敵データが登録されていません。");
        }


    }

    /// <summary>
    /// モデルの底面とカプセルコライダーの底面を一致させる関数
    /// </summary>
    /// <param name="paramName">[引数の説明]</param>
    /// <returns>[戻り値の説明]</returns>	
    void SetModelBottomToCapsuleColBottom(GameObject targetModel)
    {
        // アタッチされたカプセルコライダーを取得
        CapsuleCollider capCol = GetComponent<CapsuleCollider>();
        if (capCol != null) 
        {
            // カプセルコライダーの底面を算出
            float colliderBottomY = capCol.center.y - (capCol.height * 0.5f);

            // モデル全体の境界ボックスを算出
            Renderer[] renderers = targetModel.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds combinedBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }

                // モデルの最上下部をローカルY座標に変換
                float modelBottomLocalY = transform.InverseTransformPoint(combinedBounds.min).y;
                float modelTopLocalY    = transform.InverseTransformPoint(combinedBounds.max).y;
                // モデルの実際の身長を算出
                float modelHeight = modelTopLocalY - modelBottomLocalY;

                // カプセルコライダーの高さと中心を更新
                capCol.height = Mathf.Max(modelHeight, capCol.radius * 2.0f);
                capCol.center = new Vector3(capCol.center.x, colliderBottomY + (capCol.height * 0.5f), capCol.center.z);

                // モデルの足元をコライダーの底面に接地させる
                float offsetY = colliderBottomY - modelBottomLocalY;
                targetModel.transform.localPosition = new Vector3(0f, offsetY, 0f);
            }
        }
    }
}
