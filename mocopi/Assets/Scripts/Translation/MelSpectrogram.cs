using System;
using UnityEngine;

/// <summary>
/// Whisper用メルスペクトログラム計算
/// 16kHz音声 → 80bin メルスペクトログラム（n_fft=400, hop=160）
/// </summary>
public static class MelSpectrogram
{
    public const int SampleRate = 16000;
    public const int NFft = 400;
    public const int HopLength = 160;
    public const int NMels = 80;
    public const int MaxFrames = 3000; //  30秒分
    public const int FreqBins = NFft / 2 + 1; // 201

    private static float[,] _melFilterBank;
    private static float[] _hannWindow;
    private static float[] _cosTable; //  DFT用コサインテーブル
    private static float[] _sinTable; //  DFT用サインテーブル
    private static bool _initialized;

    /// <summary>
    /// メルフィルタバンクとDFTテーブルを初期化（初回のみ）
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;
        _melFilterBank = CreateMelFilterBank(NMels, NFft, SampleRate);
        _hannWindow = CreateHannWindow(NFft);
        PrecomputeDFTTables();
        _initialized = true;
    }

    /// <summary>
    /// 16kHz音声からメルスペクトログラムを計算
    /// </summary>
    /// <param name="audio">16kHz PCMサンプル</param>
    /// <returns>float[NMels * MaxFrames]（row-major: mel × frame）</returns>
    public static float[] Compute(float[] audio)
    {
        Initialize();

        //  30秒分（480000サンプル）にパディングまたはトリム
        int targetSamples = SampleRate * 30;
        float[] padded = new float[targetSamples];
        int copyLen = Mathf.Min(audio.Length, targetSamples);
        Array.Copy(audio, padded, copyLen);

        float[] result = new float[NMels * MaxFrames];
        float[] frame = new float[NFft];
        float[] powerSpectrum = new float[FreqBins];

        for (int t = 0; t < MaxFrames; t++)
        {
            int start = t * HopLength;

            //  フレーム抽出 + 窓関数適用
            for (int i = 0; i < NFft; i++)
            {
                int idx = start + i;
                frame[i] = (idx < padded.Length) ? padded[idx] * _hannWindow[i] : 0f;
            }

            //  DFT → パワースペクトル（n_fft=400はpower of 2でないためDFTを使用）
            ComputePowerSpectrum(frame, powerSpectrum);

            //  メルフィルタバンク適用 + log
            for (int m = 0; m < NMels; m++)
            {
                float sum = 0f;
                for (int k = 0; k < FreqBins; k++)
                {
                    sum += _melFilterBank[m, k] * powerSpectrum[k];
                }
                sum = Mathf.Max(sum, 1e-10f);
                result[m * MaxFrames + t] = Mathf.Log10(sum);
            }
        }

        //  Whisperの正規化: max値でクランプし、スケーリング
        float maxVal = float.MinValue;
        for (int i = 0; i < result.Length; i++)
        {
            if (result[i] > maxVal) maxVal = result[i];
        }

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = Mathf.Max(result[i], maxVal - 8.0f);
            result[i] = (result[i] + 4.0f) / 4.0f;
        }

        return result;
    }

    private static void PrecomputeDFTTables()
    {
        //  cos/sinテーブルを事前計算して高速化
        _cosTable = new float[FreqBins * NFft];
        _sinTable = new float[FreqBins * NFft];

        for (int k = 0; k < FreqBins; k++)
        {
            for (int n = 0; n < NFft; n++)
            {
                float angle = -2f * Mathf.PI * k * n / NFft;
                _cosTable[k * NFft + n] = Mathf.Cos(angle);
                _sinTable[k * NFft + n] = Mathf.Sin(angle);
            }
        }
    }

    private static void ComputePowerSpectrum(float[] frame, float[] powerSpectrum)
    {
        for (int k = 0; k < FreqBins; k++)
        {
            float sumReal = 0f;
            float sumImag = 0f;
            int offset = k * NFft;

            for (int n = 0; n < NFft; n++)
            {
                sumReal += frame[n] * _cosTable[offset + n];
                sumImag += frame[n] * _sinTable[offset + n];
            }

            powerSpectrum[k] = sumReal * sumReal + sumImag * sumImag;
        }
    }

    private static float[] CreateHannWindow(int size)
    {
        float[] window = new float[size];
        for (int i = 0; i < size; i++)
        {
            window[i] = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * i / size));
        }
        return window;
    }

    private static float HzToMel(float hz)
    {
        return 2595f * Mathf.Log10(1f + hz / 700f);
    }

    private static float MelToHz(float mel)
    {
        return 700f * (Mathf.Pow(10f, mel / 2595f) - 1f);
    }

    private static float[,] CreateMelFilterBank(int nMels, int nFft, int sampleRate)
    {
        int freqBins = nFft / 2 + 1;
        float fMax = sampleRate / 2f;
        float melMin = HzToMel(0f);
        float melMax = HzToMel(fMax);

        //  メル空間で等間隔にポイントを配置
        float[] melPoints = new float[nMels + 2];
        for (int i = 0; i < nMels + 2; i++)
        {
            melPoints[i] = melMin + (melMax - melMin) * i / (nMels + 1);
        }

        //  Hz空間に戻してFFTビンに変換
        int[] binPoints = new int[nMels + 2];
        for (int i = 0; i < nMels + 2; i++)
        {
            float hz = MelToHz(melPoints[i]);
            binPoints[i] = Mathf.RoundToInt(hz * nFft / sampleRate);
        }

        float[,] filterBank = new float[nMels, freqBins];
        for (int m = 0; m < nMels; m++)
        {
            int startBin = binPoints[m];
            int centerBin = binPoints[m + 1];
            int endBin = binPoints[m + 2];

            for (int k = startBin; k < centerBin; k++)
            {
                if (k >= 0 && k < freqBins && centerBin != startBin)
                {
                    filterBank[m, k] = (float)(k - startBin) / (centerBin - startBin);
                }
            }

            for (int k = centerBin; k <= endBin; k++)
            {
                if (k >= 0 && k < freqBins && endBin != centerBin)
                {
                    filterBank[m, k] = (float)(endBin - k) / (endBin - centerBin);
                }
            }
        }

        return filterBank;
    }
}
