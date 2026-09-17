using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class BoostMoviePlayer : MonoBehaviour
{
    [SerializeField] private GameObject movieCanvas;
    [SerializeField] private GameObject opaqueBackground;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource movieAudioSource;
    [SerializeField] private VideoClip beamClip;
    [SerializeField] private VideoClip boostClip;
    [SerializeField] private float prepareTimeoutSeconds = 10f;

    private bool completed;
    private bool failed;

    private void Awake()
    {
        movieCanvas.SetActive(false);
        opaqueBackground.SetActive(false);

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;

        movieAudioSource.ignoreListenerPause = true;
        movieAudioSource.spatialBlend = 0f;

        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.controlledAudioTrackCount = 1;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, movieAudioSource);

        videoPlayer.loopPointReached += OnMovieFinished;
        videoPlayer.errorReceived += OnMovieError;

        // 最初に再生する透過動画だけ先に準備しておく
        if (beamClip != null)
        {
            videoPlayer.clip = beamClip;
            videoPlayer.Prepare();
        }
    }

    public IEnumerator Play()
    {
        movieCanvas.SetActive(true);

        if (beamClip != null)
        {
            // bi-mu は背景を隠さず、動画のアルファ越しにゲーム画面を見せる
            yield return StartCoroutine(PlayClip(beamClip, false));
        }
        else
        {
            Debug.LogWarning("ブースト前の bi-mu 動画が設定されていません。");
        }

        if (boostClip != null)
        {
            // inupati2 は従来どおり黒背景付きで再生する
            yield return StartCoroutine(PlayClip(boostClip, true));
        }
        else
        {
            Debug.LogWarning("ブースト動画の inupati2 が設定されていません。");
        }

        videoPlayer.Stop();
        opaqueBackground.SetActive(false);
        movieCanvas.SetActive(false);
    }

    private IEnumerator PlayClip(VideoClip clip, bool showOpaqueBackground)
    {
        completed = false;
        failed = false;

        opaqueBackground.SetActive(showOpaqueBackground);

        if (videoPlayer.clip != clip || !videoPlayer.isPrepared)
        {
            videoPlayer.Stop();
            videoPlayer.clip = clip;
            videoPlayer.Prepare();
        }

        float prepareDeadline = Time.realtimeSinceStartup + prepareTimeoutSeconds;
        while (!videoPlayer.isPrepared && !failed && Time.realtimeSinceStartup < prepareDeadline)
        {
            yield return null;
        }

        if (!videoPlayer.isPrepared && !failed)
        {
            failed = true;
            Debug.LogError($"動画の準備がタイムアウトしました: {clip.name}");
        }

        if (!failed)
        {
            videoPlayer.time = 0;
            videoPlayer.Play();

            yield return new WaitUntil(
                () => completed || failed
            );
        }
    }

    private void OnMovieFinished(VideoPlayer source)
    {
        completed = true;
    }

    private void OnMovieError(VideoPlayer source, string message)
    {
        failed = true;
        string clipName = source.clip != null ? source.clip.name : "未設定の動画";
        Debug.LogError($"動画の再生に失敗しました ({clipName}): {message}");
    }
}
