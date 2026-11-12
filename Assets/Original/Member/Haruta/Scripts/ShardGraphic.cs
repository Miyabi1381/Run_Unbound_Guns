// ======================================================================================
// File: ShardGraphic.cs
//
// Brief: 割れた画面の「破片」1枚を描画するUIクラス
//        好きな形の多角形に、画面キャプチャの一部を貼り付けて表示する
//        (ScreenShatterがスクリプトから自動で生成するので、手動でアタッチする必要はない)
//
// Author: Nagaya Haruta
// ======================================================================================
using UnityEngine;
using UnityEngine.UI;

// UIを描画するにはCanvasRendererが必要(ImageやTextと違い、自作のGraphicには自動で付かない)
[RequireComponent(typeof(CanvasRenderer))]
public class ShardGraphic : MaskableGraphic
{
    // 破片に貼るテクスチャ(画面キャプチャ)
    Texture shardTexture;
    // 多角形の頂点(破片の中心からの相対位置)
    Vector2[] vertices;
    // 各頂点が画面キャプチャのどこに対応するか(0～1)
    Vector2[] uvs;

    // このUIが描画に使うテクスチャ
    public override Texture mainTexture
    {
        get { return (shardTexture != null) ? shardTexture : s_WhiteTexture; }
    }


    // 破片の形とテクスチャを設定する
    public void Setup(Texture texture, Vector2[] shardVertices, Vector2[] shardUVs)
    {
        shardTexture = texture;
        vertices     = shardVertices;
        uvs          = shardUVs;

        // 形とテクスチャが変わったので、描画し直してもらう
        SetAllDirty();
    }


    // UIのメッシュを作る処理(Unityが必要な時に呼び出す)
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (vertices == null || vertices.Length < 3) return;

        // 頂点を登録する
        for (int i = 0; i < vertices.Length; i++)
        {
            vh.AddVert(vertices[i], color, uvs[i]);
        }

        // 頂点0を基準に、扇形に三角形を張って多角形にする
        for (int i = 1; i < vertices.Length - 1; i++)
        {
            vh.AddTriangle(0, i, i + 1);
        }
    }
}
