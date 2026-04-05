using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerGameOver : MonoBehaviour
{
    [Header("ゲーム終了時に表示するキャンバス")]
    [SerializeField] private GameObject gameOverCanvas;

    [Header("ゲーム終了時に無効化するスクリプト")]
    [SerializeField] private MonoBehaviour[] disableOnGameOver;

    [Header("時間を止めるかどうか")]
    [SerializeField] private bool stopTimeOnGameOver = true;

    private void Start()
    {
        //  ゲームオーバーのキャンバスは最初は非表示にする
        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(false);
        }
    }

    //  ゲームオーバー処理
    public void GameOver()
    {
        //  ゲームオーバーのキャンバスを表示
        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
        }
        //  指定されたスクリプトを無効化する
        foreach (var script in disableOnGameOver)
        {
            if (script != null)
            {
                script.enabled = false;
            }
        }
        //  時間を止める
        if (stopTimeOnGameOver)
        {
            Time.timeScale = 0f;
        }
    }


}
