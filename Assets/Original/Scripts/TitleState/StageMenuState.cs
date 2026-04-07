using UnityEngine;

public class StageMenuState : TitleStateBase
{
    // タイトルマネージャー
    MenuSelector menuSelector = new MenuSelector();


    // 渡されたシーンステータスに移行
    public override void Enter(TitleManager manager)
    {
        titleManager = manager;
        titleManager.stageMenuUI.SetActive(true);

        menuSelector.Init(titleManager.stageMenuUI);
    }


    // シーンステータスから離脱
    public override void Exit()
    {
        titleManager.stageMenuUI.SetActive(false);
    }


    // 入力を取得して実行
    public override void HandleInput()
    {
        // インデックスのカーソル移動処理
        if (Input.GetKeyDown(KeyCode.UpArrow))  // 上方向
            menuSelector.Move(-1);
        if (Input.GetKeyDown(KeyCode.DownArrow))// 下方向
            menuSelector.Move(1);

        // 決定キーの処理
        if (Input.GetKeyDown(KeyCode.Space))
            Decide();

        // 戻るキーの処理
        if (Input.GetKeyDown(KeyCode.Escape))
            titleManager.BackState();
    }


    // インデックスの決定後の処理
    void Decide()
    {
        // インデックスの数を取得
        int index = menuSelector.GetIndex();

        // 分岐
        switch (index)
        {
            case 0: // DesertStage
                titleManager.modeActions[index].Invoke();
                break;

            case 1: // JungleStage
                titleManager.modeActions[index].Invoke();
                break;

            case 2: // MoonStage
                titleManager.modeActions[index].Invoke();
                break;
        }

    }

    // 更新処理
    public override void Update()
    {
    }

}
