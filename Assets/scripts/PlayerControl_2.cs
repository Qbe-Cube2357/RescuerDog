using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

[RequireComponent(typeof(Rigidbody))] // Rigidbodyを必須にする
public class PlayerControl_2 : MonoBehaviour
{
    [Header("Speed Settings (物理調整用)")]
    public float walkForce = 15f;      // 歩き時の移動力の強さ
    public float runForce = 30f;       // 走り時の移動力の強さ
    public float maxSpeed = 8f;        // 速度の最高制限（加速しすぎるのを防ぐ）
    public float rotateSpeed = 150f;   // 旋回速度の倍率（物理回転用に少し高めを推奨）

    [Header("Input Settings (操作切り替え)")]
    public bool useSensorData = true; // trueでM5Stick(data.txt)を利用、falseでキーボードを利用

    [Header("M5Stick Input (Simulated)")]
    [Range(-1f, 1f)] public float leftInput = 0f;
    [Range(-1f, 1f)] public float rightInput = 0f;
    public float maxGyroValue = 200f;
    private string filePath;

    [Header("おもちゃ追従設定")]
    private List<ToyFollower> collectedToys = new List<ToyFollower>();

    [Header("Simulation Smoothness")]
    public float responseSpeed = 5f;

    private Animator anim;
    private Rigidbody rb;

    // FixedUpdateへ値を渡すための変数
    private float forwardComponent = 0f;
    private float rotateComponent = 0f;
    private float currentMaxSpeed = 0f;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        // 物理移動をスムーズにするため、インスペクターの手動設定の代わりに、
        // 衝突時にガタつかない設定（Interpolate）をコードから有効化します
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Application.dataPath は Project/Assets を指すため、一つ上の階層の external を指定
        filePath = Path.Combine(Application.dataPath, "../external/data.txt");
    }

    void Update()
    {

        // 1. インスペクターのフラグに応じて入力を切り替える
        if (useSensorData)
        {
            ReadSensorData();
        }
        else
        {
            SimulateM5StickInput();
        }

        // 2. 左右の入力から「前進・後退」と「旋回」の成分を抽出
        forwardComponent = (leftInput + rightInput) / 2f;
        rotateComponent = leftInput - rightInput;

        // 3. 入力の強さに応じて、歩きか走りかを判定
        float inputIntensity = Mathf.Max(Mathf.Abs(leftInput), Mathf.Abs(rightInput));

        // 力の強さとアニメーション用の最大速度を設定
        float currentForce = walkForce;
        currentMaxSpeed = walkSpeedSimulate(); // アニメーション比率用

        if (inputIntensity > 0.8f)
        {
            currentForce = runForce;
            currentMaxSpeed = runSpeedSimulate();
        }

        // 4. アニメーション（Blend Tree）への連携（Updateで行うのが適切）
        if (anim != null)
        {
            float targetSpeed = currentMaxSpeed * forwardComponent;
            float speedRatio = Mathf.Abs(targetSpeed) / runSpeedSimulate();
            anim.SetFloat("Speed", Mathf.Clamp01(speedRatio));
        }
    }

    // 物理演算はFixedUpdateで実行する
    void FixedUpdate()
    {
        // --- 1. 回転処理 (AngularVelocityの直接書き換え、またはMoveRotation) ---
        // 旋回成分がある場合、Y軸まわりに回転速度を与える
        float targetRotation = rotateSpeed * rotateComponent;
        rb.angularVelocity = new Vector3(0f, targetRotation * Mathf.Deg2Rad, 0f);

        // --- 2. 移動処理 (AddForce) ---
        // 現在の進行方向（transform.forward）に向かって力を加える
        Vector3 moveForce = transform.forward * forwardComponent * (currentMaxSpeed == runSpeedSimulate() ? runForce : walkForce);

        // 水平方向（X, Z）の現在の速度をチェックし、制限速度内であれば力を加える
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (horizontalVelocity.magnitude < maxSpeed)
        {
            rb.AddForce(moveForce, ForceMode.Acceleration);
        }
    }

    // 元のコードの速度基準を維持するためのヘルパー関数
    private float walkSpeedSimulate() => 4f;
    private float runSpeedSimulate() => 8f;

    /// <summary>
    /// キーボード入力を使って、M5Stickのハンドホイール回転（角速度）をシミュレートするメソッド
    /// </summary>
    void SimulateM5StickInput()
    {
        float targetLeft = 0f;
        if (Input.GetKey(KeyCode.Q)) targetLeft = 1f;
        else if (Input.GetKey(KeyCode.A)) targetLeft = -1f;

        if (Input.GetKey(KeyCode.LeftShift) && targetLeft != 0f) targetLeft *= 1.0f;
        else if (targetLeft != 0f && !Input.GetKey(KeyCode.LeftShift)) targetLeft *= 0.6f;

        leftInput = Mathf.MoveTowards(leftInput, targetLeft, responseSpeed * Time.deltaTime);

        float targetRight = 0f;
        if (Input.GetKey(KeyCode.E)) targetRight = 1f;
        else if (Input.GetKey(KeyCode.D)) targetRight = -1f;

        if (Input.GetKey(KeyCode.LeftShift) && targetRight != 0f) targetRight *= 1.0f;
        else if (targetRight != 0f && !Input.GetKey(KeyCode.LeftShift)) targetRight *= 0.6f;

        rightInput = Mathf.MoveTowards(rightInput, targetRight, responseSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Toy"))
        {
            ToyFollower toy = other.GetComponent<ToyFollower>();
            if (toy != null && !collectedToys.Contains(toy))
            {
                Transform targetTransform = collectedToys.Count == 0 ? this.transform : collectedToys[collectedToys.Count - 1].transform;
                toy.StartFollowing(targetTransform);
                collectedToys.Add(toy);
                Debug.Log($"おもちゃをゲット！ 現在の合計: {collectedToys.Count}個");
            }
        }

        // --- 障害物に当たった時の処理 ---
        if (other.CompareTag("Obstacle"))
        {
            DropHalfToys();
        }
    }

    public void DropHalfToys()
    {
        if (collectedToys.Count == 0) return;

        // 半分の数を計算（切り上げ）
        int dropCount = Mathf.CeilToInt(collectedToys.Count / 2f);
        Debug.Log($"障害物に衝突！ おもちゃを {dropCount} 個落とします！");

        for (int i = 0; i < dropCount; i++)
        {
            // リストの後ろ（しっぽ側）から順番に落とす
            int lastIndex = collectedToys.Count - 1;
            ToyFollower toyToDrop = collectedToys[lastIndex];

            // リストから外す
            collectedToys.RemoveAt(lastIndex);

            // おもちゃを物理的に散らばらせる
            toyToDrop.Drop();
        }

        // 落とさなかった残りのおもちゃ達の「追いかけるターゲット」を繋ぎ直す
        for (int i = 0; i < collectedToys.Count; i++)
        {
            Transform nextTarget = (i == 0) ? this.transform : collectedToys[i - 1].transform;
            collectedToys[i].StartFollowing(nextTarget);
        }
    }

    /// <summary>
    /// data.txt を読み込み、leftInput と rightInput を更新する
    /// </summary>
    void ReadSensorData()
    {
        if (!File.Exists(filePath)) return;

        try
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fileStream, Encoding.UTF8))
            {
                string line = reader.ReadLine();
                if (string.IsNullOrEmpty(line)) return;

                string[] data = line.Split(',');

                if (data.Length >= 2)
                {
                    // 文字列をfloatに変換
                    if (float.TryParse(data[0], out float rawLeft) && float.TryParse(data[1], out float rawRight))
                    {
                        // maxGyroValue を基準に数値を -1.0 〜 1.0 の範囲に正規化して代入
                        leftInput = Mathf.Clamp(rawLeft / maxGyroValue, -1f, 1f);
                        rightInput = Mathf.Clamp(rawRight / maxGyroValue, -1f, 1f);
                    }
                }
            }
        }
        catch (IOException)
        {
            // 読み込み衝突時の無視処理
        }
    }

    public List<ToyFollower> GetCollectedToys() => collectedToys;
    public void ClearCollectedToys() => collectedToys.Clear();
}