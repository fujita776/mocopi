using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHandHolder
{
    //  生成したオブジェクトをアタッチする際、この Transform の位置・回転を参照する
    Transform HandTransform { get; }

    //  現在、手元に持っているオブジェクトを取得するプロパティ
    GameObject HeldItem { get; }

    //  指定されたオブジェクトを手元にアタッチする処理を行うメソッド
    void AttachItem(GameObject item);

    //  現在手元に保持しているオブジェクトを解放する処理を行うメソッド
    void DetachItem();

    //  指定されたターゲットへ、指定した力でアイテムを投げる
    void ThrowItem(Transform target, float force);
}