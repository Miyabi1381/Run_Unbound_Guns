using UnityEngine;
using TMPro;



public class TitleManager : MonoBehaviour
{
    TitleStateBase currentState;

    // UI参照
    [Tooltip("Press Space Key")]
    public GameObject pressSpaceKeyUI;
    [Tooltip("Press Space Key")]
    public GameObject mainMenuUI;
    [Tooltip("Press Space Key")]
    public GameObject modeUI;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
//        ChangeState(new PressSpaceKeyState());
    }

    // Update is called once per frame
    void Update()
    {
        currentState?.HandleInput();
        currentState?.Update();
    }


    public void ChangeState(TitleStateBase newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter(this);
    }
}
