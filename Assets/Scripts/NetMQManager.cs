using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System;
using System.Text;

public class NetMQManager : MonoBehaviour
{
    private GazeCursorController gazeCursorController;
    
    private Thread listenerThread;
    private bool listenerRunning = true;
    private SubscriberSocket subscriberSocket;

    private DealerSocket dealerSocket;
    private Thread dealerThread;

    private TimeSpan timeout;
    private string currentIdentity;

    [SerializeField] public AppConfig appConfig;
    [SerializeField] private GameObject selfGazeTracker;
    [SerializeField] private GameObject[] otherGazeTrackers;

    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    private Vector3 selfGazePosition;
    private int buildNumber;

    // Connection destinations
    private const string connectionIP = "tcp://127.0.0.1";
    private string connectionAddressPub = $"{connectionIP}:7788";
    private string connectionAddressRouter = $"{connectionIP}:7789";

    // Is connected to publisher
    private bool isConnectedPublisher = false;
    private bool isConnectedRouter = false;

    // Public events
    public static event Action OnNetMQConnected;
    public static event Action OnNetMQDisconnected;

    void Start()
    {
        Debug.Log("Starting ZMQ...");
        InitializeSubscriberAndDealer();
        gazeCursorController = gameObject.GetComponent<GazeCursorController>();
    }

    private void InitializeSubscriberAndDealer()
    {
        timeout = TimeSpan.FromMilliseconds(100);

        AsyncIO.ForceDotNet.Force();

        // Subscriber listener thread
        currentIdentity = SystemInfo.deviceName;
        listenerThread = new Thread(ListenerLoop);
        listenerThread.IsBackground = true;
        listenerThread.Start();

        // Dealer
        BuildAsset buildAsset = BuildAsset.LoadBuildInfo();
        buildNumber = buildAsset.BuildNumber;

        dealerThread = new Thread(SenderLoop);
        dealerThread.IsBackground = true;
        dealerThread.Start();
    }

    private void Update()
    {
        // Process incoming messages
        while (messageQueue.TryDequeue(out string message))
        {
            ProcessMessage(message);
        }

        selfGazePosition = selfGazeTracker.transform.position;
    }

    // Sends the self eye gaze postion through the dealer to the router
    private void SenderLoop()
    {
        using (dealerSocket = new DealerSocket())
        {
            dealerSocket.Options.Linger = TimeSpan.Zero;
            dealerSocket.Options.Identity = Encoding.ASCII.GetBytes(currentIdentity);
            dealerSocket.Connect(connectionAddressRouter);
           
            Debug.Log("[NetMQ] Connected to router " + connectionAddressRouter);

            // Handshake with build info
            dealerSocket.TrySendFrame(timeout, "BUILD: " + buildNumber.ToString());

            // Sending the current eye gaze position to the server
            while (listenerRunning)
            {
                try
                {
                    dealerSocket.TrySendFrame(timeout, selfGazePosition.ToString());
                }
                catch (Exception innerEx)
                {
                    Debug.LogError("[NetMQ] Router sending error: " + innerEx.Message);
                    break;
                }
            }
        }
    }

    // Listens to new topics coming out of the publisher
    private void ListenerLoop()
    {
        while (listenerRunning)
        {
            try
            {
                using (subscriberSocket = new SubscriberSocket())
                {
                    subscriberSocket.Options.Linger = TimeSpan.Zero;
                    subscriberSocket.Connect(connectionAddressPub);
                    subscriberSocket.SubscribeToAnyTopic();

                    Debug.Log("[NetMQ] Connected to publisher " + connectionAddressPub);
                    UpdateConnectionState(true); // Signal connected

                    while (listenerRunning && subscriberSocket != null)
                    {
                        try
                        {
                            if (subscriberSocket.TryReceiveFrameString(timeout, out string recv))
                            {
                                messageQueue.Enqueue(recv);
                            }
                        }
                        catch (Exception innerEx)
                        {
                            Debug.LogError("[NetMQ] Receive error: " + innerEx.Message);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NetMQ] Connection failed: " + ex.Message);
            }

            if (listenerRunning)
            {
                UpdateConnectionState(false); //  Signal disconnected
                Debug.Log("[NetMQ] Retrying connection in 3 seconds...");
                Thread.Sleep(3000);
            }
        }
    }

    private void UpdateConnectionState(bool connectedNow)
    {
        if (connectedNow != isConnectedPublisher)
        {
            isConnectedPublisher = connectedNow;
            if (isConnectedPublisher)
            {
                Debug.Log("[NetMQ] Connection established.");
                OnNetMQConnected?.Invoke();
            }
            else
            {
                Debug.LogWarning("[NetMQ] Connection lost.");
                OnNetMQDisconnected?.Invoke();
            }
        }
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
            case "Gaze":
                HandleIncomingGazeInfo(payload);
                break;
            default:
                Debug.LogWarning($"Unknown topic: {subtopics[^1]}");
                break;
        }
    }

    private void HandleIncomingGazeInfo(string signal)
    {
        if (signal.Length <= 1)
            return;

        string[] pairs = signal.Split(';'); // Each ID delinated by ;

        int i = 0;
        foreach (var pair in pairs)
        {
            if (pair.Length <= 1)
                continue;

            // Extract client identity and their gaze positions from the string
            var identityAndPosition = pair.Split('/');

            string identity = identityAndPosition[0];
            Vector3 position = StringToVector3(identityAndPosition[1]);

            // Check if the owner of this position isn't the same as this hololens
            if (identity != this.currentIdentity)
            {
                var gazeObject = otherGazeTrackers[i];
                gazeObject.transform.position = position;
                i++;
            }
        }
    }

    public static Vector3 StringToVector3(string sVector)
    {
        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector3 result = new Vector3(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]),
            float.Parse(sArray[2]));

        return result;
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
        Cleanup();
    }

    private void OnDisable()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        listenerRunning = false;
        listenerThread?.Join();
        dealerThread?.Join();
        subscriberSocket?.Close();
        subscriberSocket?.Dispose();
        dealerSocket?.Close();
        dealerSocket?.Dispose();
        
        NetMQConfig.Cleanup(false);
    }
}
