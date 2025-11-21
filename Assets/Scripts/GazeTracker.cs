using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Photon.Pun;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GazeTracker : MonoBehaviour
{
    public bool newDataToBeSent = false;
    [SerializeField] private GameObject selfGazeObj = default;
    //public GameObject Cursor;
    [SerializeField] private float defaultDistanceInMeters = 3;
    public GameObject parentObj = default;
    public GameObject ScreenObj = default;
    private GameObject ScreenQuadFront = default;
    private GameObject ScreenQuadBack = default;
    private Vector3 lastHitPos = default;
    public Vector3 curGazeOrigin = default;

    // Start is called before the first frame update
    void Start()
    {
        //Cursor = GameObject.Find("DefaultGazeCursorCloseSurface_Invisible(Clone)");
        parentObj = GameObject.Find("ScreenObject");
        ScreenObj = GameObject.Find("ScreenObject");
        lastHitPos = Vector3.zero;

        ScreenQuadFront = GameObject.Find("ScreenSurfaceQuad (1)");
        ScreenQuadBack = GameObject.Find("ScreenSurfaceQuad");
        
        selfGazeObj.transform.parent = parentObj.transform;

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        var gazeProvider = CoreServices.InputSystem?.EyeGazeProvider;
        if (gazeProvider != null)
        {
            curGazeOrigin = gazeProvider.GazeOrigin;
            if (GetLocalHitOnPlanePlaneBased(ScreenQuadFront, ScreenQuadBack, ScreenObj, gazeProvider.GazeOrigin, gazeProvider.GazeDirection, out Vector3 localHitPosition, out Quaternion planeRotation))
            {
                if (!(selfGazeObj.transform.localPosition == localHitPosition && selfGazeObj.transform.localRotation == planeRotation))
                {
                    newDataToBeSent = true;
                    selfGazeObj.transform.localPosition = localHitPosition;  // ScreenObj.transform.InverseTransformPoint(localHitPosition);
                                                                             //transform.localRotation = Quaternion.Inverse(ScreenObj.transform.rotation) * Quaternion.LookRotation(gazeProvider.HitNormal, Vector3.up);
                    selfGazeObj.transform.localRotation = planeRotation; // Quaternion.Inverse(ScreenObj.transform.rotation) * planeRotation;
                }
                else
                {
                    //Debug.Log("Current update same as before");
                }

            }
        }
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
}
