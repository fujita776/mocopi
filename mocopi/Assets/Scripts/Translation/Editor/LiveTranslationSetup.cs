using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ライブ翻訳システムを現在のシーンの既存オブジェクトに統合するエディタツール
/// - AudioChunkProvider → 既存のLipSync用AudioSourceオブジェクトに追加
/// - SubtitleUI → 既存のCanvasに追加
/// - Whisper/Translation/Manager → 最小限のオブジェクト1つ
/// メニュー: GameObject > Live Translation > Setup In Scene
/// </summary>
public class LiveTranslationSetup
{
    [MenuItem("GameObject/Live Translation/Setup In Scene")]
    public static void SetupInScene()
    {
        if (Object.FindObjectOfType<LiveTranslationManager>() != null)
        {
            EditorUtility.DisplayDialog("Live Translation", "このシーンには既にLiveTranslationManagerが存在します。", "OK");
            return;
        }

        //  既存のLipSync用AudioSourceオブジェクトを探す
        var lipSync = Object.FindObjectOfType<uLipSync.uLipSync>();
        GameObject audioTarget = null;
        if (lipSync != null)
        {
            audioTarget = lipSync.gameObject;
        }
        else
        {
            //  uLipSyncがなければAudioSourceを持つオブジェクトを探す
            var audioSources = Object.FindObjectsOfType<AudioSource>();
            if (audioSources.Length > 0)
            {
                audioTarget = audioSources[0].gameObject;
            }
        }

        //  AudioChunkProvider を既存オブジェクトに追加
        AudioChunkProvider chunkProvider;
        if (audioTarget != null)
        {
            chunkProvider = Undo.AddComponent<AudioChunkProvider>(audioTarget);
            Debug.Log($"[LiveTranslationSetup] AudioChunkProvider を '{audioTarget.name}' に追加");
        }
        else
        {
            //  AudioSourceが見つからなければ新規作成
            var audioObj = new GameObject("TranslationAudioCapture");
            Undo.RegisterCreatedObjectUndo(audioObj, "Create Audio Capture");
            audioObj.AddComponent<AudioSource>();
            chunkProvider = audioObj.AddComponent<AudioChunkProvider>();
            Debug.LogWarning("[LiveTranslationSetup] AudioSourceが見つからなかったため新規オブジェクトを作成しました");
        }

        //  既存のCanvasを探す
        Canvas existingCanvas = null;
        foreach (var c in Object.FindObjectsOfType<Canvas>())
        {
            //  SubtitleCanvas（前回セットアップの残り）は除外
            if (c.gameObject.name == "SubtitleCanvas") continue;
            if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
            {
                existingCanvas = c;
                break;
            }
        }

        //  字幕パネルをCanvasに追加
        Transform canvasTransform;
        if (existingCanvas != null)
        {
            canvasTransform = existingCanvas.transform;
            Debug.Log($"[LiveTranslationSetup] 既存Canvas '{existingCanvas.name}' に字幕UIを追加");
        }
        else
        {
            var canvasObj = new GameObject("SubtitleCanvas");
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Subtitle Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasTransform = canvasObj.transform;
        }

        //  字幕背景パネル
        var panelObj = new GameObject("SubtitlePanel");
        panelObj.transform.SetParent(canvasTransform, false);

        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.02f);
        panelRect.anchorMax = new Vector2(0.9f, 0.12f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.6f);

        //  字幕テキスト
        var textObj = new GameObject("SubtitleText");
        textObj.transform.SetParent(panelObj.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 5f);
        textRect.offsetMax = new Vector2(-20f, -5f);

        var tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = "";
        tmpText.fontSize = 32;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;
        tmpText.enableWordWrapping = true;

        var subtitleUI = panelObj.AddComponent<SubtitleUI>();

        Undo.RegisterCreatedObjectUndo(panelObj, "Create Subtitle Panel");

        //  翻訳マネージャ（最小限のオブジェクト1つ）
        var managerObj = new GameObject("TranslationManager");
        Undo.RegisterCreatedObjectUndo(managerObj, "Create Translation Manager");

        var manager = managerObj.AddComponent<LiveTranslationManager>();
        var whisper = managerObj.AddComponent<WhisperProcessor>();
        var translation = managerObj.AddComponent<TranslationProcessor>();

        //  参照を設定
        SetPrivateField(subtitleUI, "subtitleText", tmpText);
        SetPrivateField(subtitleUI, "backgroundPanel", panelImage);

        SetPrivateField(manager, "audioChunkProvider", chunkProvider);
        SetPrivateField(manager, "whisperProcessor", whisper);
        SetPrivateField(manager, "translationProcessor", translation);
        SetPrivateField(manager, "subtitleUI", subtitleUI);

        AssignModelAssets(whisper, translation);

        Selection.activeGameObject = managerObj;

        string audioTargetName = audioTarget != null ? audioTarget.name : "新規オブジェクト";
        string canvasName = existingCanvas != null ? existingCanvas.name : "新規Canvas";

        Debug.Log("[LiveTranslationSetup] セットアップ完了");
        EditorUtility.DisplayDialog("Live Translation",
            $"セットアップ完了\n\n" +
            $"AudioChunkProvider → {audioTargetName}\n" +
            $"字幕UI → {canvasName}\n" +
            $"TranslationManager → TranslationManager\n\n" +
            "InspectorでModelAssetの割り当てを確認してください。",
            "OK");
    }

    /// <summary>
    /// 前回セットアップした翻訳システムを削除する
    /// </summary>
    [MenuItem("GameObject/Live Translation/Remove From Scene")]
    public static void RemoveFromScene()
    {
        //  TranslationManager オブジェクトを削除
        var manager = Object.FindObjectOfType<LiveTranslationManager>();
        if (manager != null)
        {
            Undo.DestroyObjectImmediate(manager.gameObject);
        }

        //  AudioChunkProvider を削除（既存オブジェクトからコンポーネントだけ除去）
        var chunkProvider = Object.FindObjectOfType<AudioChunkProvider>();
        if (chunkProvider != null)
        {
            Undo.DestroyObjectImmediate(chunkProvider);
        }

        //  SubtitlePanel を削除
        var subtitleUI = Object.FindObjectOfType<SubtitleUI>();
        if (subtitleUI != null)
        {
            Undo.DestroyObjectImmediate(subtitleUI.gameObject);
        }

        //  前回作った SubtitleCanvas があれば削除
        var oldCanvas = GameObject.Find("SubtitleCanvas");
        if (oldCanvas != null)
        {
            Undo.DestroyObjectImmediate(oldCanvas);
        }

        //  前回作った LiveTranslation があれば削除
        var oldRoot = GameObject.Find("LiveTranslation");
        if (oldRoot != null)
        {
            Undo.DestroyObjectImmediate(oldRoot);
        }

        Debug.Log("[LiveTranslationSetup] 翻訳システムを削除しました");
    }

    private static void SetPrivateField(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogWarning($"[LiveTranslationSetup] フィールド '{fieldName}' が見つかりません");
        }
    }

    private static void AssignModelAssets(WhisperProcessor whisper, TranslationProcessor translation)
    {
        string[] whisperEncoderGuids = AssetDatabase.FindAssets("encoder_model", new[] { "Assets/Models/Whisper" });
        string[] whisperDecoderGuids = AssetDatabase.FindAssets("decoder_model", new[] { "Assets/Models/Whisper" });

        if (whisperEncoderGuids.Length > 0)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Unity.Sentis.ModelAsset>(
                AssetDatabase.GUIDToAssetPath(whisperEncoderGuids[0]));
            if (asset != null) SetPrivateField(whisper, "encoderModelAsset", asset);
        }
        if (whisperDecoderGuids.Length > 0)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Unity.Sentis.ModelAsset>(
                AssetDatabase.GUIDToAssetPath(whisperDecoderGuids[0]));
            if (asset != null) SetPrivateField(whisper, "decoderModelAsset", asset);
        }

        string[] opusEncoderGuids = AssetDatabase.FindAssets("encoder_model", new[] { "Assets/Models/OpusMT" });
        string[] opusDecoderGuids = AssetDatabase.FindAssets("decoder_model", new[] { "Assets/Models/OpusMT" });

        if (opusEncoderGuids.Length > 0)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Unity.Sentis.ModelAsset>(
                AssetDatabase.GUIDToAssetPath(opusEncoderGuids[0]));
            if (asset != null) SetPrivateField(translation, "encoderModelAsset", asset);
        }
        if (opusDecoderGuids.Length > 0)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Unity.Sentis.ModelAsset>(
                AssetDatabase.GUIDToAssetPath(opusDecoderGuids[0]));
            if (asset != null) SetPrivateField(translation, "decoderModelAsset", asset);
        }
    }
}
