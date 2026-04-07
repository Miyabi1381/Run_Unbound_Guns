using UnityEngine;

public abstract class TitleStateBase
{
    // タイトルマネージャー
    protected TitleManager titleManager;

    // 渡されたシーンステータスに移行
    public virtual void Enter(TitleManager manager)
    {
        titleManager = manager;
    }

    // シーンステータスから離脱
    public virtual void Exit() {}

    // 入力を取得して実行
    public virtual void HandleInput() {}

    // 更新処理
    public virtual void Update() {}

}
