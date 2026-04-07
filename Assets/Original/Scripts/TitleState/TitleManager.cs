using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TitleManager : MonoBehaviour
{
    // 現在のシーンステータス
    TitleStateBase currentState;

    // ステータスの履歴
    Stack<TitleStateBase> stateStack = new Stack<TitleStateBase>();

    // シーンリファレンス
    public SceneReference DesertStageScene;
    public SceneReference JungleStageScene;
    public SceneReference MoonStageScene;

    // ラムダ式シーン切り替え
    public Action[] modeActions;

    // UI参照
    [Tooltip("Press Space Key")]
    public GameObject pressSpaceKeyUI;
    [Tooltip("メインメニューUI")]
    public GameObject mainMenuUI;
    [Tooltip("モード選択UI")]
    public GameObject modeUI;
    [Tooltip("ステージ(今後難易度分けする予定)選択UI")]
    public GameObject stageMenuUI;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Action配列を生成
        modeActions = new Action[]
            {
                () => GameManager.instance.LoadScene(DesertStageScene),
                () => GameManager.instance.LoadScene(JungleStageScene),
                () => GameManager.instance.LoadScene(MoonStageScene)
            };

        // 初期状態(全て非表示)
        pressSpaceKeyUI.SetActive(false);
        mainMenuUI.SetActive(false);
        modeUI.SetActive(false);
        stageMenuUI.SetActive(false);

        // シーンステータスをPress Space Keyにセット
        ChangeState(new PressSpaceKeyState());
    }

    // Update is called once per frame
    void Update()
    {
        // 現在のシーンステータスの関数が呼び出されたかどうかを判定し、実行する
        currentState?.HandleInput();
        currentState?.Update(); 
    }


    // シーンステータスを移行する関数
    public void ChangeState(TitleStateBase newState)
    {
        // シーンステータスから離脱したら、現在のシーンステータスを保存する
        if (currentState != null)
        {
            currentState?.Exit();
            stateStack.Push(currentState);
        }

        // 次のシーンステータスに移行する
        currentState = newState;
        currentState.Enter(this);
    }


    // シーンステータスを戻す関数
    public void BackState()
    {
        // 戻れるシーンがなければ処理を終了する
        if (stateStack.Count <= 0) return;

        // 現在のシーンステータスを離脱し、前のシーンに移行する
        currentState?.Exit();
        currentState = stateStack.Pop();
        currentState.Enter(this);
    }

}
