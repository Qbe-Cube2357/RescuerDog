using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public sealed class PhaseAnnouncementUI : MonoBehaviour
{
    [Header("参照")]
    [SerializeField]
    private TMP_Text phaseText;

    [SerializeField]
    private RectTransform animationTarget;

    [Header("スケール")]
    [SerializeField, Min(0f)]
    private float startScaleMultiplier = 0.2f;

    [SerializeField, Min(0f)]
    private float largeScaleMultiplier = 1.25f;

    [SerializeField, Min(0f)]
    private float exitScaleMultiplier = 0.1f;

    [Header("時間")]
    [SerializeField, Min(0f)]
    private float growDuration = 0.22f;

    [SerializeField, Min(0f)]
    private float settleDuration = 0.16f;

    [SerializeField, Min(0f)]
    private float displayDuration = 1.5f;

    [SerializeField, Min(0f)]
    private float exitDuration = 0.35f;

    private CanvasGroup canvasGroup;
    private Sequence animationSequence;
    private Vector3 normalScale;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (animationTarget == null)
        {
            animationTarget = GetComponent<RectTransform>();
        }

        normalScale = animationTarget.localScale;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void Play(string message)
    {
        if (phaseText == null || animationTarget == null)
        {
            Debug.LogError(
                "PhaseAnnouncementUIの参照が不足しています。",
                this
            );
            return;
        }

        // 前回のアニメーションが残っていたら中止
        animationSequence?.Kill();
        animationSequence = null;

        phaseText.text = message;

        canvasGroup.alpha = 1f;
        animationTarget.localScale =
            normalScale * startScaleMultiplier;

        animationSequence = DOTween.Sequence();

        // Time.timeScaleが0でもUIアニメーションを完了させる
        animationSequence.SetUpdate(true);

        // 小さい状態から大きくする
        animationSequence.Append(
            animationTarget
                .DOScale(
                    normalScale * largeScaleMultiplier,
                    growDuration
                )
                .SetEase(Ease.OutCubic)
        );

        // 大きい状態から通常サイズへ戻す
        animationSequence.Append(
            animationTarget
                .DOScale(normalScale, settleDuration)
                .SetEase(Ease.OutQuad)
        );

        // 通常サイズで表示
        animationSequence.AppendInterval(displayDuration);

        // 縮小と透明化を同時に行う
        animationSequence.Append(
            animationTarget
                .DOScale(
                    normalScale * exitScaleMultiplier,
                    exitDuration
                )
                .SetEase(Ease.InCubic)
        );

        animationSequence.Join(
            canvasGroup
                .DOFade(0f, exitDuration)
                .SetEase(Ease.InQuad)
        );

        animationSequence.OnComplete(() =>
        {
            canvasGroup.alpha = 0f;
            animationTarget.localScale = normalScale;
            animationSequence = null;
        });
    }

    public void HideImmediate()
    {
        animationSequence?.Kill();
        animationSequence = null;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (animationTarget != null)
        {
            animationTarget.localScale = normalScale;
        }
    }

    private void OnDisable()
    {
        animationSequence?.Kill();
        animationSequence = null;
    }

    private void OnDestroy()
    {
        animationSequence?.Kill();
    }
}