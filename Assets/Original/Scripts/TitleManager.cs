using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;



public class TitleManager : MonoBehaviour
{
    enum TitleState
    {
        PressSpaceKey,
        MainMenu,
        ModeMenu,
        Settings
    }

    [Tooltip("Press Any Keyの表示")]
    [SerializeField] GameObject pressSpaceKeyTxt;
    [Tooltip("選択肢の表示 例)モードセレクト、設定、終了")]
    [SerializeField] GameObject startSelect;
    [Tooltip("モードセレクトの表示 例)チュートリアル、PvE、PvP")]
    [SerializeField] GameObject modeSelect;
    [Tooltip("現在画面に表示されている選択肢の一覧")]
    GameObject[] currentSelect;
    [Tooltip("現在選択している選択肢(カーソル)")]
    private int currentChoiceIndex;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pressSpaceKeyTxt.SetActive(true);
        startSelect.SetActive(false);
        modeSelect.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        /* ----- カーソルの処理 ----- */

        // カーソルの移動（上）
        if (( (Input.GetKeyDown(KeyCode.UpArrow)) || (Input.GetKeyDown(KeyCode.W)) || (Input.GetKeyDown(KeyCode.WheelUp)) )
            && currentSelect != null)
        {
            currentChoiceIndex--;

            // 現在のインデックスが選択肢の先頭なら最後尾にカーソルを移動する
            if (currentChoiceIndex < 0)
            {
                currentChoiceIndex = currentSelect.Length - 1;
            }

            SelectChoice();
        }


        // カーソルの移動（下）
        if (((Input.GetKeyDown(KeyCode.DownArrow)) || (Input.GetKeyDown(KeyCode.S)) || (Input.GetKeyDown(KeyCode.WheelDown)))
            && currentSelect != null)
        {
            currentChoiceIndex++;

            // 現在のインデックスが選択肢の最後尾なら先頭にカーソルを移動する
            if (currentChoiceIndex >= currentSelect.Length)
            {
                currentChoiceIndex = 0;
            }

            SelectChoice();
        }

        /* -------------------------- */


        /* ----- 選択を決定する処理 ----- */

        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Todo: 決定する処理。ただし、ステートマシンをここで。ステートの判断はここではない方がいいのかも？

            Debug.Log("決定キーが実行されました。");
        }


        /* ------------------------------ */
/*        // スタート画面にいるときかつ、スペースキー入力を受け取ったらセレクト画面に行く
        if (pressSpaceKeyTxt.activeSelf && Input.GetKeyDown(KeyCode.Space))
        {
            // Press Space Keyを非表示にする
            pressSpaceKeyTxt.SetActive(false);

            StartSelect();
        }

        // PressSpaceKeyが非表示の間
        if (!pressSpaceKeyTxt.activeSelf)
        {
            // スタート画面以外の時にESCを押されたら前の画面に戻る
            pressSpaceKeyTxt.SetActive(true);
        }

*/    }


    // 最初のキー入力
    public void PressSpaceKey()
    {
        // PressSpaceKeyを取得
        SetMenu(pressSpaceKeyTxt);

        // PressSpaceKeyを表示
        pressSpaceKeyTxt.SetActive(false);
    }



    // スタートセレクト画面
    public void StartSelect()
    {
        // 選択肢を取得
        SetMenu(startSelect);

        // 最初の選択肢を表示
        startSelect.SetActive(true);
    }


    // モードセレクト画面
    public void ModeSelect()
    {
        // 選択肢を取得
        SetMenu(modeSelect);

        // モード選択を表示
        modeSelect.SetActive(true);
    }


    // 選択肢を設定する処理
    void SetMenu(GameObject menu)
    {
        currentSelect = new GameObject[menu.transform.childCount];

        for (int i = 0; i < menu.transform.childCount; i++)
        {
            currentSelect[i] = menu.transform.GetChild(i).gameObject;
        }
    }


    // 選択肢を選択する処理
    void SelectChoice()
    {
        // 現在のカーソルの位置の選択肢を選択したものとして登録
        GameObject choice = currentSelect[currentChoiceIndex];

        // Todo: 選択中少し右に出す&点滅させる

        Debug.Log("選択中: " + choice.name);
    }

}
