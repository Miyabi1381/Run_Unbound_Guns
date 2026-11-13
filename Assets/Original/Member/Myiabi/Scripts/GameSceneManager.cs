// ======================================================================================
// copyright: Copyright(c)2026 BannoMiyabi.
// ======================================================================================
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
    [Tooltip("HPスクリプト")]
    [SerializeField] HPScript hpScript;
    [Tooltip("GameClearのテキストオブジェクト")]
    GameObject clearTxt;
    [Tooltip("GameOverのテキストオブジェクト")]
    GameObject gameoverTxt;
    [Tooltip("コルーチンタイム")]
    [SerializeField] private float coroutineTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        clearTxt    = GameObject.Find("GameClear");
        gameoverTxt = GameObject.Find("GameOver");
        clearTxt.SetActive(false);
        gameoverTxt.SetActive(false);
    }

    // 更新処理
    private void Update()
    {   
        // バックスペースを押されたらシーン切り替え処理（デバッグ）
        if (Input.GetKey(KeyCode.Backspace)) StartCoroutine(ClearSquence());
    }

    // 通知を受け取れるよう有効にする
    void OnEnable()
    {
        Debug.Log("有効化");
        // クリア処理のメソッドを追加
        eSpawner.onAllEnemiesKilled += HandleClear;
        // ゲームオーバー処理のメソッドを追加
        hpScript.onDeadPlayer += HandleGameover;
    }


    // 通知の受け取りを無効にする
    private void OnDisable()
    {
        Debug.Log("無効化");
        // クリア処理のメソッドを削除
        if (eSpawner != null)
            eSpawner.onAllEnemiesKilled -= HandleClear;
        // ゲームオーバー処理のメソッドを削除
        if (hpScript != null)
            hpScript.onDeadPlayer -= HandleGameover;
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


    // ゲームオーバー処理
    void HandleGameover()
    {
        Debug.Log(this);

        // クリアテキストを表示
        gameoverTxt.SetActive(true);
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
        // カーソルの固定を解除する
        Cursor.lockState = CursorLockMode.Confined;
        // 指定秒後、シーンをロードする
        GameManager.instance.LoadScene(TitleScene);
    }


}
