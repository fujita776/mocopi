using System.Collections;
using UnityEngine;

/// <summary>
/// ライブ翻訳パイプラインの統合管理
/// マイク音声 → Whisper(STT) → opus-mt(ja→en翻訳) → 字幕UI
/// </summary>
public class LiveTranslationManager : MonoBehaviour
{
    [Header("コンポーネント")]
    [SerializeField] private AudioChunkProvider audioChunkProvider;
    [SerializeField] private WhisperProcessor whisperProcessor;
    [SerializeField] private TranslationProcessor translationProcessor;
    [SerializeField] private SubtitleUI subtitleUI;

    [Header("設定")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool warmupOnStart = true;

    private bool _initialized;
    private bool _running;
    private Coroutine _pipelineCoroutine;

    void Start()
    {
        if (!ValidateComponents()) return;

        if (!InitializeProcessors()) return;

        if (warmupOnStart)
        {
            StartCoroutine(WarmupAndStart());
        }
        else if (autoStart)
        {
            StartTranslation();
        }
    }

    void OnDestroy()
    {
        StopTranslation();
    }

    /// <summary>
    /// 翻訳パイプラインを開始
    /// </summary>
    public void StartTranslation()
    {
        if (!_initialized || _running) return;

        _running = true;
        audioChunkProvider.onChunkReady.AddListener(OnAudioChunkReady);
        Debug.Log("[LiveTranslationManager] 翻訳パイプライン開始");
    }

    /// <summary>
    /// 翻訳パイプラインを停止
    /// </summary>
    public void StopTranslation()
    {
        _running = false;

        if (audioChunkProvider != null)
        {
            audioChunkProvider.onChunkReady.RemoveListener(OnAudioChunkReady);
        }

        if (_pipelineCoroutine != null)
        {
            StopCoroutine(_pipelineCoroutine);
            _pipelineCoroutine = null;
        }
    }

    private bool ValidateComponents()
    {
        if (audioChunkProvider == null)
            audioChunkProvider = GetComponentInChildren<AudioChunkProvider>();
        if (whisperProcessor == null)
            whisperProcessor = GetComponentInChildren<WhisperProcessor>();
        if (translationProcessor == null)
            translationProcessor = GetComponentInChildren<TranslationProcessor>();
        if (subtitleUI == null)
            subtitleUI = GetComponentInChildren<SubtitleUI>();

        if (audioChunkProvider == null || whisperProcessor == null ||
            translationProcessor == null || subtitleUI == null)
        {
            Debug.LogError("[LiveTranslationManager] 必要なコンポーネントが見つかりません。" +
                "AudioChunkProvider, WhisperProcessor, TranslationProcessor, SubtitleUI を設定してください");
            enabled = false;
            return false;
        }

        return true;
    }

    private bool InitializeProcessors()
    {
        if (!whisperProcessor.Initialize())
        {
            Debug.LogError("[LiveTranslationManager] WhisperProcessor の初期化に失敗");
            enabled = false;
            return false;
        }

        if (!translationProcessor.Initialize())
        {
            Debug.LogError("[LiveTranslationManager] TranslationProcessor の初期化に失敗");
            enabled = false;
            return false;
        }

        _initialized = true;
        return true;
    }

    private IEnumerator WarmupAndStart()
    {
        Debug.Log("[LiveTranslationManager] ウォームアップ中...");
        subtitleUI.ShowProcessing();

        yield return whisperProcessor.Warmup();

        subtitleUI.HideSubtitle();

        if (autoStart)
        {
            StartTranslation();
        }
    }

    private void OnAudioChunkReady(float[] audioChunk)
    {
        if (!_running) return;

        //  前の処理がまだ実行中なら新しいチャンクはスキップ
        if (whisperProcessor.IsProcessing || translationProcessor.IsProcessing) return;

        _pipelineCoroutine = StartCoroutine(ProcessPipeline(audioChunk));
    }

    private IEnumerator ProcessPipeline(float[] audioChunk)
    {
        subtitleUI.ShowProcessing();

        //  STT: 音声 → 日本語テキスト
        string japaneseText = null;
        yield return whisperProcessor.Transcribe(audioChunk, result => japaneseText = result);

        if (string.IsNullOrWhiteSpace(japaneseText))
        {
            subtitleUI.HideSubtitle();
            yield break;
        }

        Debug.Log($"[LiveTranslation] STT結果: {japaneseText}");

        //  翻訳: 日本語 → 英語
        string englishText = null;
        yield return translationProcessor.Translate(japaneseText, result => englishText = result);

        if (string.IsNullOrWhiteSpace(englishText))
        {
            subtitleUI.HideSubtitle();
            yield break;
        }

        Debug.Log($"[LiveTranslation] 翻訳結果: {englishText}");

        //  字幕表示
        subtitleUI.ShowSubtitle(englishText);
    }
}
