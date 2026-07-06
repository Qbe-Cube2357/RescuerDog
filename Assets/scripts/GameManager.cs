using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // TextMeshProを使うために追加

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ゲームの状態を定義
    private enum GameState
    {
        Countdown,  // 開始前カウントダウン
        Playing,    // プレイ中（タイマー減少中）
        TimeUp,     // 終了演出中
        Ending      // シーン遷移中
    }
    private GameState currentState;

    [Header("シーン設定")]
    public string resultSceneName = "result"; // 仕様に合わせてresultに変更

    [Header("UI設定 (TextMeshPro)")]
    public Image uiFill;
    public TextMeshProUGUI countdownText; // カウントダウンとTimeUpを表示するテキスト
    public TextMeshProUGUI timerText;     // 残り時間を表示するテキスト

    [Header("プレイヤー管理")]
    public GameObject mainPlayer;

    [Header("敵の管理")]
    public GameObject[] toyPrefab;
    public GameObject mobPrefab;
    public GameObject bossPrefab;
    public Transform[] spawnPoints;

    [Header("ステージ状態の管理")]
    public float CountTime = 60f; // 60秒からカウントダウンするように変更
    private float gameTimer = 0;

    [Header("モブ生成制限")]
    public int initialMobCount = 10;

    [Header("スコア設定")]
    public TextMeshProUGUI scoreText; // スコアを表示するUIテキスト
    private int currentScore = 0;     // 現在の獲得ポイント

    private void Awake()
    {
        // シングルトンの設定
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        gameTimer = CountTime;

        // 最初はカウントダウン状態からスタート
        StartCoroutine(GameLoopCoroutine());

        UpdateScoreUI(); // 初期スコア（0）を表示
        StartCoroutine(GameLoopCoroutine());
    }

    // ゲームの流れをコントロールするコルーチン
    private IEnumerator GameLoopCoroutine()
    {
        // ----- 1. カウントダウン状態 -----
        currentState = GameState.Countdown;
        timerText.text = "60"; // 初期表示

        countdownText.gameObject.SetActive(true);
        countdownText.text = "3";
        yield return new WaitForSeconds(1f);
        countdownText.text = "2";
        yield return new WaitForSeconds(1f);
        countdownText.text = "1";
        yield return new WaitForSeconds(1f);

        countdownText.text = "GO!";

        // ----- 2. プレイ中状態へ移行 -----
        currentState = GameState.Playing;
        Spawntoy(); // 仕様：「タイマー起動」と同時に「toyPrefab生成」

        yield return new WaitForSeconds(1f);
        countdownText.gameObject.SetActive(false); // GO! を消す

        // プレイ中（Update関数でタイマーが減るのを受け持つため、ここではループを監視）
        while (currentState == GameState.Playing)
        {
            yield return null;
        }

        // ----- 3. タイムアップ状態 -----
        // (Update側からcurrentStateがTimeUpに変えられてここに来ます)
        countdownText.gameObject.SetActive(true);
        countdownText.text = "Time Up";

        // 仕様：「TimeUpとテキストが現れる（3秒）」
        yield return new WaitForSeconds(3.0f);

        // ----- 4. リザルトシーンへ遷移 -----
        currentState = GameState.Ending;
        SceneManager.LoadScene(resultSceneName);
    }

    private void Update()
    {
        // プレイ中以外はタイマーを減らさない
        if (currentState != GameState.Playing) return;

        // タイマーを減らす
        gameTimer -= Time.deltaTime;

        // タイマーUIの更新（少数第1位まで表示）
        if (gameTimer < 0) gameTimer = 0;
        uiFill.fillAmount = Mathf.InverseLerp(0, 60, gameTimer);
        timerText.text = $"{gameTimer:F0}";

        // タイマーが0になったら
        if (gameTimer <= 0f)
        {
            currentState = GameState.TimeUp; // 状態をTimeUpに変更（コルーチン側が動き出す）
        }
    }


    // --- 生成ロジック ---
    void Spawntoy()
    {
        if (spawnPoints.Length == 0 || toyPrefab.Length == 0) return;

        for (int i = 0; i < toyPrefab.Length; i++)
        {
            Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Instantiate(toyPrefab[i], sp.position, sp.rotation);
        }
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {currentScore}";
        }
    }

    // （※既存のSpawnMobなどのコードは省略していますが、必要に応じてそのまま残して大丈夫です）
}
