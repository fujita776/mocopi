using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CaptureAndCreate2DObject : MonoBehaviour
{
    [Header("キャプチャの設定")]
    [Header("キャプチャ専用のカメラ")]
    [SerializeField] private Camera renderCamera;

    [Header("キャプチャの対象となるレイヤー")]
    [SerializeField] private LayerMask targetLayer;

    [Header("キャプチャしたものの解像度")]
    [SerializeField] private int textureSize = 512;


    [Header("スプライト化の設定")]
    [Header("スプライトの表示スケールのもの")]
    [SerializeField] private float pixelsPerUnit = 100f;

    [Header("スプライトのメッシュの形状(Tightで輪郭に沿う形)")]
    [SerializeField] private SpriteMeshType meshType = SpriteMeshType.Tight;

    [Header("スプライトのピボット位置")]
    [SerializeField] private Vector2 pivot = new Vector2(0.5f, 0.5f);

    [Header("生成先のTowerPieceSpawner")]
    [SerializeField] private TowerPieceSpawner spawner;

    //  生成したピースを通知するイベント
    public event Action<DroppablePiece> OnPieceCreated;

    //  キャプチャしてスプライト化し、ピースを生成する。
    public void CaptureAndCreate()
    {
        StartCoroutine(DoCaptureAndCreate());
    }

    private IEnumerator DoCaptureAndCreate()
    {
        if (renderCamera == null)
        {
            Debug.LogError("キャプチャ専用のカメラが未設定です。");
            yield break;
        }
        if (spawner == null)
        {
            Debug.LogError("生成先のTowerPieceSpawnerが未設定です。");
            yield break;
        }
        if (textureSize <= 0)
        {
            Debug.LogError("textureSizeは1以上にしてください。");
            yield break;
        }

        Texture2D capturedTex = null;
        yield return CameraCapture.CaptureTexture2D(
            renderCamera,
            targetLayer,
            textureSize,
            tex => capturedTex = tex
        );

        //  キャプチャ失敗のチェック
        if (capturedTex == null)
        {
            Debug.LogError("[CaptureAndCreate2DObject] キャプチャに失敗しました。");
            yield break;
        }

        //  Texture2DからSpriteにする
        var sprite = SpriteBuilder.CreateSprite(
            capturedTex,
            pixelsPerUnit,
            meshType,
            pivot
        );

        if (sprite == null)
        {
            Debug.LogError("[CaptureAndCreate2DObject] Sprite の生成に失敗しました。");
            
            yield break;
        }

        //  ピースを生成する
        var pieceGO = spawner.Spawn(sprite);
        if (pieceGO == null)
        {
            Debug.LogError("[CaptureAndCreate2DObject] ピース生成に失敗しました。");
            yield break;
        }

        //  ピース生成の通知
        var droppablePiece = pieceGO.GetComponent<DroppablePiece>();
        if(droppablePiece != null)
        {
            OnPieceCreated?.Invoke(droppablePiece);
        }
    }
}
