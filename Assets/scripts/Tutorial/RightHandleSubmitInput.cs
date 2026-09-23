using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RightHandleSubmitInput : MonoBehaviour
{
    [Header("参照")]
    [SerializeField]
    private HandleInputReader inputReader;

    [Header("ハンドル判定")]
    [SerializeField, Range(0.01f, 1f)]
    private float triggerThreshold = 0.35f;

    [SerializeField, Range(0f, 0.99f)]
    private float releaseThreshold = 0.15f;

    [SerializeField]
    private bool requireNeutralOnEnable = true;

    [Header("キーボード")]
    [SerializeField]
    private KeyCode keyboardSubmitKey = KeyCode.N;

    public event Action Submitted;

    private bool rightReady;

    private void OnEnable()
    {
        ResetState();
    }

    private void Update()
    {
        if (keyboardSubmitKey != KeyCode.None &&
            Input.GetKeyDown(keyboardSubmitKey))
        {
            Submitted?.Invoke();
            return;
        }

        if (inputReader == null)
        {
            return;
        }

        float value = inputReader.RightInput;

        if (!rightReady)
        {
            if (Mathf.Abs(value) <= releaseThreshold)
            {
                rightReady = true;
            }

            return;
        }

        if (value >= triggerThreshold)
        {
            rightReady = false;
            Submitted?.Invoke();
        }
        else if (value <= -triggerThreshold)
        {
            // 後ろ入力では進めない。
            // ニュートラルへ戻るまで再入力も受け付けない。
            rightReady = false;
        }
    }

    public void ResetState()
    {
        rightReady = !requireNeutralOnEnable;
    }

    private void OnValidate()
    {
        triggerThreshold =
            Mathf.Max(0.01f, triggerThreshold);

        releaseThreshold = Mathf.Clamp(
            releaseThreshold,
            0f,
            triggerThreshold - 0.01f
        );
    }
}