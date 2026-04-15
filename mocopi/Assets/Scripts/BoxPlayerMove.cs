using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoxPlayerMove : MonoBehaviour
{
    [Header("移動設定")]
    [Tooltip("移動速度（単位: ユニット/秒）")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("X軸の最小値")]
    [SerializeField] private float minX = -5f;
    [Tooltip("X軸の最大値")]
    [SerializeField] private float maxX = 5f;


    //  自動生成されたInput Actionクラスのインスタンス
    private PlayerInputSystem movementAction;

    private void Awake()
    {
        movementAction = new PlayerInputSystem();
    }

    private void OnEnable()
    {
        //  必要なアクションマップを有効化
        movementAction.PlayerBoxMove.Enable();
    }


    private void OnDisable()
    {
        movementAction.PlayerBoxMove.Disable();
    }

    void Update()
    {
        //  Moveアクションの入力値を取得し、X成分を利用
        Vector2 input = movementAction.PlayerBoxMove.Move.ReadValue<Vector2>();
        float horizontal = input.x;

        //  入力に基づいて移動量を計算
        Vector3 movement = new Vector3(horizontal * moveSpeed * Time.deltaTime, 0f, 0f);

        //  現在の位置に移動量を加算
        transform.position += movement;

        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
    }
}
