using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialUIController : MonoBehaviour
{
    [Header("共通表示ルート")]
    [SerializeField]
    private GameObject guideRoot;

    [SerializeField]
    private Image bubbleImage;

    [SerializeField]
    private Image leftCharacterImage;

    [SerializeField]
    private Image rightCharacterImage;

    [Header("練習時のデフォルト画像")]
    [SerializeField]
    private Sprite defaultPracticeBubbleSprite;

    [SerializeField]
    private Sprite defaultPracticeCharacterSprite;

    [SerializeField]
    private bool showDefaultPracticeCharacter = true;

    [SerializeField]
    private TutorialCharacterSide
        defaultPracticeCharacterSide =
            TutorialCharacterSide.Left;

    [Header("会話UI")]
    [SerializeField]
    private GameObject dialogueRoot;

    [SerializeField]
    private TMP_Text dialogueText;

    [SerializeField]
    private TMP_Text pageText;

    [SerializeField]
    private TMP_Text advanceGuideText;

    [Header("練習UI")]
    [SerializeField]
    private GameObject practiceRoot;

    [SerializeField]
    private TMP_Text practiceTitleText;

    [SerializeField]
    private TMP_Text practiceInstructionText;

    [SerializeField]
    private TMP_Text practiceProgressText;

    [SerializeField]
    private Image practiceProgressFill;

    private void Awake()
    {
        HideAll();
    }

    public void ShowDialogue(
        TutorialDialoguePage page,
        int pageIndex,
        int pageCount)
    {
        if (page == null)
        {
            return;
        }

        guideRoot?.SetActive(true);
        dialogueRoot?.SetActive(true);
        practiceRoot?.SetActive(false);

        ApplyGuideVisual(
            page.bubbleSprite,
            page.characterSprite,
            page.showCharacter,
            page.characterSide
        );

        if (dialogueText != null)
        {
            dialogueText.text = page.text;
        }

        if (pageText != null)
        {
            pageText.gameObject.SetActive(
                pageCount > 1
            );

            pageText.text =
                $"{pageIndex + 1} / {pageCount}";
        }

        if (advanceGuideText != null)
        {
            advanceGuideText.text =
                "右ハンドルを前へ回す / Nキー";
        }
    }

    public void ShowPractice(TutorialStepData step)
    {
        if (step == null)
        {
            return;
        }

        guideRoot?.SetActive(true);
        dialogueRoot?.SetActive(false);
        practiceRoot?.SetActive(true);

        if (step.overridePracticeVisual)
        {
            ApplyGuideVisual(
                step.practiceBubbleSprite,
                step.practiceCharacterSprite,
                step.showPracticeCharacter,
                step.practiceCharacterSide
            );
        }
        else
        {
            ApplyGuideVisual(
                defaultPracticeBubbleSprite,
                defaultPracticeCharacterSprite,
                showDefaultPracticeCharacter,
                defaultPracticeCharacterSide
            );
        }

        if (practiceTitleText != null)
        {
            practiceTitleText.text =
                step.practiceTitle;
        }

        if (practiceInstructionText != null)
        {
            practiceInstructionText.text =
                step.practiceInstruction;
        }

        UpdatePracticeProgress(
            0f,
            string.Empty
        );
    }

    public void UpdatePracticeProgress(
        float normalizedProgress,
        string progressText)
    {
        if (practiceProgressFill != null)
        {
            practiceProgressFill.fillAmount =
                Mathf.Clamp01(normalizedProgress);
        }

        if (practiceProgressText != null)
        {
            practiceProgressText.text =
                progressText;
        }
    }

    public void HideAll()
    {
        guideRoot?.SetActive(false);
        dialogueRoot?.SetActive(false);
        practiceRoot?.SetActive(false);
    }

    private void ApplyGuideVisual(
        Sprite bubbleSprite,
        Sprite characterSprite,
        bool showCharacter,
        TutorialCharacterSide characterSide)
    {
        if (bubbleImage != null)
        {
            bool showBubble =
                bubbleSprite != null;

            bubbleImage.gameObject.SetActive(
                showBubble
            );

            if (showBubble)
            {
                bubbleImage.sprite =
                    bubbleSprite;
            }
        }

        SetCharacterImage(
            leftCharacterImage,
            characterSprite,
            showCharacter &&
            characterSide ==
                TutorialCharacterSide.Left
        );

        SetCharacterImage(
            rightCharacterImage,
            characterSprite,
            showCharacter &&
            characterSide ==
                TutorialCharacterSide.Right
        );
    }

    private static void SetCharacterImage(
        Image image,
        Sprite sprite,
        bool shouldShow)
    {
        if (image == null)
        {
            return;
        }

        bool visible =
            shouldShow &&
            sprite != null;

        image.gameObject.SetActive(visible);

        if (visible)
        {
            image.sprite = sprite;
        }
    }
}