using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("フェーズ管理")]
    [SerializeField]
    private PhaseManager phaseManager;

    // ゲームの状態を定義
    private enum GameState
    {
        Countdown,  // 開始前カウントダウン
        Playing,    // プレイ中（タイマー減少中）
        TimeUp,     // 終了演出中
        Ending      // シーン遷移中
    }
    private GameState currentState;

    // 外部のスクリプトから「現在プレイ中か」を確認するため
    public bool IsPlaying => currentState == GameState.Playing && !isBoostMoviePlaying;

    [Header("シーン設定")]
    public string resultSceneName = "ResultScene"; // 仕様に合わせてresultに変更

    [Header("UI設定 (TextMeshPro)")]
    public Image uiFill;
    public TextMeshProUGUI countdownText; // カウントダウンとTimeUpを表示するテキスト
    public TextMeshProUGUI timerText;     // 残り時間を表示するテキスト

    [Header("プレイヤー管理")]
    public GameObject mainPlayer;

    [Header("モブ生成制限")]
    public int initialMobCount = 10;

    [Header("スコア設定")]
    [SerializeField]
    private TextMeshProUGUI scoreText;

    public static int currentScore = 0;
    public int Score => currentScore;

    [Header("Cookie設定")]
    [SerializeField, Min(1)]
    private int scorePerCookie = 3;

    [SerializeField]
    private CookiePlusScoreUI cookiePlusScoreUI;

    [SerializeField, Min(0.1f)]
    private float cookieScoreCommitDelay = 2f;

    public static int currentCookieCount = 0;

    public int ScorePerCookie => scorePerCookie;
    public int CookieCount => currentCookieCount;

    // CookiePlusScoreUIに現在表示している加算予定値
    private int pendingCookieScore;

    // ScoreTextへ反映済みの値
    private int displayedScoreTarget;

    // ScoreTextのアニメーション途中の値
    private int animatedScoreValue;

    private Coroutine cookieScoreCommitRoutine;
    private Tweener scoreTextTween;

    [Header("ブースト演出設定")]
    public float boostStartDelay = 1.0f;
    private bool isStartingBoost = false;
    private PlayerControl_2 playerControl;

    [Header("ブーストゲージ管理")]
    public Image boostUiFill;
    public Image boostUiFill_Effect;
    private Tweener sequence_BS;
    public float currentBSValue = 0f;
    // UIに実際に表示している値
    private float displayBSValue = 0f;
    // ブースト開始中か
    private bool isBoosting = false;
    // ゲージ増加アニメーション中か
    private bool isValueUp_BS = false;
    // ゲージが満タンから0になるまでの時間
    public float boostDecreaseTime = 10f;

    [Header("ブーストUI演出設定")]
    public GameObject corgiFaceNormal;
    public GameObject corgiFaceExite;
    public GameObject corgiFaceBoost;
    private Sequence boostUiEffectSequence;
    private Vector3 boostUiDefaultScale;

    public GameObject boostModePrefab; // ブーストした時に描画するエフェクト
    public RectTransform boostFaceEffectRoot; // 描画するエフェクトの親
    public float boostFaceEffectDuration = 1.0f;
    private GameObject boostModeEffect; // 犬顔の後ろに映すエフェクト

    [Header("ブーストMovie演出設定")]
    [SerializeField] private BoostMoviePlayer boostMoviePlayer;

    private bool isBoostMoviePlaying;
    private float timeScaleBeforeMovie;
    private bool audioPauseBeforeMovie;


    [Header("BGM設定")]
    public AudioClip bgmClip;
    private AudioSource bgmAudioSource;

    private void Awake()
    {
        Instance = this;

        // BGM用AudioSourceを作成
        bgmAudioSource = gameObject.AddComponent<AudioSource>();
        bgmAudioSource.clip = bgmClip;
        bgmAudioSource.loop = true;
        bgmAudioSource.playOnAwake = false;

        playerControl = mainPlayer.GetComponent<PlayerControl_2>();
    }

    private void Start()
    {
        currentScore = 0;
        currentCookieCount = 0;

        pendingCookieScore = 0;
        displayedScoreTarget = 0;
        animatedScoreValue = 0;

        if (scoreText != null)
        {
            scoreText.SetText("{0:000000}", 0);
        }

        if (cookiePlusScoreUI != null)
        {
            cookiePlusScoreUI.HideImmediate();
        }

        boostUiDefaultScale = boostUiFill_Effect.transform.localScale;
        SetBoostFace(false);

        phaseManager?.PrepareForCountdown();
        StartCoroutine(GameLoopCoroutine());
    }

    // ゲームの流れをコントロールするコルーチン
    private IEnumerator GameLoopCoroutine()
    {
        // ----- 1. カウントダウン状態 -----
        currentState = GameState.Countdown;

        Animator countdownAnimator = countdownText.GetComponent<Animator>();

        countdownText.gameObject.SetActive(true);
        countdownAnimator.SetTrigger("CountDown");
        countdownText.text = "3";
        yield return new WaitForSeconds(1f);

        countdownAnimator.SetTrigger("CountDown");
        countdownText.text = "2";
        yield return new WaitForSeconds(1f);

        countdownAnimator.SetTrigger("CountDown");
        countdownText.text = "1";
        yield return new WaitForSeconds(1f);

        countdownAnimator.SetTrigger("CountDown");
        countdownText.text = "GO!";

        // ----- 2. プレイ中状態へ移行 -----
        currentState = GameState.Playing;
        phaseManager.BeginPhases();// 仕様：「タイマー起動」と同時に「toyPrefab生成」

        yield return new WaitForSeconds(1f);
        countdownText.gameObject.SetActive(false); // GO! を消す

        // BGM開始
        if (bgmClip != null)
        {
            bgmAudioSource.Play();
        }

        // プレイ中（Update関数でタイマーが減るのを受け持つため、ここではループを監視）
        while (currentState == GameState.Playing)
        {
            yield return null;
        }


        // ----- 3. タイムアップ状態 -----(Update側からcurrentStateがTimeUpに変えられてここに来ます)
        bgmAudioSource.Stop();

        countdownText.gameObject.SetActive(true);
        countdownText.text = "Time Up";

        // 仕様：「TimeUpとテキストが現れる（3秒）」
        yield return new WaitForSeconds(3.0f);

        while (PhotoBoothController.Instance != null && PhotoBoothController.Instance.IsBusy)
        {
            yield return null;
        }

        // ----- 4. リザルトシーンへ遷移 -----
        currentState = GameState.Ending;
        SceneManager.LoadScene(resultSceneName);
    }

    private void Update()
    {
        // プレイ中以外はタイマーを減らさない
        if (currentState != GameState.Playing) return;


        // ブースト中だったら、BSゲージを減らす
        if (isBoosting)
        {
            currentBSValue -= Time.deltaTime / boostDecreaseTime;

            // 0～1の範囲に制限
            currentBSValue = Mathf.Clamp01(currentBSValue);

            // UIに反映
            displayBSValue = currentBSValue;
            boostUiFill.fillAmount = displayBSValue;

            // ゲージが空になったらブースト終了
            if (currentBSValue <= 0f)
            {
                currentBSValue = 0f;
                displayBSValue = 0f;
                boostUiFill.fillAmount = 0f;

                isBoosting = false;

                SetBoostFace(false);
                StopBoostUiEffect();

                if (playerControl != null)
                {
                    playerControl.EndBoostMode();
                }
            }
        }
    }

    public void FinishGameFromPhaseManager()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        currentState = GameState.TimeUp;
    }

    // ----- Cookie獲得処理 -----

    public void GrantCookies(int cookieCount)
    {
        if (cookieCount <= 0)
        {
            return;
        }

        // 実際に獲得したCookie数と最終スコアは
        // 納品成立時点で確定しておく
        currentCookieCount += cookieCount;
        currentScore += cookieCount * scorePerCookie;
    }

    /// <summary>
    /// Cookieプレハブが1個、犬へ到着したときに呼ぶ。
    /// </summary>
    public void NotifyCookieArrived()
    {
        AddPendingCookieScore(scorePerCookie);
    }

    /// <summary>
    /// Cookie演出を開始できなかった場合に呼ぶ。
    /// </summary>
    public void NotifyCookiesWithoutEffect(int cookieCount)
    {
        if (cookieCount <= 0)
        {
            return;
        }

        AddPendingCookieScore(cookieCount * scorePerCookie);
    }

    private void AddPendingCookieScore(int amount)
    {
        pendingCookieScore += amount;

        if (cookiePlusScoreUI != null)
        {
            cookiePlusScoreUI.ShowScore(pendingCookieScore);
        }

        // 新しいCookieが来たので、確定までの待ち時間をリセット
        if (cookieScoreCommitRoutine != null)
        {
            StopCoroutine(cookieScoreCommitRoutine);
        }

        cookieScoreCommitRoutine =
            StartCoroutine(CommitCookieScoreRoutine());
    }

    private IEnumerator CommitCookieScoreRoutine()
    {
        yield return new WaitForSeconds(cookieScoreCommitDelay);

        cookieScoreCommitRoutine = null;

        CommitPendingCookieScore();
    }

    private void CommitPendingCookieScore()
    {
        if (pendingCookieScore <= 0)
        {
            return;
        }

        int scoreToCommit = pendingCookieScore;
        pendingCookieScore = 0;

        // ScoreTextに表示する累計値へ加算
        displayedScoreTarget += scoreToCommit;

        AnimateScoreText(displayedScoreTarget);

        if (cookiePlusScoreUI != null)
        {
            cookiePlusScoreUI.Hide();
        }
    }

    private void AnimateScoreText(int targetScore)
    {
        if (scoreTextTween != null &&
            scoreTextTween.IsActive())
        {
            scoreTextTween.Kill();
        }

        if (scoreText == null)
        {
            animatedScoreValue = targetScore;
            return;
        }

        scoreTextTween = DOTween.To(
            () => animatedScoreValue,
            value =>
            {
                animatedScoreValue = value;
                scoreText.SetText(
                    "{0:000000}",
                    animatedScoreValue
                );
            },
            targetScore,
            0.5f
        )
        .SetEase(Ease.OutQuad);
    }

    private IEnumerator StartBoostRoutine()
    {
        isStartingBoost = true;

        // ブースト突入前の予告動画
        yield return StartCoroutine(PlayBoostMovieRoutine());

        // ブースト突入前の予告演出
        boostUiFill_Effect.transform
            .DOScale(boostUiDefaultScale * 1.3f, 0.2f)
            .SetLoops(4, LoopType.Yoyo);

        yield return new WaitForSeconds(boostStartDelay);

        if (boostModePrefab != null || boostFaceEffectRoot != null)
        {
            boostModeEffect = Instantiate(boostModePrefab, boostFaceEffectRoot);

        }

        yield return new WaitForSeconds(boostFaceEffectDuration);

        isBoosting = true;
        isStartingBoost = false;

        SetBoostFace(true);
        StartBoostUiEffect();

        if (playerControl != null)
        {
            playerControl.StartBoostMode();
        }
    }

    // -----『ブーストゲージUI関連』-----

    public void AddBoostValue(float amount)
    {
        // すでにブースト中ならゲージを増やさない
        if (isBoosting)
        {
            return;
        }

        // 目標値を増加
        currentBSValue += amount / 100f;

        // 0～1に制限
        currentBSValue = Mathf.Clamp01(currentBSValue);

        // 現在の増加アニメーションを停止
        if (sequence_BS != null && sequence_BS.IsActive())
        {
            sequence_BS.Kill();
        }

        // 増加アニメーション開始
        ValueUpAnim();
    }

    void ValueUpAnim()
    {
        isValueUp_BS = true;

        sequence_BS = DOTween.To(
            () => displayBSValue,
            value =>
            {
                displayBSValue = value;
                boostUiFill.fillAmount = displayBSValue;
            },
            currentBSValue,
            0.5f
        )
        .SetEase(Ease.OutQuad)
        .OnComplete(() =>
        {
            isValueUp_BS = false;

            displayBSValue = currentBSValue;
            boostUiFill.fillAmount = displayBSValue;

            // 増加アニメーションが完全に終了してから
            // 満タンならブースト開始
            // if (currentBSValue >= 1f)
            // {
            //     isBoosting = true;
            //     StartCoroutine(StartBoostRoutine());
            // }

            if (currentBSValue >= 1f && !isBoosting && !isStartingBoost)
            {
                StartCoroutine(StartBoostRoutine());
            }
        });
    }

    // -----『ブースト中の顔画像UI関連』-----
    private void SetBoostFace(bool boost)
    {
        if (corgiFaceNormal != null)
        {
            corgiFaceNormal.SetActive(!boost);
        }

        if (corgiFaceExite != null)
        {
            corgiFaceExite.SetActive(false);
        }

        if (corgiFaceBoost != null)
        {
            corgiFaceBoost.SetActive(boost);
        }
    }

    private void StartBoostUiEffect()
    {
        if (boostUiEffectSequence != null && boostUiEffectSequence.IsActive())
        {
            boostUiEffectSequence.Kill();
        }

        boostUiEffectSequence = DOTween.Sequence()
            .Append(boostUiFill_Effect.transform.DOScale(boostUiDefaultScale * 1.2f, 0.25f))
            .Append(boostUiFill_Effect.transform.DOScale(boostUiDefaultScale, 0.25f))
            .SetLoops(-1, LoopType.Restart);
    }

    private void StopBoostUiEffect()
    {
        if (boostUiEffectSequence != null && boostUiEffectSequence.IsActive())
        {
            boostUiEffectSequence.Kill();
        }

        boostUiFill_Effect.transform.localScale = boostUiDefaultScale;

        // ビジュアルエフェクトを消す
        Destroy(boostModeEffect);

    }

    private void PlayBoostFaceEffect()
    {
        if (boostModePrefab == null || boostFaceEffectRoot == null)
        {
            return;
        }

        GameObject effect = Instantiate(boostModePrefab, boostFaceEffectRoot);
        // effect.transform.localPosition = Vector3.zero;
        // effect.transform.localRotation = Quaternion.identity;
        // effect.transform.localScale = Vector3.one;

        // Destroy(effect, boostFaceEffectDuration);
    }

    // -----『ブースト中の動画関連』-----
    private IEnumerator PlayBoostMovieRoutine()
    {
        if (boostMoviePlayer == null)
        {
            yield break;
        }

        isBoostMoviePlaying = true;

        timeScaleBeforeMovie = Time.timeScale;
        audioPauseBeforeMovie = AudioListener.pause;

        Time.timeScale = 0f;
        AudioListener.pause = true;

        yield return StartCoroutine(boostMoviePlayer.Play());

        AudioListener.pause = audioPauseBeforeMovie;
        Time.timeScale = timeScaleBeforeMovie;

        isBoostMoviePlaying = false;
    }
}
