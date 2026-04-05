using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TriggerDestroyAndCount : MonoBehaviour
{
    private FoodObject foodObj;

    [Header("破棄時の効果音")]
    [SerializeField] private AudioClip destroyClip;

    private void Awake()
    {
        // 同じオブジェクトにアタッチされた FoodObject を取得
        foodObj = GetComponent<FoodObject>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 「特定のトリガーオブジェクト」と衝突した場合のみスコア加算し、効果音の再生と破棄
        if (other.CompareTag("DestroyFood"))
        {
            FoodScore.Instance.AddCount(foodObj.foodType);

            if(destroyClip != null)
                AudioSource.PlayClipAtPoint(destroyClip, transform.position);

            Destroy(gameObject);
        }
    }
}
