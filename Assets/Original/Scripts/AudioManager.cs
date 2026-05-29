using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // インスタンス
    public static AudioManager instance;

    // オーディオソース
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource seSource;

    // データリスト
    [SerializeField] private AudioData[] bgmDatas;
    [SerializeField] private AudioData[] seDatas;

    // データリスト（名前とファイルの一致）
    private Dictionary<string, AudioClip> bgmDictionary;
    private Dictionary<string, AudioClip> seDictionary;


    void Awake()
    {
        // データをまとめる辞書を生成
        bgmDictionary = new Dictionary<string, AudioClip>();
        seDictionary  = new Dictionary<string, AudioClip>();
    }


    // BGMデータリストを設定する
    public void SetBGMData(AudioData newBGMDatas)
    {
        // 既にデータが入っている場合、初期化する
        if (bgmDictionary != null)
            bgmDictionary.Clear();

        // オーディオデータ(BGM)の設定
        foreach (AudioData bgm in bgmDatas)
        {
            bgmDictionary[bgm.name] = bgm.audioClip;
        }

    }

    // SEデータリストを設定する
    public void SetSEData(AudioData newSEDatas)
    {
        // 既にデータが入っている場合、初期化する
        if (seDictionary != null)
            seDictionary.Clear();

        // オーディオデータ(SE)の設定
        foreach (AudioData se in seDatas)
        {
            seDictionary[se.name] = se.audioClip;
        }

    }

    // BGMを再生する
    public void PlayBGM(string name)
    {
        // 引数で渡されたBGMを見つけたら再生　なければ警告
        if (bgmDictionary.ContainsKey(name))
        {
            bgmSource.clip = bgmDictionary[name];   ///< クリップに登録
            bgmSource.loop = true;                  ///< ループ再生設定をオンにする
            bgmSource.Play();                       ///< 再生する
        }
        else
            Debug.LogWarning("BGMが見つかりませんでした。：" + name);
    }


    // SEを再生する
    public void PlaySE(string name)
    {
        // 引数で渡されたSEを見つけたら再生　なければ警告
        if (seDictionary.ContainsKey(name))
            seSource.PlayOneShot(seDictionary[name]);   ///< 一度だけ再生
        else
            Debug.LogWarning("SEが見つかりませんでした。：" + name);
    }
}


[System.Serializable]
public class AudioData
{
    public string name;
    public AudioClip audioClip;
}