using UnityEngine;

public interface TitleStateBase
{
    void Enter(TitleManager tManager);
    void Exit();
    void HandleInput();
    void Update();

}
