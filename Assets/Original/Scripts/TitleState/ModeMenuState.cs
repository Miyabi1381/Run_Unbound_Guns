using UnityEngine;

public class ModeMenuState : TitleStateBase
{
    // タイトルマネージャー
    MenuSelector menuSelector = new MenuSelector();


    // 渡されたシーンステータスに移行
    public override void Enter(TitleManager manager)
    {
        titleManager = manager;
        titleManager.modeUI.SetActive(true);

        menuSelector.Init(titleManager.modeUI);
    }


    // シーンステータスから離脱
    public override void Exit()
    {
        titleManager.modeUI.SetActive(false);
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
            case 0:
                Debug.Log("チュートリアルモードへ移行");
                break;

            case 1:
                Debug.Log("ソロモードへ移行");
                titleManager.modeActions[index].Invoke();

                break;

            case 2:
                Debug.Log("マルチモードへ移行");
                break;
        }

    }


    // 更新処理
    public override  void Update()
    {
    }
}
