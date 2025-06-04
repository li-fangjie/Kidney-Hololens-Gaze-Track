using Photon.Pun;
using UnityEngine;

namespace MRTK.Tutorials.MultiUserCapabilities
{
    public class PhotonUser : MonoBehaviour
    {
        private PhotonView pv;
        private string username;

        private int user_num = 0;

        private void Start()
        {
            pv = GetComponent<PhotonView>();

            if (!pv.IsMine) return;
            GameObject temp_user_name;
            do
            {
                username = "User" + PhotonNetwork.NickName + "_" + user_num.ToString();
                temp_user_name = GameObject.Find(username);
                user_num = user_num + 1;
            } while (temp_user_name != null);

            //GameObject temp_user_name = GameObject.Find("User" + PhotonNetwork.NickName);
            //if (temp_user_name == null)
            //{
            //    username = "User" + PhotonNetwork.NickName;
            //}
            //else
            //{
            //    while (temp_user_name != null)
            //    {
            //        //user_num = int.Parse(PhotonNetwork.NickName);
            //        user_num = user_num + 1;
            //        username = "User" + PhotonNetwork.NickName + "_" + user_num.ToString();
            //        temp_user_name = GameObject.Find(username);
            //    }
            //}
            pv.RPC("PunRPC_SetNickName", RpcTarget.AllBuffered, username);
        }

        [PunRPC]
        private void PunRPC_SetNickName(string nName)
        {
            gameObject.name = nName;
        }

        [PunRPC]
        private void PunRPC_ShareAzureAnchorId(string anchorId)
        {
            GenericNetworkManager.Instance.azureAnchorId = anchorId;

            Debug.Log("\nPhotonUser.PunRPC_ShareAzureAnchorId()");
            Debug.Log("GenericNetworkManager.instance.azureAnchorId: " + GenericNetworkManager.Instance.azureAnchorId);
            Debug.Log("Azure Anchor ID shared by user: " + pv.Controller.UserId);
        }

        public void ShareAzureAnchorId()
        {
            if (pv != null)
                pv.RPC("PunRPC_ShareAzureAnchorId", RpcTarget.AllBuffered,
                    GenericNetworkManager.Instance.azureAnchorId);
            else
                Debug.LogError("PV is null");
        }
    }
}
