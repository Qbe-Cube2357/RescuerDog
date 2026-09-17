using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// 左右ハンドルの入力値を読み取る、シーン単位の入力コンポーネント。
/// シーンをまたいだ状態保持や、m5プロセスの起動管理は行わない。
/// </summary>
[DefaultExecutionOrder(-100)]
public sealed class HandleInputReader : MonoBehaviour
{
    [Header("Input Mode")]
    [SerializeField]
    private bool useSensorData = true;

    [Header("Sensor Settings")]
    [SerializeField, Min(0.001f)]
    private float maxGyroValue = 200f;

    [Tooltip("実機で左ハンドルの前後が逆だった場合に有効にする")]
    [SerializeField]
    private bool invertLeftSensor;

    [Tooltip("実機で右ハンドルの前後が逆だった場合に有効にする")]
    [SerializeField]
    private bool invertRightSensor;

    [Header("Keyboard Simulation")]
    [SerializeField]
    private KeyCode leftForwardKey = KeyCode.Q;

    [SerializeField]
    private KeyCode leftBackwardKey = KeyCode.A;

    [SerializeField]
    private KeyCode rightForwardKey = KeyCode.E;

    [SerializeField]
    private KeyCode rightBackwardKey = KeyCode.D;

    public float LeftInput { get; private set; }
    public float RightInput { get; private set; }

    public string FilePath => filePath;

    private string filePath;

    private void Awake()
    {
#if UNITY_EDITOR
        filePath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../external/data.txt")
        );
#else
        filePath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../../external/data.txt")
        );
#endif
    }

    private void Update()
    {
        if (useSensorData)
        {
            ReadSensorData();
        }
        else
        {
            ReadKeyboardInput();
        }
    }

    private void OnDisable()
    {
        LeftInput = 0f;
        RightInput = 0f;
    }

    private void ReadSensorData()
    {
        if (!File.Exists(filePath))
        {
            LeftInput = 0f;
            RightInput = 0f;
            return;
        }

        try
        {
            using FileStream stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite
            );

            using StreamReader reader = new StreamReader(stream);

            string line = reader.ReadLine();

            // 書き込み途中などで空になっている場合は、
            // 前回の正常な入力値を一時的に維持する。
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            string[] values = line.Split(',');

            if (values.Length < 2)
            {
                return;
            }

            bool leftParsed = float.TryParse(
                values[0],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float rawLeft
            );

            bool rightParsed = float.TryParse(
                values[1],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float rawRight
            );

            if (!leftParsed || !rightParsed)
            {
                return;
            }

            float normalizedLeft = Mathf.Clamp(
                rawLeft / maxGyroValue,
                -1f,
                1f
            );

            float normalizedRight = Mathf.Clamp(
                rawRight / maxGyroValue,
                -1f,
                1f
            );

            LeftInput = invertLeftSensor
                ? -normalizedLeft
                : normalizedLeft;

            RightInput = invertRightSensor
                ? -normalizedRight
                : normalizedRight;
        }
        catch (IOException)
        {
            // m5側と読み書きが競合した場合は、
            // 次のフレームで再度読み取る。
        }
    }

    private void ReadKeyboardInput()
    {
        LeftInput = ReadKeyboardAxis(
            leftForwardKey,
            leftBackwardKey
        );

        RightInput = ReadKeyboardAxis(
            rightForwardKey,
            rightBackwardKey
        );
    }

    private static float ReadKeyboardAxis(
        KeyCode forwardKey,
        KeyCode backwardKey
    )
    {
        float value = 0f;

        if (Input.GetKey(forwardKey))
        {
            value += 1f;
        }

        if (Input.GetKey(backwardKey))
        {
            value -= 1f;
        }

        return value;
    }
}