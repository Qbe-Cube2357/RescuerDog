using UnityEngine;

public class PlayerControl_2 : MonoBehaviour
{
    [Header("Speed Settings")]
    public float walkSpeed = 4f;       // 通常（歩き）の最大速度
    public float runSpeed = 8f;        // 高速（走り）の最大速度
    public float rotateSpeed = 100f;   // 旋回速度の倍率

    [Header("M5Stick Input (Simulated)")]
    // インスペクターで値が -1.0 ～ 1.0 に動くのを確認できます
    [Range(-1f, 1f)] public float leftInput = 0f;
    [Range(-1f, 1f)] public float rightInput = 0f;

    [Header("Simulation Smoothness")]
    public float responseSpeed = 5f;   // キーを押した時の値の変化の速さ

    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // 1. キーボード入力でM5Stickの挙動をシミュレート
        SimulateM5StickInput();

        // 2. 左右の入力から「前進・後退（Vertical）」と「旋回（Horizontal）」の成分を抽出
        float forwardComponent = (leftInput + rightInput) / 2f;
        float rotateComponent = leftInput - rightInput;

        // 3. 入力（回す速度）の強さに応じて、歩きか走りかを判定
        // Mathf.Max で、左右どちらか片方でも激しく回っていれば強度が上がります
        float inputIntensity = Mathf.Max(Mathf.Abs(leftInput), Mathf.Abs(rightInput));

        float currentMaxSpeed = walkSpeed;
        // 入力の絶対値が 0.8 を超えたら「激しく回している（走り）」とみなす
        if (inputIntensity > 0.8f)
        {
            currentMaxSpeed = runSpeed;
        }

        // 4. 実際の移動速度と回転量を計算
        float targetSpeed = currentMaxSpeed * forwardComponent;
        float targetRotation = rotateSpeed * rotateComponent;

        // 5. 移動と回転の処理
        transform.position += transform.forward * targetSpeed * Time.deltaTime;
        transform.Rotate(0, targetRotation * Time.deltaTime, 0);

        // 6. Animator（Blend Tree）への連携
        if (anim != null)
        {
            float speedRatio = Mathf.Abs(targetSpeed) / runSpeed;
            anim.SetFloat("Speed", Mathf.Clamp01(speedRatio));
        }
    }

    /// <summary>
    /// キーボード入力を使って、M5Stickのハンドホイール回転（角速度）をシミュレートするメソッド
    /// </summary>
    void SimulateM5StickInput()
    {
        // --- 左ホイールのシミュレート (Q: 時計回り / A: 反時計回り) ---
        float targetLeft = 0f;
        if (Input.GetKey(KeyCode.Q)) targetLeft = 1f;       // 時計回り（前進成分）
        else if (Input.GetKey(KeyCode.A)) targetLeft = -1f;  // 反時計回り（後退成分）

        // Shiftを一緒に押していたら、強制的に最大値（走り）にするシミュレート用
        if (Input.GetKey(KeyCode.LeftShift) && targetLeft != 0f) targetLeft *= 1.0f;
        // 通常キーだけなら、少し手前（0.6：歩き限界値付近）に抑える（テストしやすくするため）
        else if (targetLeft != 0f && !Input.GetKey(KeyCode.LeftShift)) targetLeft *= 0.6f;

        // 値を滑らかに変化させる（M5Stickのホイールが回り始める/止まる慣性の表現）
        leftInput = Mathf.MoveTowards(leftInput, targetLeft, responseSpeed * Time.deltaTime);


        // --- 右ホイールのシミュレート (E: 時計回り / D: 反時計回り) ---
        float targetRight = 0f;
        if (Input.GetKey(KeyCode.E)) targetRight = 1f;       // 時計回り（前進成分）
        else if (Input.GetKey(KeyCode.D)) targetRight = -1f;  // 反時計回り（後退成分）

        if (Input.GetKey(KeyCode.LeftShift) && targetRight != 0f) targetRight *= 1.0f;
        else if (targetRight != 0f && !Input.GetKey(KeyCode.LeftShift)) targetRight *= 0.6f;

        rightInput = Mathf.MoveTowards(rightInput, targetRight, responseSpeed * Time.deltaTime);
    }
}