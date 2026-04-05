using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Sentis;

/// <summary>
/// Whisper STT推論（Sentis使用）
/// 16kHz音声 → メルスペクトログラム → encoder → decoder → 日本語テキスト
/// </summary>
public class WhisperProcessor : MonoBehaviour
{
    [Header("モデル（.sentis / .onnx）")]
    [SerializeField] private ModelAsset encoderModelAsset;
    [SerializeField] private ModelAsset decoderModelAsset;

    [Header("設定")]
    [SerializeField] private int maxTokens = 128;
    [SerializeField] private int decoderStepsPerFrame = 4;
    [SerializeField] private BackendType backendType = BackendType.GPUCompute;

    private IWorker _encoderWorker;
    private IWorker _decoderWorker;
    private WhisperTokenizer _tokenizer;
    private bool _initialized;
    private bool _processing;

    public bool IsInitialized => _initialized;
    public bool IsProcessing => _processing;

    void OnDestroy()
    {
        Dispose();
    }

    /// <summary>
    /// モデルとトークナイザを初期化
    /// </summary>
    public bool Initialize()
    {
        if (_initialized) return true;

        //  トークナイザ読み込み
        string vocabPath = Path.Combine(Application.streamingAssetsPath, "Translation", "whisper_vocab.json");
        _tokenizer = new WhisperTokenizer();
        if (!_tokenizer.Load(vocabPath))
        {
            Debug.LogError("[WhisperProcessor] トークナイザの読み込みに失敗");
            return false;
        }

        //  Sentisモデル読み込み
        if (encoderModelAsset == null || decoderModelAsset == null)
        {
            Debug.LogError("[WhisperProcessor] Encoder/Decoder ModelAsset が設定されていません");
            return false;
        }

        var encoderModel = ModelLoader.Load(encoderModelAsset);
        var decoderModel = ModelLoader.Load(decoderModelAsset);

        _encoderWorker = WorkerFactory.CreateWorker(backendType, encoderModel);
        _decoderWorker = WorkerFactory.CreateWorker(backendType, decoderModel);

        //  メルスペクトログラムの初期化
        MelSpectrogram.Initialize();

        _initialized = true;
        Debug.Log("[WhisperProcessor] 初期化完了");
        return true;
    }

    /// <summary>
    /// 音声から日本語テキストを生成（コルーチン）
    /// </summary>
    /// <param name="audio16kHz">16kHzのPCMサンプル</param>
    /// <param name="onComplete">完了時コールバック（認識テキスト）</param>
    public IEnumerator Transcribe(float[] audio16kHz, System.Action<string> onComplete)
    {
        if (!_initialized || _processing)
        {
            onComplete?.Invoke("");
            yield break;
        }

        _processing = true;

        //  メルスペクトログラム計算
        float[] melData = MelSpectrogram.Compute(audio16kHz);

        //  Encoder実行
        using var inputFeatures = new TensorFloat(
            new TensorShape(1, MelSpectrogram.NMels, MelSpectrogram.MaxFrames),
            melData
        );

        _encoderWorker.Execute(inputFeatures);
        var encoderOutput = _encoderWorker.PeekOutput() as TensorFloat;
        encoderOutput.MakeReadable();

        yield return null;

        //  Decoder: greedy decode
        var tokens = new List<int>(_tokenizer.DecoderStartTokens);
        int stepCount = 0;

        for (int step = 0; step < maxTokens; step++)
        {
            using var inputIds = new TensorInt(new TensorShape(1, tokens.Count), tokens.ToArray());

            _decoderWorker.SetInput("input_ids", inputIds);
            _decoderWorker.SetInput("encoder_hidden_states", encoderOutput);
            _decoderWorker.Execute();

            var logits = _decoderWorker.PeekOutput() as TensorFloat;
            logits.MakeReadable();

            //  最後のトークン位置のlogitsからargmax
            int nextToken = ArgMaxLastPosition(logits, tokens.Count);

            if (nextToken == WhisperTokenizer.EotToken)
                break;

            tokens.Add(nextToken);

            //  フレームあたりのデコードステップ数を制限（ゲームループをブロックしない）
            stepCount++;
            if (stepCount >= decoderStepsPerFrame)
            {
                stepCount = 0;
                yield return null;
            }
        }

        //  特殊トークンを除いてデコード
        string text = _tokenizer.Decode(tokens.ToArray());

        _processing = false;
        onComplete?.Invoke(text);
    }

    /// <summary>
    /// ウォームアップ用: ダミーデータで1回推論を走らせる
    /// </summary>
    public IEnumerator Warmup()
    {
        if (!_initialized) yield break;

        float[] dummyAudio = new float[AudioChunkProvider.TargetSampleRate]; // 1秒分
        yield return StartCoroutine(Transcribe(dummyAudio, _ => { }));

        Debug.Log("[WhisperProcessor] ウォームアップ完了");
    }

    private int ArgMaxLastPosition(TensorFloat logits, int seqLen)
    {
        //  logits shape: [batch, seq_len, vocab_size]
        int vocabSize = logits.shape[2];
        int offset = (seqLen - 1) * vocabSize;

        int bestIdx = 0;
        float bestVal = float.MinValue;

        for (int i = 0; i < vocabSize; i++)
        {
            float val = logits[0, seqLen - 1, i];
            if (val > bestVal)
            {
                bestVal = val;
                bestIdx = i;
            }
        }

        return bestIdx;
    }

    private void Dispose()
    {
        _encoderWorker?.Dispose();
        _decoderWorker?.Dispose();
        _encoderWorker = null;
        _decoderWorker = null;
        _initialized = false;
    }
}
