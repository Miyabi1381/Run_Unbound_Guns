/*
 * @file    GameManager.cs
 * 
 * @brief   ゲーム全体のシーン遷移/設定など
 * 
 * @author  
 * 
 * @date    
 */


using UnityEngine;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    // インスタンス
    public static GameManager instance;


    // 起動時に一番最初に走る処理
    private void Awake()
    {
        // インスタンスを生成し、シーンを跨いでも消されないように設定
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        // インスタンスがなければ明示的削除
        else
        {
            Destroy(gameObject);
        }

    }


    // シーンをロードする関数
    public void LoadScene(SceneReference sceneRef)
    {
        SceneManager.LoadScene(sceneRef.sceneName);
    }


    // ゲームを終了する関数
    public void QuitGame()
    {
        Application.Quit();
    }
}
