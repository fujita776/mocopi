using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System;
using System.IO.Ports;
using System.Threading;

[System.Serializable]
public class ButtonScenePair
{
    [SerializeField] private InputActionReference actionReference;
    [SerializeField] private string sceneName;

    public InputActionReference ActionReference => actionReference;
    public string SceneName => sceneName;
}

public class SceneChange : MonoBehaviour
{
    [Header("M5StampC3入力を使うかどうか")]
    [SerializeField] private bool useM5Input = true;

    [Header("M5StampC3のシリアル設定")]
    [SerializeField] private string portName = "COM3";
    [SerializeField] private int baudRate = 115200;

    [SerializeField] private string m5Token = "BTN:DOWN";
    [Header("M5StampC3で特定の入力を受け取ったときに切り替えるシーン名")]
    [SerializeField] private string m5SceneName = "DomyRoom";

    [Header("ボタンでの入力とシーン名のマッピング")]
    [SerializeField] private ButtonScenePair[] mappings;

    [Header("シーン切り替え前に鳴らす効果音 (必要な時)")]
    [SerializeField] private AudioClip preLoadClip;

    [Header("シーン切り替え前の効果音の音量")]
    [SerializeField, Range(0f, 1f)]
    private float loadVolume = 1f;

    //  効果音を鳴らすAudioSource
    private AudioSource audioSource;

    //  M5StampC3用シリアルポート関連
    private SerialPort sp;
    private Thread readThread;
    private volatile bool running;

    private readonly object m5Lock = new object();
    private string m5QueuedLine;  //  serialから読み取った行を一時的に保存する変数

    private void Awake()
    {
        //  preLoadClipに音が入っている場合
        if (preLoadClip != null)
        {
            //  AudioSourceがなければ追加する
            audioSource = gameObject.GetComponent<AudioSource>();
            if(audioSource ==  null )
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.loop = false;
        }
    }

    private void OnEnable()
    {
        foreach (var map in mappings)
        {
            var action = map.ActionReference?.action;
            if (action != null)
            {
                action.performed += OnAnyAction;
                action.Enable();
            }
        }

        if (useM5Input) TryOpenSerial();
    }

    private void OnDisable()
    {
        foreach (var map in mappings)
        {
            var action = map.ActionReference?.action;
            if (action != null)
            {
                action.performed -= OnAnyAction;
                action.Disable();
            }
        }

        if (useM5Input) CloseSerial();
    }

    private void OnAnyAction(InputAction.CallbackContext ctx)
    {
        // 押されたアクションがどのマッピングかを探してシーン遷移
        foreach (var map in mappings)
        {
            if (ctx.action == map.ActionReference.action && !string.IsNullOrEmpty(map.SceneName))
            {
                LoadSceneWithOptionalSE(map.SceneName);
                break;
            }
        }
    }

    private IEnumerator PlayThenLoad(string sceneName)
    {
        audioSource.PlayOneShot(preLoadClip, loadVolume);
        //  効果音の長さだけ待ってからロード
        yield return new WaitForSeconds(preLoadClip.length);
        SceneManager.LoadScene(sceneName);
    }

    private void Update()
    {
        if (!useM5Input) return;

        //  M5StampC3からの入力があれば処理する
        string line = null;
        lock (m5Lock)
        {
            if (!string.IsNullOrEmpty(m5QueuedLine))
            {
                line = m5QueuedLine;
                m5QueuedLine = null;
            }
        }

        if (line != null)
            HandleM5Token(line);
    }

    private void HandleM5Token(string token)
    {
        if (string.IsNullOrEmpty(m5SceneName)) return;

        if (string.Equals(token, m5Token, StringComparison.OrdinalIgnoreCase))
        {
            LoadSceneWithOptionalSE(m5SceneName);
        }
    }

    private void LoadSceneWithOptionalSE(string sceneName)
    {
        Time.timeScale = 1f;

        if (preLoadClip != null)
            StartCoroutine(PlayThenLoad(sceneName));
        else
            SceneManager.LoadScene(sceneName);
    }

    private void TryOpenSerial()
    {
        try
        {
            sp = new SerialPort(portName, baudRate)
            {
                NewLine = "\n",
                ReadTimeout = 200,
                DtrEnable = true,
                RtsEnable = true
            };
            sp.Open();

            running = true;
            readThread = new Thread(ReadLoop);
            readThread.IsBackground = true;
            readThread.Start();

            Debug.Log($"[M5] Serial opened: {portName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[M5] Failed to open serial {portName}: {e.Message}");
            CloseSerial();
        }
    }

    private void ReadLoop()
    {
        while (running)
        {
            try
            {
                var line = sp.ReadLine().Trim();
                lock (m5Lock) m5QueuedLine = line;
            }
            catch (TimeoutException) { }
            catch (Exception)
            {
                running = false;
            }
        }
    }

    private void CloseSerial()
    {
        running = false;
        try { readThread?.Join(300); } catch { }

        try
        {
            if (sp != null)
            {
                if (sp.IsOpen) sp.Close();
                sp.Dispose();
            }
        }
        catch { }

        sp = null;
        readThread = null;
    }
}
