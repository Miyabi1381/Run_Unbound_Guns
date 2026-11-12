// ======================================================================================
// File: EnemyAvatarChange.cs
//
// Brief: 敵のモデルをステージに合わせて自動変更する
//
// Author: Banno Miyabi
// Date: 2026/11/11
// 
// copyright: Copyright(c)2026 BannoMiyabi.
// ======================================================================================
using UnityEngine;
using System.Collections.Generic;

// memo: クラスの属性
[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    // アニメーション群
    [System.Serializable]
    public struct AnimationGroup
    {
        public string animationName;    ///< アニメーションの分類名
        public AnimationClip clip;      ///< アニメーションファイル
    }

    // 敵に渡すデータ群
    [System.Serializable]
    public struct EnemySetData
    {
        public string sceneName;                  ///< 現在のシーン
        public GameObject modelPrefab;            ///< 表示するモデル
        public List<AnimationGroup> animations;   ///< アニメーション群
    }

    // リストの中にインスペクターから複数個データを追加可能
    public List<EnemySetData> sceneEnemySetting = new List<EnemySetData>();
}
