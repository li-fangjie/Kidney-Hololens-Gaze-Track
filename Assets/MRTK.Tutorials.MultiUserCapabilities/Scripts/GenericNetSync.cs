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
        public ExtendedEyeGazeDataProvider extendedEyeGazeDataProvider;
        [HideInInspector] public string deviceName;
        public AppConfig appConfig;

        public GameObject parentObj = default;
        public GameObject ScreenObj = default;
        private GameObject ScreenQuadFront = default;
        private GameObject ScreenQuadBack = default;

        public GameObject Cursor;

        private Camera mainCamera;

        private Vector3 networkLocalPosition;
        private Quaternion networkLocalRotation;

        private Vector3 startingLocalPosition;
        private Quaternion startingLocalRotation;

        private Vector3 lastHitPos = default;
        private bool newDataToBeSent = false;

        void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                if (newDataToBeSent == true)
                {
                    newDataToBeSent = false;
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
            deviceName = SystemInfo.deviceName;
            PhotonNetwork.SerializationRate = 30;
            PhotonNetwork.SendRate = 3;
            Cursor = GameObject.Find("DefaultGazeCursorCloseSurface_Invisible(Clone)");
            extendedEyeGazeDataProvider = GameObject.Find("ArucoTrackingScriptHolder").GetComponent<ExtendedEyeGazeDataProvider>();
            appConfig = GameObject.Find("ArucoTrackingScriptHolder").GetComponent<AppConfig>();
            parentObj = GameObject.Find("ScreenObject");
            ScreenObj = GameObject.Find("ScreenObject");
            ScreenQuadFront = GameObject.Find("ScreenSurfaceQuad (1)");
            ScreenQuadBack = GameObject.Find("ScreenSurfaceQuad");

            lastHitPos = Vector3.zero;
            newDataToBeSent = false;

            Debug.Log(extendedEyeGazeDataProvider);
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

        public static bool GetLocalHitOnPlane(GameObject planeObjectFront, GameObject planeObjectBack, GameObject planeObjectParent, Vector3 rayOrigin, Vector3 rayDirection, out Vector3 localHitPosition, out Quaternion planeRotation)
        {
            localHitPosition = Vector3.zero;
            planeRotation = Quaternion.identity;

            // Build the ray
            Ray ray = new Ray(rayOrigin, rayDirection.normalized);

            // Check if the GameObject has a collider
            Collider colliderFront = planeObjectFront.GetComponent<Collider>();
            if (colliderFront == null)
            {
                Debug.LogWarning("The plane GameObject has no collider.");
            }
            else
            {
                // Raycast against the collider
                if (colliderFront.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity))
                {
                    //Debug.Log("Front Hit");
                    // Convert hit point to local coordinates
                    localHitPosition = planeObjectParent.transform.InverseTransformPoint(hitInfo.point);

                    // Get the plane's rotation in world space
                    planeRotation = Quaternion.Inverse(planeObjectParent.transform.rotation) * planeObjectFront.transform.rotation;

                    return true;
                }
            }

            Collider colliderBack = planeObjectBack.GetComponent<Collider>();
            if (colliderBack == null)
            {
                Debug.LogWarning("The plane GameObject has no collider.");
            }
            else
            {
                if (colliderBack.Raycast(ray, out RaycastHit hitInfoBack, Mathf.Infinity))
                {
                    //Debug.Log("Back Hit");
                    // Convert hit point to local coordinates
                    localHitPosition = planeObjectParent.transform.InverseTransformPoint(hitInfoBack.point);

                    // Get the plane's rotation in world space
                    planeRotation = Quaternion.Inverse(planeObjectParent.transform.rotation) * planeObjectBack.transform.rotation;

                    return true;
                }
            }


            return false; // No hit
        }


        public static bool GetLocalHitOnPlanePlaneBased(GameObject planeObjectFront, GameObject planeObjectBack, GameObject planeObjectParent, Vector3 rayOrigin, Vector3 rayDirection, out Vector3 localHitPosition, out Quaternion planeRotation)
        {
            localHitPosition = Vector3.zero;
            planeRotation = Quaternion.identity;

            // Build the ray
            Ray ray = new Ray(rayOrigin, rayDirection.normalized);

            Plane screenPlane = new Plane(planeObjectFront.transform.forward, planeObjectFront.transform.position);
            if (screenPlane.Raycast(ray, out float hitPosParam))
            {
                //Debug.Log("Back Hit");
                // Convert hit point to local coordinates
                localHitPosition = planeObjectParent.transform.InverseTransformPoint(ray.GetPoint(hitPosParam));

                if (!IsPointInFrontOfQuad(planeObjectFront, rayOrigin))
                {
                    planeRotation = Quaternion.Inverse(planeObjectParent.transform.rotation) * planeObjectFront.transform.rotation;
                }
                else
                {
                    planeRotation = Quaternion.Inverse(planeObjectParent.transform.rotation) * planeObjectBack.transform.rotation;
                }
                // Get the plane's rotation in world space

                return true;
            }


            return false; // No hit
        }

        public static bool IsPointInFrontOfQuad(GameObject quadObject, Vector3 point)
        {
            Vector3 quadPosition = quadObject.transform.position;
            Vector3 quadForward = quadObject.transform.forward;

            // Direction from quad to point
            Vector3 toPoint = point - quadPosition;

            // Dot product: positive if on the forward side
            float dot = Vector3.Dot(quadForward, toPoint);

            return dot > 0f;
        }


        // private void FixedUpdate()
        private void Update()
        {
            if (appConfig != null && (appConfig.appOperation == false || appConfig.gazeShareOperation == false))
            {
                //Debug.Log("Not sharing gaze");
                return;
            }
            if (!photonView.IsMine)
            {
                transform.localPosition = networkLocalPosition;
                transform.localRotation = networkLocalRotation;
            }

            if (photonView.IsMine && isUser)
            {

#if ENABLE_WINMD_SUPPORT
                System.DateTime timestamp = System.DateTime.Now;
                var combinedGazeReadingInWorldSpace = extendedEyeGazeDataProvider.GetWorldSpaceGazeReading(ExtendedEyeGazeDataProvider.GazeType.Combined, timestamp);
                //var combinedGazeReadingInWorldSpace = extendedEyeGazeDataProvider.GetCameraSpaceGazeReading(ExtendedEyeGazeDataProvider.GazeType.Combined, timestamp);

                if (combinedGazeReadingInWorldSpace.IsValid)
                {
                    //Debug.Log($"Successful Extended Gaze Read at: {timestamp.ToFileTimeUtc()}");
                    if (GetLocalHitOnPlanePlaneBased(ScreenQuadFront, ScreenQuadBack, ScreenObj, combinedGazeReadingInWorldSpace.EyePosition, combinedGazeReadingInWorldSpace.GazeDirection, out Vector3 localHitPosition, out Quaternion planeRotation))
                    {
                        if (!(transform.localPosition == localHitPosition && transform.localRotation == planeRotation))
                        {
                            newDataToBeSent = true;
                            transform.localPosition = localHitPosition;  // ScreenObj.transform.InverseTransformPoint(localHitPosition);
                                                                         //transform.localRotation = Quaternion.Inverse(ScreenObj.transform.rotation) * Quaternion.LookRotation(gazeProvider.HitNormal, Vector3.up);
                            transform.localRotation = planeRotation; // Quaternion.Inverse(ScreenObj.transform.rotation) * planeRotation;
                        } else
                        {
                            //Debug.Log("Current update same as before");
                        }
                        
                    }
                }

#else
                var gazeProvider = CoreServices.InputSystem?.EyeGazeProvider;
                if (gazeProvider != null)
                {
                    if (GetLocalHitOnPlanePlaneBased(ScreenQuadFront, ScreenQuadBack, ScreenObj, gazeProvider.GazeOrigin, gazeProvider.GazeDirection, out Vector3 localHitPosition, out Quaternion planeRotation))
                    {
                        if (!(transform.localPosition == localHitPosition && transform.localRotation == planeRotation))
                        {
                            newDataToBeSent = true;
                            transform.localPosition = localHitPosition;  // ScreenObj.transform.InverseTransformPoint(localHitPosition);
                                                                         //transform.localRotation = Quaternion.Inverse(ScreenObj.transform.rotation) * Quaternion.LookRotation(gazeProvider.HitNormal, Vector3.up);
                            transform.localRotation = planeRotation; // Quaternion.Inverse(ScreenObj.transform.rotation) * planeRotation;
                        }
                        else
                        {
                            //Debug.Log("Current update same as before");
                        }

                    }
                }
#endif




            }
        }
    }
}
