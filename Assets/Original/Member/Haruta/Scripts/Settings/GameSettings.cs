using System;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

// プレイヤーが変更する設定（音量・感度）を持ち、保存・読み込み・反映を行う
// staticクラスなのでシーンに置く必要はなく、ゲーム起動時に自動で読み込んで反映する
// 他のスクリプトは GameSettings.NormalSensitivity などで値を読む
public static class GameSettings
{
    // ==================================================================================
    // 保存するデータ（JsonUtilityでJSONにするため、publicフィールドのクラスにする）
    // ==================================================================================
    [Serializable]
    private class SettingsData
    {
        // データの形式のバージョン（項目を増やした時に古いファイルを見分けるため）
        public int version = 1;

        // 音量（0～1）
        public float masterVolume = DefaultVolume;
        public float bgmVolume    = DefaultVolume;
        public float seVolume     = DefaultVolume;

        // 感度（倍率。1で元の感度）
        public float normalSensitivity = DefaultSensitivity;
        public float adsSensitivity    = DefaultSensitivity;

        // ★キーコンフィグを追加する時はここに項目を足す
    }

    // ==================================================================================
    // 初期値・範囲
    // ==================================================================================
    public const float DefaultVolume      = 0.8f;
    public const float MinSensitivity     = 0.1f;
    public const float MaxSensitivity     = 3.0f;
    public const float DefaultSensitivity = 1.0f;

    // 保存先のファイル名（Application.persistentDataPathの中に作る）
    private const string FileName = "settings.json";

    // BGM・SEを分けて音量調整するためのAudioMixer（Resourcesフォルダ内のパス。無ければ全体音量だけ反映する）
    private const string MixerPath = "Audios/GameAudioMixer";
    // AudioMixerで公開（Expose）するパラメーター名
    private const string MixerMasterParam = "MasterVolume";
    private const string MixerBGMParam    = "BGMVolume";
    private const string MixerSEParam     = "SEVolume";

    // ==================================================================================
    // 値の取得（読み取り専用。変更はSetXxxから行う）
    // ==================================================================================
    public static float MasterVolume      => data.masterVolume;
    public static float BGMVolume         => data.bgmVolume;
    public static float SEVolume          => data.seVolume;
    public static float NormalSensitivity => data.normalSensitivity;
    public static float ADSSensitivity    => data.adsSensitivity;

    // 値が変わった時の通知（感度などを使う側が、変わった瞬間に反映したい時に登録する）
    public static event Action OnChanged;

    // 今の設定
    private static SettingsData data = new SettingsData();
    // 保存していない変更があるか
    private static bool isDirty;
    // 読み込んだAudioMixer（無ければnull）
    private static AudioMixer mixer;

    // 保存先のフルパス
    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);


    // ==================================================================================
    // 値の変更（範囲内に収めてから反映する。保存はSaveで行う）
    // ==================================================================================
    public static void SetMasterVolume(float value)      { data.masterVolume      = Mathf.Clamp01(value); Changed(); }
    public static void SetBGMVolume(float value)         { data.bgmVolume         = Mathf.Clamp01(value); Changed(); }
    public static void SetSEVolume(float value)          { data.seVolume          = Mathf.Clamp01(value); Changed(); }
    public static void SetNormalSensitivity(float value) { data.normalSensitivity = ClampSensitivity(value); Changed(); }
    public static void SetADSSensitivity(float value)    { data.adsSensitivity    = ClampSensitivity(value); Changed(); }


    // 音量を初期値に戻す
    public static void ResetVolume()
    {
        data.masterVolume = DefaultVolume;
        data.bgmVolume    = DefaultVolume;
        data.seVolume     = DefaultVolume;
        Changed();
    }


    // 感度を初期値に戻す
    public static void ResetSensitivity()
    {
        data.normalSensitivity = DefaultSensitivity;
        data.adsSensitivity    = DefaultSensitivity;
        Changed();
    }


    // ==================================================================================
    // 保存・読み込み
    // ==================================================================================

    // 変更があればファイルに保存する（設定画面を閉じた時に呼ぶ）
    public static void Save()
    {
        if (!isDirty) return;

        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            isDirty = false;
        }
        catch (Exception e)
        {
            // 保存に失敗してもゲームは止めない（次に閉じた時にもう一度試す）
            Debug.LogWarning("設定の保存に失敗しました：" + e.Message);
        }
    }


    // ファイルから読み込む（無い・壊れている時は初期値）
    public static void Load()
    {
        data = new SettingsData();

        try
        {
            if (File.Exists(FilePath))
            {
                // 初期値の入ったデータに上書きするので、ファイルに無い項目は初期値のまま残る
                JsonUtility.FromJsonOverwrite(File.ReadAllText(FilePath), data);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("設定の読み込みに失敗したため、初期値を使います：" + e.Message);
            data = new SettingsData();
        }

        // ファイルを手で書き換えられていても範囲内に収める
        data.masterVolume      = Mathf.Clamp01(data.masterVolume);
        data.bgmVolume         = Mathf.Clamp01(data.bgmVolume);
        data.seVolume          = Mathf.Clamp01(data.seVolume);
        data.normalSensitivity = ClampSensitivity(data.normalSensitivity);
        data.adsSensitivity    = ClampSensitivity(data.adsSensitivity);

        isDirty = false;
        Apply();
    }


    // ==================================================================================
    // 反映
    // ==================================================================================

    // 値をゲームに反映する
    public static void Apply()
    {
        if (mixer != null)
        {
            // AudioMixerがある時は、全体・BGM・SEをそれぞれのグループに反映する
            AudioListener.volume = 1f;
            mixer.SetFloat(MixerMasterParam, ToDecibel(data.masterVolume));
            mixer.SetFloat(MixerBGMParam,    ToDecibel(data.bgmVolume));
            mixer.SetFloat(MixerSEParam,     ToDecibel(data.seVolume));
        }
        else
        {
            // AudioMixerが無い時は、全体音量だけをAudioListenerで反映する
            AudioListener.volume = data.masterVolume;
        }

        // 感度は使う側（MouseLookScriptなど）が GameSettings.NormalSensitivity を読んで反映する
        OnChanged?.Invoke();
    }


    // 値が変わった時の共通処理
    static void Changed()
    {
        isDirty = true;
        Apply();
    }


    static float ClampSensitivity(float value)
    {
        return Mathf.Clamp(value, MinSensitivity, MaxSensitivity);
    }


    // 0～1の音量をAudioMixer用のデシベル（-80～0）に変換する
    static float ToDecibel(float volume)
    {
        return volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20f;
    }


    // ==================================================================================
    // 初期化
    // ==================================================================================

    // ゲーム起動時（最初のシーンを読み込む前）に自動で読み込む
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        // 前回のプレイで登録された通知を消しておく（ドメインのリロードを切っている時用）
        OnChanged = null;

        mixer = Resources.Load<AudioMixer>(MixerPath);
        Load();

        // AudioMixerへの反映は起動直後だと効かないことがあるので、シーンを読み込むたびにもう一度反映する
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 保存せずにゲームを終了した時のために、終了時にも保存する
        Application.quitting -= Save;
        Application.quitting += Save;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Apply();
    }
}
