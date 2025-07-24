using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Photon.Pun;
using Photon.Realtime;  // Needed for DisconnectCause
using System.IO;
using UnityEngine;

namespace MRTK.Tutorials.MultiUserCapabilities
{
    // Responsible for managing the objects of each user.
    public class GenericNetSync : MonoBehaviourPun, IPunObservable
    {
        private Vector3 networkLocalPosition;
        private Quaternion networkLocalRotation;

        private Vector3 startingLocalPosition;
        private Quaternion startingLocalRotation;

        private GameObject Cursor;
        private GameObject parentObj;
        private GameObject ScreenObj;
        private GameObject ScreenQuadFront;
        private GameObject ScreenQuadBack;
        private Vector3 lastHitPos = Vector3.zero;

        public bool isUser = true;

        private void Start()
        {
            // Set Photon network rates
            PhotonNetwork.SerializationRate = 30;
            PhotonNetwork.SendRate = 3;

            // Find cursor
            Cursor = GameObject.Find("DefaultGazeCursorCloseSurface_Invisible(Clone)");
            if (Cursor == null)
                Debug.LogWarning("[GenericNetSync] Cursor not found. Gaze interaction may not work.");

            // Find main screen object and parent
            parentObj = GameObject.Find("ScreenObject");
            ScreenObj = parentObj;
            if (parentObj == null || ScreenObj == null)
                Debug.LogWarning("[GenericNetSync] ScreenObject not found. This may break scene placement or gaze alignment.");

            // Find front and back quads for screen plane
            ScreenQuadFront = GameObject.Find("ScreenSurfaceQuad (1)");
            ScreenQuadBack = GameObject.Find("ScreenSurfaceQuad");
            if (ScreenQuadFront == null || ScreenQuadBack == null)
                Debug.LogWarning("[GenericNetSync] ScreenSurfaceQuads not found. Plane-based gaze projection may fail.");

            lastHitPos = Vector3.zero;

            // Only do these things if this instance represents the local user
            if (isUser)
            {
                if (parentObj != null)
                {
                    transform.parent = parentObj.transform;
                }

                if (photonView.IsMine)
                {
                    if (GenericNetworkManager.Instance != null)
                    {
                        GenericNetworkManager.Instance.localUser = photonView;
                    }
                    else
                    {
                        Debug.LogWarning("[GenericNetSync] GenericNetworkManager.Instance is null. Could not assign localUser.");
                    }
                }
            }

            // Store initial transforms
            startingLocalPosition = transform.localPosition;
            startingLocalRotation = transform.localRotation;

            networkLocalPosition = startingLocalPosition;
            networkLocalRotation = startingLocalRotation;
        }

        // Implement IPunObservable for syncing transform data across the network
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Send current transform to other clients
                stream.SendNext(transform.localPosition);
                stream.SendNext(transform.localRotation);
            }
            else
            {
                // Receive data and update local variables for interpolation
                networkLocalPosition = (Vector3)stream.ReceiveNext();
                networkLocalRotation = (Quaternion)stream.ReceiveNext();
            }
        }

        private void Update()
        {
            // For remote objects, smooth the position and rotation updates
            if (!photonView.IsMine)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, networkLocalPosition, Time.deltaTime * 10);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, networkLocalRotation, Time.deltaTime * 10);
            }
        }
    }
}
