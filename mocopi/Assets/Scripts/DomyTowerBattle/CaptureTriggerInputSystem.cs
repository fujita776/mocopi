using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CaptureTriggerInputSystem : MonoBehaviour
{
    [Header("2Dオブジェクト生成マネージャー")]
    [SerializeField] private CaptureAndCreate2DObject captureManager;
    public bool waitCapcture = false;

    [Header("ピースを移動するコントローラー(current部分を自動でセットさせるため)")]
    [SerializeField] private PieceInputController pieceInputController;

    private TowerGameControls gameControls;

    private void Awake()
    {
        if (!captureManager)
            captureManager = FindObjectOfType<CaptureAndCreate2DObject>();

        gameControls = new TowerGameControls();
        gameControls.UI.Falt.performed += OnCapturePerformed;
    }

    private void OnEnable()
    {
        gameControls.Enable();
        if (captureManager != null)
        {
            captureManager.OnPieceCreated += HandlePieceCreated;
        }
    }
    private void OnDisable()
    {
        gameControls.Disable();
        if (captureManager != null)
        {
            captureManager.OnPieceCreated -= HandlePieceCreated;
        }
    }

    private void OnCapturePerformed(InputAction.CallbackContext ctx)
    {
        if (waitCapcture) return;

        if (captureManager)
        {
            captureManager.CaptureAndCreate();

        }
        else
            Debug.LogWarning("captureManagerがありません。");

    }

    // ピース生成時に呼ばれる
    private void HandlePieceCreated(DroppablePiece drop)
    {
        // 自動でPieceInputControllerのcurrentにセットする
        if (pieceInputController != null && drop != null)
            pieceInputController.SetCurrent(drop);
    }

    private void Update()
    {
        if(pieceInputController != null)
        {
            waitCapcture = pieceInputController.HasControlPiece;
        }
        else
        {
            waitCapcture = false;
        }
    }
}
