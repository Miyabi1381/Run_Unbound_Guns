using UnityEngine;
using UnityEngine.EventSystems;

// ポーズ画面のボタンにマウスが乗ったら選択、クリックしたら決定をPauseMenuに伝える
// ポーズメニューの項目と、確認ダイアログの「はい／いいえ」の両方に使う
public class PauseMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    // どの画面のボタンか
    public enum ButtonGroup
    {
        Menu,       // ポーズメニューの項目
        Confirm     // 確認ダイアログの「はい／いいえ」
    }

    [Tooltip("ポーズメニュー")]
    [SerializeField] private PauseMenu pauseMenu;
    [Tooltip("どの画面のボタンか")]
    [SerializeField] private ButtonGroup group = ButtonGroup.Menu;
    [Tooltip("番号（0から）\nMenu: PauseMenuのMenu Itemsでの番号\nConfirm: 0=はい / 1=いいえ")]
    [SerializeField] private int index;


    // マウスが乗った時
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (pauseMenu != null) pauseMenu.OnButtonHover(group, index);
    }


    // クリックされた時（左クリックのみ）
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (pauseMenu != null) pauseMenu.OnButtonClick(group, index);
    }
}
