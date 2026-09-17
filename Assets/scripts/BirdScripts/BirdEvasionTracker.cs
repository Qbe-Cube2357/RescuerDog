using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerControl_2))]
public sealed class BirdEvasionTracker : MonoBehaviour
{
    [Header("回避判定")]
    [SerializeField, Range(0f, 2f)]
    private float steeringThreshold = 0.35f;

    [SerializeField, Range(0f, 1f)]
    private float minimumForwardInput = 0.2f;

    [SerializeField, Min(0f)]
    private float minimumDirectionHoldTime = 0.12f;

    [SerializeField, Min(0f)]
    private float minimumSwitchInterval = 0.15f;

    [SerializeField, Min(0.01f)]
    private float maximumSwitchInterval = 1.2f;

    [SerializeField, Min(1)]
    private int requiredDirectionChanges = 3;

    private PlayerControl_2 player;

    private bool isTracking;
    private int lastAcceptedDirection;
    private int candidateDirection;
    private int directionChangeCount;

    private float candidateHoldTime;
    private float timeSinceLastAcceptedDirection;

    public float Progress =>
        requiredDirectionChanges <= 0
            ? 1f
            : Mathf.Clamp01(
                (float)directionChangeCount / requiredDirectionChanges
            );

    private void Awake()
    {
        player = GetComponent<PlayerControl_2>();
    }

    public void BeginTracking()
    {
        isTracking = true;
        ResetSequence();
    }

    public void StopTracking()
    {
        isTracking = false;
        ResetSequence();
    }

    /// <summary>
    /// ロックオン中に毎フレーム呼ぶ。
    /// 回避条件を満たした瞬間にtrueを返す。
    /// </summary>
    public bool Tick(float deltaTime)
    {
        if (!isTracking || player == null)
        {
            return false;
        }

        timeSinceLastAcceptedDirection += deltaTime;

        bool isMovingForward =
            player.ForwardInput >= minimumForwardInput;

        bool hasStrongSteering =
            Mathf.Abs(player.SteeringInput) >= steeringThreshold;

        if (!isMovingForward || !hasStrongSteering)
        {
            candidateDirection = 0;
            candidateHoldTime = 0f;

            if (lastAcceptedDirection != 0 &&
                timeSinceLastAcceptedDirection > maximumSwitchInterval)
            {
                ResetSequence();
            }

            return false;
        }

        int currentDirection =
            player.SteeringInput > 0f ? 1 : -1;

        if (candidateDirection != currentDirection)
        {
            candidateDirection = currentDirection;
            candidateHoldTime = 0f;
        }

        candidateHoldTime += deltaTime;

        if (candidateHoldTime < minimumDirectionHoldTime)
        {
            return false;
        }

        // 最初の左右入力を基準方向として記録する
        if (lastAcceptedDirection == 0)
        {
            AcceptDirection(currentDirection);
            return false;
        }

        // 同じ方向を維持しているだけでは加算しない
        if (currentDirection == lastAcceptedDirection)
        {
            return false;
        }

        // 細かすぎる入力ノイズを無視する
        if (timeSinceLastAcceptedDirection < minimumSwitchInterval)
        {
            return false;
        }

        // 次の入力が遅すぎた場合は最初からやり直す
        if (timeSinceLastAcceptedDirection > maximumSwitchInterval)
        {
            directionChangeCount = 0;
            AcceptDirection(currentDirection);
            return false;
        }

        directionChangeCount++;
        AcceptDirection(currentDirection);

        return directionChangeCount >= requiredDirectionChanges;
    }

    private void AcceptDirection(int direction)
    {
        lastAcceptedDirection = direction;
        candidateDirection = direction;
        candidateHoldTime = 0f;
        timeSinceLastAcceptedDirection = 0f;
    }

    private void ResetSequence()
    {
        lastAcceptedDirection = 0;
        candidateDirection = 0;
        directionChangeCount = 0;
        candidateHoldTime = 0f;
        timeSinceLastAcceptedDirection = 0f;
    }
}