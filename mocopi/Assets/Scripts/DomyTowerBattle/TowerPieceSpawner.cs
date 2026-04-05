using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class TowerPieceSpawner : MonoBehaviour
{
    [Header("生成物の親")]
    [SerializeField] private Transform spawnParent;

    [Header("ピース数のカウント用コンポーネント")]
    [SerializeField] private PieceCounter pieceCounter;

    [Header("生成位置の基準座標")]
    [SerializeField] private Vector3 spawnPosition = Vector3.zero;

    [Header("基準位置からどれだけ上に出すか")]
    [SerializeField] private float spawnHeight = 5f;

    [Header("SpriteRendererの描画順")]
    [SerializeField] private int sortingOrder = 100;

    [Header("SortingLayerの名前")]
    [SerializeField] private string sortingLayerName = "";

    [Header("落下中の重力の倍率(1が標準)")]
    [SerializeField] private float gravityScale = 1f;

    [Header("生成直後プレビュー状態にする")]
    [SerializeField] private bool startInPreview = true;

    [Header("プレビュー中だと分かるように名前を変更")]
    [SerializeField] private string previewChangeName = "[Preview]";

    [Header("Physicsの調節(跳ねる、滑るなどの抑制)")]
    [SerializeField] private PhysicsMaterial2D physicsMaterial2D;
    [Header("跳ね返りの抑制")]
    [SerializeField,Min(0f)] private float linearDrag = 0.8f;
    [Header("滑りの抑制")]
    [SerializeField,Min(0f)] private float angularDrag = 1.2f;
    [Header("当たり判定の検出モード")]
    [SerializeField] private CollisionDetectionMode2D collisionMode = CollisionDetectionMode2D.Continuous;
    [Header("表示の滑らかさ")]
    [SerializeField] private RigidbodyInterpolation2D interpolation = RigidbodyInterpolation2D.Interpolate;

    [Header("高さ計測用HeightMeter2Dの参照")]
    [SerializeField] private HeightMeter2D heightMeter;

    [Header("落下時の効果音")]
    [SerializeField] private AudioClip audioClip;
    [Header("音量の設定")]
    [SerializeField, Range(0f, 1f)] private float audioVolume = 1f;

    public Transform SpawnParent => spawnParent;
    public Vector3 SpawnPosition => spawnPosition;
    public float SpawnHeight => spawnHeight;

    //  Spriteからピースを生成して返す
    public GameObject Spawn(Sprite sprite)
    {
        if(sprite == null)
        {
            Debug.LogError($"TowerPieseSpawnerのSpriteがnullです");
            return null;
        }

        //  GameObjectを生成して親子付けして、出現位置を決める
        var spawnedPiece = new GameObject("CapturedObject2D");
        if (spawnParent) spawnedPiece.transform.SetParent(spawnParent, true);
        spawnedPiece.transform.position = spawnPosition + Vector3.up * SpawnHeight;

        //  画像を見えるように出力する
        var spriteRen = spawnedPiece.AddComponent<SpriteRenderer>();
        spriteRen.sprite = sprite;
        spriteRen.sortingOrder = sortingOrder;
        if (!string.IsNullOrEmpty(sortingLayerName))  //  レイヤー名がある時、idを設定する
        {
            int id = SortingLayer.NameToID(sortingLayerName);
            if (id != 0) spriteRen.sortingLayerID = id;
        }

        //  当たり判定の生成
        var poly = spawnedPiece.AddComponent<PolygonCollider2D>();
        poly.isTrigger = false;

        if(physicsMaterial2D)
        {
            //  2D物理マテリアルの設定
            poly.sharedMaterial = physicsMaterial2D;
        }

        //  物理の生成
        var rb = spawnedPiece.AddComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;


        rb.drag = linearDrag;  //  跳ね返りの抑制
        rb.angularDrag = angularDrag;  //  滑りの抑制
        rb.collisionDetectionMode = collisionMode;  //  貫通防止の当たり判定の検出モード
        rb.interpolation = interpolation;  //  表示の滑らかさ


        //  プレビュー時の操作
        var drop = spawnedPiece.AddComponent<DroppablePiece>();
        //  落下時の効果音設定
        var src = spawnedPiece.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.volume = audioVolume;
        src.spatialBlend = 0f;

        drop.SetupAudio(src, audioClip);

        //  高さ計測用のイベント登録
        if (heightMeter)
        {
            drop.OnPieceStopped += heightMeter.MeasureNow;
        }

        //  プレビューの停止
        if (startInPreview)
        {
            rb.isKinematic = true;
            spawnedPiece.name = previewChangeName + spawnedPiece.name;
        }

        if(pieceCounter)
        {
            drop.OnPieceStopped += pieceCounter.PieceStopped;
        }

        //  ピースを返す
        return spawnedPiece;
    }
}
