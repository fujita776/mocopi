using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 既存のAudioSource（LipSync等）に相乗りして音声を取得し、
/// 16kHzにリサンプルしてチャンク単位で提供する。
/// 同じGameObjectのAudioSourceのOnAudioFilterReadから音声データを受け取る。
/// </summary>
public class AudioChunkProvider : MonoBehaviour
{
    [Header("チャンク設定")]
    [SerializeField] private float chunkDurationSeconds = 5f;
    [SerializeField] private float silenceThreshold = 0.01f;
    [SerializeField] private float silenceTimeoutSeconds = 1.5f;

    public const int TargetSampleRate = 16000;

    public UnityEvent<float[]> onChunkReady = new UnityEvent<float[]>();

    private readonly object _lockObject = new object();
    private List<float> _resampleBuffer = new List<float>();
    private int _systemSampleRate;
    private float _silenceTimer;
    private bool _hasVoice;
    private int _maxChunkSamples;

    void OnEnable()
    {
        _systemSampleRate = AudioSettings.outputSampleRate;
        _maxChunkSamples = Mathf.CeilToInt(chunkDurationSeconds * TargetSampleRate);
        _silenceTimer = 0f;
        _hasVoice = false;

        Debug.Log("[AudioChunkProvider] OnAudioFilterRead 経由で音声取得開始");
    }

    void Update()
    {
        float[] chunk = null;

        lock (_lockObject)
        {
            if (_resampleBuffer.Count >= _maxChunkSamples)
            {
                chunk = _resampleBuffer.GetRange(0, _maxChunkSamples).ToArray();
                _resampleBuffer.RemoveRange(0, _maxChunkSamples);
                _hasVoice = false;
            }
            else if (_hasVoice && _silenceTimer >= silenceTimeoutSeconds && _resampleBuffer.Count > TargetSampleRate / 2)
            {
                chunk = _resampleBuffer.ToArray();
                _resampleBuffer.Clear();
                _hasVoice = false;
                _silenceTimer = 0f;
            }
        }

        if (chunk != null)
        {
            onChunkReady.Invoke(chunk);
        }
    }

    /// <summary>
    /// 同じGameObjectのAudioSourceから自動的に呼ばれる
    /// uLipSyncやMuteSourceと共存可能（実行順序に依存しない・データをコピーするだけ）
    /// </summary>
    void OnAudioFilterRead(float[] data, int channels)
    {
        //  モノラル化 + RMS計算
        int monoSampleCount = data.Length / channels;
        float rms = 0f;

        float[] monoSamples = new float[monoSampleCount];
        for (int i = 0; i < monoSampleCount; i++)
        {
            float sample = data[i * channels];
            monoSamples[i] = sample;
            rms += sample * sample;
        }
        rms = Mathf.Sqrt(rms / monoSampleCount);

        //  リサンプル → 16kHz
        float ratio = (float)TargetSampleRate / _systemSampleRate;
        int resampledCount = Mathf.CeilToInt(monoSampleCount * ratio);
        float[] resampled = new float[resampledCount];

        for (int i = 0; i < resampledCount; i++)
        {
            float srcIndex = i / ratio;
            int idx0 = Mathf.FloorToInt(srcIndex);
            int idx1 = Mathf.Min(idx0 + 1, monoSampleCount - 1);
            float frac = srcIndex - idx0;
            resampled[i] = monoSamples[idx0] * (1f - frac) + monoSamples[idx1] * frac;
        }

        lock (_lockObject)
        {
            _resampleBuffer.AddRange(resampled);

            if (rms >= silenceThreshold)
            {
                _hasVoice = true;
                _silenceTimer = 0f;
            }
            else if (_hasVoice)
            {
                float frameDuration = (float)monoSampleCount / _systemSampleRate;
                _silenceTimer += frameDuration;
            }

            int maxBufferSize = _maxChunkSamples * 2;
            if (_resampleBuffer.Count > maxBufferSize)
            {
                _resampleBuffer.RemoveRange(0, _resampleBuffer.Count - maxBufferSize);
            }
        }

        //  data配列は変更しない（uLipSync/MuteSourceの動作を妨げない）
    }

    public void ClearBuffer()
    {
        lock (_lockObject)
        {
            _resampleBuffer.Clear();
            _hasVoice = false;
            _silenceTimer = 0f;
        }
    }
}
