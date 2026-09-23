using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text;

public class StoryManager : MonoBehaviour
{
    [Header("M5Stick Input (File Read)")]
    [Range(-1f, 1f)] public float leftInput = 0f;
    [Range(-1f, 1f)] public float rightInput = 0f;
    private string filePath;
    public float sum = 0, s;

    [Header("Sensor Sensitivity")]
    [Tooltip("M5Stickから送られてくる角速度がどれくらいで最大値(1.0)になるかの基準値")]
    public float maxGyroValue = 200f;
    public float timer1 = 0f;
    public float targetTime = 30f;
    public float next = 5f;

    [Header("紙芝居")]
    public Image b_1;
    public Image i_11;
    public Image i_12;
    public Image b_2;
    public Image i_21;
    public Image i_22;
    public Image i_23;
    public Image i_3;
    public Image b_4;
    public Image i_41;
    public Image i_42;
    public int ImoveSpeedB = -2000;
    public int ImoveSpeedTop = -1000;
    public int ImoveSpeedBottom = 1000;
    public int ImoveSpeedMR = -2000;
    public int ImoveSpeedMU = -500;

    [Header("BGM")]
    public AudioClip bgmClip;
    private AudioSource bgmAudioSource;
    public AudioClip[] seClip;
    private AudioSource seAudioSource;

    [Header("シーン遷移")]
    [SerializeField] private string targetScene = "MenuScene";

    int c = 0;
    int[] f = { 1, 1, 1, 1, 1, 1, 1, 1 };
    float timer = 0f;

    // Awake関数の中に引っ越ししました（ゲーム起動時に一番最初に実行されます）
    void Awake()
    {
        // BGM用AudioSourceを作成・設定
        bgmAudioSource = gameObject.AddComponent<AudioSource>();
        bgmAudioSource.clip = bgmClip;
        bgmAudioSource.loop = true;
        bgmAudioSource.playOnAwake = false;

        // BGM用AudioSourceを作成・設定
        seAudioSource = gameObject.AddComponent<AudioSource>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // BGM開始
        if (bgmClip != null)
        {
            bgmAudioSource.Play();
        }

        // RescuerDog/external/data.txt の絶対パスを生成
        // Application.dataPath は Project/Assets を指すため、一つ上の階層の external を指定
        filePath = Path.Combine(Application.dataPath, "../external/data.txt");
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (sum > next || Input.GetKeyDown(KeyCode.Space))
        {
            c++;
            timer = 0;
        }

        if (c == 0)
        {
            // 非表示にする
            b_2.gameObject.SetActive(false);
            i_21.gameObject.SetActive(false);
            i_22.gameObject.SetActive(false);
            i_23.gameObject.SetActive(false);
            i_3.gameObject.SetActive(false);
            b_4.gameObject.SetActive(false);
            i_41.gameObject.SetActive(false);
            i_42.gameObject.SetActive(false);


            // 表示にする場合
            //targetObject.SetActive(true);
            if (timer <= 1)
            {
                MoveImageY(b_1, ImoveSpeedB);
            }
            else if (1 <= timer && timer <= 2)
            {
                MoveImageY(i_11, ImoveSpeedTop);
            }
            else if (2 <= timer && timer <= 3)
            {
                PlaySE(0);
                MoveImageY(i_12, ImoveSpeedBottom);
            }
            else
            {
                PlaySE(1);
            }
        }
        else if (c == 1)
        {
            // 非表示にする
            b_1.gameObject.SetActive(false);
            i_11.gameObject.SetActive(false);
            i_12.gameObject.SetActive(false);

            // 表示にする場合
            b_2.gameObject.SetActive(true);
            i_21.gameObject.SetActive(true);
            i_22.gameObject.SetActive(true);
            i_23.gameObject.SetActive(true);

            if (timer <= 1)
            {
                MoveImageY(b_2, ImoveSpeedB);
            }
            if (1 <= timer && timer <= 2)
            {
                MoveImageY(i_21, ImoveSpeedTop);
            }
            else if (2 <= timer && timer <= 3)
            {
                PlaySE(2);

                // 現在の位置情報を取得
                Vector3 currentPos = i_22.transform.position;

                // Z座標だけを滑らかに増やす（移動スピードに応じて加算）
                currentPos.x += ImoveSpeedMR * Time.deltaTime;
                currentPos.y += ImoveSpeedMU * Time.deltaTime;

                // XとYはそのまま維持し、Z座標だけを適用する
                RectTransform rect = i_22.rectTransform;
                rect.anchoredPosition +=
                    new Vector2(ImoveSpeedMR, ImoveSpeedMU) * Time.deltaTime;
            }
            else if (3 <= timer && timer <= 4)
            {
                PlaySE(3);
                MoveImageY(i_23, ImoveSpeedBottom);
            }
            else if (4 <= timer && timer <= 5)
            {
                PlaySE(4);
            }
        }
        else if (c == 2)
        {

            // 非表示にする
            b_2.gameObject.SetActive(false);
            i_21.gameObject.SetActive(false);
            i_22.gameObject.SetActive(false);
            i_23.gameObject.SetActive(false);

            // 表示にする場合
            i_3.gameObject.SetActive(true);

            if (timer <= 1)
            {
                MoveImageY(i_3, ImoveSpeedB);
            }
            else
            {
                PlaySE(5);
            }
        }
        else if (c == 3)
        {
            // 非表示にする
            i_3.gameObject.SetActive(false);

            // 表示にする場合
            b_4.gameObject.SetActive(true);
            i_41.gameObject.SetActive(true);
            i_42.gameObject.SetActive(true);

            if (timer <= 1)
            {
                MoveImageY(b_4, ImoveSpeedB);
            }
            else if (1 <= timer && timer <= 2)
            {
                MoveImageY(i_41, ImoveSpeedTop);
            }
            else if (2 <= timer && timer <= 3)
            {
                PlaySE(6);
                MoveImageY(i_42, ImoveSpeedBottom);
            }
            else
            {
                PlaySE(7);
            }
        }
        else if (c == 4)
        {
            // 選択されたシーンを読み込む
            SceneManager.LoadScene(targetScene);
        }








        //犬かきハンドルによる操作
        //一定の時間が経ったら回転の合計をリセット
        if (s != sum)
        {
            s = sum;
            timer1 = 0f;
        }
        else
        {
            timer1 += Time.deltaTime;
        }

        if (timer1 >= targetTime)
        {
            sum = 0;

            timer1 = 0f;
        }

        //M5取得  sum > nextscene || Input.GetKeyDown(KeyCode.Space)
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


    void MoveImageY(Image targetImage, float speed)
    {
        if (targetImage == null) return;

        RectTransform rect = targetImage.rectTransform;
        rect.anchoredPosition += Vector2.up * speed * Time.deltaTime;
    }

    // 共通のSE再生用関数
    void PlaySE(int seIndex)
    {
        if (f[seIndex] == 1)
        {
            f[seIndex] = 0;
            if (seClip != null && seClip.Length > seIndex && seClip[seIndex] != null)
            {
                seAudioSource.PlayOneShot(seClip[seIndex]);
            }
        }
    }


}