using UnityEngine;

public class RandomPicker : MonoBehaviour
{
    public GameObject[] objects;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // インデックスを生成(ランダムに一つ生成)
        int index = Random.Range(0, objects.Length);

        for (int i = 0; i < objects.Length; i++)
        {
            // ランダムにピックアップしたステージ以外を非表示にする
            if (i != index) objects[i].SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}
