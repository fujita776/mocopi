using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceCounter : MonoBehaviour
{
    [Header("参照するTowerPieceSpawner")]
    [SerializeField] private TowerPieceSpawner spawner;

    //  現在のピース数
    public int CurrentCount
    {
        get;
        private set;
    }

    //  シーケンス中の最大ピース数
    public int MaxCount
    {
        get;
        private set;
    }

    public event Action<int> OnCountChanged;  //  ピース数が変化したときに発火するイベント
    public event Action<int> OnMaxCountUpdated;  //  シーケンス中の最大ピース数が更新されたときに発火するイベント

    private void Awake()
    {
        if(!spawner)
        {
            spawner = FindObjectOfType<TowerPieceSpawner>();
        }
    }

    //  ピースが完全に停止した状態で呼び出されるメソッド
    public void PieceStopped()
    {
        RecountPiece();
    }

    private void RecountPiece()
    {
        if(!spawner || !spawner.SpawnParent)
            return;

        var pieces = spawner.SpawnParent.GetComponentsInChildren<DroppablePiece>(includeInactive: false);

        int count = 0;

        foreach (var p in pieces)
        {
            if(p && p.HasDropped && p.HasCounted)
                count++;
        }

        if(count != CurrentCount)
        {
            CurrentCount = count;
            OnCountChanged?.Invoke(CurrentCount);
        }

        if(CurrentCount > MaxCount)
        {
            MaxCount = CurrentCount;
            OnMaxCountUpdated?.Invoke(MaxCount);
        }
    }

    public void InitializeMax(int value) 
    {
        MaxCount = Mathf.Max(0, value);
        OnMaxCountUpdated?.Invoke(MaxCount);
    }
}
