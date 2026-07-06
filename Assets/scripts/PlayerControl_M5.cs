using UnityEngine;
using System.IO;
using System.Text;

public class PlayerControl_M5 : MonoBehaviour
{
    [Header("Speed Settings")]
    public float walkSpeed = 4f;       // 通常（歩き）の最大速度
    public float runSpeed = 8f;        // 高速（走り）の最大速度
    public float rotateSpeed = 100f;   // 旋回速度の倍率

    [Header("M5Stick Input (File Read)")]
    [Range(-1f, 1f)] public float leftInput = 0f;
    [Range(-1f, 1f)] public float rightInput = 0f;

    [Header("Sensor Sensitivity")]
    [Tooltip("M5Stickから送られてくる角速度がどれくらいで最大値(1.0)になるかの基準値")]
    public float maxGyroValue = 200f;

    private Animator anim;
    private string filePath;

    void Start()
    {
        anim = GetComponent<Animator>();

        // RescuerDog/external/data.txt の絶対パスを生成
        // Application.dataPath は Project/Assets を指すため、一つ上の階層の external を指定
        filePath = Path.Combine(Application.dataPath, "../external/data.txt");

        // Debug.Log("Data file path: " + filePath);
    }

    void Update()
    {
        // 1. data.txt からセンサーデータを読み込む
        ReadSensorData();

        // 2. 左右の入力から「前進・後退」と「旋回」の成分を抽出
        float forwardComponent = (leftInput + rightInput) / 2f;
        float rotateComponent = leftInput - rightInput;

        // 3. 入力（回す速度）の強さに応じて、歩きか走りかを判定
        float inputIntensity = Mathf.Max(Mathf.Abs(leftInput), Mathf.Abs(rightInput));
        float currentMaxSpeed = walkSpeed;

        // 激しく回している（入力強度が0.8以上）なら走る
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
    /// data.txt を安全に読み込んで、leftInput と rightInput に割り当てる
    /// </summary>
    void ReadSensorData()
    {
        if (!File.Exists(filePath)) return;

        try
        {
            // C++側との同時書き込み・読み込みの衝突（エラー）を防ぐ設定でファイルを開く
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fileStream, Encoding.UTF8))
            {
                string line = reader.ReadLine();
                if (string.IsNullOrEmpty(line)) return;

                // カンマで分割 (加速度x,y,z, 角速度x,y,z)
                string[] data = line.Split(',');

                if (data.Length >= 6)
                {
                    // 今回は仮に「角速度Y (val[4])」をホイールの回転として使用
                    // M5Stickの持ち方や軸に合わせて、data[3]〜[5] のいずれかに変更してください
                    float gyroY = float.Parse(data[5]);

                    // 角速度の生データを -1.0 〜 1.0 の範囲に正規化（クランプ）
                    float normalizedInput = Mathf.Clamp(gyroY / maxGyroValue, -1f, 1f);

                    // M5Stickが1台の構成であるため、今回は仕様の確認用に
                    // 「傾き」や「もう一つの軸」、あるいは「C++側で2台分のデータを1行に書き出す」などの拡張が必要です。
                    // ここでは仮に、取得した1台分の回転データをそのままシミュレート用に分配します。
                    if (normalizedInput > 0)
                    {
                        // 時計回りに回している時：仕様「右を回すと前進＋左回り、左を回すと前進＋右回り」
                        // 1台のM5Stickの数値がプラスの時は、右ホイールが回っていると仮定してみるテスト用
                        rightInput = normalizedInput;
                        leftInput = 0f;
                    }
                    else if (normalizedInput < 0)
                    {
                        // 反時計回りの時
                        leftInput = Mathf.Abs(normalizedInput);
                        rightInput = 0f;
                    }
                    else
                    {
                        leftInput = 0f;
                        rightInput = 0f;
                    }
                }
            }
        }
        catch (IOException)
        {
            // ファイルが完全に使用中の場合は次のフレームで読み込むため、エラーを無視します
        }
    }
}