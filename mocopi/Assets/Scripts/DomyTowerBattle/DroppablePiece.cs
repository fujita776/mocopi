using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DroppablePiece : MonoBehaviour
{
    [Header("プレビュー時の操作量")]
    [Header("ボタン1回で回す角度")]
    [SerializeField] private float rotateDommy = 15f;  //  回転の角度
    [Header("移動量")]
    [SerializeField] private float moveDommy = 0.25f;  //  移動の距離

    [Header("デバッグ用の表示(落としたかどうか)")]
    [SerializeField] private bool hasDropped = false;

    [Header("停止とみなす条件")]
    [SerializeField] private float stopVelocityThreshold = 0.05f;  //  この速度以下なら停止とみなす
    [SerializeField] private float stopAngularVelocityThreshold = 5f;  //  この角速度以下なら停止とみなす
    [SerializeField] private float stopDuration = 0.5f;  //  この時間以上停止状態が続いたら完全停止とみなす

    [Header("落下開始時の効果音")]
    [SerializeField] private AudioClip dropSound;

    [SerializeField] private AudioSource audioSource;

    //  カウント済みかどうかのフラグ
    public bool HasCounted 
    { 
        get;
        private set; 
    } 

    //  このピースが完全に停止した時のイベント
    public event System.Action OnPieceStopped;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        //  プレビュー開始時に停止状態でなければ停止する
        if(!rb.isKinematic && !hasDropped)
        {
            rb.isKinematic = true;
        }
    }

    public void SetupAudio(AudioSource source, AudioClip clip)
    {
        audioSource = source;
        dropSound = clip;
    }

    public void Drop()
    {
        if(hasDropped) return;
        hasDropped = true;
        rb.isKinematic = false;  //  kinematicを解除すると、spawn時に設定済みのgravityScaleに変わる

        if(dropSound && audioSource)
        {
            audioSource.PlayOneShot(dropSound);
        }

        //  停止監視コルーチンを開始する
        StartCoroutine(WatchStop());
    }

    //  プレビュー中の左回転
    public void RotateLeft()
    {
        if(hasDropped) return;
        transform.Rotate(0f, 0f, rotateDommy);  //  Z軸左回り回転
    }

    //  プレビュー中の右回転
    public void RotateRight()
    {
        if(hasDropped) return;
        transform.Rotate(0f, 0f, -rotateDommy);  //  Z軸右回り回転

    }

    //  プレビュー中の微移動
    public void MoveStep(Vector2 dir)
    {
        if(hasDropped) return;

        if(dir.sqrMagnitude <= 0f) return;

        //  正規化して1ステップ分だけ移動する
        Vector2 step = dir.normalized * moveDommy;
        transform.position += (Vector3)step;

    }

    //  落下済みかどうかの外部参照
    public bool HasDropped => hasDropped;

    private IEnumerator WatchStop()
    {
        float t = 0f;

        var wait = new WaitForFixedUpdate();

        while(true) 
        {
            //  停止状態かどうかをチェック
            bool isStopped = rb.IsSleeping() || (rb.velocity.sqrMagnitude < (stopVelocityThreshold * stopVelocityThreshold) && Mathf.Abs(rb.angularVelocity) < stopAngularVelocityThreshold);

            //  時間が経過したら完全停止とみなす
            t = isStopped ? t + Time.fixedDeltaTime : 0f;

            //  完全停止したらループ終了
            if (t >= stopDuration)
            {
                break;
            }
            //  次のフレームまで待機
            yield return wait;
        }

        if(HasCounted)
            yield break;

        HasCounted = true;
        OnPieceStopped?.Invoke();
    }
}
