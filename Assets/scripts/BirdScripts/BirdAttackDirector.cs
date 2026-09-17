using System.Collections;
using UnityEngine;

public sealed class BirdAttackDirector : MonoBehaviour
{
    [Header("参照")]
    [SerializeField]
    private PlayerControl_2 player;

    [SerializeField]
    private BirdEvasionTracker evasionTracker;

    [SerializeField]
    private BirdWarningUI warningUI;

    [SerializeField]
    private BirdAttackAgent birdPrefab;

    [SerializeField]
    private Transform runtimeBirdRoot;

    [Header("最初の襲撃")]
    [SerializeField]
    private Vector2 initialDelayRange =
        new Vector2(20f, 28f);

    [Header("通常の襲撃間隔")]
    [SerializeField]
    private Vector2 baseIntervalRange =
        new Vector2(28f, 36f);

    [SerializeField, Min(1f)]
    private float minimumInterval = 10f;

    [Header("玩具数による危険度")]
    [SerializeField, Min(1)]
    private int toyCountForMaximumRisk = 10;

    [SerializeField, Range(0f, 0.9f)]
    private float maximumIntervalReduction = 0.6f;

    [SerializeField]
    private AnimationCurve toyRiskCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("ロックオン")]
    [SerializeField]
    private Vector2 lockOnDurationRange =
        new Vector2(5f, 6f);

    [Header("ロックオン時の鳥の位置")]
    [SerializeField, Min(0f)]
    private float lockOnHeight = 8f;

    private BirdAttackAgent activeBird;
    private bool attackModeEnabled;

    private IEnumerator Start()
    {
        if (!ValidateReferences())
        {
            yield break;
        }

        float initialDelay =
            RandomInRange(initialDelayRange);

        yield return WaitForPlayableSeconds(initialDelay);

        while (enabled)
        {
            yield return RunAttack();

            float nextInterval =
                CalculateNextAttackInterval();

            yield return WaitForPlayableSeconds(
                nextInterval
            );
        }
    }

    private IEnumerator RunAttack()
    {
        Vector3 spawnPosition =
            CalculateSpawnPosition();

        Transform parent =
            runtimeBirdRoot != null
                ? runtimeBirdRoot
                : transform;

        activeBird = Instantiate(
            birdPrefab,
            spawnPosition,
            Quaternion.identity,
            parent
        );

        activeBird.Initialize(
            player,
            lockOnHeight
        );

        activeBird.BeginLockOn();

        float lockOnDuration =
            RandomInRange(lockOnDurationRange);

        float elapsedTime = 0f;
        bool escaped = false;

        evasionTracker.BeginTracking();
        warningUI.ShowLockOn();

        while (elapsedTime < lockOnDuration &&
               activeBird != null)
        {
            if (IsGamePlaying())
            {
                elapsedTime += Time.deltaTime;

                escaped =
                    evasionTracker.Tick(Time.deltaTime);

                float remainingTime =
                    1f - elapsedTime / lockOnDuration;

                warningUI.UpdateView(
                    remainingTime,
                    evasionTracker.Progress
                );

                if (escaped)
                {
                    break;
                }
            }

            yield return null;
        }

        evasionTracker.StopTracking();

        if (activeBird == null)
        {
            warningUI.Hide();
            yield break;
        }

        if (escaped)
        {
            warningUI.ShowEscaped();
            activeBird.BeginEscape();
        }
        else
        {
            warningUI.Hide();
            activeBird.BeginDive();
        }

        // 鳥が離脱してDestroyされるまで待つ
        while (activeBird != null)
        {
            yield return null;
        }
    }

    private float CalculateNextAttackInterval()
    {
        float normalizedToyCount =
            Mathf.Clamp01(
                (float)player.CarriedToyCount /
                toyCountForMaximumRisk
            );

        float risk =
            Mathf.Clamp01(
                toyRiskCurve.Evaluate(
                    normalizedToyCount
                )
            );

        float intervalMultiplier =
            Mathf.Lerp(
                1f,
                1f - maximumIntervalReduction,
                risk
            );

        float minimum =
            Mathf.Max(
                minimumInterval,
                baseIntervalRange.x *
                intervalMultiplier
            );

        float maximum =
            Mathf.Max(
                minimum,
                baseIntervalRange.y *
                intervalMultiplier
            );

        return Random.Range(minimum, maximum);
    }

    private Vector3 CalculateSpawnPosition()
    {
        return
            player.transform.position +
            Vector3.up * lockOnHeight;
    }

    private IEnumerator WaitForPlayableSeconds(
        float seconds
    )
    {
        float elapsedTime = 0f;

        while (elapsedTime < seconds)
        {
            if (IsGamePlaying())
            {
                elapsedTime += Time.deltaTime;
            }

            yield return null;
        }
    }

    private bool IsGamePlaying()
    {
        return
            attackModeEnabled &&
            (GameManager.Instance == null ||
             GameManager.Instance.IsPlaying);
    }

    private float RandomInRange(Vector2 range)
    {
        float minimum =
            Mathf.Min(range.x, range.y);

        float maximum =
            Mathf.Max(range.x, range.y);

        return Random.Range(minimum, maximum);
    }

    private bool ValidateReferences()
    {
        bool isValid =
            player != null &&
            evasionTracker != null &&
            warningUI != null &&
            birdPrefab != null;

        if (!isValid)
        {
            Debug.LogError(
                "[BirdAttackDirector] " +
                "インスペクターの参照が不足しています。",
                this
            );
        }

        return isValid;
    }

    private void OnDisable()
    {
        evasionTracker?.StopTracking();
        warningUI?.Hide();

        if (activeBird != null)
        {
            Destroy(activeBird.gameObject);
            activeBird = null;
        }
    }

    public void SetAttackMode(bool isEnabled)
    {
        attackModeEnabled = isEnabled;

        if (isEnabled)
        {
            return;
        }

        evasionTracker?.StopTracking();
        warningUI?.Hide();

        if (activeBird != null)
        {
            Destroy(activeBird.gameObject);
            activeBird = null;
        }
    }
}