using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 翻訳字幕を表示するUI
/// Update駆動（コルーチン不使用 — 親Canvasが非アクティブでも安全）
/// </summary>
public class SubtitleUI : MonoBehaviour
{
    [Header("UI要素")]
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private Image backgroundPanel;

    [Header("表示設定")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float displayDuration = 5f;

    [Header("処理中表示")]
    [SerializeField] private string processingIndicator = "...";

    private float _backgroundBaseAlpha;
    private float _currentAlpha;
    private float _targetAlpha;
    private float _displayTimer;
    private bool _autoHide;

    void Start()
    {
        if (subtitleText == null)
        {
            Debug.LogError("[SubtitleUI] subtitleText が設定されていません");
            enabled = false;
            return;
        }

        if (backgroundPanel != null)
        {
            _backgroundBaseAlpha = backgroundPanel.color.a;
        }

        _currentAlpha = 0f;
        _targetAlpha = 0f;
        ApplyAlpha();
    }

    void Update()
    {
        //  フェード処理
        if (!Mathf.Approximately(_currentAlpha, _targetAlpha))
        {
            float speed = 1f / Mathf.Max(fadeDuration, 0.01f);
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, speed * Time.deltaTime);
            ApplyAlpha();
        }

        //  自動非表示タイマー
        if (_autoHide && _targetAlpha > 0f)
        {
            _displayTimer -= Time.deltaTime;
            if (_displayTimer <= 0f)
            {
                _targetAlpha = 0f;
                _autoHide = false;
            }
        }
    }

    public void ShowSubtitle(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        subtitleText.text = text;
        _targetAlpha = 1f;
        _displayTimer = displayDuration;
        _autoHide = true;
    }

    public void ShowProcessing()
    {
        subtitleText.text = processingIndicator;
        _targetAlpha = 1f;
        _autoHide = false;
    }

    public void HideSubtitle()
    {
        _targetAlpha = 0f;
        _autoHide = false;
    }

    private void ApplyAlpha()
    {
        if (subtitleText != null)
        {
            subtitleText.alpha = _currentAlpha;
        }

        if (backgroundPanel != null)
        {
            var color = backgroundPanel.color;
            color.a = _backgroundBaseAlpha * _currentAlpha;
            backgroundPanel.color = color;
        }
    }
}
