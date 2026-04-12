using UnityEngine;
using Whisper.Utils;

/// <summary>
/// 既存のuLipSyncMicrophone用AudioSourceから音声データを読み取り、
/// WhisperStreamに流すブリッジ。
/// マイクの二重キャプチャを防ぎ、リップシンクとSTTを共存させる。
/// </summary>
public class AudioBridge : MonoBehaviour
{
    [Header("音声ソース（LipSyncR等のAudioSource）")]
    [SerializeField] private AudioSource sourceAudioSource;

    [Header("チャンク設定")]
    [SerializeField] private float chunkIntervalSeconds = 0.5f;

    private int _lastReadPos;
    private float _timer;
    private System.Action<AudioChunk> _onChunkReady;

    /// <summary>
    /// チャンク受信コールバックを設定
    /// </summary>
    public void SetChunkCallback(System.Action<AudioChunk> callback)
    {
        _onChunkReady = callback;
    }

    /// <summary>
    /// AudioSourceを自動検出して設定
    /// </summary>
    public void AutoDetectAudioSource()
    {
        if (sourceAudioSource != null) return;

        //  uLipSyncMicrophoneがあるオブジェクトのAudioSourceを探す
        var lipSyncMic = FindObjectOfType<uLipSync.uLipSyncMicrophone>();
        if (lipSyncMic != null)
        {
            sourceAudioSource = lipSyncMic.GetComponent<AudioSource>();
            Debug.Log($"[AudioBridge] uLipSyncMicrophone のAudioSourceを検出: {lipSyncMic.gameObject.name}");
            return;
        }

        //  uLipSyncがあるオブジェクトのAudioSourceを探す
        var lipSync = FindObjectOfType<uLipSync.uLipSync>();
        if (lipSync != null)
        {
            sourceAudioSource = lipSync.GetComponent<AudioSource>();
            Debug.Log($"[AudioBridge] uLipSync のAudioSourceを検出: {lipSync.gameObject.name}");
        }
    }

    public bool HasSource => sourceAudioSource != null;

    void Update()
    {
        if (sourceAudioSource == null || sourceAudioSource.clip == null) return;
        if (_onChunkReady == null) return;

        _timer += Time.deltaTime;
        if (_timer < chunkIntervalSeconds) return;
        _timer = 0f;

        ReadAndSendChunk();
    }

    private void ReadAndSendChunk()
    {
        var clip = sourceAudioSource.clip;
        if (clip == null) return;

        int currentPos = sourceAudioSource.timeSamples;
        if (currentPos == _lastReadPos) return;

        int sampleCount;
        if (currentPos > _lastReadPos)
        {
            sampleCount = currentPos - _lastReadPos;
        }
        else
        {
            //  ループ時
            sampleCount = (clip.samples - _lastReadPos) + currentPos;
        }

        if (sampleCount <= 0) return;

        float[] data = new float[sampleCount * clip.channels];
        clip.GetData(data, _lastReadPos);
        _lastReadPos = currentPos;

        var chunk = new AudioChunk
        {
            Data = data,
            Frequency = clip.frequency,
            Channels = clip.channels,
            Length = (float)sampleCount / clip.frequency,
            IsVoiceDetected = true
        };

        _onChunkReady.Invoke(chunk);
    }

    /// <summary>
    /// 読み取り位置をリセット
    /// </summary>
    public void ResetPosition()
    {
        _lastReadPos = 0;
        _timer = 0f;
    }
}
