using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//  シーン切り替えた後でも最高到達した高さとピースの数を残しておくためのもの
public static class HoldBest
{
    public static float BestHeight = 0f;
    public static int BestCount = 0;
}