using UnityEngine;

public class MenuSelector
{
    [Tooltip("選択肢のインデックス")]
    int index;
    [Tooltip("選択されたオブジェクト")]
    GameObject[] choices;


    public void Init(GameObject parent)
    {
        // 親子関係の子の数を数えて、その分インデックスの数に設定する
        int count = parent.transform.childCount;
        choices = new GameObject[count];

        for (int i = 0; i < count; i++)
        {
            choices[i] = parent.transform.GetChild(i).gameObject;
        }

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
        // ToDo: 今後UIの更新もここで行う
        Debug.Log("選択中：" + choices[index].name);
    }


    // インデックスを取得する関数
    public int GetIndex()
    {
        return index;
    }

}
