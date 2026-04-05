using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CameraCapture
{
    /// <summary>
    ///  カメラが映しているあるレイヤーに存在しているオブジェクトをTexture2Dにして受け取る。
    /// </summary>
    public static IEnumerator CaptureTexture2D(
        Camera camera,  //  キャプチャに使うカメラ
        LayerMask targetLayer,  //  このレイヤーのみ映す
        int textureSize,  //  出力する画像の幅と高さ
        Action<Texture2D> onCompleted  //  完成した後に送るコールバック
        )
    {
        if(camera == null)
        {
            Debug.LogError("カメラが存在しません");
            yield break;
        }

        if(textureSize <= 0)
        {
            Debug.LogError("テクスチャのサイズは0より大きくして下さい");
            yield break;
        }

        //  カメラの表示レイヤーと出力先を保存する
        int originalMask = camera.cullingMask;
        RenderTexture originalRT = camera.targetTexture;

        //  カメラが描いた絵を受け取るための画面の作成
        var renderTexture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32);
        renderTexture.Create();

        camera.cullingMask = targetLayer;  //  指定したレイヤーの物のみ描画する
        camera.targetTexture = renderTexture;  //  カメラの出力先を変更する

        yield return new WaitForEndOfFrame();  //  このフレームの最後に処理する
        camera.Render();  //  レンダリングする

        RenderTexture prev = RenderTexture.active;  //  現在値を保存
        RenderTexture.active = renderTexture;  //  renderTextureを現在の読み取り先に指定する

        var tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);  //  絵を格納する箱を用意する

        tex.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);  //  現在のRenderTexture.activeからTexture2Dへコピーする

        tex.Apply();  //  コピーしたピクセルを確定する

        RenderTexture.active = prev;  //  読み取り先を元の状態に戻す

        //  カメラ設定を元に戻す
        camera.targetTexture = originalRT;
        camera.cullingMask = originalMask;

        renderTexture.Release();
        UnityEngine.Object.Destroy(renderTexture);

        //  コールバックで結果を返す(呼び出した側がtexを消す)
        onCompleted?.Invoke(tex);

    }
}
