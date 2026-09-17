using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BirdWarningUI : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField]
    private GameObject warningRoot;

    [SerializeField]
    private TMP_Text warningText;

    [SerializeField]
    private Image remainingTimeFill;

    [SerializeField]
    private Image evasionProgressFill;

    [Header("表示設定")]
    [SerializeField, Min(0f)]
    private float escapedMessageDuration = 0.8f;

    private Coroutine hideCoroutine;

    [Header("警告アニメーション")]
    [SerializeField]
    private RectTransform animatedRoot;

    [SerializeField]
    private Vector2 pulseScaleRange =
        new Vector2(0.92f, 1.08f);

    [SerializeField, Min(0.01f)]
    private float pulseSpeed = 1.8f;

    private Vector3 animatedRootBaseScale;
    private float pulseElapsedTime;
    private bool isPulsing;

    private void Awake()
    {
        if (animatedRoot != null)
        {
            animatedRootBaseScale =
                animatedRoot.localScale;
        }

        Hide();
    }

    private void Update()
    {
        if (!isPulsing || animatedRoot == null)
        {
            return;
        }

        pulseElapsedTime += Time.unscaledDeltaTime;

        float wave =
            Mathf.Sin(
                pulseElapsedTime *
                pulseSpeed *
                Mathf.PI *
                2f
            );

        // -1～1を0～1に変換
        float normalizedWave =
            (wave + 1f) * 0.5f;

        float minimumScale =
            Mathf.Min(
                pulseScaleRange.x,
                pulseScaleRange.y
            );

        float maximumScale =
            Mathf.Max(
                pulseScaleRange.x,
                pulseScaleRange.y
            );

        float scale =
            Mathf.Lerp(
                minimumScale,
                maximumScale,
                normalizedWave
            );

        animatedRoot.localScale =
            animatedRootBaseScale * scale;
    }

    public void ShowLockOn()
    {
        StopHideCoroutine();

        pulseElapsedTime = 0f;
        isPulsing = true;

        if (warningRoot != null)
        {
            warningRoot.SetActive(true);
        }

        if (warningText != null)
        {
            warningText.text =
                "鳥に狙われている！\n左右に泳いで泳げ！";
        }

        UpdateView(1f, 0f);
    }

    public void UpdateView(
        float remainingTimeNormalized,
        float evasionProgressNormalized
    )
    {
        if (remainingTimeFill != null)
        {
            remainingTimeFill.fillAmount =
                Mathf.Clamp01(remainingTimeNormalized);
        }

        if (evasionProgressFill != null)
        {
            evasionProgressFill.fillAmount =
                Mathf.Clamp01(evasionProgressNormalized);
        }
    }

    public void ShowEscaped()
    {
        StopHideCoroutine();
        StopPulse();

        if (warningRoot != null)
        {
            warningRoot.SetActive(true);
        }

        if (warningText != null)
        {
            warningText.text = "回避成功！";
        }

        if (evasionProgressFill != null)
        {
            evasionProgressFill.fillAmount = 1f;
        }

        hideCoroutine =
            StartCoroutine(HideAfterDelay());
    }

    public void Hide()
    {
        StopHideCoroutine();
        StopPulse();

        if (warningRoot != null)
        {
            warningRoot.SetActive(false);
        }
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(
            escapedMessageDuration
        );

        hideCoroutine = null;

        if (warningRoot != null)
        {
            warningRoot.SetActive(false);
        }
    }

    private void StopHideCoroutine()
    {
        if (hideCoroutine == null)
        {
            return;
        }

        StopCoroutine(hideCoroutine);
        hideCoroutine = null;
    }
    private void StopPulse()
    {
        isPulsing = false;
        pulseElapsedTime = 0f;

        if (animatedRoot != null)
        {
            animatedRoot.localScale =
                animatedRootBaseScale;
        }
    }
}