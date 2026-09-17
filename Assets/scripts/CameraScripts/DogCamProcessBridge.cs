using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

public sealed class DogCamProcessBridge : MonoBehaviour
{
    [Header("dogcam設定")]
    [SerializeField]
    private string executableName = "cam.out";

    public bool IsReady { get; private set; }
    public bool IsCapturePending => capturePending;

    public event Action<string> PhotoSaved;
    public event Action<string> CaptureFailed;

    private Process dogCamProcess;

    private readonly ConcurrentQueue<string> standardOutputLines =
        new ConcurrentQueue<string>();

    private readonly ConcurrentQueue<string> standardErrorLines =
        new ConcurrentQueue<string>();

    private bool capturePending;
    private volatile bool shuttingDown;

    private void Start()
    {
        StartDogCamProcess();
    }

    private void Update()
    {
        while (standardOutputLines.TryDequeue(out string line))
        {
            HandleProtocolLine(line);
        }

        while (standardErrorLines.TryDequeue(out string line))
        {
            Debug.LogWarning("[DogCam stderr] " + line);
        }
    }

    public bool RequestCapture(string outputPath)
    {
        if (!IsReady || capturePending || !IsProcessRunning())
        {
            return false;
        }

        try
        {
            string directory = Path.GetDirectoryName(outputPath);

            if (string.IsNullOrEmpty(directory))
            {
                return false;
            }

            Directory.CreateDirectory(directory);

            capturePending = true;

            dogCamProcess.StandardInput.WriteLine(
                "CAPTURE|" + outputPath
            );

            dogCamProcess.StandardInput.Flush();
            return true;
        }
        catch (Exception exception)
        {
            capturePending = false;

            Debug.LogError(
                "[DogCam] 撮影命令を送信できませんでした: " +
                exception.Message
            );

            return false;
        }
    }

    private void StartDogCamProcess()
    {
        if (dogCamProcess != null)
        {
            return;
        }

        string dogCamDirectory = GetDogCamDirectory();
        string executablePath = Path.Combine(
            dogCamDirectory,
            executableName
        );

        if (!File.Exists(executablePath))
        {
            Debug.LogError(
                "[DogCam] 実行ファイルが見つかりません: " +
                executablePath
            );

            return;
        }

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = dogCamDirectory,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            dogCamProcess = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            dogCamProcess.OutputDataReceived += OnOutputDataReceived;
            dogCamProcess.ErrorDataReceived += OnErrorDataReceived;
            dogCamProcess.Exited += OnProcessExited;

            if (!dogCamProcess.Start())
            {
                Debug.LogError("[DogCam] 起動に失敗しました。");
                dogCamProcess.Dispose();
                dogCamProcess = null;
                return;
            }

            dogCamProcess.BeginOutputReadLine();
            dogCamProcess.BeginErrorReadLine();

            Debug.Log("[DogCam] プロセスを起動しました。");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[DogCam] 起動エラー: " +
                exception.Message
            );

            dogCamProcess?.Dispose();
            dogCamProcess = null;
        }
    }

    private string GetDogCamDirectory()
    {
#if UNITY_EDITOR
        // Unityプロジェクト直下のdogcam
        return Path.GetFullPath(
            Path.Combine(Application.dataPath, "../dogcam")
        );
#else
        // macOSビルドでは、RescuerDog.appと同じ場所に
        // dogcamフォルダを配置する想定
        return Path.GetFullPath(
            Path.Combine(Application.dataPath, "../../dogcam")
        );
#endif
    }

    private void OnOutputDataReceived(
        object sender,
        DataReceivedEventArgs eventArgs)
    {
        if (!string.IsNullOrEmpty(eventArgs.Data))
        {
            standardOutputLines.Enqueue(eventArgs.Data);
        }
    }

    private void OnErrorDataReceived(
        object sender,
        DataReceivedEventArgs eventArgs)
    {
        if (!string.IsNullOrEmpty(eventArgs.Data))
        {
            standardErrorLines.Enqueue(eventArgs.Data);
        }
    }

    private void OnProcessExited(object sender, EventArgs eventArgs)
    {
        if (!shuttingDown)
        {
            standardOutputLines.Enqueue(
                "ERROR|PROCESS_EXITED"
            );
        }
    }

    private void HandleProtocolLine(string line)
    {
        if (line == "READY")
        {
            IsReady = true;
            Debug.Log("[DogCam] READYを受信しました。");
            return;
        }

        const string savedPrefix = "SAVED|";

        if (line.StartsWith(savedPrefix, StringComparison.Ordinal))
        {
            capturePending = false;

            string savedPath =
                line.Substring(savedPrefix.Length);

            Debug.Log(
                "[DogCam] 写真を保存しました: " +
                savedPath
            );

            PhotoSaved?.Invoke(savedPath);
            return;
        }

        const string errorPrefix = "ERROR|";

        if (line.StartsWith(errorPrefix, StringComparison.Ordinal))
        {
            string errorCode =
                line.Substring(errorPrefix.Length);

            Debug.LogError(
                "[DogCam] エラーを受信しました: " +
                errorCode
            );

            if (errorCode == "PROCESS_EXITED")
            {
                IsReady = false;
            }

            // 撮影命令に直接対応するエラーのみ
            // 撮影失敗として通知する
            if (capturePending &&
                (errorCode == "SAVE_FAILED" ||
                 errorCode == "UNKNOWN_COMMAND" ||
                 errorCode == "PROCESS_EXITED"))
            {
                capturePending = false;
                CaptureFailed?.Invoke(errorCode);
            }

            return;
        }

        Debug.LogWarning(
            "[DogCam] 未知の標準出力: " + line
        );
    }

    private bool IsProcessRunning()
    {
        try
        {
            return dogCamProcess != null &&
                   !dogCamProcess.HasExited;
        }
        catch
        {
            return false;
        }
    }

    private void OnDestroy()
    {
        StopDogCamProcess();
    }

    private void OnApplicationQuit()
    {
        StopDogCamProcess();
    }

    private void StopDogCamProcess()
    {
        if (shuttingDown)
        {
            return;
        }

        shuttingDown = true;
        IsReady = false;

        if (dogCamProcess == null)
        {
            return;
        }

        try
        {
            if (!dogCamProcess.HasExited)
            {
                dogCamProcess.StandardInput.WriteLine("QUIT");
                dogCamProcess.StandardInput.Flush();

                if (!dogCamProcess.WaitForExit(1000))
                {
                    dogCamProcess.Kill();
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[DogCam] 終了処理エラー: " +
                exception.Message
            );
        }
        finally
        {
            dogCamProcess.Dispose();
            dogCamProcess = null;
        }
    }
}