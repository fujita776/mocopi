using UnityEngine;

/// <summary>
/// ライブ翻訳システムの共通設定
/// 1つのアセットを全シーンで共有する
/// </summary>
[CreateAssetMenu(fileName = "LiveTranslationSettings", menuName = "Live Translation/Settings")]
public class LiveTranslationSettings : ScriptableObject
{
    [Header("Whisperモデル")]
    public string modelPath = "ggml-small.bin";
    public bool isModelPathInStreamingAssets = true;

    [Header("Whisper推論")]
    public bool useGpu = false;
    public bool flashAttention = false;

    [Header("Whisperストリーミング")]
    [Tooltip("処理間隔（秒）")]
    public float stepSec = 3f;
    [Tooltip("前セグメントから引き継ぐ秒数")]
    public float keepSec = 0.2f;
    [Tooltip("最大処理長（秒）")]
    public float lengthSec = 10f;
    public bool updatePrompt = true;
    public bool dropOldBuffer = true;
    public bool useVad = true;
    public bool noContext = true;

    [Header("AudioBridge")]
    [Tooltip("AudioSourceからの読み取り間隔（秒）")]
    public float chunkIntervalSeconds = 0.5f;

    [Header("字幕UI")]
    public float fadeDuration = 0.3f;
    public float displayDuration = 5f;
    public int fontSize = 32;

    [Header("ON/OFF切替キー")]
    public KeyCode subtitleToggleKey = KeyCode.F1;
    public KeyCode translationToggleKey = KeyCode.F2;

    [Header("幻覚フィルタ")]
    [Tooltip("Whisperが無音時に生成する既知のフレーズ（部分一致で除外）")]
    public string[] hallucinationFilter = new[]
    {
        "ご視聴ありがとうございました",
        "今日もお会いしましょう",
        "お疲れ様でした",
        "チャンネル登録よろしくお願いします",
        "ありがとうございました",
        "おやすみなさい",
        "Thank you for watching",
        "Thanks for watching",
        "See you next time",
        "Bye",
        "Thank you.",
        "(笑)",
    };
}
