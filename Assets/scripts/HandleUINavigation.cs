using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ハンドル入力をUnity UIの選択・押下へ変換する。
/// </summary>
[DisallowMultipleComponent]
public sealed class HandleUINavigation : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private HandleInputReader inputReader;

    [Tooltip("ハンドルで選択する順番にボタンを登録する")]
    [SerializeField]
    private List<Button> buttons = new List<Button>();

    [Header("Initial Selection")]
    [SerializeField, Min(0)]
    private int initialSelectedIndex;

    [Header("Gesture Settings")]
    [Tooltip("この値を越えたときに1回の操作として扱う")]
    [SerializeField, Range(0.01f, 1f)]
    private float triggerThreshold = 0.35f;

    [Tooltip("この値以下に戻ると、次の操作を受け付ける")]
    [SerializeField, Range(0f, 0.99f)]
    private float releaseThreshold = 0.15f;

    [Tooltip("シーン開始後、一度ニュートラルになるまで入力を無効にする")]
    [SerializeField]
    private bool requireNeutralOnEnable = true;

    private int selectedIndex = -1;
    private bool leftReady;
    private bool rightReady;
    private bool hasStarted;

    private void OnEnable()
    {
        ResetGestureState();

        // 初回はStartで選択する。
        // コンポーネントが再度有効化された場合はこちらで選択する。
        if (hasStarted)
        {
            SelectInitialButton();
        }
    }

    private void Start()
    {
        hasStarted = true;
        SelectInitialButton();
    }

    private void Update()
    {
        if (inputReader == null)
        {
            return;
        }

        ProcessLeftHandle(inputReader.LeftInput);
        ProcessRightHandle(inputReader.RightInput);
    }

    private void ProcessLeftHandle(float value)
    {
        if (!leftReady)
        {
            if (Mathf.Abs(value) <= releaseThreshold)
            {
                leftReady = true;
            }

            return;
        }

        if (value >= triggerThreshold)
        {
            MoveSelection(1);
            leftReady = false;
        }
        else if (value <= -triggerThreshold)
        {
            MoveSelection(-1);
            leftReady = false;
        }
    }

    private void ProcessRightHandle(float value)
    {
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
            SubmitCurrentButton();
            rightReady = false;
        }
        else if (value <= -triggerThreshold)
        {
            // 後ろ回転では何もしないが、
            // ニュートラルへ戻るまで次の入力を受け付けない。
            rightReady = false;
        }
    }

    private void MoveSelection(int direction)
    {
        if (buttons.Count == 0)
        {
            return;
        }

        // マウスクリックなどで選択状態が変わっていた場合に同期する。
        if (!TrySynchronizeSelectedIndex())
        {
            SelectInitialButton();
            return;
        }

        for (int step = 1; step <= buttons.Count; step++)
        {
            int candidateIndex = WrapIndex(
                selectedIndex + direction * step
            );

            if (IsUsable(buttons[candidateIndex]))
            {
                SelectButton(candidateIndex);
                return;
            }
        }
    }

    private void SubmitCurrentButton()
    {
        if (!TrySynchronizeSelectedIndex())
        {
            // 選択が解除されている場合は初期ボタンを選択するだけにする。
            // 意図しない即時押下を防止するため、この回は押下しない。
            SelectInitialButton();
            return;
        }

        Button currentButton = buttons[selectedIndex];

        if (!IsUsable(currentButton))
        {
            return;
        }

        BaseEventData eventData = new BaseEventData(EventSystem.current);

        ExecuteEvents.Execute(
            currentButton.gameObject,
            eventData,
            ExecuteEvents.submitHandler
        );
    }

    private void SelectInitialButton()
    {
        if (EventSystem.current == null || buttons.Count == 0)
        {
            selectedIndex = -1;
            return;
        }

        int startIndex = Mathf.Clamp(
            initialSelectedIndex,
            0,
            buttons.Count - 1
        );

        for (int offset = 0; offset < buttons.Count; offset++)
        {
            int candidateIndex = WrapIndex(startIndex + offset);

            if (IsUsable(buttons[candidateIndex]))
            {
                SelectButton(candidateIndex);
                return;
            }
        }

        selectedIndex = -1;
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void SelectButton(int index)
    {
        if (EventSystem.current == null)
        {
            return;
        }

        selectedIndex = index;

        EventSystem.current.SetSelectedGameObject(
            buttons[selectedIndex].gameObject
        );
    }

    private bool TrySynchronizeSelectedIndex()
    {
        if (EventSystem.current == null)
        {
            selectedIndex = -1;
            return false;
        }

        GameObject selectedObject =
            EventSystem.current.currentSelectedGameObject;

        for (int i = 0; i < buttons.Count; i++)
        {
            Button button = buttons[i];

            if (button != null &&
                button.gameObject == selectedObject &&
                IsUsable(button))
            {
                selectedIndex = i;
                return true;
            }
        }

        selectedIndex = -1;
        return false;
    }

    private bool IsUsable(Button button)
    {
        return button != null &&
               button.isActiveAndEnabled &&
               button.gameObject.activeInHierarchy &&
               button.interactable;
    }

    private int WrapIndex(int index)
    {
        int count = buttons.Count;
        return (index % count + count) % count;
    }

    private void ResetGestureState()
    {
        leftReady = !requireNeutralOnEnable;
        rightReady = !requireNeutralOnEnable;
    }

    private void OnValidate()
    {
        triggerThreshold = Mathf.Max(0.01f, triggerThreshold);

        releaseThreshold = Mathf.Clamp(
            releaseThreshold,
            0f,
            triggerThreshold - 0.01f
        );
    }
}