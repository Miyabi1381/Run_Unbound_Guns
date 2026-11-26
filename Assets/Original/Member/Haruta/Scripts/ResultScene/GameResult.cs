using UnityEngine;

// リザルトの種類
public enum ResultType
{
    None,       // 結果なし（ResultSceneを単体で再生した時など）
    Clear,      // ステージクリア
    GameOver    // ゲームオーバー
}

// ステージの結果をResultSceneへ渡すためのデータ
// staticクラスなのでシーンをまたいでも値が残り、GameManagerやDontDestroyOnLoadに頼らずに受け渡しできる
public static class GameResult
{
    // 結果の種類
    public static ResultType Type { get; private set; } = ResultType.None;
    // 結果が出たステージのシーン名
    public static string StageName { get; private set; } = "";

    // 戦績
    // 撃破数
    public static int   KillCount   { get; private set; }
    // 到達WAVE
    public static int   ReachedWave { get; private set; }
    // プレイ時間（秒）
    public static float PlayTime    { get; private set; }

    // 結果が記録されているか
    public static bool HasResult => Type != ResultType.None;


    // 結果を記録する
    public static void Set(ResultType type, string stageName)
    {
        Type      = type;
        StageName = stageName;
    }


    // 戦績を記録する（結果とは別に呼ぶ。呼ばなければ全て0のまま）
    public static void SetStats(int killCount, int reachedWave, float playTime)
    {
        KillCount   = killCount;
        ReachedWave = reachedWave;
        PlayTime    = playTime;
    }


    // 結果を初期化する（タイトルへ戻る時に呼ぶ）
    public static void Clear()
    {
        Type        = ResultType.None;
        StageName   = "";
        KillCount   = 0;
        ReachedWave = 0;
        PlayTime    = 0f;
    }


    // 再生開始時に必ず初期化する
    // （エディタの設定で「ドメインのリロード」を切っていると、static変数が前回のプレイの値のまま残るため）
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        Clear();
    }
}
