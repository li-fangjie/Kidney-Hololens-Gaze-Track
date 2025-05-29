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
        [SerializeField] private float defaultDistanceInMeters = 3;
        public GameObject parentObj = default;
        public GameObject ScreenObj = default;

        public GameObject Cursor;

        private Camera mainCamera;

        private Vector3 networkLocalPosition;
        private Quaternion networkLocalRotation;

        private Vector3 startingLocalPosition;
        private Quaternion startingLocalRotation;

        private Vector3 lastHitPos = default;

        void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                Debug.Log("PhotoSerializeView Writing");
                stream.SendNext(transform.localPosition);
                stream.SendNext(transform.localRotation);
            }
            else
            {
                Debug.Log("PhotoSerializeView Receiving");
                networkLocalPosition = (Vector3) stream.ReceiveNext();
                networkLocalRotation = (Quaternion) stream.ReceiveNext();
            }
        }

        private void Start()
        {
            PhotonNetwork.SerializationRate = 30;
            PhotonNetwork.SendRate = 30;
            Cursor = GameObject.Find("DefaultGazeCursorCloseSurface_Invisible(Clone)");
            parentObj = GameObject.Find("ScreenObject");
            ScreenObj = GameObject.Find("ScreenObject");
            lastHitPos = Vector3.zero;

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
        private void FixedUpdate()
        {
            if (!photonView.IsMine)
            {
                transform.localPosition = networkLocalPosition;
                transform.localRotation = networkLocalRotation;
            }

            if (photonView.IsMine && isUser)
            {
                var gazeProvider = CoreServices.InputSystem?.EyeGazeProvider;
                if (gazeProvider != null)
                {    
                    // EyeTrackingTarget lookedAtEyeTarget = EyeTrackingTarget.LookedAtEyeTarget;
                    Vector3 curHitPos = gazeProvider.HitPosition;
                    if (!curHitPos.Equals(lastHitPos))
                    {
                        // if nothing is hit, gazeProvider.HitPosition returns the last hit pos data; Used this fact to check if there is any hit positions.
                        // TODO: this is horrible approach, you should change to use physics.raycast for proper collider detection. 
                        // https://docs.unity3d.com/ScriptReference/Physics.Raycast.html
                        transform.localPosition = ScreenObj.transform.InverseTransformPoint(curHitPos);
                        transform.localRotation = Quaternion.Inverse(ScreenObj.transform.rotation) * Quaternion.LookRotation(gazeProvider.HitNormal, Vector3.up);
                        lastHitPos = curHitPos; 
                    }
                    else
                    {
                        transform.localPosition = ScreenObj.transform.InverseTransformPoint(gazeProvider.GazeOrigin + gazeProvider.GazeDirection.normalized * defaultDistanceInMeters);
                        transform.localRotation = Quaternion.Inverse(ScreenObj.transform.rotation) * Cursor.transform.rotation;
                    }
                    // transform.localPosition = ScreenObj.transform.InverseTransformPoint(Cursor.transform.position);
                    
                }
         
            }
        }
    }
}
