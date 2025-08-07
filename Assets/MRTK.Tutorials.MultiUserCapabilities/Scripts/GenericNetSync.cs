using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Photon.Pun;
using System.IO;
using UnityEngine;

namespace MRTK.Tutorials.MultiUserCapabilities
{
    // Responsible for managing the objects of each user.
    public class GenericNetSync : MonoBehaviourPun, IPunObservable
    {
        [SerializeField] private bool isUser = default;
        [SerializeField] private GameObject selfCursorObj = default;
        [SerializeField] private GameObject scriptHolder = default;
        public GameObject parentObj = default;

        private Vector3 networkLocalPosition;
        private Quaternion networkLocalRotation;

        private Vector3 startingLocalPosition;
        private Quaternion startingLocalRotation;


        void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                if (scriptHolder.GetComponent<GazeTracker>().newDataToBeSent == true)
                {
                    scriptHolder.GetComponent<GazeTracker>().newDataToBeSent = false;
                    Debug.Log("OnPhotonSerializeView Writing");
                    stream.SendNext(transform.localPosition);
                    stream.SendNext(transform.localRotation);

                }
            }
            else
            {
                //Debug.Log("OnPhotonSerializeView Receiving");
                networkLocalPosition = (Vector3)stream.ReceiveNext();
                networkLocalRotation = (Quaternion)stream.ReceiveNext();
            }
        }

        private void Start()
        {
            PhotonNetwork.SerializationRate = 30;
            PhotonNetwork.SendRate = 3;
            parentObj = GameObject.Find("ScreenObject");
            selfCursorObj = GameObject.Find("SelfGazeObj");
            scriptHolder = GameObject.Find("ArucoTrackingScriptHolder");

            if (isUser)
            {
                if (parentObj != null)
                {
                    transform.parent = parentObj.transform;
                }
                // if (TableAnchor.Instance != null) transform.parent = FindObjectOfType<TableAnchor>().transform;

                if (photonView.IsMine) GenericNetworkManager.Instance.localUser = photonView;
            }

            var trans = transform;
            startingLocalPosition = trans.localPosition;
            startingLocalRotation = trans.localRotation;

            networkLocalPosition = startingLocalPosition;
            networkLocalRotation = startingLocalRotation;
        }

        // private void FixedUpdate()


        // private void FixedUpdate()
        private void FixedUpdate()
        {
            if (!photonView.IsMine)
            {
                transform.localPosition = networkLocalPosition;
                transform.localRotation = networkLocalRotation;
            }

            if (photonView.IsMine && isUser)
            {
                transform.localPosition = selfCursorObj.transform.localPosition;
                transform.localRotation = selfCursorObj.transform.localRotation;
            }
        }
    }
}