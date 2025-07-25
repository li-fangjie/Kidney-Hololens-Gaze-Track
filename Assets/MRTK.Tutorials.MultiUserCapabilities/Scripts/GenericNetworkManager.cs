using System;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace MRTK.Tutorials.MultiUserCapabilities
{
    public class GenericNetworkManager : MonoBehaviourPunCallbacks
    {
        public static GenericNetworkManager Instance;

        [HideInInspector] public string azureAnchorId = "";
        [HideInInspector] public PhotonView localUser;

        private bool isConnected;
        private bool reconnecting = false;
        private float reconnectDelay = 5f;
        private float maxReconnectDelay = 30f;

        public static event Action OnReadyToStartNetwork;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            Debug.Log("🔌 Starting Photon Network Connection...");
            ConnectToNetwork();
        }

        private void ConnectToNetwork()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.LogWarning("No internet connection available. Retrying in 10s...");
                Invoke(nameof(ConnectToNetwork), 10f);
                return;
            }

            if (!PhotonNetwork.IsConnected)
            {
                try
                {
                    PhotonNetwork.AutomaticallySyncScene = true;
                    PhotonNetwork.ConnectUsingSettings();
                    Debug.Log("Connecting to Photon...");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Connection error: {e.Message}");
                    ScheduleReconnect();
                }
            }
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log("Connected to Photon Master Server.");
            PhotonNetwork.JoinLobby();
            isConnected = true;
            reconnecting = false;
            reconnectDelay = 5f; // Reset delay after success
            OnReadyToStartNetwork?.Invoke();
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarning($"Disconnected from Photon: {cause}");

            isConnected = false;
            reconnecting = false;

            switch (cause)
            {
                case DisconnectCause.DnsExceptionOnConnect:
                    Debug.LogError("DNS resolution failed. Check Wi-Fi or DNS config.");
                    break;
                case DisconnectCause.ExceptionOnConnect:
                case DisconnectCause.Exception:
                    Debug.LogError("Exception during connection. Possibly network or firewall.");
                    break;
                case DisconnectCause.ServerTimeout:
                case DisconnectCause.ClientTimeout:
                    Debug.LogWarning("Timeout detected.");
                    break;
                case DisconnectCause.DisconnectByServerLogic:
                    Debug.LogWarning("Disconnected by server logic.");
                    break;
            }

            ScheduleReconnect();
        }

        private void ScheduleReconnect()
        {
            if (!reconnecting)
            {
                reconnecting = true;
                Debug.Log($"📡 Scheduling reconnect in {reconnectDelay} seconds...");
                Invoke(nameof(Reconnect), reconnectDelay);

                // Exponential backoff
                reconnectDelay = Mathf.Min(reconnectDelay * 2f, maxReconnectDelay);
            }
        }

        private void Reconnect()
        {
            reconnecting = false;

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.LogWarning("🌐 Still no internet. Will retry...");
                ScheduleReconnect();
                return;
            }

            if (!PhotonNetwork.IsConnected)
            {
                try
                {
                    if (PhotonNetwork.NetworkClientState == ClientState.Disconnected)
                    {
                        Debug.Log("🔄 Attempting Photon reconnection...");
                        PhotonNetwork.Reconnect();  // You can use ReconnectAndRejoin if you want to rejoin rooms
                    }
                    else
                    {
                        Debug.Log("⚠️ Skipped reconnect: Already connecting or connected.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Reconnect error: {e.Message}");
                    ScheduleReconnect();
                }
            }
        }

        private void Update()
        {
            // Catch unexpected disconnections
            if (!PhotonNetwork.IsConnected && isConnected)
            {
                Debug.LogWarning("⚠️ Photon unexpectedly disconnected. Triggering reconnect...");
                isConnected = false;
                ScheduleReconnect();
            }
        }

        private void StartNetwork(string ipAddress, string port)
        {
            throw new NotImplementedException();
        }
    }
}
