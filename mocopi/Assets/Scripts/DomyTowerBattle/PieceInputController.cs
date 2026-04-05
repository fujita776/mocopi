using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PieceInputController : MonoBehaviour
{
    [Header("操作対象のDroppablePiece")]
    [SerializeField] private DroppablePiece current;  //  TowerGameManagerから都度セットする

    [Header("入力設定")]
    [SerializeField] private float inputDeadzone = 0.15f;

    [Header("滑らかにする移動設定")]
    [SerializeField] private float moveSpeed = 3f;      //  押し具合に応じた移動速度
    [SerializeField] private float smoothTime = 0.06f;

    [Header("左右移動の制限")]
    [SerializeField] private bool useLimitX = true;
    [SerializeField] private float minX = -5f;
    [SerializeField] private float maxX = 5f;

    private bool canDrop = true;  //  連射対策のフラグ

    // 今、操作しているピースがあるか
    public bool HasControlPiece => current != null && !current.HasDropped;

    private TowerGameControls controls;
    private float moveAxisX;

    //  滑らに移動させるための変数
    private Vector3 targetPosition;
    private Vector3 smoothVelocity;
    private bool isHoldingMove;

    private void Awake()
    {
        controls = new TowerGameControls();

        //  入力値を受け取る
        controls.GamePlay.Move.performed += ctx =>
        {
            moveAxisX = ctx.ReadValue<float>();

            //  デッドゾーン内は無視
            if (Mathf.Abs(moveAxisX) <= inputDeadzone)
            {
                isHoldingMove = false;
                return;
            }

            isHoldingMove = true;
        };

        controls.GamePlay.Move.canceled += _ =>
        {
            moveAxisX = 0f;
            isHoldingMove = false;
        };

        //  落下入力
        controls.GamePlay.Drop.performed += _ =>
        {
            if (current == null || current.HasDropped) return;
            if (!canDrop) return; // 連射対策

            canDrop = false;
            current.Drop();
        };
        controls.GamePlay.Drop.canceled += _ =>
        {
            canDrop = true;
        };
    }

    private void OnEnable() => controls?.Enable();

    private void OnDisable() => controls?.Disable();

    private void OnDestroy()
    {
        //  InputActionAssetを破棄してリークを防ぐ
        controls?.Dispose();
    }

    private void Update()
    {
        if (current == null || current.HasDropped) return;

        if (isHoldingMove && Mathf.Abs(moveAxisX) > inputDeadzone)
        {
            targetPosition += Vector3.right * (moveAxisX * moveSpeed * Time.deltaTime);
        }

        //  X軸の移動制限
        if (useLimitX)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        }

        //  現在位置をターゲットへ追従させる
        current.transform.position = Vector3.SmoothDamp(
            current.transform.position,
            targetPosition,
            ref smoothVelocity,
            smoothTime
        );
    }

    //  外部のGameManagerなどから現在の操作対象を差し替える
    public void SetCurrent(DroppablePiece piece)
    {
        current = piece;
        moveAxisX = 0f;
        isHoldingMove = false;
        smoothVelocity = Vector3.zero;

        if (current != null)
        {
            //  ターゲット位置を現在位置に同期して瞬間移動を防ぐ
            targetPosition = current.transform.position;
        }
        else
        {
            targetPosition = Vector3.zero;
        }

        canDrop = true;
    }
}