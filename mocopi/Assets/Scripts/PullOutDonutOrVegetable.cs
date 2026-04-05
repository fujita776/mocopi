using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PullOutDonutOrVegetable : MonoBehaviour
{
    [SerializeField] private GameObject donut;
    [SerializeField] private GameObject vegetable;

    [Header("ドーナッツか野菜が現れる位置")]
    [SerializeField] private Transform spawnPoint;

    [Header("投げ先のターゲット")]
    [SerializeField] private Transform throwTargetJ;
    [SerializeField] private Transform throwTargetK;
    [SerializeField] private Transform throwTargetL;

    [Header("プレイヤーの手元保持コンポーネント")]
    [SerializeField] private MonoBehaviour domyHandHolder;

    [Header("投げる設定")]
    [Tooltip("投げる力")]
    [SerializeField] private float throwForce = 10f;

    [Header("投げる時の効果音")]
    [SerializeField] private AudioClip throwClip;

    [Header("音を鳴らすAudioSourse")]
    [SerializeField] private AudioSource audioSource;

    [Header("かごに入らない場合の消滅設定")]
    [SerializeField] private float lifeTime = 5f;

    private IHandHolder currentHandHolder;

    private void Awake()
    {
        if (domyHandHolder != null)
        {
            currentHandHolder = domyHandHolder as IHandHolder;
            if (currentHandHolder == null)
            {
                Debug.LogWarning("domyHandHolder に IHandHolder を実装したコンポーネントをアサインしてください。");
                currentHandHolder = new NullHandHolder();
            }
        }
        else
        {
            currentHandHolder = new NullHandHolder();
            Debug.LogWarning("有効な IHandHolder が見つかりませんでした。NullHandHolder を使用します。");
        }

        //  AudioSourceが設定されていなければ自動取得または作成する
        if (audioSource == null)
        {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }
        //  ループしない
        audioSource.loop = false;
    }

    private void Update()
    {
        // ① 手元にアイテムがない場合は、O または P キーで生成して手元にアタッチ
        if (currentHandHolder.HeldItem == null)
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                GameObject item = SpawnObject(donut);
                if (item != null)
                    currentHandHolder.AttachItem(item);
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                GameObject item = SpawnObject(vegetable);
                if (item != null)
                    currentHandHolder.AttachItem(item);
            }
        }

        // ② すでに手元にアイテムがある場合、J・K・L キーで投げる処理を行う
        if (currentHandHolder.HeldItem != null)
        {
            if (Input.GetKeyDown(KeyCode.J) && throwTargetJ != null)
            {
                ThrowWithAutoDestroy(throwTargetJ);
            }
            else if (Input.GetKeyDown(KeyCode.K) && throwTargetK != null)
            {
                ThrowWithAutoDestroy(throwTargetK);
            }
            else if (Input.GetKeyDown(KeyCode.L) && throwTargetL != null)
            {
                ThrowWithAutoDestroy(throwTargetL);
            }
    
        }

        // ③ I キーで手放す（投げる処理とは別に単純にデタッチ）
        if (Input.GetKeyDown(KeyCode.I) && currentHandHolder.HeldItem != null)
        {
            currentHandHolder.DetachItem();
        }
    }

    /// <summary>
    /// 指定されたプレハブを生成し、そのオブジェクトを返す
    /// </summary>
    private GameObject SpawnObject(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("生成するプレハブが設定されていません。");
            return null;
        }

        Vector3 position = (spawnPoint != null) ? spawnPoint.position : currentHandHolder.HandTransform.position;
        Quaternion rotation = (spawnPoint != null) ? spawnPoint.rotation : currentHandHolder.HandTransform.rotation;
        return Instantiate(prefab, position, rotation);
    }

    //  投げる時の効果音の再生
    private void PlayThrowSound()
    {
        if (throwClip != null && audioSource != null)
            audioSource.PlayOneShot(throwClip);
    }
    ///  <summary>
    ///  投げて、効果音を鳴らし、自動消滅コンポーネントを追加して、経過時間後に破棄する
    ///  </summary>
    private void ThrowWithAutoDestroy(Transform target)
    {
        GameObject thrown = currentHandHolder.HeldItem;
        currentHandHolder.ThrowItem(target, throwForce);  //  投げる
        PlayThrowSound();  //  投げる効果音

        //  自動消滅のコンポーネントを追加して、設定を渡す
        var disapperThrew = thrown.AddComponent<DisappearThrewAfter>();
        disapperThrew.lifeTime = lifeTime;
    }
}
