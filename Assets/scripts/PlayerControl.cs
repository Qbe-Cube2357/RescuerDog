using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    [Header("Speed Settings")]
    public float walkSpeed = 4f;       // 歩き時の速度（アニメーションの0.5に対応）
    public float runSpeed = 8f;        // 走り時の速度（アニメーションの1.0に対応）
    public float rotateSpeed = 100f;
    bool isRotating = false;

    private Animator anim;

    void Start()
    {
        // 自身のGameObjectからAnimatorコンポーネントを取得
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        float v = Input.GetAxis("Vertical");
        float h = Input.GetAxis("Horizontal");

        // 1. Shiftキーの入力によって「現在の最大移動速度」を決める
        float currentMaxSpeed = walkSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentMaxSpeed = runSpeed;
        }

        // 2. 実際の移動速度を計算（入力値 v を掛ける）
        float targetSpeed = currentMaxSpeed * v;

        // 3. 移動と回転の処理
        transform.position += transform.forward * targetSpeed * Time.deltaTime;
        transform.Rotate(0, rotateSpeed * h * Time.deltaTime, 0);



        // --- Animatorへの連携処理 ---
        if (anim != null)
        {
            // 4. 全体（走り時の最大速度）に対して、今どれくらいの速度で動いているかの割合（0.0 ～ 1.0）を計算
            // Mathf.Abs(targetSpeed) で後退時もプラスの速度として扱います
            float speedRatio = Mathf.Abs(targetSpeed) / runSpeed;

            // Animatorのパラメーター「Speed」に値をセット
            anim.SetFloat("Speed", speedRatio);
        }
    }
}