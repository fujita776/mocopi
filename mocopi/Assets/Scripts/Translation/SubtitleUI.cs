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
            //  起動時に背景Imageを描画OFF（黒矩形が一瞬出るのを防ぐ）
            //  GameObject自体は触らない（このコンポーネントが止まらないように）
            backgroundPanel.enabled = false;
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

        //  完全に非表示になったら背景Imageの描画を無効化（黒乗算矩形を消す）
        //  Image.enabled だけトグル（GameObjectのSetActiveは触らない: SubtitleUI自身が止まるため）
        if (backgroundPanel != null)
        {
            bool shouldBeVisible = _currentAlpha > 0.001f || _targetAlpha > 0.001f;
            if (backgroundPanel.enabled != shouldBeVisible)
            {
                backgroundPanel.enabled = shouldBeVisible;
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

    /// <summary>
    /// 短時間の通知表示（F1/F2切替時やPTT ON/OFFなど）
    /// </summary>
    public void ShowNotification(string text, float duration)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        subtitleText.text = text;
        _targetAlpha = 1f;
        _displayTimer = duration;
        _autoHide = true;
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
