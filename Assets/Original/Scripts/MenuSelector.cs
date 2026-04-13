using UnityEngine;

public class MenuSelector
{
    [Tooltip("選択肢のインデックス")]
    int index;
    [Tooltip("選択されたオブジェクト")]
    GameObject[] choices;
    [Tooltip("デフォルトの位置情報")]
    Vector3[] defaultPos;
    [Tooltip("選択中の位置情報")]
    Vector3 selectPos = new Vector3(30, 0, 0);


    public void Init(GameObject parent)
    {
        // 親子関係の子の数を数えて、その分インデックスの数に設定する
        int count = parent.transform.childCount;
        choices = new GameObject[count];

        if (defaultPos == null || defaultPos.Length != count)
        {
            defaultPos = new Vector3[count];
    
            for (int i = 0; i < count; i++)
            {
                // 選択肢を取得
                choices[i] = parent.transform.GetChild(i).gameObject;

                // 選択肢の元の位置を親基準で保存
                defaultPos[i] = choices[i].transform.localPosition;
            }

        }

        else
        {
            // 2回目以降のInit時は、まず全員を元の位置に強制的に戻す
            for (int i = 0; i < count; i++)
            {
                choices[i] = parent.transform.GetChild(i).gameObject;
                choices[i].transform.localPosition = defaultPos[i];
            }
        }

        // 初期化
        ResetPosition();
        index = 0;
        Select();
    }


    // インデックスの移動処理 
    public void Move(int dir)
    {
        index = (index + dir + choices.Length) % choices.Length;
        Select();
    }


    // 選択処理
    public void Select()
    {
        // インデックスが選択されているとき、指定の距離だけ右にずらす
        for (int i = 0; i < choices.Length; i++)
        {
            if (i == index)
                choices[i].transform.localPosition = defaultPos[i] + selectPos;
            else
                choices[i].transform.localPosition = defaultPos[i];
        }
    }


    // 選択肢の位置情報をリセットする関数
    void ResetPosition()
    {
        for (int i = 0; i < choices.Length; i++)
        {
            choices[i].transform.localPosition = defaultPos[i];
        }

    }


    // インデックスを取得する関数
    public int GetIndex()
    {
        return index;
    }

}
