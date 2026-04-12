using System.Threading.Tasks;
using UnityEngine;
using Whisper;
using Whisper.Utils;

/// <summary>
/// ライブ翻訳パイプラインの統合管理
/// 既存のuLipSyncMicrophoneの音声 → whisper.unity（STT/翻訳） → 字幕UI
/// Whisper内蔵の翻訳機能を使い、opus-mtは不要。
/// </summary>
public class LiveTranslationManager : MonoBehaviour
{
    [Header("Whisper STT")]
    [SerializeField] private WhisperManager whisperManager;

    [Header("音声ブリッジ（既存AudioSourceからデータ取得）")]
    [SerializeField] private AudioBridge audioBridge;

    [Header("UI")]
    [SerializeField] private SubtitleUI subtitleUI;

    [Header("動作設定")]
    [SerializeField] private bool autoStart = true;

    [Header("ON/OFF切替キー")]
    [Tooltip("字幕の表示ON/OFFを切り替えるキー")]
    [SerializeField] private KeyCode subtitleToggleKey = KeyCode.F1;
    [Tooltip("翻訳ON/OFFを切り替えるキー（ON=英語, OFF=日本語）")]
    [SerializeField] private KeyCode translationToggleKey = KeyCode.F2;

    private const string PrefSubtitleEnabled = "LiveTranslation.SubtitleEnabled";
    private const string PrefTranslationEnabled = "LiveTranslation.TranslationEnabled";

    private WhisperStream _stream;
    private bool _initialized;
    private bool _subtitleEnabled = true;
    private bool _translationEnabled = true;

    async void Start()
    {
        _subtitleEnabled = PlayerPrefs.GetInt(PrefSubtitleEnabled, 1) == 1;
        _translationEnabled = PlayerPrefs.GetInt(PrefTranslationEnabled, 1) == 1;
        Debug.Log($"[LiveTranslation] 初期設定: 字幕={_subtitleEnabled}, 翻訳={_translationEnabled}");

        if (!ValidateComponents()) return;

        audioBridge.AutoDetectAudioSource();
        if (!audioBridge.HasSource)
        {
            Debug.LogError("[LiveTranslation] AudioBridge: 音声ソースが見つかりません");
            enabled = false;
            return;
        }

        //  Whisper設定
        whisperManager.language = "ja";
        whisperManager.translateToEnglish = _translationEnabled;

        //  モデル読み込み
        if (!whisperManager.IsLoaded && !whisperManager.IsLoading)
        {
            Debug.Log("[LiveTranslation] Whisperモデル読み込み開始...");
            await whisperManager.InitModel();
        }

        while (whisperManager.IsLoading)
            await Task.Yield();

        if (!whisperManager.IsLoaded)
        {
            Debug.LogError("[LiveTranslation] Whisperモデルの読み込みに失敗");
            enabled = false;
            return;
        }

        Debug.Log("[LiveTranslation] Whisperモデル読み込み完了");

        //  マイクなしでストリーム作成（手動モード）
        var freq = AudioSettings.outputSampleRate;
        _stream = await whisperManager.CreateStream(freq, 1);
        _stream.OnResultUpdated += OnResult;

        audioBridge.SetChunkCallback(chunk => _stream.AddToStream(chunk));

        _initialized = true;

        if (autoStart)
            StartTranslation();

        Debug.Log("[LiveTranslation] 初期化完了");
    }

    void Update()
    {
        if (Input.GetKeyDown(subtitleToggleKey))
            ToggleSubtitle();

        if (Input.GetKeyDown(translationToggleKey))
            ToggleTranslation();
    }

    public void ToggleSubtitle()
    {
        _subtitleEnabled = !_subtitleEnabled;
        PlayerPrefs.SetInt(PrefSubtitleEnabled, _subtitleEnabled ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"[LiveTranslation] 字幕: {(_subtitleEnabled ? "ON" : "OFF")}");

        if (!_subtitleEnabled)
            subtitleUI.HideSubtitle();
    }

    public async void ToggleTranslation()
    {
        if (!_initialized) return;

        _translationEnabled = !_translationEnabled;
        PlayerPrefs.SetInt(PrefTranslationEnabled, _translationEnabled ? 1 : 0);
        PlayerPrefs.Save();

        //  Whisperの翻訳モードを切替
        whisperManager.translateToEnglish = _translationEnabled;

        //  ストリームを再作成して設定を反映
        await RecreateStream();

        Debug.Log($"[LiveTranslation] 翻訳: {(_translationEnabled ? "ON (英語)" : "OFF (日本語)")}");
    }

    private async Task RecreateStream()
    {
        //  旧ストリームを解除
        if (_stream != null)
        {
            _stream.OnResultUpdated -= OnResult;
            audioBridge.SetChunkCallback(null);
        }

        //  新ストリームを作成
        var freq = AudioSettings.outputSampleRate;
        _stream = await whisperManager.CreateStream(freq, 1);
        _stream.OnResultUpdated += OnResult;
        _stream.StartStream();

        audioBridge.SetChunkCallback(chunk => _stream.AddToStream(chunk));
        audioBridge.ResetPosition();
    }

    void OnDisable() => CleanupAll();
    void OnDestroy() => CleanupAll();
    void OnApplicationQuit() => CleanupAll();

    private void CleanupAll()
    {
        if (_stream != null)
        {
            _stream.OnResultUpdated -= OnResult;
            _stream = null;
        }

        if (audioBridge != null)
            audioBridge.SetChunkCallback(null);
    }

    public void StartTranslation()
    {
        if (!_initialized) return;

        _stream.StartStream();
        audioBridge.ResetPosition();

        Debug.Log("[LiveTranslation] パイプライン開始");
    }

    private void OnResult(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!_subtitleEnabled) return;

        Debug.Log($"[LiveTranslation] 結果: {text}");
        subtitleUI.ShowSubtitle(text);
    }

    private bool ValidateComponents()
    {
        if (whisperManager == null)
            whisperManager = GetComponentInChildren<WhisperManager>();
        if (audioBridge == null)
            audioBridge = GetComponentInChildren<AudioBridge>();
        if (subtitleUI == null)
            subtitleUI = FindObjectOfType<SubtitleUI>();

        if (whisperManager == null || audioBridge == null || subtitleUI == null)
        {
            Debug.LogError("[LiveTranslation] 必要なコンポーネントが見つかりません: " +
                "WhisperManager, AudioBridge, SubtitleUI");
            enabled = false;
            return false;
        }

        return true;
    }
}
