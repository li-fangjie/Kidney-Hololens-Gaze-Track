using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Photon.Pun;
using System;
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
    public Vector3 curGazeDirection { get; private set; }

    private readonly EyeGazeSmoother validationSmoother = new EyeGazeSmoother();
    private DateTime lastProviderTimestamp = DateTime.MinValue;
    private Ray latestSmoothedRay;
    public bool hasSmoothedRay { get; private set; } = false;

    public bool hasSmoothedScreenHit { get; private set; } = false;
    public Vector3 smoothedGazeOrigin { get; private set; }
    public Vector3 smoothedGazeDirection { get; private set; }
    public Vector3 smoothedGazeLocalPosition { get; private set; }
    public Quaternion smoothedGazeLocalRotation { get; private set; }

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

        if (gazeProvider == null)
        {
            hasSmoothedScreenHit = false;
            return;
        }

        Vector3 gazeOrigin = gazeProvider.GazeOrigin;
        Vector3 gazeDirection = gazeProvider.GazeDirection.normalized;
        curGazeOrigin = gazeOrigin;
        curGazeDirection = gazeDirection;

        // Preserve the existing raw gaze path and SelfGazeObj behavior.
        if (GetLocalHitOnPlanePlaneBased(ScreenQuadFront, ScreenQuadBack, ScreenObj, gazeOrigin, gazeDirection, out Vector3 localHitPosition, out Quaternion planeRotation))
        {
            if (!(selfGazeObj.transform.localPosition == localHitPosition && selfGazeObj.transform.localRotation == planeRotation))
            {
                newDataToBeSent = true;
                selfGazeObj.transform.localPosition = localHitPosition;
                selfGazeObj.transform.localRotation = planeRotation;
            }
        }

        // FixedUpdate may observe the same provider sample more than once. Only
        // advance the stateful MRTK smoother when the provider timestamp changes.
        DateTime providerTimestamp = gazeProvider.Timestamp;
        if (!hasSmoothedRay || providerTimestamp != lastProviderTimestamp)
        {
            latestSmoothedRay = validationSmoother.SmoothGaze(new Ray(gazeOrigin, gazeDirection));
            smoothedGazeOrigin = latestSmoothedRay.origin;
            smoothedGazeDirection = latestSmoothedRay.direction;
            lastProviderTimestamp = providerTimestamp;
            hasSmoothedRay = true;
        }

        // Re-project every FixedUpdate so raw and smoothed gaze use the same
        // current screen transform, even between new eye-provider samples.
        hasSmoothedScreenHit = false;
        if (hasSmoothedRay && GetLocalHitOnPlanePlaneBased(
                ScreenQuadFront,
                ScreenQuadBack,
                ScreenObj,
                latestSmoothedRay.origin,
                latestSmoothedRay.direction,
                out Vector3 smoothedLocalHitPosition,
                out Quaternion smoothedPlaneRotation))
        {
            hasSmoothedScreenHit = true;
            smoothedGazeLocalPosition = smoothedLocalHitPosition;
            smoothedGazeLocalRotation = smoothedPlaneRotation;
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
