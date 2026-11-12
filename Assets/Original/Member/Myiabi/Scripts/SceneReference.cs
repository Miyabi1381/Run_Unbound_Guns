// エディタでのみ実行されるようにする。ビルド後に存在しないコードを消すため
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


// Unity専用のデータクラス(ScriptableObject)
[CreateAssetMenu(menuName ="Scene Refernce")]
public class SceneReference : ScriptableObject
{
    // シーンアセット
#if UNITY_EDITOR
    public SceneAsset sceneAsset;
#endif

    // シーン名
    public string sceneName;


#if UNITY_EDITOR
    void OnValidate()
    {
        if (sceneAsset != null) 
        {
            // シーン名にインスペクターで取得したシーン名を代入
            sceneName = sceneAsset.name;
        }
    }
#endif

}
