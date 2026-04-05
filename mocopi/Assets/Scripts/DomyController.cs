using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DomyController : MonoBehaviour
{
    // 自動生成されたInput Actionクラスのインスタンス
    private DomyInputActions inputActions;

    // 移動速度
    [SerializeField]private float moveSpeed = 5f;

    //  今、トリガー内にいる手元に保持しているコンポーネント
    private IHandHolder currentHolder = new NullHandHolder();
    private void Awake()
    {
        // InputActionアセットのインスタンスを生成
        inputActions = new DomyInputActions();
    }

    private void OnEnable()
    {
        // 必要なアクションマップを有効化
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        // アクションマップを無効化しておく
        inputActions.Player.Disable();
    }

    private void Update()
    {
        //  MoveアクションからVector2型の入力値を取得する
        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        //  入力に基づいてキャラクターを移動する
        Vector3 movement = new Vector3(moveInput.x, 0, moveInput.y);
        transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);
    }
}
