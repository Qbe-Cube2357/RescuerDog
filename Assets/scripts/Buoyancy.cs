using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Buoyancy : MonoBehaviour
{
    [Header("水の物理設定")]
    [SerializeField] private float waterLevel = 0f;      // 海面のY座標
    [SerializeField] private float floatThreshold = 2f;  // 完全に沈んだとみなす深さ
    [SerializeField] private float buoyancyForce = 15f;  // 浮力の強さ

    [Header("水中の抵抗")]
    [SerializeField] private float waterDrag = 3f;
    [SerializeField] private float waterAngularDrag = 1f;

    [Header("ぷかぷか挙動 (Sin波)")]
    [SerializeField] private float bobbingFrequency = 2f;  // ぷかぷかの速さ
    [SerializeField] private float bobbingForce = 2f;      // ぷかぷかの強さ

    [Header("エフェクト設定")]
    private AudioSource AudioSource;
    public AudioClip splashSE;
    [SerializeField] private GameObject splashEffectPrefab; // 着水エフェクトのPrefab
    [SerializeField] private float minSplashVelocity = 2f;  // エフェクトが出る最小の落下速度

    [Header("移動時エフェクト")]
    [SerializeField] private GameObject moveEffectPrefab;   // 移動時（引き波等）エフェクトのPrefab
    public AudioClip movesplashSE;
    [SerializeField] private float moveEffectThreshold = 1f; // エフェクトが発生する最小の水平速度
    [SerializeField] private float moveEffectInterval = 0.3f; // エフェクトの生成間隔（秒）

    private Rigidbody rb;
    private float defaultDrag;
    private float defaultAngularDrag;

    // 状態管理用のフラグ
    private bool isUnderwater = false;
    private float moveEffectTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        defaultDrag = rb.linearDamping;
        defaultAngularDrag = rb.angularDamping;

        isUnderwater = transform.position.y < waterLevel;

        AudioSource = gameObject.AddComponent<AudioSource>();//変数「audioSource」にAudioSourceコンポネントを入れる
    }

    void FixedUpdate()
    {
        bool currentlyUnderwater = transform.position.y < waterLevel;

        // 【着水の瞬間】
        if (currentlyUnderwater && !isUnderwater)
        {
            if (rb.linearVelocity.y < -minSplashVelocity)
            {
                TriggerSplashEffect();
            }
        }

        isUnderwater = currentlyUnderwater;

        // --- 浮力と抵抗の物理処理 ---
        if (isUnderwater)
        {
            float depth = waterLevel - transform.position.y;
            float floatFactor = Mathf.Clamp01(depth / floatThreshold);

            // 1. 基本的な浮力
            Vector3 buoyancy = Vector3.up * buoyancyForce * floatFactor;
            rb.AddForce(buoyancy, ForceMode.Acceleration);

            // 2. 永続的なぷかぷか挙動 (サイン波による微小な上下の力)
            // Time.time を使うことで、静止した後も常に変化する力が加わり続けます
            float bobbingY = Mathf.Sin(Time.time * bobbingFrequency) * bobbingForce;
            Vector3 bobbingVector = Vector3.up * bobbingY;
            rb.AddForce(bobbingVector, ForceMode.Acceleration);

            // 3. 移動時エフェクトの処理
            HandleMoveEffect();

            // 抵抗の減衰
            rb.linearDamping = Mathf.Lerp(rb.linearDamping, waterDrag, Time.fixedDeltaTime * 5f);
            rb.angularDamping = Mathf.Lerp(rb.angularDamping, waterAngularDrag, Time.fixedDeltaTime * 5f);
        }
        else
        {
            rb.linearDamping = Mathf.Lerp(rb.linearDamping, defaultDrag, Time.fixedDeltaTime * 5f);
            rb.angularDamping = Mathf.Lerp(rb.angularDamping, defaultAngularDrag, Time.fixedDeltaTime * 5f);
        }
    }

    /// <summary>
    /// 着水エフェクトを生成するメソッド
    /// </summary>
    private void TriggerSplashEffect()
    {
        if (splashEffectPrefab != null)
        {
            AudioSource.PlayOneShot(splashSE); //サウンド流す

            Vector3 splashPosition = new Vector3(transform.position.x, waterLevel, transform.position.z);
            GameObject splash = Instantiate(splashEffectPrefab, splashPosition, Quaternion.identity);
            Destroy(splash, 2f);
        }
    }

    /// <summary>
    /// 移動時のエフェクト生成を管理するメソッド
    /// </summary>
    private void HandleMoveEffect()
    {
        if (moveEffectPrefab == null) return;

        // 水平方向（X, Z軸）の速度ベクトルを計算
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // 確認用ログ
        // Debug.Log($"{horizontalVelocity}");

        // 一定以上の速度で移動しているか判定
        if (horizontalVelocity.magnitude > moveEffectThreshold)
        {
            moveEffectTimer += Time.fixedDeltaTime;

            if (moveEffectTimer >= moveEffectInterval)
            {
                // うるさかった
                // AudioSource.PlayOneShot(movesplashSE); //サウンド流す

                TriggerMoveEffect();
                moveEffectTimer = 0f; // タイマーリセット
            }
        }
        else
        {
            // 止まっている時はタイマーを進めない（動き出した瞬間にすぐ出るようにする場合はここでリセット）
            moveEffectTimer = moveEffectInterval;
        }
    }

    /// <summary>
    /// 移動時エフェクトを生成するメソッド
    /// </summary>
    private void TriggerMoveEffect()
    {
        // 海面の高さにエフェクトを生成
        Vector3 effectPosition = new Vector3(transform.position.x, waterLevel, transform.position.z);

        // 進行方向を向いた回転を作る（パーティクルに方向性がある場合のため）
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Quaternion effectRotation = horizontalVelocity != Vector3.zero ? Quaternion.LookRotation(horizontalVelocity) : Quaternion.identity;

        GameObject moveEffect = Instantiate(moveEffectPrefab, effectPosition, effectRotation);
        Destroy(moveEffect, 1.5f);
    }
}