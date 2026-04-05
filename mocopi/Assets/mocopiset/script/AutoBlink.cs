using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoBlink : MonoBehaviour
{
    //とじ目のBlendShapesが登録されているSkinnedMeshRendererを取得
    //まほば君の場合は顔のオブジェクトが持っている
    public SkinnedMeshRenderer face_SkinnedMeshRenderer;

    //とじ目の要素数
    public int eyeClose_KeyNumber = 5;

    //SetBlendShapeWeightに使用する数値
    //0でまぶたが完全に開く、100でまぶたが完全に閉じる
    private float weight = 0.0f;

    //時間を計測するための変数
    public float countTime = 0.0f;
    //まばたきの発動タイミングの変数
    public float blinkTriggerTime = 5.0f;

    [Header("まばたきの発動タイミングの最小の時間")]
    public float blinkTriggerTimeMin = 3.0f;

    [Header("まばたきの発動タイミングの最大の時間")]
    public float blinkTriggerTimeMax = 7.0f;

    void FixedUpdate()
    {
        //FixedUpdateは初期設定のままなら
        CheckCountTime();
    }

    void CheckCountTime()
    {
        //時間を計測
        countTime += Time.deltaTime;
        //時間がまばたきの発動タイミングを超えたら
        if (countTime > blinkTriggerTime)
        {
            //計測時間をリセット
            countTime = 0.0f;
            //まばたきの発動タイミングの変数に
            //blinkTriggerTimeMinからblinkTriggerTimeMaxの間でランダムな数値を取得
            blinkTriggerTime = UnityEngine.Random.Range(blinkTriggerTimeMin, blinkTriggerTimeMax);

            //目を閉じる処理開始
            StartCoroutine("CloseEye");
        }
    }

    IEnumerator CloseEye()
    {
        //閉じる際の絶妙な中目加減
        weight = 80.0f;
        //中目にしてちょっとだけ次の処理を待つ
        face_SkinnedMeshRenderer.SetBlendShapeWeight(eyeClose_KeyNumber, weight);
        //yield return new WaitForSeconds(0.05f);
        //まぶたを完全に閉じてちょっとだけ次の処理を待つ
        face_SkinnedMeshRenderer.SetBlendShapeWeight(eyeClose_KeyNumber, 100.0f);
        yield return new WaitForSeconds(0.1f);

        //目を開く処理開始
        StartCoroutine("OpenEye");
    }

    IEnumerator OpenEye()
    {
        //開く際の絶妙な中目加減
        weight = 60.0f;
        //中目にしてちょっとだけ次の処理を待つ
        face_SkinnedMeshRenderer.SetBlendShapeWeight(eyeClose_KeyNumber, weight);
        yield return new WaitForSeconds(0.0001f);
        //まぶたを完全に開く
        face_SkinnedMeshRenderer.SetBlendShapeWeight(eyeClose_KeyNumber, 0.0f);
    }

}
