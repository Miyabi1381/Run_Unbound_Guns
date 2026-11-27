using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// 設定画面（SettingsPanelに付ける）
// ・上部のタブボタンで「音量/感度」「操作」のページを切り替える
// ・スライダーとGameSettingsをつなぎ、動かした瞬間に反映、画面を閉じた時に保存する
public class SettingsMenu : MonoBehaviour
{
    // タブ1つ分のデータ
    [Serializable]
    public class Tab
    {
        [Tooltip("タブのボタン（クリックでこのタブに切り替わる）")]
        public Button button;
        [Tooltip("タブを選んだ時に表示するページ")]
        public GameObject page;
        [Tooltip("選択中かどうかで色を変える画像（任意。空ならボタンのImageを使う）")]
        public Graphic highlight;
    }

    // スライダー1本分のデータ
    [Serializable]
    public class SliderRow
    {
        [Tooltip("スライダー")]
        public Slider slider;
        [Tooltip("値を表示するテキスト（任意）")]
        public TextMeshProUGUI valueText;
    }

    [Header("タブ（左から順に）")]
    [SerializeField] private Tab[] tabs = new Tab[2];
    [Tooltip("選択中のタブの色")]
    [SerializeField] private Color selectedTabColor   = new Color(1f, 1f, 1f, 0.4f);
    [Tooltip("選択していないタブの色")]
    [SerializeField] private Color unselectedTabColor = new Color(1f, 1f, 1f, 0.08f);

    [Header("音量（0～1）")]
    [SerializeField] private SliderRow masterVolume;
    [SerializeField] private SliderRow bgmVolume;
    [SerializeField] private SliderRow seVolume;
    [Tooltip("音量の表示の書式（{0}に0～100が入る）")]
    [SerializeField] private string volumeFormat = "{0}";

    [Header("感度（倍率）")]
    [SerializeField] private SliderRow normalSensitivity;
    [SerializeField] private SliderRow adsSensitivity;
    [Tooltip("感度の表示の書式（{0}に倍率が入る。{0:0.0}で小数1桁）")]
    [SerializeField] private string sensitivityFormat = "{0:0.0}";

    [Header("入力")]
    [Tooltip("キーボード・コントローラーでタブを切り替えられるようにするか（Q/E・LB/RB）")]
    [SerializeField] private bool useTabShortcut = true;

    // 選択中のタブの番号
    private int tabIndex;


    void Awake()
    {
        // タブボタンのクリックを登録する（番号をラムダ式に渡すため、ループ変数をコピーしておく）
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            if (tabs[i].button != null)
                tabs[i].button.onClick.AddListener(() => SelectTab(index));
        }

        // スライダーの範囲を設定し、動かした時の処理を登録する
        SetupSlider(masterVolume,      0f, 1f, GameSettings.SetMasterVolume);
        SetupSlider(bgmVolume,         0f, 1f, GameSettings.SetBGMVolume);
        SetupSlider(seVolume,          0f, 1f, GameSettings.SetSEVolume);
        SetupSlider(normalSensitivity, GameSettings.MinSensitivity, GameSettings.MaxSensitivity, GameSettings.SetNormalSensitivity);
        SetupSlider(adsSensitivity,    GameSettings.MinSensitivity, GameSettings.MaxSensitivity, GameSettings.SetADSSensitivity);
    }


    // 設定画面が表示された時
    void OnEnable()
    {
        // 保存されている値をスライダーに反映し、最初のタブから始める
        RefreshAll();
        SelectTab(0);
    }


    // 設定画面が閉じられた時
    void OnDisable()
    {
        // 変更をファイルに保存する
        GameSettings.Save();

        // Unity側の選択を外しておく（閉じた後にキー操作でスライダーが動かないように）
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }


    void Update()
    {
        if (!useTabShortcut || tabs.Length == 0) return;

        // Q / LB で左のタブ、E / RB で右のタブ
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Joystick1Button4))
            SelectTab((tabIndex - 1 + tabs.Length) % tabs.Length);
        else if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Joystick1Button5))
            SelectTab((tabIndex + 1) % tabs.Length);
    }


    // ==================================================================================
    // タブ
    // ==================================================================================

    // タブを切り替える（タブボタンのクリックからも呼ばれる）
    public void SelectTab(int index)
    {
        if (index < 0 || index >= tabs.Length) return;

        tabIndex = index;

        for (int i = 0; i < tabs.Length; i++)
        {
            bool selected = i == tabIndex;

            // 選択中のページだけ表示する
            if (tabs[i].page != null) tabs[i].page.SetActive(selected);

            // タブの色を変える
            Graphic graphic = tabs[i].highlight != null ? tabs[i].highlight
                            : tabs[i].button    != null ? tabs[i].button.targetGraphic : null;
            if (graphic != null) graphic.color = selected ? selectedTabColor : unselectedTabColor;
        }
    }


    // ==================================================================================
    // スライダー
    // ==================================================================================

    // スライダーの範囲と、動かした時の処理を設定する
    void SetupSlider(SliderRow row, float min, float max, Action<float> onChanged)
    {
        if (row == null || row.slider == null) return;

        row.slider.minValue     = min;
        row.slider.maxValue     = max;
        row.slider.wholeNumbers = false;

        row.slider.onValueChanged.AddListener(value =>
        {
            onChanged(value);
            UpdateValueText(row);
        });
    }


    // 全てのスライダーと表示を、今の設定の値に合わせる
    void RefreshAll()
    {
        // SetValueWithoutNotifyで、値を変えても「動かした時の処理」を呼ばないようにする
        SetSliderValue(masterVolume,      GameSettings.MasterVolume);
        SetSliderValue(bgmVolume,         GameSettings.BGMVolume);
        SetSliderValue(seVolume,          GameSettings.SEVolume);
        SetSliderValue(normalSensitivity, GameSettings.NormalSensitivity);
        SetSliderValue(adsSensitivity,    GameSettings.ADSSensitivity);
    }


    void SetSliderValue(SliderRow row, float value)
    {
        if (row == null || row.slider == null) return;

        row.slider.SetValueWithoutNotify(value);
        UpdateValueText(row);
    }


    // 値のテキストを更新する
    void UpdateValueText(SliderRow row)
    {
        if (row == null || row.slider == null || row.valueText == null) return;

        float value = row.slider.value;

        // 音量のスライダーは0～100、感度のスライダーは倍率で表示する
        bool isVolume = row == masterVolume || row == bgmVolume || row == seVolume;
        row.valueText.text = isVolume
            ? string.Format(volumeFormat, Mathf.RoundToInt(value * 100f))
            : string.Format(sensitivityFormat, value);
    }


    // ==================================================================================
    // 初期値に戻す（「初期値に戻す」ボタンのOnClickに登録して使う）
    // ==================================================================================

    // 音量と感度を初期値に戻す
    public void ResetSoundAndSensitivity()
    {
        GameSettings.ResetVolume();
        GameSettings.ResetSensitivity();
        RefreshAll();
    }
}
