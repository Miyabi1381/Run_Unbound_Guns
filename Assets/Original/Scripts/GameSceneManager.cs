using System;
using System.Collections;
using UnityEngine;

public class GameSceneManager : MonoBehaviour
{
    // シーンリファレンス
    public SceneReference TitleScene;

    [Tooltip("クリアしたか")]
    public Action onClear;
    [Tooltip("敵スポナー")]
    [SerializeField] EnemySpawner eSpawner;
    [Tooltip("GameClearのテキストオブジェクト")]
    GameObject clearTxt;
    [Tooltip("コルーチンタイム")]
    [SerializeField] private float coroutineTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        clearTxt = GameObject.Find("GameClear");
        clearTxt.SetActive(false);
    }


    // 通知を受け取れるよう有効にする
    void OnEnable()
    {
        Debug.Log("有効化");
        // クリア処理のメソッドを追加
        eSpawner.onAllEnemiesKilled += HandleClear;
    }


    // 通知の受け取りを無効にする
    private void OnDisable()
    {
        Debug.Log("無効化");
        // クリア処理のメソッドを削除
        eSpawner.onAllEnemiesKilled -= HandleClear;
    }


    // クリア処理
    void HandleClear()
    {
        Debug.Log(this);

        // クリアテキストを表示
        clearTxt.SetActive(true);
        // シーン切り替え処理
        StartCoroutine(ClearSquence());
    }


    // コルーチン処理
    IEnumerator ClearSquence()
    {
        // 指定秒処理を止める
        yield return new WaitForSeconds(coroutineTime);

        if (GameManager.instance == null)
        {
            Debug.LogError("GameManagerのインスタンスが存在しません！");
        }

        if (TitleScene == null)
        {
            Debug.LogError("TitleScene 変数がインスペクターで設定されていません！");
        }

        // 指定秒後、シーンをロードする
        GameManager.instance.LoadScene(TitleScene);
    }


}
