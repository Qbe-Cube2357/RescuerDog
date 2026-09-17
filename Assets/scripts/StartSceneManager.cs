using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text;
using UnityEngine.UI;

public class StartSceneManager : MonoBehaviour
{
    [Header("M5Stick Input (File Read)")]
    [Range(-1f, 1f)] public float leftInput = 0f;
    [Range(-1f, 1f)] public float rightInput = 0f;

    [Header("Sensor Sensitivity")]
    [Tooltip("M5Stickから送られてくる角速度がどれくらいで最大値(1.0)になるかの基準値")]
    public float maxGyroValue = 200f;
    public float timer1 = 0f;
    public float targetTime = 30f;
    public float nextscene = 300f;

    [Header("UI Settings")]
    public TMPro.TextMeshProUGUI targetText; // 点滅させたいテキスト
    public float timer2 = 0f;
    public float interval = 0.5f; // 点滅の間隔（秒）

    public AudioClip sound1;
    AudioSource audioSource;

    private Animator anim;
    private string filePath;
    public float sum = 0, s;

    void Start()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TMPro.TextMeshProUGUI>();
        }

        anim = GetComponent<Animator>();

        // RescuerDog/external/data.txt の絶対パスを生成
        // Application.dataPath は Project/Assets を指すため、一つ上の階層の external を指定
        filePath = Path.Combine(Application.dataPath, "../external/data.txt");

        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        //一定の時間が経ったら回転の合計をリセット
        timer2 += Time.deltaTime;
        if (s != sum)
        {
            s = sum;
            timer1 = 0f;
        }
        else
        {
            timer1 += Time.deltaTime;
        }

        if (timer2 >= interval && targetText != null)
        {
            targetText.enabled = !targetText.enabled;
            timer2 = 0f;
        }

        if (timer1 >= targetTime)
        {
            sum = 0;

            timer1 = 0f;
        }


        if (sum > nextscene || Input.GetKeyDown(KeyCode.Space))
        {
            //audioSource.PlayOneShot(sound1);
            //Debug.Log("play");

            //PlayScene1に遷移
            // SceneManager.LoadScene("MainScene");
            SceneManager.LoadScene("MenuScene");
        }


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

                if (data.Length >= 2)
                {
                    // 文字列をfloatに変換
                    if (float.TryParse(data[0], out float rawLeft) && float.TryParse(data[1], out float rawRight))
                    {
                        // maxGyroValue を基準に数値を -1.0 〜 1.0 の範囲に正規化して代入
                        leftInput = Mathf.Clamp(rawLeft / maxGyroValue, -1f, 1f);
                        rightInput = Mathf.Clamp(rawRight / maxGyroValue, -1f, 1f);
                    }

                    //どれだけ回転したか合計に加える
                    sum += Mathf.Abs(leftInput + rightInput) * Time.deltaTime;
                }
            }
        }
        catch (IOException)
        {
            // ファイルが完全に使用中の場合は次のフレームで読み込むため、エラーを無視します
        }
    }
}