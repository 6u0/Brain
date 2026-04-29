using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using extOSC;
using UnityEngine;

public class OperationPanelOSC : MonoBehaviour
{
    [Header("OSC")]
    [SerializeField] private OSCReceiver receiver;
    [SerializeField] private OSCTransmitter transmitter;

    [Header("References")]
    [SerializeField] private ExperienceManager experienceManager;

    [Header("Operation Panel Auto Setup")]
    [SerializeField] private bool prepareOperationPanelOnPlay = true;
    [SerializeField] private bool autoLaunchPanelServer = true;
    [SerializeField] private bool autoOpenOperationPanel = true;
    [SerializeField] private string panelRelativePath = "../操作パネル";
    [SerializeField] private string panelStartFile = "index.html";
    [SerializeField] private string panelServerFile = "server.js";
    [SerializeField] private int panelServerPort = 8080;
    [SerializeField] private string fallbackUnityOscIp = "127.0.0.1";
    [SerializeField] private int defaultUnityOscPort = 9000;
    [SerializeField] private float panelLaunchTimeoutSeconds = 8f;
    [SerializeField] private float panelLaunchRetryIntervalSeconds = 1.2f;
    [SerializeField] private int panelLaunchMaxAttempts = 3;

    private bool oscPanelConnected;
    private float oscPanelLastPingTime;
    private readonly float oscPanelTimeout = 5f;
    private bool panelUrlOpened;
    private Process panelServerProcess;
    private bool hasLoggedWaitingPanel;
    private string resolvedPanelDirectory;
    private string resolvedLanAddress;
    private Coroutine prepareRoutine;

    private void Awake()
    {
        EnsureExperienceManagerReference();
    }

    private void Start()
    {
        LogStartupConfiguration();
        EnsureExperienceManagerReference();

        if (receiver != null)
        {
            receiver.Bind("/phase/start", OnStart);
            receiver.Bind("/phase/next", OnNext);
            receiver.Bind("/phase/prev", OnPrev);
            receiver.Bind("/phase/query", OnQuery);
            receiver.Bind("/panel/ping", OnPanelPing);
            UnityEngine.Debug.Log("[OperationPanelOSC] OSC bindings registered.");
        }
        else
        {
            UnityEngine.Debug.LogWarning("[OperationPanelOSC] OSCReceiver is not assigned.");
        }

        if (experienceManager != null)
        {
            experienceManager.PhaseChanged += HandlePhaseChanged;
            UnityEngine.Debug.Log("[OperationPanelOSC] ExperienceManager linked.");
        }
        else
        {
            UnityEngine.Debug.LogWarning("[OperationPanelOSC] ExperienceManager is not assigned.");
        }

        if (prepareOperationPanelOnPlay)
        {
            UnityEngine.Debug.Log("[OperationPanelOSC] Auto preparing operation panel on Play.");
            PrepareOperationPanel();
        }
    }

    private void Update()
    {
        if (oscPanelConnected && Time.time - oscPanelLastPingTime > oscPanelTimeout)
        {
            oscPanelConnected = false;
            SendConnectionStatus();
            UnityEngine.Debug.LogWarning("[OperationPanelOSC] Panel OSC connection timed out.");
        }
        else if (!oscPanelConnected && !hasLoggedWaitingPanel)
        {
            hasLoggedWaitingPanel = true;
            UnityEngine.Debug.Log("[OperationPanelOSC] Waiting for panel ping.");
        }
    }

    private void OnDestroy()
    {
        if (experienceManager != null)
        {
            experienceManager.PhaseChanged -= HandlePhaseChanged;
        }

        ShutdownOperationPanelServer();
    }

    public bool IsPanelConnected()
    {
        return oscPanelConnected;
    }

    public void PrepareOperationPanel()
    {
        if (prepareRoutine != null)
        {
            StopCoroutine(prepareRoutine);
        }

        prepareRoutine = StartCoroutine(PrepareOperationPanelRoutine());
    }

    private void OnPanelPing(OSCMessage message)
    {
        bool wasConnected = oscPanelConnected;
        oscPanelConnected = true;
        oscPanelLastPingTime = Time.time;
        hasLoggedWaitingPanel = false;

        if (!wasConnected)
        {
            UnityEngine.Debug.Log("[OperationPanelOSC] Panel ping received.");
        }

        SendAutoConfiguration();
        SendConnectionStatus();
    }

    private void OnStart(OSCMessage message)
    {
        if (experienceManager == null)
        {
            return;
        }

        if (experienceManager.currentPhase == ExperienceManager.Phase.Wait)
        {
            experienceManager.OnStartButtonPressed();
            return;
        }

        experienceManager.ForceNextPhase();
    }

    private void OnNext(OSCMessage message)
    {
        if (experienceManager == null)
        {
            return;
        }

        experienceManager.ForceNextPhase();
    }

    private void OnPrev(OSCMessage message)
    {
        if (experienceManager == null)
        {
            return;
        }

        experienceManager.ForcePrevPhase();
    }

    private void OnQuery(OSCMessage message)
    {
        if (experienceManager == null)
        {
            return;
        }

        SendPhase(experienceManager.currentPhase);
        SendAutoConfiguration();
        SendConnectionStatus();
    }

    private void HandlePhaseChanged(ExperienceManager.Phase phase)
    {
        SendPhase(phase);
        SendConnectionStatus();
    }

    private void SendPhase(ExperienceManager.Phase phase)
    {
        if (transmitter == null || experienceManager == null)
        {
            return;
        }

        string label = experienceManager.GetPhaseLabel(phase);
        var message = new OSCMessage("/phase/current");
        message.AddValue(OSCValue.String(label));
        transmitter.Send(message);
        UnityEngine.Debug.Log($"[OperationPanelOSC] Phase sent: {label}");
    }

    private void SendAutoConfiguration()
    {
        if (transmitter == null)
        {
            return;
        }

        string unityOscIp = GetPanelAccessibleHost();
        var config = new OSCMessage("/panel/auto_config");
        config.AddValue(OSCValue.String(unityOscIp));
        config.AddValue(OSCValue.Int(defaultUnityOscPort));
        config.AddValue(OSCValue.Int(panelServerPort));
        transmitter.Send(config);
        UnityEngine.Debug.Log($"[OperationPanelOSC] Auto OSC config sent: target={unityOscIp}:{defaultUnityOscPort}, panelPort={panelServerPort}");
    }

    private void SendConnectionStatus()
    {
        if (transmitter == null)
        {
            return;
        }

        var status = new OSCMessage("/panel/unity_status");
        status.AddValue(OSCValue.Int(1));
        status.AddValue(OSCValue.Int(oscPanelConnected ? 1 : 0));
        transmitter.Send(status);
    }

    private IEnumerator PrepareOperationPanelRoutine()
    {
        UnityEngine.Debug.Log("[OperationPanelOSC] Preparing operation panel.");

        bool panelReady = false;
        if (autoLaunchPanelServer)
        {
            for (int attempt = 1; attempt <= Mathf.Max(1, panelLaunchMaxAttempts); attempt++)
            {
                TryStartOperationPanelServer();
                yield return WaitForPanelHttpReady(panelLaunchTimeoutSeconds);
                panelReady = IsPanelTcpReadyNow();
                if (panelReady)
                {
                    break;
                }

                if (attempt < panelLaunchMaxAttempts)
                {
                    yield return new WaitForSeconds(panelLaunchRetryIntervalSeconds);
                }
            }
        }
        else
        {
            panelReady = IsPanelTcpReadyNow();
        }

        if (panelReady)
        {
            if (autoOpenOperationPanel)
            {
                OpenOperationPanelUi();
            }
        }
        else
        {
            UnityEngine.Debug.LogError("[OperationPanelOSC] Operation panel server did not become ready.");
        }

        SendAutoConfiguration();
        SendConnectionStatus();

        if (experienceManager != null)
        {
            SendPhase(experienceManager.currentPhase);
        }

        prepareRoutine = null;
    }

    private void TryStartOperationPanelServer()
    {
        if (IsPanelTcpReadyNow())
        {
            return;
        }

        if (panelServerProcess != null && !panelServerProcess.HasExited)
        {
            return;
        }

        string panelDir = ResolvePanelDirectory();
        string serverScriptPath = Path.Combine(panelDir, panelServerFile);
        if (!File.Exists(serverScriptPath))
        {
            UnityEngine.Debug.LogError($"[OperationPanelOSC] Server script was not found: {serverScriptPath}");
            return;
        }

        if (TryStartProcess("node", panelServerFile, panelDir))
        {
            return;
        }

        if (TryStartProcess("node.exe", panelServerFile, panelDir))
        {
            return;
        }

        TryStartProcess("cmd.exe", $"/c node \"{panelServerFile}\"", panelDir);
    }

    private void ShutdownOperationPanelServer()
    {
        if (panelServerProcess == null || panelServerProcess.HasExited)
        {
            return;
        }

        try
        {
            panelServerProcess.Kill();
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"[OperationPanelOSC] Failed to stop panel server: {e.Message}");
        }
    }

    private void OpenOperationPanelUi()
    {
        if (panelUrlOpened)
        {
            return;
        }

        string panelUrl = BuildPanelUrl();
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = panelUrl,
                UseShellExecute = true
            });
            panelUrlOpened = true;
            UnityEngine.Debug.Log($"[OperationPanelOSC] Panel URL opened: {panelUrl}");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"[OperationPanelOSC] Failed to open panel UI: {e.Message}");
        }
    }

    private string ResolvePanelDirectory()
    {
        if (!string.IsNullOrEmpty(resolvedPanelDirectory) && Directory.Exists(resolvedPanelDirectory))
        {
            return resolvedPanelDirectory;
        }

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string[] candidates =
        {
            Path.GetFullPath(Path.Combine(Application.dataPath, "Brain/操作パネル")),
            Path.GetFullPath(Path.Combine(Application.dataPath, panelRelativePath)),
            Path.GetFullPath(Path.Combine(Application.dataPath, "../操作パネル")),
            Path.GetFullPath(Path.Combine(projectRoot, "操作パネル")),
            Path.GetFullPath(Path.Combine(projectRoot, "Brain/操作パネル"))
        };

        foreach (string candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                resolvedPanelDirectory = candidate;
                return resolvedPanelDirectory;
            }
        }

        resolvedPanelDirectory = FindPanelDirectoryByFiles(Application.dataPath);
        if (!string.IsNullOrEmpty(resolvedPanelDirectory))
        {
            return resolvedPanelDirectory;
        }

        resolvedPanelDirectory = FindPanelDirectoryByFiles(projectRoot);
        if (!string.IsNullOrEmpty(resolvedPanelDirectory))
        {
            return resolvedPanelDirectory;
        }

        resolvedPanelDirectory = candidates[0];
        return resolvedPanelDirectory;
    }

    private string BuildPanelUrl()
    {
        return $"http://{GetPanelAccessibleHost()}:{panelServerPort}/{panelStartFile}";
    }

    private void LogStartupConfiguration()
    {
        string panelDir = ResolvePanelDirectory();
        UnityEngine.Debug.Log($"[OperationPanelOSC] panelDir={panelDir}");
        UnityEngine.Debug.Log($"[OperationPanelOSC] panelUrl={BuildPanelUrl()}");
        UnityEngine.Debug.Log($"[OperationPanelOSC] unityOsc={GetPanelAccessibleHost()}:{defaultUnityOscPort}");
    }

    private void OnPanelServerExited(object sender, EventArgs e)
    {
        if (panelServerProcess == null)
        {
            return;
        }

        UnityEngine.Debug.LogWarning($"[OperationPanelOSC] Panel server exited: code={panelServerProcess.ExitCode}");
    }

    private void OnPanelServerOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Data))
        {
            return;
        }

        UnityEngine.Debug.Log($"[OperationPanelServer] {e.Data}");
    }

    private void OnPanelServerErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Data))
        {
            return;
        }

        UnityEngine.Debug.LogError($"[OperationPanelServer] {e.Data}");
    }

    private bool TryStartProcess(string fileName, string arguments, string workingDirectory)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            panelServerProcess = Process.Start(psi);
            if (panelServerProcess == null)
            {
                return false;
            }

            panelServerProcess.EnableRaisingEvents = true;
            panelServerProcess.Exited += OnPanelServerExited;
            panelServerProcess.OutputDataReceived += OnPanelServerOutputDataReceived;
            panelServerProcess.ErrorDataReceived += OnPanelServerErrorDataReceived;
            panelServerProcess.BeginOutputReadLine();
            panelServerProcess.BeginErrorReadLine();
            return true;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"[OperationPanelOSC] Failed to start process {fileName}: {e.Message}");
            return false;
        }
    }

    private IEnumerator WaitForPanelHttpReady(float timeoutSeconds)
    {
        float elapsed = 0f;
        while (elapsed < timeoutSeconds)
        {
            if (IsPanelTcpReadyNow())
            {
                yield break;
            }

            elapsed += 0.2f;
            yield return new WaitForSeconds(0.2f);
        }
    }

    private bool IsPanelTcpReadyNow()
    {
        try
        {
            using (var client = new TcpClient())
            {
                var connectTask = client.ConnectAsync("127.0.0.1", panelServerPort);
                if (!connectTask.Wait(150))
                {
                    return false;
                }

                return client.Connected;
            }
        }
        catch
        {
            return false;
        }
    }

    private void EnsureExperienceManagerReference()
    {
        if (experienceManager != null)
        {
            return;
        }

        experienceManager = FindFirstObjectByType<ExperienceManager>();
        if (experienceManager != null)
        {
            UnityEngine.Debug.Log("[OperationPanelOSC] ExperienceManager was assigned automatically.");
        }
    }

    private string FindPanelDirectoryByFiles(string searchRoot)
    {
        if (string.IsNullOrEmpty(searchRoot) || !Directory.Exists(searchRoot))
        {
            return null;
        }

        try
        {
            foreach (string directory in Directory.EnumerateDirectories(searchRoot, "*", SearchOption.AllDirectories))
            {
                string serverPath = Path.Combine(directory, panelServerFile);
                string startFilePath = Path.Combine(directory, panelStartFile);
                if (File.Exists(serverPath) && File.Exists(startFilePath))
                {
                    return directory;
                }
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"[OperationPanelOSC] Panel directory search failed under {searchRoot}: {e.Message}");
        }

        return null;
    }

    private string GetPanelAccessibleHost()
    {
        if (!string.IsNullOrEmpty(resolvedLanAddress))
        {
            return resolvedLanAddress;
        }

        try
        {
            string hostName = Dns.GetHostName();
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);
            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily != AddressFamily.InterNetwork)
                {
                    continue;
                }

                string candidate = address.ToString();
                if (candidate.StartsWith("127.", StringComparison.Ordinal))
                {
                    continue;
                }

                resolvedLanAddress = candidate;
                return resolvedLanAddress;
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"[OperationPanelOSC] Failed to resolve LAN IP: {e.Message}");
        }

        resolvedLanAddress = fallbackUnityOscIp;
        return resolvedLanAddress;
    }
}
