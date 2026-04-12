using UnityEngine;
using Whisper.Utils;

/// <summary>
/// uLipSyncMicrophoneが使用しているマイクのAudioClipから直接音声を読み取り、
/// WhisperStreamに流すブリッジ。
/// Microphone.GetPosition()で録音位置を追跡するため、安定したデータ供給が可能。
/// </summary>
public class AudioBridge : MonoBehaviour
{
    [Header("チャンク設定")]
    [SerializeField] private float chunkIntervalSeconds = 0.5f;

    private AudioClip _micClip;
    private string _micDeviceName;
    private int _lastReadPos;
    private float _timer;
    private System.Action<AudioChunk> _onChunkReady;
    private bool _ready;

    public void SetChunkCallback(System.Action<AudioChunk> callback)
    {
        _onChunkReady = callback;
    }

    /// <summary>
    /// uLipSyncMicrophoneが使っているマイクデバイスとAudioClipを自動検出
    /// </summary>
    public void AutoDetectAudioSource()
    {
        _ready = false;

        var lipSyncMic = FindObjectOfType<uLipSync.uLipSyncMicrophone>();
        if (lipSyncMic != null)
        {
            var audioSource = lipSyncMic.GetComponent<AudioSource>();
            if (audioSource != null && audioSource.clip != null)
            {
                _micClip = audioSource.clip;

                //  uLipSyncMicrophoneが使っているデバイス名を取得
                var deviceField = lipSyncMic.device;
                _micDeviceName = deviceField.name;

                _ready = true;
                Debug.Log($"[AudioBridge] マイク検出: {_micDeviceName}, clip={_micClip.frequency}Hz {_micClip.channels}ch");
                return;
            }
        }

        //  フォールバック: 録音中のマイクデバイスを探す
        foreach (var device in Microphone.devices)
        {
            if (Microphone.IsRecording(device))
            {
                _micDeviceName = device;
                //  AudioSourceからclipを取得
                var audioSources = FindObjectsOfType<AudioSource>();
                foreach (var src in audioSources)
                {
                    if (src.clip != null && src.isPlaying)
                    {
                        _micClip = src.clip;
                        _ready = true;
                        Debug.Log($"[AudioBridge] フォールバック検出: {device}, clip={_micClip.frequency}Hz");
                        return;
                    }
                }
            }
        }

        Debug.LogWarning("[AudioBridge] マイクAudioClipが見つかりません。uLipSyncMicrophoneの初期化完了後に再検出します。");
    }

    public bool HasSource => _ready;

    void Update()
    {
        //  遅延初期化: uLipSyncMicrophoneの起動を待つ
        if (!_ready)
        {
            TryLateDetect();
            if (!_ready) return;
        }

        if (_onChunkReady == null) return;
        if (_micClip == null || string.IsNullOrEmpty(_micDeviceName)) return;
        if (!Microphone.IsRecording(_micDeviceName)) return;

        _timer += Time.deltaTime;
        if (_timer < chunkIntervalSeconds) return;
        _timer = 0f;

        ReadAndSendChunk();
    }

    private void TryLateDetect()
    {
        var lipSyncMic = FindObjectOfType<uLipSync.uLipSyncMicrophone>();
        if (lipSyncMic == null || !lipSyncMic.isRecording) return;

        var audioSource = lipSyncMic.GetComponent<AudioSource>();
        if (audioSource == null || audioSource.clip == null) return;

        _micClip = audioSource.clip;
        _micDeviceName = lipSyncMic.device.name;
        _lastReadPos = Microphone.GetPosition(_micDeviceName);
        _ready = true;

        Debug.Log($"[AudioBridge] 遅延検出成功: {_micDeviceName}, clip={_micClip.frequency}Hz {_micClip.channels}ch");
    }

    private void ReadAndSendChunk()
    {
        int micPos = Microphone.GetPosition(_micDeviceName);
        if (micPos == _lastReadPos) return;

        int sampleCount;
        if (micPos > _lastReadPos)
        {
            sampleCount = micPos - _lastReadPos;
        }
        else
        {
            //  リングバッファ一周
            sampleCount = (_micClip.samples - _lastReadPos) + micPos;
        }

        if (sampleCount <= 0) return;

        float[] data = new float[sampleCount * _micClip.channels];
        _micClip.GetData(data, _lastReadPos);
        _lastReadPos = micPos;

        var chunk = new AudioChunk
        {
            Data = data,
            Frequency = _micClip.frequency,
            Channels = _micClip.channels,
            Length = (float)sampleCount / _micClip.frequency,
            IsVoiceDetected = true
        };

        _onChunkReady?.Invoke(chunk);
    }

    public void ResetPosition()
    {
        if (!string.IsNullOrEmpty(_micDeviceName) && Microphone.IsRecording(_micDeviceName))
            _lastReadPos = Microphone.GetPosition(_micDeviceName);
        else
            _lastReadPos = 0;
        _timer = 0f;
    }
}
