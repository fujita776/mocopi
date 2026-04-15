using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//  インスペクタービューでシリアライズが可能なクラス
[System.Serializable]
public class ShapeKeyPair
{
    [Tooltip("SkinnedMeshのBlendShape名")]
    public string shapeName;
    [Tooltip("このキーが押されたら切り替える")]
    public KeyCode key;

    //  ランタイム中に自動でセットされるフィールド
    [HideInInspector] public int index;
    [HideInInspector] public float weight;
    [HideInInspector] public bool duttonTriggered;
}

public class BlendShapeController : MonoBehaviour
{
    [Header("対象の頭部オブジェクト")]
    public GameObject head;
    [Header("操作するシェイプとキーの組み合わせ")]
    public ShapeKeyPair[] pairs;

    private SkinnedMeshRenderer skinnedMeshRenderer;

    void Start()
    {
        if(head == null)
        {
            Debug.LogError("頭部オブジェクトが設定されていません。");
            enabled = false;
            return;
        }

        //  スキンメッシュレンダラーを取得
        skinnedMeshRenderer = head.GetComponent<SkinnedMeshRenderer>();
        if(skinnedMeshRenderer == null)
        {
            Debug.LogError("頭部オブジェクトにスキンメッシュレンダラーが存在していません。");
            enabled = false;
            return;
        }

        //  各シェイプのインデックスと初期値をセットする
        foreach (var pair in pairs)
        {
            pair.index = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(pair.shapeName);

            if (pair.index < 0)
                Debug.LogWarning($"ブレンドシェイプの{pair.shapeName}が見つかりません。");

            pair.weight = 0;
            pair.duttonTriggered = false;
        }
    }

    void Update()
    {
        foreach(var pair in pairs)
        {
            //  キーが離れていたら、ボタンの状態をリセットする
            if(!Input.GetKey(pair.key))
            {
                pair.duttonTriggered = false;
                continue;
            }

            //  該当するキーが押されている時
            if(!pair.duttonTriggered)
            {
                pair.weight = (pair.weight == 0f) ? 100f : 0f;
                skinnedMeshRenderer.SetBlendShapeWeight(pair.index, pair.weight);
                pair.duttonTriggered = true;
            }

        }
    }
}