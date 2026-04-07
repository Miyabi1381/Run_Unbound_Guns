using UnityEngine;

public class PressSpaceKeyState : TitleStateBase
{
    // 渡されたシーンステータスに移行
    public override void Enter(TitleManager manager)
    {
        titleManager = manager;
        titleManager.pressSpaceKeyUI.SetActive(true);
    }


    // シーンステータスから離脱
    public override void Exit()
    {
        titleManager.pressSpaceKeyUI.SetActive(false);
    }


    // 入力を取得して実行
    public override void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            titleManager.ChangeState(new MainMenuState());
        }
    }


    // 更新処理
    public override void Update()
    {
    }
}
