using DG.Tweening;
using TMPro;
using UnityEngine;

public sealed class CookiePlusScoreUI : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField]
    private TextMeshProUGUI plusScoreText;

    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private RectTransform animationTarget;

    [Header("表示形式")]
    [SerializeField]
    private string numberFormat = "+ {0:000}";

    [Header("出現アニメーション")]
    [SerializeField, Min(0.01f)]
    private float appearDuration = 0.2f;

    [SerializeField, Min(0f)]
    private float appearMoveDistance = 25f;

    [SerializeField, Min(1f)]
    private float appearScale = 1.15f;

    [Header("消滅アニメーション")]
    [SerializeField, Min(0.01f)]
    private float fadeDuration = 0.4f;

    [SerializeField, Min(0f)]
    private float fadeMoveDistance = 20f;

    private Vector2 defaultPosition;
    private Vector3 defaultScale;
    private bool isVisible;

    private void Awake()
    {
        if (animationTarget == null)
        {
            animationTarget = transform as RectTransform;
        }

        if (animationTarget != null)
        {
            defaultPosition = animationTarget.anchoredPosition;
            defaultScale = animationTarget.localScale;
        }

        HideImmediate();
    }

    public void ShowScore(int score)
    {
        if (plusScoreText != null)
        {
            plusScoreText.text = string.Format(numberFormat, score);
        }

        if (canvasGroup == null || animationTarget == null)
        {
            return;
        }

        canvasGroup.DOKill();
        animationTarget.DOKill();

        if (!isVisible)
        {
            isVisible = true;

            canvasGroup.alpha = 0f;

            animationTarget.anchoredPosition =
                defaultPosition - Vector2.up * appearMoveDistance;

            animationTarget.localScale =
                defaultScale * 0.85f;

            canvasGroup
                .DOFade(1f, appearDuration);

            animationTarget
                .DOAnchorPos(defaultPosition, appearDuration)
                .SetEase(Ease.OutCubic);

            animationTarget
                .DOScale(defaultScale, appearDuration)
                .SetEase(Ease.OutBack);
        }
        else
        {
            // すでに表示中なら、Cookie到着ごとに軽く弾ませる
            animationTarget.anchoredPosition = defaultPosition;
            animationTarget.localScale = defaultScale;

            animationTarget
                .DOScale(defaultScale * appearScale, 0.08f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.OutQuad);
        }
    }

    public void Hide()
    {
        if (!isVisible)
        {
            return;
        }

        isVisible = false;

        if (canvasGroup == null || animationTarget == null)
        {
            return;
        }

        canvasGroup.DOKill();
        animationTarget.DOKill();

        canvasGroup
            .DOFade(0f, fadeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                animationTarget.anchoredPosition = defaultPosition;
                animationTarget.localScale = defaultScale;
            });

        animationTarget
            .DOAnchorPos(
                defaultPosition + Vector2.up * fadeMoveDistance,
                fadeDuration
            )
            .SetEase(Ease.OutQuad);
    }

    public void HideImmediate()
    {
        isVisible = false;

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (animationTarget != null)
        {
            animationTarget.DOKill();
            animationTarget.anchoredPosition = defaultPosition;
            animationTarget.localScale = defaultScale;
        }
    }

    private void OnDestroy()
    {
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
        }

        if (animationTarget != null)
        {
            animationTarget.DOKill();
        }
    }
}