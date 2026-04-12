using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Whisper;

/// <summary>
/// ライブ翻訳システムを現在のシーンにセットアップするエディタツール
/// 設定はLiveTranslationSettings（ScriptableObject）から読み込む
/// </summary>
public class LiveTranslationSetup
{
    private const string SettingsPath = "Assets/Scripts/Translation/LiveTranslationSettings.asset";

    [MenuItem("GameObject/Live Translation/Setup In Scene")]
    public static void SetupInScene()
    {
        if (Object.FindObjectOfType<LiveTranslationManager>() != null)
        {
            EditorUtility.DisplayDialog("Live Translation", "このシーンには既にLiveTranslationManagerが存在します。", "OK");
            return;
        }

        //  ScriptableObject読み込み
        var settings = AssetDatabase.LoadAssetAtPath<LiveTranslationSettings>(SettingsPath);
        if (settings == null)
        {
            //  存在しなければ作成
            settings = ScriptableObject.CreateInstance<LiveTranslationSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LiveTranslationSetup] 設定アセットを作成: {SettingsPath}");
        }

        //  メインオブジェクト作成
        var managerObj = new GameObject("TranslationManager");
        Undo.RegisterCreatedObjectUndo(managerObj, "Setup Live Translation");

        var whisperManager = managerObj.AddComponent<WhisperManager>();
        var audioBridge = managerObj.AddComponent<AudioBridge>();
        var manager = managerObj.AddComponent<LiveTranslationManager>();

        //  WhisperManager のprivateフィールドをSettingsから設定
        SetField(whisperManager, "modelPath", settings.modelPath);
        SetField(whisperManager, "isModelPathInStreamingAssets", settings.isModelPathInStreamingAssets);
        SetField(whisperManager, "useGpu", settings.useGpu);
        SetField(whisperManager, "flashAttention", settings.flashAttention);

        //  publicフィールドはSettingsから直接
        whisperManager.noContext = settings.noContext;
        whisperManager.useVad = settings.useVad;
        whisperManager.dropOldBuffer = settings.dropOldBuffer;
        whisperManager.stepSec = settings.stepSec;
        whisperManager.keepSec = settings.keepSec;
        whisperManager.lengthSec = settings.lengthSec;
        whisperManager.updatePrompt = settings.updatePrompt;

        //  AudioBridge設定
        SetField(audioBridge, "chunkIntervalSeconds", settings.chunkIntervalSeconds);

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
        tmpText.fontSize = settings.fontSize;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;
        tmpText.enableWordWrapping = true;

        var jaFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/font/nikumaru SDF.asset");
        if (jaFont != null)
            tmpText.font = jaFont;
        else
            Debug.LogWarning("[LiveTranslationSetup] nikumaru SDF.asset が見つかりません");

        var subtitleUI = panelObj.AddComponent<SubtitleUI>();
        SetField(subtitleUI, "fadeDuration", settings.fadeDuration);
        SetField(subtitleUI, "displayDuration", settings.displayDuration);
        Undo.RegisterCreatedObjectUndo(panelObj, "Create Subtitle Panel");

        //  参照を設定
        SetField(subtitleUI, "subtitleText", tmpText);
        SetField(subtitleUI, "backgroundPanel", panelImage);

        SetField(manager, "settings", settings);
        SetField(manager, "whisperManager", whisperManager);
        SetField(manager, "audioBridge", audioBridge);
        SetField(manager, "subtitleUI", subtitleUI);

        Selection.activeGameObject = managerObj;
        EditorUtility.SetDirty(managerObj);

        Debug.Log("[LiveTranslationSetup] セットアップ完了");
        EditorUtility.DisplayDialog("Live Translation",
            "セットアップ完了\n\n" +
            $"設定: {SettingsPath}\n" +
            "全シーンで同じ設定アセットを参照します。\n" +
            "設定変更はアセットを1つ編集するだけでOK。",
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

    //  ヘルパー: SerializedObject経由でprivateフィールドを設定
    private static void SetField(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null) { prop.objectReferenceValue = value; so.ApplyModifiedProperties(); }
    }

    private static void SetField(Object target, string fieldName, string value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null) { prop.stringValue = value; so.ApplyModifiedProperties(); }
    }

    private static void SetField(Object target, string fieldName, bool value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null) { prop.boolValue = value; so.ApplyModifiedProperties(); }
    }

    private static void SetField(Object target, string fieldName, float value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null) { prop.floatValue = value; so.ApplyModifiedProperties(); }
    }

    private static void SetField(Object target, string fieldName, int value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null) { prop.intValue = value; so.ApplyModifiedProperties(); }
    }
}
