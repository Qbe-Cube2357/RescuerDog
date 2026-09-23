using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControl_2 : MonoBehaviour
{
    [Header("Speed Settings (物理調整用)")]
    public float walkForce = 15f;
    public float runForce = 30f;
    public float maxSpeed = 8f;
    public float rotateSpeed = 150f;

    [Header("Input Settings (操作切り替え)")]
    public bool useSensorData = true;

    [Header("M5Stick Input (Simulated)")]
    [Range(-1f, 1f)] public float leftInput;
    [Range(-1f, 1f)] public float rightInput;
    public float maxGyroValue = 200f;

    [Header("Simulation Smoothness")]
    public float responseSpeed = 5f;

    [Header("おもちゃ追従設定")]
    private readonly List<ToyFollower> collectedToys =
        new List<ToyFollower>();

    [Header("プレイヤーが当たった時のエフェクト")]
    public GameObject conflictEffectPrefab;
    public AudioClip conflictSE;

    [Header("ブースト時の設定")]
    public float boostSpeedMultiplier = 1.5f;
    public float boostScaleMultiplier = 1.5f;
    public float boostScaleDuration = 1f;

    [Header("チュートリアル用のキー変数")]
    [SerializeField]
    private KeyCode leftForwardKey = KeyCode.Q;

    [SerializeField]
    private KeyCode leftBackwardKey = KeyCode.A;

    [SerializeField]
    private KeyCode rightForwardKey = KeyCode.E;

    [SerializeField]
    private KeyCode rightBackwardKey = KeyCode.D;

    // ----- チュートリアル用 -----
    private bool tutorialControlOverride; // チュートリアル用
    private bool tutorialControlEnabled; // チュートリアル用
    private bool CanProcessInput
    {
        get
        {
            if (tutorialControlOverride)
            {
                return tutorialControlEnabled;
            }

            return
                GameManager.Instance != null &&
                GameManager.Instance.IsPlaying;
        }
    }
    // --------------------

    private Animator anim;
    private Rigidbody rb;
    private AudioSource audioSource;
    private Process m5Process;
    private string filePath;


    // FixedUpdateへ渡す移動入力。
    private float forwardComponent;
    private float rotateComponent;
    private float currentMaxSpeed;

    private bool isBoostMode;
    private Vector3 normalScale;
    private Coroutine scaleCoroutine;

    // 鳥の回避判定など、外部システムから参照する読み取り専用情報。
    public float SteeringInput => rotateComponent;
    public float ForwardInput => forwardComponent;
    public int CarriedToyCount => collectedToys.Count;

    private void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        normalScale = transform.localScale;
        currentMaxSpeed = WalkSpeedSimulate();

        filePath = ExternalPaths.SensorDataFilePath;
    }

    private void Update()
    {
        if (!CanProcessInput)
        {
            ResetHandleInput();
            forwardComponent = 0f;
            rotateComponent = 0f;
            return;
        }

        if (useSensorData)
        {
            ReadSensorData();
        }
        else
        {
            SimulateM5StickInput();
        }

        forwardComponent = (leftInput + rightInput) / 2f;
        rotateComponent = leftInput - rightInput;

        float inputIntensity = Mathf.Max(
            Mathf.Abs(leftInput),
            Mathf.Abs(rightInput)
        );

        currentMaxSpeed = inputIntensity > 0.8f
            ? RunSpeedSimulate()
            : WalkSpeedSimulate();

        UpdateMovementAnimation();
    }

    private void FixedUpdate()
    {
        if (!CanProcessInput)
        {
            rb.angularVelocity = Vector3.zero;
            return;
        }

        float targetRotation = rotateSpeed * rotateComponent;
        rb.angularVelocity = new Vector3(
            0f,
            targetRotation * Mathf.Deg2Rad,
            0f
        );

        float speedMultiplier = isBoostMode
            ? boostSpeedMultiplier
            : 1f;

        float force = currentMaxSpeed == RunSpeedSimulate()
            ? runForce
            : walkForce;

        Vector3 moveForce =
            transform.forward *
            forwardComponent *
            force *
            speedMultiplier;

        Vector3 horizontalVelocity = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        );

        if (horizontalVelocity.magnitude < maxSpeed * speedMultiplier)
        {
            rb.AddForce(moveForce, ForceMode.Acceleration);
        }
    }

    private void UpdateMovementAnimation()
    {
        if (anim == null)
        {
            return;
        }

        float targetSpeed = currentMaxSpeed * forwardComponent;
        float speedRatio = Mathf.Abs(targetSpeed) / RunSpeedSimulate();
        anim.SetFloat("Speed", Mathf.Clamp01(speedRatio));
    }

    private float WalkSpeedSimulate() => 4f;
    private float RunSpeedSimulate() => 8f;

    /// <summary>
    /// キーボード入力で左右ハンドルの角速度をシミュレートする。
    /// Q/Aが左の前後、E/Dが右の前後、Shiftが高速回転。
    /// </summary>
    private void SimulateM5StickInput()
    {
        float targetLeft = 0f;
        if (Input.GetKey(leftForwardKey))
        {
            targetLeft = 1f;
        }
        else if (Input.GetKey(leftBackwardKey))
        {
            targetLeft = -1f;
        }

        if (targetLeft != 0f && !Input.GetKey(KeyCode.LeftShift))
        {
            targetLeft *= 0.6f;
        }

        leftInput = Mathf.MoveTowards(
            leftInput,
            targetLeft,
            responseSpeed * Time.deltaTime
        );

        float targetRight = 0f;
        if (Input.GetKey(rightForwardKey))
        {
            targetRight = 1f;
        }
        else if (Input.GetKey(rightBackwardKey))
        {
            targetRight = -1f;
        }

        if (targetRight != 0f && !Input.GetKey(KeyCode.LeftShift))
        {
            targetRight *= 0.6f;
        }

        rightInput = Mathf.MoveTowards(
            rightInput,
            targetRight,
            responseSpeed * Time.deltaTime
        );
    }

    /// <summary>
    /// data.txtを読み込み、左右の移動入力と生の角速度を更新する。
    /// </summary>
    private void ReadSensorData()
    {
        if (!File.Exists(filePath))
        {
            ResetHandleInput();
            return;
        }

        try
        {
            using FileStream fileStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite
            );

            using StreamReader reader = new StreamReader(
                fileStream,
                Encoding.UTF8
            );

            string line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            string[] data = line.Split(',');
            if (data.Length < 2)
            {
                return;
            }

            bool leftParsed = float.TryParse(
                data[0],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float rawLeft
            );

            bool rightParsed = float.TryParse(
                data[1],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float rawRight
            );

            if (!leftParsed || !rightParsed)
            {
                return;
            }

            float gyroScale = Mathf.Max(Mathf.Abs(maxGyroValue), 0.001f);

            leftInput = Mathf.Clamp(
                rawLeft / gyroScale,
                -1f,
                1f
            );

            rightInput = Mathf.Clamp(
                rawRight / gyroScale,
                -1f,
                1f
            );
        }
        catch (IOException)
        {
            // M5側の書き込みと重なった場合は、次のフレームで再試行する。
        }
    }

    private void ResetHandleInput()
    {
        leftInput = 0f;
        rightInput = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Toy") || other.CompareTag("Treasure"))
        {
            ToyFollower toy = other.GetComponent<ToyFollower>();
            if (toy != null && !collectedToys.Contains(toy))
            {
                Transform targetTransform = collectedToys.Count == 0
                    ? transform
                    : collectedToys[collectedToys.Count - 1].transform;

                toy.StartFollowing(targetTransform);

                if (toy.TryGetComponent<
                    BalloonTreasureController>(
                        out BalloonTreasureController
                            balloonTreasure))
                {
                    balloonTreasure.ReleaseBalloon();
                }

                collectedToys.Add(toy);

                UnityEngine.Debug.Log(
                    $"おもちゃをゲット！ 現在の合計: {collectedToys.Count}個"
                );
            }
        }

        // 鳥側からReceiveBirdHitを呼ぶため、ここではObstacleだけを扱う。
        if (other.CompareTag("Obstacle"))
        {
            ReceiveHazardHit();
        }
    }

    public void DropHalfToys()
    {
        if (collectedToys.Count == 0)
        {
            return;
        }

        int dropCount = Mathf.CeilToInt(collectedToys.Count / 2f);
        UnityEngine.Debug.Log(
            $"障害物に衝突！ おもちゃを {dropCount} 個落とします！"
        );

        for (int i = 0; i < dropCount; i++)
        {
            int lastIndex = collectedToys.Count - 1;
            ToyFollower toyToDrop = collectedToys[lastIndex];
            collectedToys.RemoveAt(lastIndex);
            toyToDrop.Drop();
        }

        for (int i = 0; i < collectedToys.Count; i++)
        {
            Transform nextTarget = i == 0
                ? transform
                : collectedToys[i - 1].transform;

            collectedToys[i].StartFollowing(nextTarget);
        }
    }

    public List<ToyFollower> GetCollectedToys() => collectedToys;
    public void ClearCollectedToys() => collectedToys.Clear();

    public void StartBoostMode()
    {
        if (isBoostMode)
        {
            return;
        }

        isBoostMode = true;
        StartScaleChange(
            normalScale * boostScaleMultiplier,
            boostScaleDuration
        );
    }

    public void EndBoostMode()
    {
        if (!isBoostMode)
        {
            return;
        }

        isBoostMode = false;
        StartScaleChange(normalScale, boostScaleDuration);
    }

    private void StartScaleChange(Vector3 targetScale, float duration)
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }

        scaleCoroutine = StartCoroutine(
            ChangeScaleRoutine(targetScale, duration)
        );
    }

    private IEnumerator ChangeScaleRoutine(
        Vector3 targetScale,
        float duration
    )
    {
        if (duration <= 0f)
        {
            transform.localScale = targetScale;
            scaleCoroutine = null;
            yield break;
        }

        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.Lerp(
                startScale,
                targetScale,
                t
            );
            yield return null;
        }

        transform.localScale = targetScale;
        scaleCoroutine = null;
    }

    /// <summary>
    /// 動的に生成された鳥から攻撃された時に呼ばれる。
    /// </summary>
    public void ReceiveBirdHit()
    {
        ReceiveHazardHit();
    }

    private void ReceiveHazardHit()
    {
        if (audioSource != null && conflictSE != null)
        {
            audioSource.PlayOneShot(conflictSE);
        }

        if (conflictEffectPrefab != null)
        {
            Instantiate(
                conflictEffectPrefab,
                transform.position,
                Quaternion.identity
            );
        }

        DropHalfToys();
    }


    private void OnDisable()
    {
        ResetHandleInput();
    }
    /// <summary>
    /// チュートリアル用
    /// </summary>
    public void SetTutorialControlEnabled(bool controlEnabled)
    {
        tutorialControlOverride = true;
        tutorialControlEnabled = controlEnabled;

        if (!controlEnabled)
        {
            ResetHandleInput();

            forwardComponent = 0f;
            rotateComponent = 0f;

            if (rb != null)
            {
                rb.linearVelocity = new Vector3(
                    0f,
                    rb.linearVelocity.y,
                    0f
                );

                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    public void ClearTutorialControlOverride()
    {
        tutorialControlOverride = false;
        tutorialControlEnabled = false;
    }

}
