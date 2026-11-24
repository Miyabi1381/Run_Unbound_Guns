using UnityEngine;

public class ModeMenuState : TitleStateBase
{
    // タイトルマネージャー
    MenuSelector menuSelector;
    // スティックが倒れているか
    private bool isStickPushed = false;

    // 渡されたシーンステータスに移行
    public override void Enter(TitleManager manager)
    {
        titleManager = manager;
        titleManager.modeUI.SetActive(true);

        menuSelector = titleManager.menuSelector;

        menuSelector.Init(titleManager.modeUI);
    }


    // シーンステータスから離脱
    public override void Exit()
    {
        titleManager.modeUI.SetActive(false);
        // ステートを抜ける時にフラグをリセットする
        isStickPushed = false;
    }


    // 入力を取得して実行
    public override void HandleInput()
    {
        // スティックの縦方向の入力を取得
        float verticalInput = Input.GetAxisRaw("Vertical");

        // インデックスのカーソル移動処理
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || (verticalInput > 0.5f && !isStickPushed))  // 上方向
        {
            AudioManager.instance.PlaySE("Cursor SE");
            menuSelector.Move(-1);
            isStickPushed = true;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) || (verticalInput < -0.5f && !isStickPushed))// 下方向
        {
            AudioManager.instance.PlaySE("Cursor SE");
            menuSelector.Move(1);
            isStickPushed = true;
        }

        // 決定キーの処理
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Joystick1Button0))
        {
            AudioManager.instance.PlaySE("Decision SE");
            Decide();
        }

        // 戻るキーの処理
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Joystick1Button1))
        {
            AudioManager.instance.PlaySE("Cancel SE");
            titleManager.BackState();
        }
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
                // ToDo: ソロモードを選択したことを何かしらでステージに通達する
                titleManager.ChangeState(titleManager.stageMenuState);
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
