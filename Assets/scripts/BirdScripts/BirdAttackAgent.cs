using UnityEngine;

[DisallowMultipleComponent]
public sealed class BirdAttackAgent : MonoBehaviour
{
    private enum BirdState
    {
        Idle,
        LockOn,
        Dive,
        Escape
    }

    [Header("旋回")]
    [SerializeField, Min(0f)]
    private float turnSpeed = 360f;

    [Header("ロックオン中の円運動")]
    [SerializeField, Min(0f)]
    private float orbitRadius = 5f;

    [SerializeField, Min(0f)]
    private float orbitAngularSpeed = 30f;

    [SerializeField]
    private bool randomizeOrbitDirection = true;

    private float orbitAngle;
    private float orbitDirection = 1f;

    [Header("突撃")]
    [SerializeField, Min(0.1f)]
    private float diveSpeed = 18f;

    [SerializeField]
    private float targetHeightOffset = 0.3f;

    [SerializeField, Min(0.01f)]
    private float arrivalDistance = 0.2f;

    [SerializeField, Min(0.1f)]
    private float maximumDiveDuration = 5f;

    [Header("離脱")]
    [SerializeField, Min(0.1f)]
    private float escapeSpeed = 14f;

    [SerializeField, Min(0f)]
    private float escapeDistance = 18f;

    [SerializeField, Min(0f)]
    private float escapeHeight = 10f;
    private BirdState currentState = BirdState.Idle;
    private PlayerControl_2 player;
    private float lockOnHeight;
    private float diveElapsedTime;

    private Vector3 escapeTarget;

    private Collider[] attackColliders;

    private void Awake()
    {
        attackColliders =
            GetComponentsInChildren<Collider>(true);

        SetAttackCollidersEnabled(false);
    }

    public void Initialize(PlayerControl_2 targetPlayer, float targetLockOnHeight)
    {
        player = targetPlayer;
        lockOnHeight = targetLockOnHeight;
        currentState = BirdState.Idle;
    }

    public void BeginLockOn()
    {
        if (player == null)
        {
            BeginEscape();
            return;
        }

        orbitAngle = Random.Range(0f, 360f);

        orbitDirection =
            randomizeOrbitDirection &&
            Random.value < 0.5f
                ? -1f
                : 1f;

        SetAttackCollidersEnabled(false);
        currentState = BirdState.LockOn;

        // 生成されたフレームから円周上へ配置する
        ApplyOrbitTransform(true);
    }

    public void BeginDive()
    {
        if (player == null)
        {
            BeginEscape();
            return;
        }

        diveElapsedTime = 0f;

        SetAttackCollidersEnabled(true);
        currentState = BirdState.Dive;
    }

    public void BeginEscape()
    {
        if (currentState == BirdState.Escape)
        {
            return;
        }

        SetAttackCollidersEnabled(false);

        Vector3 awayDirection;

        if (player != null)
        {
            awayDirection =
                transform.position - player.transform.position;
            awayDirection.y = 0f;
        }
        else
        {
            awayDirection = -transform.forward;
        }

        if (awayDirection.sqrMagnitude < 0.001f)
        {
            awayDirection = -transform.forward;
        }

        awayDirection.Normalize();

        escapeTarget =
            transform.position +
            awayDirection * escapeDistance +
            Vector3.up * escapeHeight;

        currentState = BirdState.Escape;
    }

    private void Update()
    {
        if (GameManager.Instance != null &&
            !GameManager.Instance.IsPlaying)
        {
            return;
        }

        switch (currentState)
        {
            case BirdState.LockOn:
                UpdateLockOn();
                break;

            case BirdState.Dive:
                UpdateDive();
                break;

            case BirdState.Escape:
                UpdateEscape();
                break;
        }
    }

    private void UpdateLockOn()
    {
        if (player == null)
        {
            BeginEscape();
            return;
        }

        orbitAngle = Mathf.Repeat(
            orbitAngle +
            orbitAngularSpeed *
            orbitDirection *
            Time.deltaTime,
            360f
        );

        ApplyOrbitTransform(false);
    }

    private void UpdateDive()
    {
        if (player == null)
        {
            BeginEscape();
            return;
        }

        diveElapsedTime += Time.deltaTime;

        // 突撃中は毎フレーム、現在のプレイヤー位置を狙う
        Vector3 currentTarget =
            player.transform.position +
            Vector3.up * targetHeightOffset;

        RotateTowards(
            currentTarget,
            Time.deltaTime
        );

        transform.position = Vector3.MoveTowards(
            transform.position,
            currentTarget,
            diveSpeed * Time.deltaTime
        );

        // 命中しなかった場合の安全な終了処理
        if (diveElapsedTime >= maximumDiveDuration)
        {
            BeginEscape();
        }
    }

    private void UpdateEscape()
    {
        RotateTowards(escapeTarget, Time.deltaTime);

        transform.position = Vector3.MoveTowards(
            transform.position,
            escapeTarget,
            escapeSpeed * Time.deltaTime
        );

        if (Vector3.Distance(
                transform.position,
                escapeTarget
            ) <= arrivalDistance)
        {
            Destroy(gameObject);
        }
    }

    private void RotateTowards(
    Vector3 targetPosition,
    float deltaTime
)
    {
        Vector3 direction =
            targetPosition - transform.position;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Vector3 normalizedDirection =
            direction.normalized;

        Vector3 upDirection = Vector3.up;

        if (Vector3.Cross(
                normalizedDirection,
                upDirection
            ).sqrMagnitude < 0.001f)
        {
            upDirection = Vector3.forward;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                normalizedDirection,
                upDirection
            );

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            turnSpeed * deltaTime
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentState != BirdState.Dive)
        {
            return;
        }

        PlayerControl_2 hitPlayer =
            other.GetComponentInParent<PlayerControl_2>();

        if (hitPlayer == null || hitPlayer != player)
        {
            return;
        }

        // BeginEscapeで即座にColliderが無効になるため、
        // 同じ攻撃での多重ヒットを防げる
        hitPlayer.ReceiveBirdHit();
        BeginEscape();
    }

    private void SetAttackCollidersEnabled(bool enabled)
    {
        if (attackColliders == null)
        {
            return;
        }

        foreach (Collider attackCollider in attackColliders)
        {
            if (attackCollider != null)
            {
                attackCollider.enabled = enabled;
            }
        }
    }

    private void ApplyOrbitTransform(bool snapRotation)
    {
        if (player == null)
        {
            return;
        }

        // 円運動の中心は、常にプレイヤーの真上
        Vector3 orbitCenter =
            player.transform.position +
            Vector3.up * lockOnHeight;

        float angleRadians =
            orbitAngle * Mathf.Deg2Rad;

        Vector3 radialDirection =
            new Vector3(
                Mathf.Cos(angleRadians),
                0f,
                Mathf.Sin(angleRadians)
            );

        transform.position =
            orbitCenter +
            radialDirection * orbitRadius;

        // 円周に沿った進行方向
        Vector3 tangentDirection =
            new Vector3(
                -Mathf.Sin(angleRadians),
                0f,
                Mathf.Cos(angleRadians)
            ) * orbitDirection;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                tangentDirection,
                Vector3.up
            );

        if (snapRotation)
        {
            transform.rotation = targetRotation;
        }
        else
        {
            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );
        }
    }
}