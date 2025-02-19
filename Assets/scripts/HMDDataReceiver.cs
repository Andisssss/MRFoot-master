using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class HMDDataReceiver : MonoBehaviour
{
    public static HMDDataReceiver Instance { get; private set; }

    private TcpListener _listener;
    private TcpClient _client;
    private NetworkStream _stream;
    private bool _isConnected = false;
    public bool IsClientConnected => _isConnected;

    [Header("Server Settings")]
    [Tooltip("Server IP Address (e.g., 127.0.0.1)")]
    public string serverIp = "127.0.0.1";
    [Tooltip("Server Port")]
    public int serverPort = 9001;

    [Header("Debug / Visual Objects")]
    [Tooltip("Debug Sphere for connection status")]
    public Transform debugSphere;
    [Tooltip("Left Foot object for zone updates")]
    public Transform leftFoot;
    [Tooltip("Right Foot object for zone updates")]
    public Transform rightFoot;

    private static readonly Queue<Action> mainThreadActions = new Queue<Action>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // prevent duplicate instances
            return;
        }
        Instance = this;
    }

    void Start()
    {
        StartServer();
    }

    void Update()
    {
        {
            // process queued actions in case of problem with client taking too much main thread
            lock (mainThreadActions)
            {
                while (mainThreadActions.Count > 0)
                {
                    mainThreadActions.Dequeue().Invoke();
                }
            }
            // process incoming data only if a client is connected.
            if (_isConnected && _stream != null && _stream.DataAvailable)
            {
                try
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.Log($"Saòemtais JSON: {receivedData}");
                    ParseAndUpdateVisualization(receivedData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Kïûda datu saòemðanâ: {ex.Message}");
                }
            }
        }
    }

    public static void RunOnMainThread(Action action)
    {
        if (action == null) return;
        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
        }
    }

    private void OnApplicationQuit()
    {
        DisconnectFromServer();
    }
    private void LogToFile(string message)
    {
        string path = Application.persistentDataPath + "/server_log.txt";
        using (StreamWriter writer = new StreamWriter(path, true))
        {
            writer.WriteLine($"{System.DateTime.Now}: {message}");
        }
    }

    private void StartServer()
    {
        try
        {
            LogToFile("Server starting on port " + serverPort);
            _listener = new TcpListener(IPAddress.Any, serverPort);
            _listener.Start();
            LogToFile("Server started on port " + serverPort);
            _listener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), _listener);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error starting the server: {ex.Message}");
        }
    }


    private void OnClientConnect(IAsyncResult result)
    {
        try
        {
            if (_client != null) // prevent multiple connections
            {
                Debug.LogWarning("Already connected to a client, rejecting new connection.");
                return;
            }

            _client = _listener.EndAcceptTcpClient(result);
            _stream = _client.GetStream();
            _isConnected = true;

            RunOnMainThread(() =>
            {
                Debug.Log("Klients savienots ar HMD.");
                if (debugSphere != null)
                {
                    var debugRenderer = debugSphere.GetComponent<Renderer>();
                    if (debugRenderer != null)
                        debugRenderer.material.color = Color.blue;
                }
            });

            // accept the next client connection if needed
            _listener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), _listener);
        }
        catch (Exception ex)
        {
            RunOnMainThread(() =>
            {
                Debug.LogError($"Kïûda pieòemot klienta savienojumu: {ex.Message}");
            });
        }
    }


    private void DisconnectFromServer()
    {
        if (_stream != null)
        {
            _stream.Close();
            _stream = null;
        }
        if (_client != null)
        {
            _client.Close();
            _client = null;
        }
        _isConnected = false;
        Debug.Log("Atvienots no servera.");
    }

    /// <summary>
    /// Parses the incoming JSON based on a MessageType field and routes it accordingly.
    /// </summary>
    /// <param name="jsonData">The JSON data received from the client.</param>
    private void ParseAndUpdateVisualization(string jsonData)
    {
        // First, try to parse the base message to know its type.
        BaseMessage baseMsg = JsonUtility.FromJson<BaseMessage>(jsonData);
        if (baseMsg == null || string.IsNullOrEmpty(baseMsg.MessageType))
        {
            Debug.LogWarning("Nederîgi ziòojumi!");
            return;
        }
        if (baseMsg.MessageType == "Feedback")
        {
            FeedbackMessage feedback = JsonUtility.FromJson<FeedbackMessage>(jsonData);
            UpdateFootFeedback(feedback);
        }
        else if (baseMsg.MessageType == "ExerciseConfig")
        {
            ExerciseConfigMessage config = JsonUtility.FromJson<ExerciseConfigMessage>(jsonData);
            GameManager.Instance?.UpdateExerciseConfiguration(config);
        }
        else if (baseMsg.MessageType == "Command")
        {
            CommandData cmdData = JsonUtility.FromJson<CommandData>(jsonData);
            ProcessCommand(cmdData);
        }
    }

    /// <summary>
    /// Updates the visual feedback on the appropriate foot based on the feedback message.
    /// </summary>
    /// <param name="feedback">The feedback message containing zone and foot info.</param>
    private void UpdateFootFeedback(FeedbackMessage feedback)
    {
        Debug.Log($"Saòemts feedback: Foot: {feedback.Foot}, Zone: {feedback.Zone}");
        if (feedback.Foot.ToLower() == "left")
        {
            GameManager.Instance?.UpdateFootStatusForFoot(feedback.Zone, "Left");
        }
        else if (feedback.Foot.ToLower() == "right")
        {
            GameManager.Instance?.UpdateFootStatusForFoot(feedback.Zone, "Right");
        }
        else if (feedback.Foot.ToLower() == "both")
        {
            GameManager.Instance?.UpdateFootStatusForFoot(feedback.Zone, "Both");
        }
    }

    /// <summary>
    /// Processes command messages sent from the client.
    /// </summary>
    /// <param name="cmdData">The command data.</param>
    private void ProcessCommand(CommandData cmdData)
    {
        Debug.Log($"Saòemta komanda: {cmdData.Command}");
        switch (cmdData.Command)
        {
            case "PREPARATION_SUCCESS":
                GameManager.Instance?.MarkPreparationSuccessful();
                break;
            case "RESTART_EXERCISE":
                GameManager.Instance?.RequestExerciseRestart();
                break;
            default:
                Debug.LogWarning("Nezinâma komanda!");
                break;
        }
    }

    /// <summary>
    /// sends a command to the client (debug only)
    /// </summary>
    /// <param name="command">The command string to send.</param>
    public void SendCommandToClient(string command)
    {
        try
        {
            if (_stream != null && _client.Connected)
            {
                byte[] commandBytes = Encoding.UTF8.GetBytes(command);
                _stream.Write(commandBytes, 0, commandBytes.Length);
                _stream.Flush();
                Debug.Log($"Nosûtîta komanda klientam: {command}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Kïûda, sûtot komandu: {ex.Message}");
        }
    }

    // --------------------- JSON Message Classes ---------------------
    [Serializable]
    public class BaseMessage
    {
        public string MessageType;
    }

    [Serializable]
    public class FeedbackMessage
    {
        public string MessageType;
        public int ExerciseID;
        public string Foot;
        public int Zone;
    }

    [Serializable]
    public class ExerciseConfigMessage
    {
        public string MessageType;
        public int ExerciseID;
        public string Name;
        public string LegsUsed;
        public int Intro;
        public int Demo;
        public int PreparationCop;
        public int TimingCop;
        public int Release;
        public int Switch;
        public int Sets;
        public ZoneSequenceItem[] ZoneSequence;
    }

    [Serializable]
    public class ZoneSequenceItem
    {
        public int Duration;
        public Vector2 GreenZoneX;
        public Vector2 GreenZoneY;
        public Vector2 RedZoneX;
        public Vector2 RedZoneY;
    }

    [Serializable]
    public class CommandData
    {
        public string Command;
    }
}
