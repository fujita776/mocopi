using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMPlayer : MonoBehaviour
{
    //  最初に生成された 1 つのインスタンスを保持する
    private static BGMPlayer instance;

    private void Awake()
    {
        //  まだインスタンスが存在していない時
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);  //  シーンを切り替えても破棄されないように
        }
        else
        {
            Destroy(gameObject);
        }

    }
}
