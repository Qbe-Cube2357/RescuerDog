using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TutorialDirector : MonoBehaviour
{
    [Header("参照")]
    [SerializeField]
    private PlayerControl_2 player;

    [SerializeField]
    private BirdEvasionTracker evasionTracker;

    [SerializeField]
    private BirdAttackAgent birdPrefab;

    [SerializeField]
    private DeliveryArea deliveryArea;

    [SerializeField]
    private TutorialUIController uiController;

    [SerializeField]
    private RightHandleSubmitInput advanceInput;

    [SerializeField]
    private Transform runtimeObjectRoot;

    [SerializeField]
    private Transform runtimeBirdRoot;

    [Header("SE")]
    [SerializeField]
    private AudioSource seAudioSource;

    [SerializeField]
    private AudioClip transitionSE;

    [SerializeField]
    private AudioClip birdEvasionSuccessSE;

    [SerializeField, Range(0f, 1f)]
    private float seVolume = 1f;

    [Header("チュートリアル手順")]
    [SerializeField]
    private List<TutorialStepData> steps =
        new List<TutorialStepData>();

    [Header("強制終了")]
    [SerializeField]
    private KeyCode forceExitKey = KeyCode.Q;

    [SerializeField, Min(0f)]
    private float forceExitHoldSeconds = 1f;

    [SerializeField]
    private string forceExitSceneName = "MenuScene";

    private readonly HashSet<ToyFollower> tutorialCollectibles =
        new HashSet<ToyFollower>();

    private int currentStepIndex = -1;
    private int currentPageIndex;
    private bool isExiting;

    private float forceExitHoldTimer;

    private bool waitingForNeutral;
    private Vector3 movementStartPosition;
    private Vector3 movementStartForward;
    private float previousYaw;
    private float accumulatedAngle;
    private float validInputHoldTime;

    private int deliveredCount;
    private int evasionSuccessCount;

    private TutorialShowcaseRig activeShowcase;
    private BirdAttackAgent activeBird;

    private TutorialStepData CurrentStep
    {
        get
        {
            if (steps == null ||
                currentStepIndex < 0 ||
                currentStepIndex >= steps.Count)
            {
                return null;
            }

            return steps[currentStepIndex];
        }
    }

    private void Start()
    {
        if (advanceInput != null)
        {
            advanceInput.Submitted += OnAdvanceSubmitted;
        }

        if (deliveryArea != null)
        {
            deliveryArea.ToyDelivered += OnToyDelivered;
            deliveryArea.SetDeliveryEnabled(false);
        }

        player?.SetTutorialControlEnabled(false);
        uiController?.HideAll();

        MoveToNextStep();
    }

    private void Update()
    {
        if (isExiting)
        {
            return;
        }

        UpdateForceExit();

        if (isExiting || CurrentStep == null)
        {
            return;
        }

        switch (CurrentStep.stepType)
        {
            case TutorialStepType.Movement:
                UpdateMovementPractice(CurrentStep);
                break;

            case TutorialStepType.Collect:
                UpdateCollectPractice(CurrentStep);
                break;

            case TutorialStepType.BirdEvasion:
                UpdateBirdPractice(CurrentStep);
                break;
        }
    }

    private void OnDestroy()
    {
        if (advanceInput != null)
        {
            advanceInput.Submitted -= OnAdvanceSubmitted;
        }

        if (deliveryArea != null)
        {
            deliveryArea.ToyDelivered -= OnToyDelivered;
        }

        player?.ClearTutorialControlOverride();
    }

    private void EnterCurrentStep()
    {
        TutorialStepData step = CurrentStep;

        if (step == null)
        {
            BeginSceneExit(forceExitSceneName);
            return;
        }

        uiController?.HideAll();

        switch (step.stepType)
        {
            case TutorialStepType.Dialogue:
                BeginDialogue(step);
                break;

            case TutorialStepType.Movement:
                BeginMovementPractice(step);
                break;

            case TutorialStepType.Collect:
                BeginCollectPractice(step);
                break;

            case TutorialStepType.Delivery:
                BeginDeliveryPractice(step);
                break;

            case TutorialStepType.Showcase:
                BeginShowcase(step);
                break;

            case TutorialStepType.BirdEvasion:
                BeginBirdPractice(step);
                break;

            case TutorialStepType.LoadScene:
                BeginSceneExit(step.destinationSceneName);
                break;
        }
    }

    private void ExitCurrentStep()
    {
        TutorialStepData step = CurrentStep;

        if (step == null)
        {
            return;
        }

        if (step.stepType == TutorialStepType.Delivery)
        {
            deliveryArea?.SetDeliveryEnabled(false);
        }

        if (activeShowcase != null)
        {
            activeShowcase.Deactivate();
            Destroy(activeShowcase.gameObject);
            activeShowcase = null;
        }

        if (step.stepType == TutorialStepType.BirdEvasion)
        {
            evasionTracker?.StopTracking();

            if (activeBird != null)
            {
                activeBird.BeginEscape();
                activeBird = null;
            }
        }
    }

    private void MoveToNextStep()
    {
        if (isExiting)
        {
            return;
        }

        int nextStepIndex = currentStepIndex + 1;

        bool hasNextStep =
            steps != null &&
            nextStepIndex >= 0 &&
            nextStepIndex < steps.Count;

        TutorialStepData nextStep =
            hasNextStep ? steps[nextStepIndex] : null;

        // 初回表示では鳴らさない。
        // LoadSceneへ進む場合も鳴らさない。
        bool shouldPlayTransitionSE =
            currentStepIndex >= 0 &&
            nextStep != null &&
            nextStep.stepType != TutorialStepType.LoadScene;

        if (shouldPlayTransitionSE)
        {
            PlaySE(transitionSE);
        }

        ExitCurrentStep();

        currentStepIndex = nextStepIndex;

        if (!hasNextStep)
        {
            BeginSceneExit(forceExitSceneName);
            return;
        }

        EnterCurrentStep();
    }

    private void BeginDialogue(TutorialStepData step)
    {
        player?.SetTutorialControlEnabled(false);
        currentPageIndex = 0;
        ShowCurrentPage(step);
    }

    private void BeginShowcase(TutorialStepData step)
    {
        player?.SetTutorialControlEnabled(false);

        if (step.showcasePrefab != null)
        {
            Vector3 position =
                step.showcaseSpawnPoint != null
                    ? step.showcaseSpawnPoint.position
                    : Vector3.zero;

            Quaternion rotation =
                step.showcaseSpawnPoint != null
                    ? step.showcaseSpawnPoint.rotation
                    : Quaternion.identity;

            activeShowcase = Instantiate(
                step.showcasePrefab,
                position,
                rotation,
                runtimeObjectRoot
            );

            activeShowcase.Activate();
        }

        currentPageIndex = 0;
        ShowCurrentPage(step);
    }

    private void ShowCurrentPage(TutorialStepData step)
    {
        if (step.pages == null ||
            step.pages.Count == 0)
        {
            MoveToNextStep();
            return;
        }

        currentPageIndex = Mathf.Clamp(
            currentPageIndex,
            0,
            step.pages.Count - 1
        );

        uiController?.ShowDialogue(
            step.pages[currentPageIndex],
            currentPageIndex,
            step.pages.Count
        );

        advanceInput?.ResetState();
    }

    private void OnAdvanceSubmitted()
    {
        TutorialStepData step = CurrentStep;

        if (step == null)
        {
            return;
        }

        bool acceptsManualAdvance =
            step.stepType == TutorialStepType.Dialogue ||
            step.stepType == TutorialStepType.Showcase;

        if (!acceptsManualAdvance)
        {
            return;
        }

        currentPageIndex++;

        if (step.pages != null &&
            currentPageIndex < step.pages.Count)
        {
            PlaySE(transitionSE);
            ShowCurrentPage(step);
        }
        else
        {
            MoveToNextStep();
        }
    }

    private void BeginMovementPractice(
        TutorialStepData step)
    {
        player?.SetTutorialControlEnabled(true);

        waitingForNeutral = true;
        validInputHoldTime = 0f;
        accumulatedAngle = 0f;

        uiController?.ShowPractice(step);

        uiController?.UpdatePracticeProgress(
            0f,
            "ハンドルを中央に戻してください"
        );
    }

    private void UpdateMovementPractice(
        TutorialStepData step)
    {
        if (player == null)
        {
            return;
        }

        float left = player.leftInput;
        float right = player.rightInput;

        if (waitingForNeutral)
        {
            bool isNeutral =
                Mathf.Abs(left) <= step.neutralThreshold &&
                Mathf.Abs(right) <= step.neutralThreshold;

            if (!isNeutral)
            {
                return;
            }

            waitingForNeutral = false;

            movementStartPosition =
                player.transform.position;

            movementStartForward =
                player.transform.forward;

            movementStartForward.y = 0f;
            movementStartForward.Normalize();

            previousYaw =
                player.transform.eulerAngles.y;

            uiController?.UpdatePracticeProgress(
                0f,
                "スタート！"
            );

            return;
        }

        bool inputMatches =
            DoesMovementInputMatch(
                step,
                left,
                right
            );

        float currentYaw =
            player.transform.eulerAngles.y;

        float yawDelta =
            Mathf.DeltaAngle(
                previousYaw,
                currentYaw
            );

        previousYaw = currentYaw;

        if (inputMatches)
        {
            validInputHoldTime += Time.deltaTime;

            if (step.movementType ==
                    TutorialMovementType.SpinLeft ||
                step.movementType ==
                    TutorialMovementType.SpinRight)
            {
                accumulatedAngle +=
                    Mathf.Abs(yawDelta);
            }
        }
        else
        {
            validInputHoldTime = 0f;
        }

        float progress;
        float target;
        string unit;

        if (step.movementType ==
                TutorialMovementType.Forward ||
            step.movementType ==
                TutorialMovementType.Backward)
        {
            Vector3 displacement =
                player.transform.position -
                movementStartPosition;

            float signedDistance =
                Vector3.Dot(
                    displacement,
                    movementStartForward
                );

            progress =
                step.movementType ==
                    TutorialMovementType.Forward
                    ? Mathf.Max(0f, signedDistance)
                    : Mathf.Max(0f, -signedDistance);

            target =
                Mathf.Max(0.1f, step.requiredDistance);

            unit = "m";
        }
        else
        {
            progress = accumulatedAngle;
            target = Mathf.Max(1f, step.requiredAngle);
            unit = "°";
        }

        uiController?.UpdatePracticeProgress(
            progress / target,
            $"{progress:0.0} / {target:0.0} {unit}"
        );

        if (inputMatches &&
            validInputHoldTime >=
                step.minimumInputHoldTime &&
            progress >= target)
        {
            MoveToNextStep();
        }
    }

    private static bool DoesMovementInputMatch(
        TutorialStepData step,
        float left,
        float right)
    {
        float threshold = step.inputThreshold;

        switch (step.movementType)
        {
            case TutorialMovementType.Forward:
                return
                    left >= threshold &&
                    right >= threshold;

            case TutorialMovementType.Backward:
                return
                    left <= -threshold &&
                    right <= -threshold;

            case TutorialMovementType.SpinLeft:
                return
                    left <= -threshold &&
                    right >= threshold;

            case TutorialMovementType.SpinRight:
                return
                    left >= threshold &&
                    right <= -threshold;

            default:
                return false;
        }
    }

    private void BeginCollectPractice(
        TutorialStepData step)
    {
        player?.SetTutorialControlEnabled(true);
        deliveryArea?.SetDeliveryEnabled(false);

        ClearTutorialCollectibles();

        if (step.collectiblePrefabs == null ||
            step.collectiblePrefabs.Length == 0 ||
            step.collectibleSpawnPoints == null ||
            step.collectibleSpawnPoints.Length == 0)
        {
            Debug.LogError(
                "回収練習のPrefabまたはSpawnPointが不足しています。",
                this
            );

            return;
        }

        for (int i = 0;
             i < step.requiredCollectCount;
             i++)
        {
            GameObject prefab =
                step.collectiblePrefabs[
                    i % step.collectiblePrefabs.Length
                ];

            Transform spawnPoint =
                step.collectibleSpawnPoints[
                    i % step.collectibleSpawnPoints.Length
                ];

            if (prefab == null || spawnPoint == null)
            {
                continue;
            }

            GameObject instance = Instantiate(
                prefab,
                spawnPoint.position,
                spawnPoint.rotation,
                runtimeObjectRoot
            );

            ToyFollower toy =
                instance.GetComponent<ToyFollower>();

            if (toy == null)
            {
                Debug.LogError(
                    $"{prefab.name}にToyFollowerがありません。",
                    prefab
                );

                Destroy(instance);
                continue;
            }

            tutorialCollectibles.Add(toy);
        }

        uiController?.ShowPractice(step);

        uiController?.UpdatePracticeProgress(
            0f,
            $"回収 0 / {step.requiredCollectCount}"
        );
    }

    private void UpdateCollectPractice(
        TutorialStepData step)
    {
        int carriedCount = 0;

        foreach (ToyFollower toy in tutorialCollectibles)
        {
            if (toy != null && toy.IsCarried)
            {
                carriedCount++;
            }
        }

        int target =
            Mathf.Max(1, step.requiredCollectCount);

        uiController?.UpdatePracticeProgress(
            (float)carriedCount / target,
            $"回収 {carriedCount} / {target}"
        );

        if (carriedCount >= target)
        {
            MoveToNextStep();
        }
    }

    private void BeginDeliveryPractice(
        TutorialStepData step)
    {
        player?.SetTutorialControlEnabled(true);

        deliveredCount = 0;
        deliveryArea?.SetDeliveryEnabled(true);

        uiController?.ShowPractice(step);

        uiController?.UpdatePracticeProgress(
            0f,
            $"納品 0 / {step.requiredDeliveryCount}"
        );
    }

    private void OnToyDelivered(ToyFollower toy)
    {
        TutorialStepData step = CurrentStep;

        if (step == null ||
            step.stepType != TutorialStepType.Delivery ||
            toy == null ||
            !tutorialCollectibles.Contains(toy))
        {
            return;
        }

        deliveredCount++;

        int target =
            Mathf.Max(1, step.requiredDeliveryCount);

        uiController?.UpdatePracticeProgress(
            (float)deliveredCount / target,
            $"納品 {deliveredCount} / {target}"
        );

        if (deliveredCount >= target)
        {
            MoveToNextStep();
        }
    }

    private void BeginBirdPractice(
        TutorialStepData step)
    {
        player?.SetTutorialControlEnabled(true);

        if (player == null ||
            birdPrefab == null ||
            evasionTracker == null)
        {
            Debug.LogError(
                "鳥回避練習の参照が不足しています。",
                this
            );

            return;
        }

        activeBird = Instantiate(
            birdPrefab,
            player.transform.position +
                Vector3.up * step.birdLockOnHeight,
            Quaternion.identity,
            runtimeBirdRoot
        );

        activeBird.Initialize(
            player,
            step.birdLockOnHeight
        );

        activeBird.BeginLockOn();

        evasionSuccessCount = 0;
        evasionTracker.BeginTracking();

        uiController?.ShowPractice(step);
    }

    private void UpdateBirdPractice(
        TutorialStepData step)
    {
        if (evasionTracker == null)
        {
            return;
        }

        int target =
            Mathf.Max(1, step.requiredEvasionSuccesses);

        bool escaped =
            evasionTracker.Tick(Time.deltaTime);

        float totalProgress =
            (
                evasionSuccessCount +
                evasionTracker.Progress
            ) / target;

        uiController?.UpdatePracticeProgress(
            totalProgress,
            $"回避 {evasionSuccessCount} / {target}\n" +
            $"方向転換 " +
            $"{evasionTracker.DirectionChangeCount} / " +
            $"{evasionTracker.RequiredDirectionChanges}"
        );

        if (!escaped)
        {
            return;
        }

        evasionSuccessCount++;
        PlaySE(birdEvasionSuccessSE);

        if (evasionSuccessCount >= target)
        {
            uiController?.UpdatePracticeProgress(
                1f,
                $"回避 {target} / {target}"
            );

            MoveToNextStep();
            return;
        }

        // 次の回避1回を最初から計測
        evasionTracker.BeginTracking();
    }

    private void UpdateForceExit()
    {
        if (forceExitHoldSeconds <= 0f)
        {
            if (Input.GetKeyDown(forceExitKey))
            {
                BeginSceneExit(forceExitSceneName);
            }

            return;
        }

        if (Input.GetKey(forceExitKey))
        {
            forceExitHoldTimer +=
                Time.unscaledDeltaTime;

            if (forceExitHoldTimer >=
                forceExitHoldSeconds)
            {
                BeginSceneExit(forceExitSceneName);
            }
        }
        else
        {
            forceExitHoldTimer = 0f;
        }
    }

    private void BeginSceneExit(string sceneName)
    {
        if (isExiting)
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(
                sceneName))
        {
            Debug.LogError(
                $"シーン '{sceneName}' がBuild Profileにありません。",
                this
            );

            return;
        }

        isExiting = true;

        player?.SetTutorialControlEnabled(false);
        deliveryArea?.SetDeliveryEnabled(false);
        uiController?.HideAll();

        ExitCurrentStep();

        SceneManager.LoadScene(sceneName);
    }

    private void ClearTutorialCollectibles()
    {
        foreach (ToyFollower toy in tutorialCollectibles)
        {
            if (toy != null)
            {
                Destroy(toy.gameObject);
            }
        }

        tutorialCollectibles.Clear();
    }

    private void PlaySE(AudioClip clip)
    {
        if (seAudioSource == null || clip == null)
        {
            return;
        }

        seAudioSource.PlayOneShot(clip, seVolume);
    }
}