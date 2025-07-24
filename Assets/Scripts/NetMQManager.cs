using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System;

public class NetMQManager : MonoBehaviour
{
    private GazeCursorController gazeCursorController;
    private Thread listenerThread;
    private bool listenerRunning = true;
    private SubscriberSocket subscriberSocket;
    [SerializeField] public AppConfig appConfig;

    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    private string connectionAddress = "tcp://192.168.0.10:7788"; // Change as needed

    void Start()
    {
        Debug.Log("Starting ZMQ...");
        InitializeSubscriber();
        gazeCursorController = gameObject.GetComponent<GazeCursorController>();
    }

    private void InitializeSubscriber()
    {
        listenerThread = new Thread(ListenerLoop);
        listenerThread.IsBackground = true;
        listenerThread.Start();
    }

    private void Update()
    {
        while (messageQueue.TryDequeue(out string message))
        {
            ProcessMessage(message);
        }
    }

    private void ListenerLoop()
    {
        AsyncIO.ForceDotNet.Force(); // Ensure proper cleanup of sockets

        while (listenerRunning)
        {
            try
            {
                using (subscriberSocket = new SubscriberSocket())
                {
                    subscriberSocket.Options.Linger = TimeSpan.Zero;
                    subscriberSocket.Connect(connectionAddress);
                    subscriberSocket.SubscribeToAnyTopic();

                    Debug.Log("[NetMQ] Connected to " + connectionAddress);

                    while (listenerRunning)
                    {
                        if (subscriberSocket.TryReceiveFrameString(TimeSpan.FromMilliseconds(100), out string recv))
                        {
                            messageQueue.Enqueue(recv);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NetMQ] Connection failed or interrupted: " + ex.Message);
                Thread.Sleep(5000); // Wait before retrying
                Debug.Log("[NetMQ] Retrying connection...");
            }
        }

        NetMQConfig.Cleanup();
    }

    private void ProcessMessage(string message)
    {
        Debug.Log($"Received message: {message}");

        string[] parts = message.Split(':');
        if (parts.Length < 2) return;

        string topic = parts[0].Trim();
        string payload = parts[1].Trim();

        string[] subtopics = topic.Split('/');
        if (subtopics.Length < 1) return;

        if (int.TryParse(subtopics[0][4..], out int targetUserId))
        {
            if (!((gazeCursorController.amIPrimaryUser && targetUserId == 1) ||
                  (!gazeCursorController.amIPrimaryUser && targetUserId == 2)))
            {
                return;
            }
        }

        switch (subtopics[^1])
        {
            case "DataCollection":
                HandleDataCollectionSignal(payload);
                break;
            case "MyCursorVisual":
                if (int.TryParse(payload, out int user1Style))
                {
                    if (user1Style == 0)
                        gazeCursorController.hideMyCursor();
                    else
                        gazeCursorController.updateMyCursorStyle(user1Style - 1);
                }
                break;
            case "OtherCursorVisual":
                if (int.TryParse(payload, out int user2Style))
                {
                    if (user2Style == 0)
                        gazeCursorController.hideOtherCursor();
                    else
                        gazeCursorController.updateOtherCursorStyle(user2Style - 1);
                }
                break;
            case "CursorSize":
                if (float.TryParse(payload, out float cursorSize))
                    gazeCursorController.setCursorScale(cursorSize);
                break;
            case "AppOperation":
                appConfig.appOperation = payload == "Start";
                break;
            case "ArUcoOperation":
                appConfig.arUcoOperation = payload == "Start";
                break;
            case "GazeShareOperation":
                appConfig.gazeShareOperation = payload == "Start";
                break;
            case "GazeSaveOperation":
                appConfig.gazeSaveOperation = payload == "Start";
                break;
            default:
                Debug.LogWarning($"Unknown topic: {subtopics[^1]}");
                break;
        }
    }

    private void HandleDataCollectionSignal(string signal)
    {
        switch (signal)
        {
            case "Start Recording":
                gazeCursorController.startRecording();
                break;
            case "Stop Recording":
                gazeCursorController.stopRecording();
                break;
            default:
                Debug.LogWarning($"Unknown data collection signal: {signal}");
                break;
        }
    }

    private void OnDestroy()
    {
        listenerRunning = false;
        listenerThread?.Join();
        subscriberSocket?.Close();
        NetMQConfig.Cleanup(false);
    }

    private void OnDisable()
    {
        listenerRunning = false;
        listenerThread?.Join();
        subscriberSocket?.Close();
        NetMQConfig.Cleanup(false);
    }
}
