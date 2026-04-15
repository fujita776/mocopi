using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SpriteBuilder
{
    public static Sprite CreateSprite(
        Texture2D tex,  //  元になる画像
        float pixelsPerUnit = 100f,  //  何ピクセルをUnityの1ユニットとみなすか
        SpriteMeshType meshType = SpriteMeshType.Tight,  //  スプライトのメッシュ形状
        Vector2? pivot = default  //  スプライトの原点
        )
    {
        if(tex == null)
            throw new ArgumentNullException(nameof(tex), "SpriteBuilderのテクスチャがnullになっています。");

        if (pixelsPerUnit <= 0)
            throw new ArgumentOutOfRangeException(nameof(pixelsPerUnit), "pixelsPerUnitの大きさは0より上にして下さい");

        if (pivot == default)
            pivot = new Vector2(0.5f, 0.5f);

        var piv = pivot ?? new Vector2(0.5f, 0.5f);

        return Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            piv,
            pixelsPerUnit,
            0,
            meshType
            );
    }
}
