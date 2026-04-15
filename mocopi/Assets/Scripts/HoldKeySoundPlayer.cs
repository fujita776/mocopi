using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class HoldKeySoundPlayer : MonoBehaviour
{
    [Header("押している間、再生させるキー")]
    [SerializeField] private InputActionReference holdAction;

    [Header("再生するAudioSource")]
    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        if(audioSource == null)
        {
            Debug.LogError($"{nameof(HoldKeySoundPlayer)}AudioSourceコンポーネントが見つかりません。", this);
            enabled = false;
            return;
        }

        //  キーを放すまで繰り返し再生される
        audioSource.loop = true;
    }

    private void OnEnable()
    {
        //  InputActionの有効化とコールバック登録
        if (holdAction != null && holdAction.action != null)
        {
            holdAction.action.started += OnHoldStarted;
            holdAction.action.canceled += OnHoldCanceled;
            holdAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        //  コールバック解除とInputActionの無効化
        if (holdAction != null && holdAction.action != null)
        {
            holdAction.action.started -= OnHoldStarted;
            holdAction.action.canceled -= OnHoldCanceled;
            holdAction.action.Disable();
        }
    }

    //  キーを押し始めたときに呼ばれる
    private void OnHoldStarted(InputAction.CallbackContext ctx)
    {
        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    //  キーを放したときに呼ばれる
    private void OnHoldCanceled(InputAction.CallbackContext ctx)
    {
        if (audioSource.isPlaying)
            audioSource.Stop();
    }
}
