using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideCanvas : MonoBehaviour
{
    [Header("非表示にしたいCanvas")]
    [SerializeField] private Canvas hideCanvas;

    [Header("表示を切り替える為のキー")]
    [SerializeField] private KeyCode key = KeyCode.U;

    private bool useKey = false; 

    // Update is called once per frame
    void Update()
    {
        if(useKey) return; 

        if(!Input.GetKeyDown(key)) return;

        hideCanvas.gameObject.SetActive(false);  //  Canvasを隠す

        useKey = true;
    }
}
