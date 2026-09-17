using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PhotoBoothController : MonoBehaviour
{
    public static PhotoBoothController Instance { get; private set; }

    [Header("DogCam")]
    [SerializeField]
    private DogCamProcessBridge dogCamBridge;

    [Header("UI")]
    [SerializeField]
    private GameObject uiRoot;

    [SerializeField]
    private TMP_Text statusText;

    [SerializeField]
    private RawImage previewImage;

    [SerializeField]
    private GameObject flashOverlay;

    [Header("時間設定")]
    [SerializeField, Min(1)]
    private int countdownStart = 3;

    [SerializeField, Min(0.1f)]
    private float countdownInterval = 1f;

    [SerializeField, Min(0.1f)]
    private float previewDuration = 1f;

    [SerializeField, Min(0.5f)]
    private float captureTimeout = 5f;

    [SerializeField, Min(0.1f)]
    private float errorDisplayDuration = 1.5f;

    public bool IsBusy { get; private set; }

    private bool captureResponseReceived;
    private bool captureSucceeded;
    private string capturedPath;
    private string captureError;
    private int pendingPhotoEvents;

    private Texture2D previewTexture;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError(
                "PhotoBoothControllerが複数存在します。"
            );

            enabled = false;
            return;
        }

        Instance = this;

        if (uiRoot != null)
        {
            uiRoot.SetActive(false);
        }

        if (previewImage != null)
        {
            previewImage.gameObject.SetActive(false);
        }

        if (flashOverlay != null)
        {
            flashOverlay.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (dogCamBridge == null)
        {
            return;
        }

        dogCamBridge.PhotoSaved += OnPhotoSaved;
        dogCamBridge.CaptureFailed += OnCaptureFailed;
    }

    private void OnDisable()
    {
        if (dogCamBridge == null)
        {
            return;
        }

        dogCamBridge.PhotoSaved -= OnPhotoSaved;
        dogCamBridge.CaptureFailed -= OnCaptureFailed;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        ReleasePreviewTexture();
    }

    public bool TryStartPhotoEvent()
    {
        pendingPhotoEvents++;
        TryStartNextPhotoEvent();
        return true;
    }

    private void TryStartNextPhotoEvent()
    {
        if (IsBusy || pendingPhotoEvents <= 0)
        {
            return;
        }

        pendingPhotoEvents--;
        IsBusy = true;

        StartCoroutine(PhotoEventRoutine());
    }

    private IEnumerator PhotoEventRoutine()
    {
        PrepareUI();

        for (int count = countdownStart; count >= 1; count--)
        {
            statusText.text = count.ToString();

            yield return new WaitForSecondsRealtime(
                countdownInterval
            );
        }

        statusText.text = "撮影中...";

        if (dogCamBridge == null || !dogCamBridge.IsReady)
        {
            yield return ShowErrorAndClose(
                "カメラを準備できませんでした"
            );

            yield break;
        }

        string photoDirectory = Path.Combine(
            Application.persistentDataPath,
            "Photos"
        );

        string fileName =
            "rescuerdog_" +
            DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") +
            ".jpg";

        string outputPath = Path.Combine(
            photoDirectory,
            fileName
        );

        captureResponseReceived = false;
        captureSucceeded = false;
        capturedPath = null;
        captureError = null;

        if (!dogCamBridge.RequestCapture(outputPath))
        {
            yield return ShowErrorAndClose(
                "撮影を開始できませんでした"
            );

            yield break;
        }

        if (flashOverlay != null)
        {
            flashOverlay.SetActive(true);

            yield return new WaitForSecondsRealtime(0.08f);

            flashOverlay.SetActive(false);
        }

        float elapsed = 0f;

        while (!captureResponseReceived &&
               elapsed < captureTimeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!captureResponseReceived)
        {
            yield return ShowErrorAndClose(
                "撮影がタイムアウトしました"
            );

            yield break;
        }

        if (!captureSucceeded)
        {
            yield return ShowErrorAndClose(
                "写真を保存できませんでした\n" +
                captureError
            );

            yield break;
        }

        if (!TryLoadPreview(capturedPath, out previewTexture))
        {
            yield return ShowErrorAndClose(
                "プレビューを読み込めませんでした"
            );

            yield break;
        }

        statusText.gameObject.SetActive(false);

        previewImage.texture = previewTexture;
        previewImage.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(
            previewDuration
        );

        FinishPhotoEvent();
    }

    private void PrepareUI()
    {
        ReleasePreviewTexture();

        uiRoot.SetActive(true);

        statusText.gameObject.SetActive(true);
        statusText.text = string.Empty;

        previewImage.texture = null;
        previewImage.gameObject.SetActive(false);

        if (flashOverlay != null)
        {
            flashOverlay.SetActive(false);
        }
    }

    private IEnumerator ShowErrorAndClose(string message)
    {
        statusText.gameObject.SetActive(true);
        statusText.text = message;

        if (previewImage != null)
        {
            previewImage.gameObject.SetActive(false);
        }

        yield return new WaitForSecondsRealtime(
            errorDisplayDuration
        );

        FinishPhotoEvent();
    }

    private bool TryLoadPreview(
        string filePath,
        out Texture2D loadedTexture)
    {
        loadedTexture = null;

        try
        {
            if (string.IsNullOrEmpty(filePath) ||
                !File.Exists(filePath))
            {
                return false;
            }

            byte[] imageBytes = File.ReadAllBytes(filePath);

            Texture2D texture = new Texture2D(
                2,
                2,
                TextureFormat.RGB24,
                false
            );

            if (!texture.LoadImage(imageBytes))
            {
                Destroy(texture);
                return false;
            }

            loadedTexture = texture;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[PhotoBooth] 読み込みエラー: " +
                exception.Message
            );

            return false;
        }
    }

    private void OnPhotoSaved(string savedPath)
    {
        if (!IsBusy)
        {
            return;
        }

        capturedPath = savedPath;
        captureSucceeded = true;
        captureResponseReceived = true;
    }

    private void OnCaptureFailed(string errorCode)
    {
        if (!IsBusy)
        {
            return;
        }

        captureError = errorCode;
        captureSucceeded = false;
        captureResponseReceived = true;
    }

    private void FinishPhotoEvent()
    {
        ReleasePreviewTexture();

        if (previewImage != null)
        {
            previewImage.texture = null;
            previewImage.gameObject.SetActive(false);
        }

        if (flashOverlay != null)
        {
            flashOverlay.SetActive(false);
        }

        if (uiRoot != null)
        {
            uiRoot.SetActive(false);
        }

        IsBusy = false;
        TryStartNextPhotoEvent();
    }

    private void ReleasePreviewTexture()
    {
        if (previewTexture == null)
        {
            return;
        }

        Destroy(previewTexture);
        previewTexture = null;
    }
}