using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FoodScore : MonoBehaviour
{
    public static FoodScore Instance;

    private int donutCount;
    private int vegetableCount;

    private void Awake()
    {
        // シングルトン的に扱う
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// FoodType に応じてスコアを加算
    /// </summary>
    /// <param name="type">ドーナツ or 野菜</param>
    public void AddCount(FoodType type)
    {
        switch (type)
        {
            case FoodType.Donut:
                donutCount++;
                break;
            case FoodType.Vegetable:
                vegetableCount++;
                break;
        }

        FindObjectOfType<FoodScoreUI>()?.UpdateScoreText();

        Debug.Log($"[ScoreManager] Donut={donutCount}, Vegetable={vegetableCount}");
    }

    public int GetDonutCount() => donutCount;
    public int GetVegetableCount() => vegetableCount;
}
