using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PieceCountUI2D : MonoBehaviour
{
    [Header("PieceCounterの参照")]
    [SerializeField] private PieceCounter counter;

    [Header("現在のピース数の表示テキスト")]
    [SerializeField] private TextMeshProUGUI[] currentCountTexts;

    [Header("シーケンス中の最大ピース数の表示テキスト")]
    [SerializeField] private TextMeshProUGUI maxCountText;


    private void Awake()
    {
        if(!counter)
            counter = FindObjectOfType<PieceCounter>();
    }

    private void OnEnable()
    {
        if (!counter)
            return;

        counter.OnCountChanged += UpdateCurrentText;
        counter.OnMaxCountUpdated += UpdateMaxText;

        //  初期表示の更新
        UpdateCurrentText(counter.CurrentCount);
        UpdateMaxText(counter.MaxCount);
    }

    private void OnDisable()
    {
        if(!counter)
            return;

        counter.OnCountChanged -= UpdateCurrentText;
        counter.OnMaxCountUpdated -= UpdateMaxText;
    }

    private void UpdateCurrentText(int count)
    {
        string s = $"{count}";
        foreach (var t in currentCountTexts)
        {
            if (t)
                t.text = s;
        }
    }

    private void UpdateMaxText(int maxCount)
    {
        string s = $"{maxCount}";
        if (maxCountText)
            maxCountText.text = s;
    }
}