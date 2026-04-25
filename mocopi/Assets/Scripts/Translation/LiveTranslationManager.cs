using System.Threading.Tasks;
using UnityEngine;
using Whisper;
using Whisper.Utils;

public enum ModeOverride
{
    UseSettings,   //  LiveTranslationSettings.defaultMode に従う
    ForceAuto,     //  このシーンでは自動認識
    ForcePushToTalk, //  このシーンではPTT
}

/// <summary>
/// ライブ翻訳パイプラインの統合管理
/// 設定はLiveTranslationSettings（ScriptableObject）で全シーン共通。
/// シーン固有の挙動はoverrideModeで上書き可能。
/// DefaultExecutionOrder=-100: WhisperManagerより先にAwakeを実行し、
/// initOnAwakeを無効化してモデルパス等の設定を反映できるようにする。
/// </summary>
[DefaultExecutionOrder(-100)]
public class LiveTranslationManager : MonoBehaviour
{
    [Header("共通設定")]
    [SerializeField] private LiveTranslationSettings settings;

    [Header("シーン固有のモード上書き")]
    [Tooltip("UseSettings以外を選ぶとこのシーンではSettings.defaultModeを上書きする")]
    [SerializeField] private ModeOverride overrideMode = ModeOverride.UseSettings;

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
    private bool _pttHeld;

    public TranslationMode EffectiveMode
    {
        get
        {
            return overrideMode switch
            {
                ModeOverride.ForceAuto => TranslationMode.Auto,
                ModeOverride.ForcePushToTalk => TranslationMode.PushToTalk,
                _ => settings != null ? settings.defaultMode : TranslationMode.Auto,
            };
        }
    }

    void Awake()
    {
        //  WhisperManagerの自動ロードを無効化（Start()で我々が制御するため）
        //  これによりApplySettings()のリフレクションが有効になる
        if (whisperManager != null)
        {
            SetPrivateField(whisperManager, "initOnAwake", false);
        }
    }

    async void Start()
    {
        _subtitleEnabled = PlayerPrefs.GetInt(PrefSubtitleEnabled, 1) == 1;
        _translationEnabled = PlayerPrefs.GetInt(PrefTranslationEnabled, 1) == 1;
        Debug.Log($"[LiveTranslation] 初期設定: 字幕={_subtitleEnabled}, 翻訳={_translationEnabled}, モード={EffectiveMode}");

        if (!ValidateComponents()) return;

        //  モデルファイルの存在チェック
        string modelPath = settings.isModelPathInStreamingAssets
            ? System.IO.Path.Combine(Application.streamingAssetsPath, settings.modelPath)
            : settings.modelPath;
        if (!System.IO.File.Exists(modelPath))
        {
            string msg = $"Whisperモデル ({settings.modelPath}) が見つかりません。\n\n" +
                "以下からダウンロードして Assets/StreamingAssets/ に配置してください:\n" +
                "https://www.dropbox.com/scl/fi/efo4ka56lvuriyalr5zkh/ggml-medium-q5_0.bin?rlkey=yy7x1pulr1od4z18o4e8ooni1&st=0ur27rb0&dl=0";
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
        _stream.OnSegmentUpdated += OnSegmentUpdated;

        audioBridge.SetChunkCallback(chunk => _stream.AddToStream(chunk));

        //  モードに応じて初期ストリーミング状態を設定
        audioBridge.SetStreamingEnabled(EffectiveMode == TranslationMode.Auto);

        _initialized = true;

        if (autoStart)
            StartTranslation();

        Debug.Log($"[LiveTranslation] 初期化完了（モード: {EffectiveMode}）");
    }

    void Update()
    {
        if (settings == null || !_initialized) return;

        //  F1: 字幕ON/OFF
        if (Input.GetKeyDown(settings.subtitleToggleKey))
            ToggleSubtitle();

        //  F2: 翻訳切替
        if (Input.GetKeyDown(settings.translationToggleKey))
            ToggleTranslation();

        //  PTT: キー押下中のみストリーミング
        if (EffectiveMode == TranslationMode.PushToTalk)
        {
            bool isHeld = Input.GetKey(settings.pushToTalkKey);
            if (isHeld != _pttHeld)
            {
                _pttHeld = isHeld;
                audioBridge.SetStreamingEnabled(isHeld);

                if (isHeld)
                {
                    subtitleUI.ShowProcessing();
                    Debug.Log("[LiveTranslation] PTT開始");
                }
                else
                {
                    Debug.Log("[LiveTranslation] PTT終了");
                }
            }
        }
    }

    private void ApplySettings()
    {
        //  publicフィールドをScriptableObjectから反映
        whisperManager.language = "ja";
        whisperManager.translateToEnglish = _translationEnabled;
        whisperManager.noContext = settings.noContext;
        whisperManager.useVad = settings.useVad;
        whisperManager.dropOldBuffer = settings.dropOldBuffer;
        whisperManager.stepSec = settings.stepSec;
        whisperManager.keepSec = settings.keepSec;
        whisperManager.lengthSec = settings.lengthSec;
        whisperManager.updatePrompt = settings.updatePrompt;
        whisperManager.initialPrompt = settings.initialPrompt;

        //  privateフィールドをリフレクションで反映（モデルパス等）
        //  モデル未読み込みの場合のみ有効（ロード後の変更は無視される）
        if (!whisperManager.IsLoaded && !whisperManager.IsLoading)
        {
            SetPrivateField(whisperManager, "modelPath", settings.modelPath);
            SetPrivateField(whisperManager, "isModelPathInStreamingAssets", settings.isModelPathInStreamingAssets);
            SetPrivateField(whisperManager, "useGpu", settings.useGpu);
            SetPrivateField(whisperManager, "flashAttention", settings.flashAttention);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            field.SetValue(target, value);
    }

    public void ToggleSubtitle()
    {
        _subtitleEnabled = !_subtitleEnabled;
        PlayerPrefs.SetInt(PrefSubtitleEnabled, _subtitleEnabled ? 1 : 0);
        PlayerPrefs.Save();

        string msg = _subtitleEnabled ? "字幕 ON" : "字幕 OFF";
        Debug.Log($"[LiveTranslation] {msg}");

        if (_subtitleEnabled)
        {
            subtitleUI.ShowNotification(msg, settings.notificationDuration);
        }
        else
        {
            subtitleUI.HideSubtitle();
        }
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

        string msg = _translationEnabled ? "翻訳 ON (英語)" : "翻訳 OFF (日本語)";
        Debug.Log($"[LiveTranslation] {msg}");

        if (_subtitleEnabled)
            subtitleUI.ShowNotification(msg, settings.notificationDuration);
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
            _stream.OnSegmentUpdated -= OnSegmentUpdated;
            audioBridge.SetChunkCallback(null);
        }

        var freq = AudioSettings.outputSampleRate;
        _stream = await whisperManager.CreateStream(freq, 1);
        _stream.OnSegmentUpdated += OnSegmentUpdated;
        _stream.StartStream();

        audioBridge.SetChunkCallback(chunk => _stream.AddToStream(chunk));

        //  モードに応じてストリーミング状態を復元
        bool streaming = EffectiveMode == TranslationMode.Auto || _pttHeld;
        audioBridge.SetStreamingEnabled(streaming);
    }

    void OnDisable() => CleanupAll();
    void OnDestroy() => CleanupAll();
    void OnApplicationQuit() => CleanupAll();

    private void CleanupAll()
    {
        if (_stream != null)
        {
            _stream.OnSegmentUpdated -= OnSegmentUpdated;
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

    private void OnSegmentUpdated(WhisperResult segment)
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
