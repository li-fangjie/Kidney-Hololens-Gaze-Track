using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using static UnityEngine.InputSystem.InputRemoting;

public class NetMQManager : MonoBehaviour
{
    private GazeCursorController gazeCursorController;
    private Thread listenerThread;
    private bool listenerRunning = true;
    private SubscriberSocket subscriberSocket;
    [SerializeField] public AppConfig appConfig;

    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();


    // Start is called before the first frame update
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
        using (subscriberSocket = new SubscriberSocket())
        {
            subscriberSocket.Connect("tcp://192.168.0.10:7788"); // Replace with the publisher's IP and port
            subscriberSocket.SubscribeToAnyTopic(); // Doing manual subscribing for now...

            while (listenerRunning)
            {
                try
                {
                    //string message = subscriberSocket.ReceiveFrameString();
                    //Debug.Log(message);

                    if (subscriberSocket.TryReceiveFrameString(out string recv))
                    {
                        print($"server recv: {recv}");
                        messageQueue.Enqueue(recv);
                        //_server.sendframe("world");
                    }
                    //ProcessMessage(message);
                }
                catch (NetMQException)
                {
                    // Handle socket closure or other issues gracefully
                }
            }
        }
        NetMQConfig.Cleanup();
    }

    private void ProcessMessage(string message)
    {
        Debug.Log($"Received message: {message}");

        // Parse topic and payload
        string[] parts = message.Split(':');
        if (parts.Length < 2) return; // Ignore invalid messages

        string topic = parts[0].Trim();
        string payload = parts[1].Trim();

        string[] subtopics = topic.Split("/");
        if (subtopics.Length < 1) return;

        if (int.TryParse(subtopics[0][4..], out int targetUserId))
        {
            Debug.Log("Received Command for User " + targetUserId);
            if (!((gazeCursorController.amIPrimaryUser && targetUserId == 1) || (!gazeCursorController.amIPrimaryUser && targetUserId == 2)))
            {
                return;
            }
        }
        else if (subtopics.Length != 1)
        {
            return;
        }

        //string myName = $"{gazeCursorController.getMyName()}";
        //if (!(subtopics[0].StartsWith(myName) || subtopics.Length == 1)) return;

        switch (subtopics[subtopics.Length - 1])
        {
            case "DataCollection":
                HandleDataCollectionSignal(payload);
                break;

            case "MyCursorVisual":
                if (int.TryParse(payload, out int user1Style))
                {
                    if (user1Style == 0)
                    {
                        gazeCursorController.hideMyCursor();
                    }
                    else
                    {
                        gazeCursorController.updateMyCursorStyle(user1Style - 1);
                    }
                }
                break;
            case "OtherCursorVisual":
                if (int.TryParse(payload, out int user2Style))
                {
                    if (user2Style == 0)
                    {
                        gazeCursorController.hideOtherCursor();
                    }
                    else
                    {
                        gazeCursorController.updateOtherCursorStyle(user2Style - 1);
                    }
                }
                break;
            case "CursorSize":
                if (float.TryParse(payload, out float cursorSize))
                    gazeCursorController.setCursorScale(cursorSize);
                break;
            case "AppOperation":
                if (payload == "Start")
                {
                    Debug.Log("Start Operation");
                    appConfig.appOperation = true;
                }
                else if (payload == "Stop")
                {
                    Debug.Log("End Operation");
                    appConfig.appOperation = false;
                }
                break;
            case "ArUcoOperation":
                if (payload == "Start")
                {
                    Debug.Log("Start Operation");
                    appConfig.arUcoOperation = true;
                }
                else if (payload == "Stop")
                {
                    Debug.Log("End Operation");
                    appConfig.arUcoOperation = false;
                }
                break;
            case "GazeShareOperation":
                if (payload == "Start")
                {
                    Debug.Log("Start Operation");
                    appConfig.gazeShareOperation = true;
                }
                else if (payload == "Stop")
                {
                    Debug.Log("End Operation");
                    appConfig.gazeShareOperation = false;
                }
                break;
            case "GazeSaveOperation":
                if (payload == "Start")
                {
                    Debug.Log("Start Operation");
                    appConfig.gazeSaveOperation = true;
                }
                else if (payload == "Stop")
                {
                    Debug.Log("End Operation");
                    appConfig.gazeSaveOperation = false;
                }
                break;
            default:
                Debug.LogWarning($"Unknown topic: {subtopics[subtopics.Length - 1]}");
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

    // Placeholder methods for implementing functionality
    private void startRecording()
    {
        Debug.Log("Start Recording called");
        // Implement start recording logic here
    }

    private void stopRecording()
    {
        Debug.Log("Stop Recording called");
        // Implement stop recording logic here
    }

    private void updateMyCursorStyle(int newIdx)
    {
        Debug.Log($"Update My Cursor Style to {newIdx}");
        // Implement my cursor style update logic here
    }

    private void updateOtherCursorStyle(int newIdx)
    {
        Debug.Log($"Update Other Cursor Style to {newIdx}");
        // Implement other cursor style update logic here
    }

    private void setCursorScale(float scale)
    {
        Debug.Log($"Set Cursor Scale to {scale}");
        // Implement cursor scaling logic here
    }

    private void OnDestroy()
    {
        listenerRunning = false;
        if (listenerThread != null)
        {
            listenerThread.Join();
        }

        subscriberSocket?.Close();
        NetMQConfig.Cleanup(false);
    }

    private void OnDisable()
    {
        listenerRunning = false;
        if (listenerThread != null)
        {
            listenerThread.Join();
        }
        subscriberSocket?.Close();
        NetMQConfig.Cleanup(false);
    }
}

//using UnityEngine;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using NetMQ;
//using NetMQ.Sockets;
//using UnityEngine.Assertions;
//using System.Net.Sockets;

//namespace MH
//{
//    public class NetMQTest : MonoBehaviour
//    {
//        public int _port = 7788;

//        public SubscriberSocket _server;

//        void Start()
//        {
//            Debug.Log("Running Force...");
//            AsyncIO.ForceDotNet.Force();
//            Debug.Log("Running New Time Span...");
//            NetMQConfig.Linger = new TimeSpan(0, 0, 1);

//            Debug.Log(" New Socket...");
//            _server = new SubscriberSocket();
//            Debug.Log("Server Options...");
//            _server.Options.Linger = new TimeSpan(0, 0, 1);
//            Debug.Log("Connecting...");
//            _server.Connect($"tcp://*:{_port}");
//            _server.SubscribeToAnyTopic();
//            print($"server on {_port}");

//            Assert.IsNotNull(_server);

//            StartCoroutine(_CoWorker());
//        }

//        void OnDisable()
//        {
//            _server?.Dispose();
//            NetMQConfig.Cleanup(false);
//        }

//        IEnumerator _CoWorker()
//        {
//            var wait = new WaitForSeconds(0f);

//            while (true)
//            {
//                //print("poll the recv...");
//                //var s_recv = _server.ReceiveFrameString();
//                //Debug.Log(s_recv);
//                if (_server.TryReceiveFrameString(out string recv))
//                {
//                    print($"server recv: {recv}");

//                    //_server.SendFrame("World");
//                }
//                else
//                {
//                    // print("no recv...");
//                }

//                yield return wait;
//            }
//        }

//    }
//}

//using UnityEngine;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using NetMQ;
//using NetMQ.Sockets;
//using UnityEngine.Assertions;

//namespace MH
//{
//    ///<summary>
//    /// test Req-Rep with NetMQ
//    ///</summary>
//    public class NetMQTest : MonoBehaviour
//    {
//        public int _port = 7788;

//        public RequestSocket _client;
//        public PublisherSocket _server;

//        public float _interval = 0.5f; // wait interval

//        private float _timer = 0;
//        private int _cnter = 1;

//        void Start()
//        {
//            AsyncIO.ForceDotNet.Force();
//            NetMQConfig.Linger = new TimeSpan(0, 0, 1);

//            //_server = new PublisherSocket();
//            //_server.Options.Linger = new TimeSpan(0, 0, 1);
//            //_server.Bind($"tcp://*:{_port}");
//            //Debug.Log($"server on {_port}");

//            _client = new RequestSocket();
//            _client.Options.Linger = new TimeSpan(0, 0, 1);
//            _client.Connect($"tcp://localhost:{_port}");
//            //_client.SubscribeToAnyTopic();
//            Debug.Log($"client connects {_port}");

//            // Assert.IsNotNull(_server);
//            Assert.IsNotNull(_client);
//        }

//        void OnDisable()
//        {
//            _client?.Dispose();
//            //_server?.Dispose();
//            NetMQConfig.Cleanup(false);
//        }

//        void OnDestroy()
//        {
//            _client?.Dispose();
//            //_server?.Dispose();
//            NetMQConfig.Cleanup(false);
//        }
//        void Update()
//        {
//            _timer += Time.deltaTime;
//            if (_timer >= _interval)
//            {
//                _timer = 0;
//                //var c_sent = $"Request {_cnter}";
//                //_client.SendFrame(c_sent);
//                //Debug.Log($"client sents: {c_sent}");

//                //var s_recv = _server.ReceiveFrameString();
//                //Debug.Log($"server receives {s_recv}");

//                //var s_sent = $"Response {_cnter}";
//                //_server.SendFrame(s_sent);
//                //Debug.Log($"Server sents: {s_sent}");

//                var c_recv = _client.ReceiveFrameString();
//                Debug.Log($"client receives {c_recv}");

//                _cnter++;
//            }
//        }
//    }
//}
