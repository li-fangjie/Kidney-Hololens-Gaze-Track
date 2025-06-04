using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace MRTK.Tutorials.MultiUserCapabilities
{
    public class PhotonLobby : MonoBehaviourPunCallbacks
    {
        public static PhotonLobby Lobby;

        private int roomNumber = 1;
        private int userIdCount;
        string roomName = "KidneyORRoom";
        string lobbyName = "KidneyORLobby";
        public Text debugText = null;
        string currentLobbyName;


        private void Awake()
        {
            if (Lobby == null)
            {
                Lobby = this;
            }
            else
            {
                if (Lobby != this)
                {
                    Destroy(Lobby.gameObject);
                    Lobby = this;
                }
            }

            DontDestroyOnLoad(gameObject);

            GenericNetworkManager.OnReadyToStartNetwork += StartNetwork;
        }

        private void CreateSpecificRoom()
        {
            TypedLobby customLobby = new TypedLobby(lobbyName, LobbyType.Default);

            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = 4;
            PhotonNetwork.CreateRoom(roomName, roomOptions, customLobby);
        }

        public override void OnConnectedToMaster()
        {
            var randomUserId = Random.Range(0, 9999999);
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.AuthValues = new AuthenticationValues();
            PhotonNetwork.AuthValues.UserId = randomUserId.ToString();
            userIdCount++;
            PhotonNetwork.NickName = SystemInfo.deviceName; // PhotonNetwork.AuthValues.UserId;

            if (PhotonNetwork.CurrentLobby == null)
            {
                currentLobbyName = "Lobby is Null";
                Debug.Log("Lobby is null");
            }
            else if (PhotonNetwork.CurrentLobby.Name == null)
            {
                currentLobbyName = "Lobby Name is Null";
                Debug.Log("Lobby.Name is null");
            }
            else
            {
                currentLobbyName = PhotonNetwork.CurrentLobby.Name;
            }
            //Debug.Log("Current Lobby: " + PhotonNetwork.CurrentLobby.Name);
            //Debug.Log("Lobby Type: " + PhotonNetwork.CurrentLobby.Type);

            //Debug.Log("Current Lobby: " + Lobby.name);
            //Debug.Log("Lobby Type: " + Lobby.GetType());
            //PhotonNetwork.JoinRandomRoom();

            TypedLobby customLobby = new TypedLobby(lobbyName, LobbyType.Default);
            PhotonNetwork.JoinLobby(customLobby);

            //RoomOptions roomOptions = new RoomOptions();
            //roomOptions.MaxPlayers = 4;
            //PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
        }

        public override void OnJoinedLobby()
        {

            if (PhotonNetwork.CurrentLobby == null)
            {
                currentLobbyName = "Lobby is Null";
                Debug.Log("Lobby is null");
            }
            else if (PhotonNetwork.CurrentLobby.Name == null)
            {
                currentLobbyName = "Lobby Name is Null";
                Debug.Log("Lobby.Name is null");
            }
            else
            {
                currentLobbyName = PhotonNetwork.CurrentLobby.Name;
            }

            currentLobbyName = PhotonNetwork.CurrentLobby.Name;
            Debug.Log("Joined Lobby: " + currentLobbyName);

            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = 4;

            PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, PhotonNetwork.CurrentLobby);
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();

            Debug.Log("\nPhotonLobby.OnJoinedRoom()");
            string currentRegion = PhotonNetwork.CloudRegion;
            Debug.Log("Connected to region: " + currentRegion);
            Debug.Log("Current room name: " + PhotonNetwork.CurrentRoom.Name);
            Debug.Log("Other players in room: " + PhotonNetwork.CountOfPlayersInRooms);
            Debug.Log("Total players in room: " + (PhotonNetwork.CountOfPlayersInRooms + 1));

            if (debugText != null)
            {
                debugText.text = "Connected to region: " + currentRegion + "\nLobby Name: " + currentLobbyName + "\nCurrent room name: " + PhotonNetwork.CurrentRoom.Name + "\n" + "Total players in room: " + (PhotonNetwork.CountOfPlayersInRooms + 1);
            }
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log("Join failed: " + message);
            //CreateRoom();
            CreateSpecificRoom();
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.Log("\nPhotonLobby.OnCreateRoomFailed()");
            Debug.LogError("Creating Room Failed");
            //CreateRoom();
            CreateSpecificRoom();
            // TODO: maybe set max tries, and quit the app if fails to create a specific room for too many times.
        }

        public override void OnCreatedRoom()
        {
            base.OnCreatedRoom();
            roomNumber++;
            Debug.Log("\nPhotonLobby.OnCreatedRoom()");
            Debug.Log("Current room name: " + PhotonNetwork.CurrentRoom.Name);
        }

        public void OnCancelButtonClicked()
        {
            PhotonNetwork.LeaveRoom();
        }

        private void StartNetwork()
        {
            PhotonNetwork.ConnectUsingSettings();
            Lobby = this;
        }

        private void CreateRoom()
        {
            var roomOptions = new RoomOptions { IsVisible = true, IsOpen = true, MaxPlayers = 10 };
            PhotonNetwork.CreateRoom("Room" + Random.Range(1, 3000), roomOptions);
        }
    }
}
