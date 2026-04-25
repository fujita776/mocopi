using UnityEngine;

public enum TranslationMode
{
    Auto,         //  常にマイクを認識
    PushToTalk,   //  キー押下中のみ認識
}

/// <summary>
/// ライブ翻訳システムの共通設定
/// 1つのアセットを全シーンで共有し、各シーンで一部オーバーライド可能
/// </summary>
[CreateAssetMenu(fileName = "LiveTranslationSettings", menuName = "Live Translation/Settings")]
public class LiveTranslationSettings : ScriptableObject
{
    [Header("動作モード")]
    [Tooltip("既定の認識モード。各シーンのLiveTranslationManagerで上書き可能")]
    public TranslationMode defaultMode = TranslationMode.Auto;

    [Header("Whisperモデル")]
    public string modelPath = "ggml-medium-q5_0.bin";
    public bool isModelPathInStreamingAssets = true;

    [Header("Whisper推論")]
    [Tooltip("Metal/Vulkan GPUを使う (macOS/Windowsで有効)")]
    public bool useGpu = true;
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

    [Header("Whisper認識ヒント")]
    [Tooltip("Whisperに与える初期プロンプト。日本語認識を誘導するのに有効")]
    [TextArea(2, 4)]
    public string initialPrompt = "以下は日本語の会話です。";

    [Header("AudioBridge")]
    [Tooltip("AudioSourceからの読み取り間隔（秒）")]
    public float chunkIntervalSeconds = 0.5f;

    [Header("字幕UI")]
    public float fadeDuration = 0.3f;
    public float displayDuration = 5f;
    public int fontSize = 32;

    [Header("キー割り当て")]
    public KeyCode subtitleToggleKey = KeyCode.F1;
    public KeyCode translationToggleKey = KeyCode.F2;
    [Tooltip("プッシュトゥトーク中に押しっぱなしにするキー")]
    public KeyCode pushToTalkKey = KeyCode.T;

    [Header("キー押下時の通知表示")]
    public float notificationDuration = 1.5f;

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
