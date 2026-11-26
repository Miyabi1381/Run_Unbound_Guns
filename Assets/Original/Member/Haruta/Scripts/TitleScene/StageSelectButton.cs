using UnityEngine;
using UnityEngine.EventSystems;

// ステージ選択ボタンにマウスが乗ったら選択、クリックしたら決定をTitleSceneManagerに伝える
public class StageSelectButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Tooltip("タイトルシーンマネージャー")]
    [SerializeField] private TitleSceneManager titleSceneManager;
    [Tooltip("TitleSceneManagerのStagesでの番号（0から）")]
    [SerializeField] private int stageIndex;


    // マウスが乗った時
    public void OnPointerEnter(PointerEventData eventData)
    {
        titleSceneManager.SelectStage(stageIndex);
    }


    // クリックされた時（左クリックのみ）
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        titleSceneManager.DecideStage(stageIndex);
    }
}
