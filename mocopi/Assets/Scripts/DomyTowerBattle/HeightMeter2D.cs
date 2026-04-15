using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightMeter2D : MonoBehaviour
{
    [Header("対象のTowerPieceSpawnerを指定する")]
    [SerializeField] private TowerPieceSpawner spaner;

    [Header("床の高さ(Y座標)を指定する")]
    [SerializeField] private Transform floorTransform;

    //  現在の高さを取得する
    public float CurrentHeight { get; private set; } = 0f;

    //  最高到達された高さを取得する
    public float MaxHeight { get; private set; } = 0f;

    //  現在の高さが変化した時の通知イベント
    public event Action<float> OnHeightChanged;

    //  ゲーム起動中の最高到達された高さが更新された時の通知イベント
    public event Action<float> OnMaxHeightUpdated;

    //  外部から最高到達された高さを更新する
    public void InitializeMax(float value)
    {
        MaxHeight = Mathf.Max(0f, value);
        OnMaxHeightUpdated?.Invoke(MaxHeight);
    }

    //  高さを再計測する
    public void MeasureNow() => Recalc();

    private void Recalc()
    {
        if (!spaner || !spaner.SpawnParent)
        {
            Debug.LogWarning("HeightMeter2D: TowerPieceSpawnerが設定されていません。");
            return;
        }
        if (!floorTransform)
        {
            Debug.LogWarning("HeightMeter2D: 床のTransformが設定されていません。");
            return;
        }

        var parent = spaner.SpawnParent;  //  スポーンされたパーツの親オブジェクト
        float floorY = floorTransform.position.y;  //  床のY座標

        float topY = float.NegativeInfinity;  //  パーツの中で一番上のY座標
        bool foundAny = false;  //  パーツが一つでも見つかったかどうか

        //   親オブジェクト以下の全てのDorppablePieceを調べる
        var pieces = parent.GetComponentsInChildren<DroppablePiece>(includeInactive: false);
        //  各パーツのコライダーから一番上のY座標を取得する
        foreach (var piece in pieces)
        {
            if (!piece.isActiveAndEnabled) continue;  //  無効化されているものは無視する

            //  パーツのコライダーから一番上のY座標を取得する
            if (TryGetTopYFromColliders(piece.transform, out float y))
            {
                if (y > topY) topY = y;
                foundAny = true;
            }
        }

        //  新しい高さを計算する
        float newHeight = foundAny ? Mathf.Max(0f, topY - floorY) : 0f;
        //   最高到達高さの更新
        if (!Mathf.Approximately(newHeight, CurrentHeight))
        {
            CurrentHeight = newHeight;
            OnHeightChanged?.Invoke(CurrentHeight);
        }
        if (CurrentHeight > MaxHeight)
        {
            MaxHeight = CurrentHeight;
            OnMaxHeightUpdated?.Invoke(MaxHeight);
        }
    }

    //  指定したTransform以下のCollider2D群から一番上のY座標を取得する
    private static bool TryGetTopYFromColliders(Transform root, out float topY)
    {
        topY = float.NegativeInfinity;

        var colliders = root.GetComponentsInChildren<Collider2D>(includeInactive: false);
        foreach (var col in colliders)
        {
            if (!col.enabled) continue;
            float colTopY = col.bounds.max.y;
            if (colTopY > topY)
            {
                topY = colTopY;
            }
        }
        return topY > float.NegativeInfinity;
    }

}