using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO.Ports;
using System.Threading;

public class M5StampC3ButtonReceiver : MonoBehaviour
{
    [Header("マイコンのポート番号")]
    public string portName = "COM3";
    public int baudRate = 115200;

    SerialPort serialPort;  //  シリアルポートオブジェクト
    Thread readThread;  //  受信スレッド
    volatile bool running;  //  受信スレッド実行フラグ
    readonly object lockObject = new object();  //  ロックオブジェクト
    string queuedLine;  //  受信データキュー

    void Start()
    {
        //  シリアルポートの初期化
        serialPort = new SerialPort(portName, baudRate)
        {
            NewLine = "¥n",  //  改行コードの設定
            ReadTimeout = 200,  //  タイムアウト時間（ミリ秒）
            DtrEnable = true,  //  データ端末準備信号を有効にする
            RtsEnable = true  //  送信要求信号を有効にする
        };

        serialPort.Open();

        //  受信スレッドの開始
        running = true;
        readThread = new Thread(ReadLoop);
        readThread.IsBackground = true;
        readThread.Start();
    }

    void ReadLoop()
    {
        while (running)
        {
            try
            {
                var line = serialPort.ReadLine().Trim();
                lock (lockObject)
                {
                    queuedLine = line;  //  最新の受信データをキューに格納
                }
            }
            catch (TimeoutException)
            {
                //  タイムアウト時は無視
            }
            catch (Exception e)
            {
                Debug.LogError("シリアルポート読み取りエラー: " + e.Message);
                running = false;
            }
        }
    }

    void Update()
    {
        string line = null;
        lock (lockObject)
        {
            if (!string.IsNullOrEmpty(queuedLine))
            {
                line = queuedLine;
                queuedLine = null;  //  キューをクリア
            }
        }
        if (line == null)
        {
            return;
        }

        if (line == "BTN:DOWN")
        {
            Debug.Log("BTN DOWN");
            //  ボタンが押されたときの処理をここに追加
        }
        else if (line == "BTN:UP")
        {
            Debug.Log("BTN UP");
        }
    }

    void OnDestroy()
    {
        //  受信スレッドの停止
        running = false;
        try 
        { 
            readThread?.Join(300);
        } catch {}
        //  シリアルポートのクローズ
        try
        { 
            if (serialPort != null && serialPort.IsOpen) serialPort.Close();
            serialPort.Dispose();
        } catch {}
    }
}
