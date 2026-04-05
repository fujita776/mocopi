using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HeightMaxSession : MonoBehaviour
{
    [SerializeField] private HeightMeter2D heightMeter;

    //  このシーンに存在しているか
    private static bool s_exists = false;

    private void Awake()
    {
        //  シーン側に複数置かれた場合の二重にならないための対策
        if (s_exists)
        {
            Destroy(gameObject);
            return;
        }
        s_exists = true;
        DontDestroyOnLoad(gameObject);

        if (!heightMeter) heightMeter = FindObjectOfType<HeightMeter2D>();

        if (!heightMeter)
        {
            //  高さ計測用のオブジェクトが見つからなかった場合は破棄する
            s_exists = false;
            Destroy(gameObject);
            return;
        }

        ApplySavedToMeter(heightMeter);
        HookMeter(heightMeter);

        SceneManager.sceneLoaded += OnSceneLoaded;  //  シーン切り替え時のイベント登録
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnhookMeter(heightMeter);
        if (s_exists) s_exists = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UnhookMeter(heightMeter);
        heightMeter = FindObjectOfType<HeightMeter2D>();

        //  次のシーンに高さ計測用オブジェクトが存在しなかった場合は破棄する
        if (!heightMeter)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            s_exists = false;
            Destroy(gameObject);
            return;
        }

        ApplySavedToMeter(heightMeter);
        HookMeter(heightMeter);
    }

    private void ApplySavedToMeter(HeightMeter2D meter)
    {
        if (!meter) return;
        meter.InitializeMax(HoldBest.BestHeight);  //  保存されている最高到達高さを適用する
    }

    private void HookMeter(HeightMeter2D meter)
    {
        if (!meter) return;
        meter.OnMaxHeightUpdated += HandleMaxHeightUpdate;
    }

    private void UnhookMeter(HeightMeter2D meter)
    {
        if (!meter) return;
        meter.OnMaxHeightUpdated -= HandleMaxHeightUpdate;
    }

    private void HandleMaxHeightUpdate(float newMaxHeight)
    {
        if(newMaxHeight > HoldBest.BestHeight)
        {
            HoldBest.BestHeight = newMaxHeight;
        }
    }
}