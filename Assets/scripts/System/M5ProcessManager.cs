using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

[DefaultExecutionOrder(-10000)]
public sealed class M5ProcessManager : MonoBehaviour
{
    private const float RestartIntervalSeconds = 3f;

    private static M5ProcessManager instance;

    private readonly ConcurrentQueue<string> errorLines =
        new ConcurrentQueue<string>();

    private Process m5Process;
    private float nextRestartTime;
    private bool quitting;
    private bool missingExecutableLogged;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject managerObject =
            new GameObject(nameof(M5ProcessManager));

        instance =
            managerObject.AddComponent<M5ProcessManager>();

        DontDestroyOnLoad(managerObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartM5Process();
    }

    private void Update()
    {
        while (errorLines.TryDequeue(out string line))
        {
            Debug.LogWarning("[M5 stderr] " + line);
        }

        if (quitting)
        {
            return;
        }

        if (m5Process != null && !IsProcessRunning())
        {
            int exitCode = TryGetExitCode();

            DisposeProcess();

            Debug.LogWarning(
                "[M5] プロセスが終了しました。ExitCode=" +
                exitCode +
                "。再起動を試みます。"
            );

            nextRestartTime =
                Time.unscaledTime + RestartIntervalSeconds;
        }

        if (m5Process == null &&
            Time.unscaledTime >= nextRestartTime)
        {
            StartM5Process();
        }
    }

    private void StartM5Process()
    {
        if (quitting || IsProcessRunning())
        {
            return;
        }

        string executablePath =
            ExternalPaths.SensorExecutablePath;

        string workingDirectory =
            ExternalPaths.ExternalDirectory;

        if (!File.Exists(executablePath))
        {
            if (!missingExecutableLogged)
            {
                Debug.LogError(
                    "[M5] 実行ファイルが見つかりません: " +
                    executablePath
                );

                missingExecutableLogged = true;
            }

            ScheduleRestart();
            return;
        }

        missingExecutableLogged = false;

        try
        {
            Directory.CreateDirectory(workingDirectory);

            if (!File.Exists(
                    ExternalPaths.SensorDataFilePath))
            {
                File.WriteAllText(
                    ExternalPaths.SensorDataFilePath,
                    "0,0\n"
                );
            }

            ProcessStartInfo startInfo =
                new ProcessStartInfo
                {
                    FileName = executablePath,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

            Process newProcess = new Process
            {
                StartInfo = startInfo
            };

            // m5.cppは値を標準出力へ頻繁に出すため、
            // 読み捨ててパイプ詰まりを防止する。
            newProcess.OutputDataReceived +=
                OnOutputDataReceived;

            newProcess.ErrorDataReceived +=
                OnErrorDataReceived;

            if (!newProcess.Start())
            {
                newProcess.Dispose();
                ScheduleRestart();
                return;
            }

            m5Process = newProcess;

            m5Process.BeginOutputReadLine();
            m5Process.BeginErrorReadLine();

            Debug.Log(
                "[M5] センサープロセスを起動しました: " +
                executablePath
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[M5] 起動に失敗しました: " +
                exception.Message
            );

            DisposeProcess();
            ScheduleRestart();
        }
    }

    private void OnOutputDataReceived(
        object sender,
        DataReceivedEventArgs eventArgs)
    {
        // 標準出力は読み捨てる。
    }

    private void OnErrorDataReceived(
        object sender,
        DataReceivedEventArgs eventArgs)
    {
        if (!string.IsNullOrWhiteSpace(eventArgs.Data))
        {
            errorLines.Enqueue(eventArgs.Data);
        }
    }

    private bool IsProcessRunning()
    {
        try
        {
            return m5Process != null &&
                   !m5Process.HasExited;
        }
        catch
        {
            return false;
        }
    }

    private int TryGetExitCode()
    {
        try
        {
            if (m5Process != null &&
                m5Process.HasExited)
            {
                return m5Process.ExitCode;
            }
        }
        catch
        {
            // 終了コードを取得できなくても終了処理は続行する。
        }

        return -1;
    }

    private void ScheduleRestart()
    {
        nextRestartTime =
            Time.unscaledTime + RestartIntervalSeconds;
    }

    private void OnApplicationQuit()
    {
        StopM5Process();
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        StopM5Process();
        instance = null;
    }

    private void StopM5Process()
    {
        quitting = true;

        if (m5Process == null)
        {
            return;
        }

        try
        {
            if (!m5Process.HasExited)
            {
                m5Process.Kill();

                if (!m5Process.WaitForExit(1000))
                {
                    Debug.LogWarning(
                        "[M5] 終了待機がタイムアウトしました。"
                    );
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[M5] 終了処理中のエラー: " +
                exception.Message
            );
        }
        finally
        {
            DisposeProcess();
        }
    }

    private void DisposeProcess()
    {
        if (m5Process == null)
        {
            return;
        }

        try
        {
            m5Process.OutputDataReceived -=
                OnOutputDataReceived;

            m5Process.ErrorDataReceived -=
                OnErrorDataReceived;

            m5Process.Dispose();
        }
        catch
        {
            // 終了時は破棄エラーを無視する。
        }

        m5Process = null;
    }
}