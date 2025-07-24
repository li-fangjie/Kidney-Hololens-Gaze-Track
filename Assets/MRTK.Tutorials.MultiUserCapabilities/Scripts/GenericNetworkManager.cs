using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace MRTK.Tutorials.MultiUserCapabilities
{
    public class GenericNetworkManager : MonoBehaviourPunCallbacks
    {
        public static GenericNetworkManager Instance;

        [HideInInspector] public string azureAnchorId = "";
        [HideInInspector] public PhotonView localUser;
        private bool isConnected;

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
            Debug.Log("Starting Photon Network Connection...");
            ConnectToNetwork();
        }

        private void ConnectToNetwork()
        {
            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.AutomaticallySyncScene = true;
                PhotonNetwork.ConnectUsingSettings();
                Debug.Log("Connecting to Photon...");
            }
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log("Connected to Master Server.");
            PhotonNetwork.JoinLobby();
            isConnected = true;
            OnReadyToStartNetwork?.Invoke();
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarning($"Disconnected from Photon: {cause}");
            isConnected = false;
            Invoke(nameof(Reconnect), 5f);
        }

        private void Reconnect()
        {
            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.ConnectUsingSettings();
                Debug.Log("Attempting reconnection...");
            }
        }

        private void Update()
        {
            if (!PhotonNetwork.IsConnected && isConnected)
            {
                Debug.LogWarning("Lost Photon connection. Attempting to reconnect...");
                isConnected = false;
                Reconnect();
            }
        }

        // Placeholder for future non-Photon networking
        private void StartNetwork(string ipAddress, string port)
        {
            throw new NotImplementedException();
        }
    }
}
