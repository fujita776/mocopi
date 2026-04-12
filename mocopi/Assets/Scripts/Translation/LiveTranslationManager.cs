using System.Threading.Tasks;
using UnityEngine;
using Whisper;
using Whisper.Utils;

/// <summary>
/// ライブ翻訳パイプラインの統合管理
/// 設定はLiveTranslationSettings（ScriptableObject）で全シーン共通化
/// </summary>
public class LiveTranslationManager : MonoBehaviour
{
    [Header("共通設定")]
    [SerializeField] private LiveTranslationSettings settings;

    [Header("コンポーネント")]
    [SerializeField] private WhisperManager whisperManager;
    [SerializeField] private AudioBridge audioBridge;
    [SerializeField] private SubtitleUI subtitleUI;

    [Header("動作設定")]
    [SerializeField] private bool autoStart = true;

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

        //  モデルファイルの存在チェック
        string modelPath = settings.isModelPathInStreamingAssets
            ? System.IO.Path.Combine(Application.streamingAssetsPath, settings.modelPath)
            : settings.modelPath;
        if (!System.IO.File.Exists(modelPath))
        {
            string msg = $"Whisperモデル ({settings.modelPath}) が見つかりません。\n\n" +
                "Dropboxからダウンロードして Assets/StreamingAssets/ に配置してください。";
            Debug.LogError($"[LiveTranslation] {msg}");
#if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog("Live Translation - モデル未配置", msg, "OK");
#endif
            enabled = false;
            return;
        }

        //  ScriptableObjectからWhisperManager設定を適用
        ApplySettings();

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

        //  AudioBridge設定
        audioBridge.AutoDetectAudioSource();
        if (!audioBridge.HasSource)
        {
            Debug.LogError("[LiveTranslation] AudioBridge: 音声ソースが見つかりません");
            enabled = false;
            return;
        }

        //  MicrophoneRecordのAudioSourceをミュート（再生音防止）
        var micAudioSource = GetComponent<AudioSource>();
        if (micAudioSource != null)
        {
            micAudioSource.volume = 0f;
            micAudioSource.mute = true;
        }

        //  ストリーム作成
        var freq = AudioSettings.outputSampleRate;
        _stream = await whisperManager.CreateStream(freq, 1);
        _stream.OnSegmentFinished += OnSegmentFinished;

        audioBridge.SetChunkCallback(chunk => _stream.AddToStream(chunk));

        _initialized = true;

        if (autoStart)
            StartTranslation();

        Debug.Log("[LiveTranslation] 初期化完了");
    }

    void Update()
    {
        if (settings == null) return;

        if (Input.GetKeyDown(settings.subtitleToggleKey))
            ToggleSubtitle();

        if (Input.GetKeyDown(settings.translationToggleKey))
            ToggleTranslation();
    }

    private void ApplySettings()
    {
        //  public フィールドをScriptableObjectから反映
        whisperManager.language = "ja";
        whisperManager.translateToEnglish = _translationEnabled;
        whisperManager.noContext = settings.noContext;
        whisperManager.useVad = settings.useVad;
        whisperManager.dropOldBuffer = settings.dropOldBuffer;
        whisperManager.stepSec = settings.stepSec;
        whisperManager.keepSec = settings.keepSec;
        whisperManager.lengthSec = settings.lengthSec;
        whisperManager.updatePrompt = settings.updatePrompt;

        //  privateフィールド（modelPath等）はEditorセットアップ時に設定済み
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

        whisperManager.translateToEnglish = _translationEnabled;
        ForceUpdateWhisperParams();
        await RecreateStream();

        Debug.Log($"[LiveTranslation] 翻訳: {(_translationEnabled ? "ON (英語)" : "OFF (日本語)")}");
    }

    private void ForceUpdateWhisperParams()
    {
        var method = whisperManager.GetType().GetMethod("UpdateParams",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (method != null)
            method.Invoke(whisperManager, null);
    }

    private async Task RecreateStream()
    {
        if (_stream != null)
        {
            _stream.OnSegmentFinished -= OnSegmentFinished;
            audioBridge.SetChunkCallback(null);
        }

        var freq = AudioSettings.outputSampleRate;
        _stream = await whisperManager.CreateStream(freq, 1);
        _stream.OnSegmentFinished += OnSegmentFinished;
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
            _stream.OnSegmentFinished -= OnSegmentFinished;
            _stream = null;
        }

        if (audioBridge != null)
            audioBridge.SetChunkCallback(null);

        foreach (var device in Microphone.devices)
        {
            if (Microphone.IsRecording(device))
                Microphone.End(device);
        }
    }

    public void StartTranslation()
    {
        if (!_initialized) return;

        _stream.StartStream();
        audioBridge.ResetPosition();

        Debug.Log("[LiveTranslation] パイプライン開始");
    }

    private void OnSegmentFinished(WhisperResult segment)
    {
        if (segment == null) return;
        string text = segment.Result?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!_subtitleEnabled) return;

        //  幻覚フィルタ
        if (settings != null && settings.hallucinationFilter != null)
        {
            foreach (var h in settings.hallucinationFilter)
            {
                if (text.Contains(h))
                {
                    Debug.Log($"[LiveTranslation] 幻覚フィルタ: \"{text}\"");
                    return;
                }
            }
        }

        Debug.Log($"[LiveTranslation] セグメント: {text}");
        subtitleUI.ShowSubtitle(text);
    }

    private bool ValidateComponents()
    {
        if (settings == null)
        {
            Debug.LogError("[LiveTranslation] LiveTranslationSettings が設定されていません");
            enabled = false;
            return false;
        }
        if (whisperManager == null)
            whisperManager = GetComponentInChildren<WhisperManager>();
        if (audioBridge == null)
            audioBridge = GetComponentInChildren<AudioBridge>();
        if (subtitleUI == null)
            subtitleUI = FindObjectOfType<SubtitleUI>();

        if (whisperManager == null || audioBridge == null || subtitleUI == null)
        {
            Debug.LogError("[LiveTranslation] 必要なコンポーネントが見つかりません");
            enabled = false;
            return false;
        }
        return true;
    }

}
