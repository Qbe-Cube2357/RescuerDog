using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class ButtonScaleFeedback : MonoBehaviour,
    ISelectHandler,
    IDeselectHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    ISubmitHandler
{
    [Header("Scale Target")]
    [Tooltip("未設定の場合は、このButton自身を拡縮します")]
    [SerializeField]
    private Transform scaleTarget;

    [Header("Scale Settings")]
    [Tooltip("選択中の大きさ")]
    [SerializeField, Min(1f)]
    private float selectedScale = 1.08f;

    [Tooltip("押下中の大きさ。selectedScaleより小さくします")]
    [SerializeField, Min(0f)]
    private float pressedScale = 1.02f;

    [Header("Animation")]
    [SerializeField, Min(0.01f)]
    private float transitionDuration = 0.1f;

    [SerializeField, Min(0.01f)]
    private float submitPressDuration = 0.07f;

    [SerializeField]
    private Ease ease = Ease.OutQuad;

    private Vector3 defaultScale;
    private Tween scaleTween;

    private bool isSelected;
    private bool isPointerDown;

    private void Awake()
    {
        if (scaleTarget == null)
        {
            scaleTarget = transform;
        }

        defaultScale = scaleTarget.localScale;
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;

        if (!isPointerDown)
        {
            AnimateScale(selectedScale, transitionDuration);
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        isPointerDown = false;

        AnimateScale(1f, transitionDuration);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        isPointerDown = true;
        AnimateScale(pressedScale, transitionDuration);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        isPointerDown = false;

        AnimateScale(
            isSelected ? selectedScale : 1f,
            transitionDuration
        );
    }

    public void OnSubmit(BaseEventData eventData)
    {
        // ハンドル・キーボードのSubmitにはPointerDown/Upがないため、
        // 縮小→選択サイズへ戻るアニメーションをここで再生する。
        scaleTween?.Kill();

        scaleTween = DOTween.Sequence()
            .SetUpdate(true)
            .Append(
                scaleTarget
                    .DOScale(
                        defaultScale * pressedScale,
                        submitPressDuration
                    )
                    .SetEase(Ease.OutQuad)
            )
            .Append(
                scaleTarget
                    .DOScale(
                        defaultScale *
                        (isSelected ? selectedScale : 1f),
                        transitionDuration
                    )
                    .SetEase(ease)
            );
    }

    private void AnimateScale(float multiplier, float duration)
    {
        scaleTween?.Kill();

        scaleTween = scaleTarget
            .DOScale(defaultScale * multiplier, duration)
            .SetEase(ease)
            .SetUpdate(true);
    }

    private void OnDisable()
    {
        scaleTween?.Kill();
        scaleTween = null;

        if (scaleTarget != null)
        {
            scaleTarget.localScale = defaultScale;
        }

        isSelected = false;
        isPointerDown = false;
    }
}