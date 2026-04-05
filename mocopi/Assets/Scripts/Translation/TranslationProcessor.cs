using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Sentis;

/// <summary>
/// opus-mt-ja-en 翻訳推論（Sentis使用）
/// 日本語テキスト → トークナイズ → encoder → decoder → 英語テキスト
/// </summary>
public class TranslationProcessor : MonoBehaviour
{
    [Header("モデル（.sentis / .onnx）")]
    [SerializeField] private ModelAsset encoderModelAsset;
    [SerializeField] private ModelAsset decoderModelAsset;

    [Header("設定")]
    [SerializeField] private int maxTokens = 128;
    [SerializeField] private int decoderStepsPerFrame = 8;
    [SerializeField] private BackendType backendType = BackendType.GPUCompute;

    private IWorker _encoderWorker;
    private IWorker _decoderWorker;
    private SentencePieceTokenizer _srcTokenizer;
    private SentencePieceTokenizer _tgtTokenizer;
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

        string basePath = Path.Combine(Application.streamingAssetsPath, "Translation");

        //  ソース（日本語）トークナイザ
        _srcTokenizer = new SentencePieceTokenizer();
        if (!_srcTokenizer.Load(Path.Combine(basePath, "opus_mt_vocab_src.json")))
        {
            Debug.LogError("[TranslationProcessor] ソーストークナイザの読み込みに失敗");
            return false;
        }

        //  ターゲット（英語）トークナイザ
        _tgtTokenizer = new SentencePieceTokenizer();
        if (!_tgtTokenizer.Load(Path.Combine(basePath, "opus_mt_vocab_tgt.json")))
        {
            Debug.LogError("[TranslationProcessor] ターゲットトークナイザの読み込みに失敗");
            return false;
        }

        //  Sentisモデル読み込み
        if (encoderModelAsset == null || decoderModelAsset == null)
        {
            Debug.LogError("[TranslationProcessor] Encoder/Decoder ModelAsset が設定されていません");
            return false;
        }

        var encoderModel = ModelLoader.Load(encoderModelAsset);
        var decoderModel = ModelLoader.Load(decoderModelAsset);

        _encoderWorker = WorkerFactory.CreateWorker(backendType, encoderModel);
        _decoderWorker = WorkerFactory.CreateWorker(backendType, decoderModel);

        _initialized = true;
        Debug.Log("[TranslationProcessor] 初期化完了");
        return true;
    }

    /// <summary>
    /// 日本語テキストを英語に翻訳（コルーチン）
    /// </summary>
    /// <param name="japaneseText">入力日本語テキスト</param>
    /// <param name="onComplete">完了時コールバック（翻訳テキスト）</param>
    public IEnumerator Translate(string japaneseText, System.Action<string> onComplete)
    {
        if (!_initialized || _processing)
        {
            onComplete?.Invoke("");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(japaneseText))
        {
            onComplete?.Invoke("");
            yield break;
        }

        _processing = true;

        //  ソーステキストのトークナイズ
        int[] srcTokens = _srcTokenizer.Encode(japaneseText);

        //  Encoder実行
        using var inputIds = new TensorInt(new TensorShape(1, srcTokens.Length), srcTokens);

        //  attention_mask: 全て1
        int[] maskData = new int[srcTokens.Length];
        for (int i = 0; i < maskData.Length; i++) maskData[i] = 1;
        using var attentionMask = new TensorInt(new TensorShape(1, srcTokens.Length), maskData);

        _encoderWorker.SetInput("input_ids", inputIds);
        _encoderWorker.SetInput("attention_mask", attentionMask);
        _encoderWorker.Execute();

        var encoderOutput = _encoderWorker.PeekOutput() as TensorFloat;
        encoderOutput.MakeReadable();

        yield return null;

        //  Decoder: greedy decode
        //  opus-mtのデコーダ開始トークンは </s> (EOS) + <pad>
        var tokens = new List<int> { _tgtTokenizer.PadTokenId };
        int stepCount = 0;

        for (int step = 0; step < maxTokens; step++)
        {
            using var decoderInputIds = new TensorInt(new TensorShape(1, tokens.Count), tokens.ToArray());

            _decoderWorker.SetInput("input_ids", decoderInputIds);
            _decoderWorker.SetInput("encoder_hidden_states", encoderOutput);
            _decoderWorker.SetInput("encoder_attention_mask", attentionMask);
            _decoderWorker.Execute();

            var logits = _decoderWorker.PeekOutput() as TensorFloat;
            logits.MakeReadable();

            int nextToken = ArgMaxLastPosition(logits, tokens.Count);

            if (nextToken == _tgtTokenizer.EosTokenId)
                break;

            tokens.Add(nextToken);

            stepCount++;
            if (stepCount >= decoderStepsPerFrame)
            {
                stepCount = 0;
                yield return null;
            }
        }

        //  デトークナイズ
        string translatedText = _tgtTokenizer.Decode(tokens.ToArray());

        _processing = false;
        onComplete?.Invoke(translatedText);
    }

    private int ArgMaxLastPosition(TensorFloat logits, int seqLen)
    {
        int vocabSize = logits.shape[2];

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
