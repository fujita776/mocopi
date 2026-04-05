using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//  投げた後、かごに入らず一定時間経過した時に自動消滅＆効果音再生を行うコンポーネント
public class DisappearThrewAfter : MonoBehaviour
{
    //  かごに入らない場合の消滅するまでの時間
    public float lifeTime = 5f;

    //  消滅した時に鳴らす音
    public AudioClip disappearClip;

    //  効果音再生用のAudioSource
    private AudioSource audioSource;

    private void Awake()
    {
        //  AudioSourceコンポーネントを追加する、ループ再生はオフ
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
    }

    private void Start()
    {
        //  コルーチンを開始する
        StartCoroutine(ExpireAfterDelay());
    }

    private IEnumerator ExpireAfterDelay()
    {
        //  lifeTimeの時間だけ待機する(かごに入った場合でも停止される)
        yield return new WaitForSeconds(lifeTime);

        //  disappearClipがされている場合は、その効果音を再生
        if(disappearClip != null )
        {
            audioSource.PlayOneShot(disappearClip);
            //  効果音の長さ分だけ待機する
            yield return new WaitForSeconds(disappearClip.length);
        }

        //  オブジェクトを破棄
        Destroy(gameObject);
    }
}
