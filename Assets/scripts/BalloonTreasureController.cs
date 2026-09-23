using UnityEngine;

[DisallowMultipleComponent]
public sealed class BalloonTreasureController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField]
    private Transform balloonRoot;

    public enum BalloonTreasureHeightMode
    {
        WorldY,
        SpawnPointRelative
    }

    [Header("上下移動")]
    [SerializeField]
    private BalloonTreasureHeightMode heightMode =
        BalloonTreasureHeightMode.WorldY;

    [Tooltip("移動範囲の最低Y。WorldYではワールド座標、RelativeではSpawnPointからの差")]
    [SerializeField]
    private float minimumY = 0f;

    [Tooltip("移動範囲の最高Y。WorldYではワールド座標、RelativeではSpawnPointからの差")]
    [SerializeField]
    private float maximumY = 1f;

    [Tooltip("1秒間に何往復するか")]
    [SerializeField, Min(0.01f)]
    private float bobFrequency = 0.5f;

    [Header("風船の離脱")]
    [SerializeField, Min(0.01f)]
    private float balloonFlySpeed = 2.5f;

    [SerializeField, Min(0f)]
    private float balloonDespawnHeight = 5f;

    private Rigidbody targetRigidbody;

    private Vector3 baseWorldPosition;
    private float elapsedTime;
    private bool isReleased;

    private bool originalUseGravity;

    public bool IsReleased => isReleased;

    private void Awake()
    {
        baseWorldPosition = transform.position;

        targetRigidbody =
            GetComponent<Rigidbody>();

        if (targetRigidbody != null)
        {
            originalUseGravity =
                targetRigidbody.useGravity;

            targetRigidbody.linearVelocity =
                Vector3.zero;

            targetRigidbody.angularVelocity =
                Vector3.zero;

            targetRigidbody.useGravity = false;
            targetRigidbody.isKinematic = true;
        }

        Vector3 startingPosition =
            CalculateBobPosition(0f);

        if (targetRigidbody != null)
        {
            targetRigidbody.position =
                startingPosition;
        }
        else
        {
            transform.position =
                startingPosition;
        }
    }

    private void Update()
    {
        if (isReleased || targetRigidbody != null)
        {
            return;
        }

        elapsedTime += Time.deltaTime;

        transform.position =
            CalculateBobPosition(elapsedTime);
    }

    private void FixedUpdate()
    {
        if (isReleased || targetRigidbody == null)
        {
            return;
        }

        elapsedTime += Time.fixedDeltaTime;

        targetRigidbody.MovePosition(
            CalculateBobPosition(elapsedTime)
        );
    }

    private Vector3 CalculateBobPosition(float time)
    {
        float radians =
            2f * Mathf.PI * bobFrequency * time;

        // 0～1を往復。time=0では0から開始する。
        float normalizedHeight =
            0.5f * (1f - Mathf.Cos(radians));

        float lowerY = Mathf.Min(
            minimumY,
            maximumY
        );

        float upperY = Mathf.Max(
            minimumY,
            maximumY
        );

        if (heightMode ==
            BalloonTreasureHeightMode
                .SpawnPointRelative)
        {
            lowerY += baseWorldPosition.y;
            upperY += baseWorldPosition.y;
        }

        Vector3 position = baseWorldPosition;

        position.y = Mathf.Lerp(
            lowerY,
            upperY,
            normalizedHeight
        );

        return position;
    }

    public void ReleaseBalloon(
        Transform flyAwayParent = null)
    {
        if (isReleased)
        {
            return;
        }

        isReleased = true;

        if (targetRigidbody != null)
        {
            targetRigidbody.linearVelocity =
                Vector3.zero;

            targetRigidbody.angularVelocity =
                Vector3.zero;

            // ToyFollowerによる追従中はKinematicを維持する。
            // ToyFollower.Drop()が呼ばれるとfalseへ戻る。
            targetRigidbody.isKinematic = true;
            targetRigidbody.useGravity =
                originalUseGravity;
        }

        if (balloonRoot == null)
        {
            Debug.LogWarning(
                $"{name}のBalloon Rootが未設定です。",
                this
            );
            return;
        }

        Transform releasedBalloon = balloonRoot;
        balloonRoot = null;

        // ゲーム中はnullなのでワールド直下へ分離される。
        releasedBalloon.SetParent(
            flyAwayParent,
            true
        );

        BalloonFlyAway flyAway =
            releasedBalloon.GetComponent<
                BalloonFlyAway
            >();

        if (flyAway == null)
        {
            flyAway =
                releasedBalloon.gameObject
                    .AddComponent<BalloonFlyAway>();
        }

        flyAway.Launch(
            balloonFlySpeed,
            balloonDespawnHeight
        );
    }
}