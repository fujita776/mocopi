using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Whisper;

/// <summary>
/// ライブ翻訳システムを現在のシーンにセットアップするエディタツール
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

        //  メインオブジェクト作成
        var managerObj = new GameObject("TranslationManager");
        Undo.RegisterCreatedObjectUndo(managerObj, "Setup Live Translation");

        var whisperManager = managerObj.AddComponent<WhisperManager>();
        var audioBridge = managerObj.AddComponent<AudioBridge>();
        var manager = managerObj.AddComponent<LiveTranslationManager>();

        //  字幕専用Canvas
        var canvasObj = new GameObject("SubtitleCanvas");
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create Subtitle Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        //  字幕パネル
        var panelObj = new GameObject("SubtitlePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);

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

        //  日本語対応フォント適用
        var jaFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/font/nikumaru SDF.asset");
        if (jaFont != null)
        {
            tmpText.font = jaFont;
        }
        else
        {
            Debug.LogWarning("[LiveTranslationSetup] nikumaru SDF.asset が見つかりません。日本語表示時に文字化けする可能性があります");
        }

        var subtitleUI = panelObj.AddComponent<SubtitleUI>();
        Undo.RegisterCreatedObjectUndo(panelObj, "Create Subtitle Panel");

        //  参照を設定
        SetPrivateField(subtitleUI, "subtitleText", tmpText);
        SetPrivateField(subtitleUI, "backgroundPanel", panelImage);

        SetPrivateField(manager, "whisperManager", whisperManager);
        SetPrivateField(manager, "audioBridge", audioBridge);
        SetPrivateField(manager, "subtitleUI", subtitleUI);

        //  WhisperManager のモデルパス設定（smallモデル）
        SetPrivateField(whisperManager, "modelPath", "ggml-small.bin");
        SetPrivateField(whisperManager, "isModelPathInStreamingAssets", true);

        Selection.activeGameObject = managerObj;

        Debug.Log("[LiveTranslationSetup] セットアップ完了");
        EditorUtility.DisplayDialog("Live Translation",
            "セットアップ完了\n\n" +
            "モデル: ggml-small.bin (StreamingAssets)\n" +
            "F1: 字幕ON/OFF\n" +
            "F2: 翻訳ON/OFF（英語/日本語切替）\n\n" +
            "AudioBridgeは実行時にuLipSyncMicrophoneのAudioSourceを自動検出します。",
            "OK");
    }

    [MenuItem("GameObject/Live Translation/Remove From Scene")]
    public static void RemoveFromScene()
    {
        var manager = Object.FindObjectOfType<LiveTranslationManager>();
        if (manager != null)
            Undo.DestroyObjectImmediate(manager.gameObject);

        var subtitleUI = Object.FindObjectOfType<SubtitleUI>();
        if (subtitleUI != null)
            Undo.DestroyObjectImmediate(subtitleUI.gameObject);

        var oldCanvas = GameObject.Find("SubtitleCanvas");
        if (oldCanvas != null)
            Undo.DestroyObjectImmediate(oldCanvas);

        var oldRoot = GameObject.Find("LiveTranslation");
        if (oldRoot != null)
            Undo.DestroyObjectImmediate(oldRoot);

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
    }

    private static void SetPrivateField(Object target, string fieldName, string value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.stringValue = value;
            so.ApplyModifiedProperties();
        }
    }

    private static void SetPrivateField(Object target, string fieldName, bool value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.boolValue = value;
            so.ApplyModifiedProperties();
        }
    }
}
