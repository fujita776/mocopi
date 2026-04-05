using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using uLipSync;

public class LipSyncController : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] protected uLipSync.uLipSync lipSync;

    void Start()
    {
        audioSource.Play();  //  リップシンクが連動する
    }
}
