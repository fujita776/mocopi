using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerGameOverZone2D : MonoBehaviour
{
    [Header("ゲームオーバー時の処理")]
    [SerializeField] private TowerGameOver gameOver;

    [Header("判定対象")]
    [SerializeField] private TowerPieceSpawner pieceSpawner;

    private Transform targetParent;

    private void Awake()
    {
        if (!gameOver)
        {
            Debug.LogError("TowerGameOverZone2DのTowerGameOverが設定されていません。");
        }

        //  判定対象の親Transformを取得する
        targetParent = pieceSpawner ? pieceSpawner.SpawnParent : null;

        if (!targetParent)
        { 
            Debug.LogError("TowerGameOverZone2Dの判定対象の親Transformが取得できません。");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //  判定対象の親配下にあるオブジェクトが触れたらゲームオーバー
        if (targetParent && collision.transform.IsChildOf(targetParent))
        {
            gameOver.GameOver();
        }
    }
}
