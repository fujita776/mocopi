using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PieceMaxSession : MonoBehaviour
{
    [SerializeField] private PieceCounter pieceCounter;

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

        if (!pieceCounter) pieceCounter = FindObjectOfType<PieceCounter>();

        if (!pieceCounter)
        {
            //  ピースカウンターが見つからなかった場合は破棄する
            s_exists = false;
            Destroy(gameObject);
            return;
        }
        ApplySavedToCounter(pieceCounter);
        HookCounter(pieceCounter);

        SceneManager.sceneLoaded += OnSceneLoaded;  //  シーン切り替え時のイベント登録
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnhookCounter(pieceCounter);
        if (s_exists) s_exists = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UnhookCounter(pieceCounter);
        pieceCounter = FindObjectOfType<PieceCounter>();

        //  次のシーンにピースカウンターが存在しなかった場合は破棄する
        if (!pieceCounter)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            s_exists = false;
            Destroy(gameObject);
            return;
        }

        ApplySavedToCounter(pieceCounter);
        HookCounter(pieceCounter);
    }

    private void ApplySavedToCounter(PieceCounter counter)
    {
        if (!counter) return;

        counter.InitializeMax(HoldBest.BestCount);  //  保存済みの最大値を適用する
    }

    private void HookCounter(PieceCounter counter)
    {
        if (!counter) return;
        counter.OnMaxCountUpdated += HandleMaxCountUpdate;
    }

    private void UnhookCounter(PieceCounter counter)
    {
        if (!counter) return;
        counter.OnMaxCountUpdated -= HandleMaxCountUpdate;
    }

    public void HandleMaxCountUpdate(int newMaxCount)
    {
        if (newMaxCount > HoldBest.BestCount)
        {
            HoldBest.BestCount = newMaxCount;
        }
    }
}