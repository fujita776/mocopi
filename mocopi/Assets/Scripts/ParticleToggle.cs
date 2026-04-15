using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleToggle : MonoBehaviour
{
    [Header("切り替えに使うキー")]
    [SerializeField] private KeyCode key = KeyCode.H;

    [Header("操作するParticleSystem")]
    [SerializeField] private ParticleSystem pSystem;

    //  再生しているかどうかのフラグ
    private bool isOn = false;

    void Awake()
    {
        if(pSystem == null)
        {
            Debug.LogError("操作するParticleSystemがアタッチされていません");
        }
    }

    void Start()
    {
        pSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void Update()
    {
        if(pSystem && Input.GetKeyDown(key))
        {
            isOn = !isOn;
            if(isOn)
                pSystem.Play();  //  再生する
            else  //  再生を止める
                pSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        }
    }
}
