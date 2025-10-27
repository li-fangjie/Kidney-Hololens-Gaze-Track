// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2018-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using MagicLeap.Android;
using MagicLeap.OpenXR.Features.Meshing;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.XRSDK;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.OpenXR;


namespace MagicLeap.MRTK.SpatialAwareness
{
    public enum MeshRenderMode
    {
        Triangles,
        PointCloud,
        Occlusion
    }

    [MixedRealityDataProvider(
        typeof(IMixedRealitySpatialAwarenessSystem),
        SupportedPlatforms.Android,
        "MagicLeap OpenXR Spatial Mesh Observer")]
    [HelpURL(
        "https://docs.microsoft.com/windows/mixed-reality/mrtk-unity/features/spatial-awareness/spatial-awareness-getting-started")]
    public class MagicLeapOpenXRSpatialMeshObserver :
        GenericXRSDKSpatialMeshObserver
    {
        /// <summary>
        /// Altering the mesh profile data at runtime may require calling ForceUpdateMeshData() to clear visuals;
        /// </summary>
        public MagicLeapOpenXRSpatialMeshObserverProfile Profile;
        private GameObject meshManagerParent;
        private bool permissionsGranted;
        private bool permissionRequested;
        private MagicLeapMeshingFeature meshingFeature;
        private ARMeshManager meshManager;
        private ARPointCloudManager pointCloudManager;
        private ARSession arSession;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="registrar">The <see cref="IMixedRealityServiceRegistrar"/> instance that loaded the service.</param>
        /// <param name="name">Friendly name of the service.</param>
        /// <param name="priority">Service priority. Used to determine order of instantiation.</param>
        /// <param name="profile">The service's configuration profile.</param>
        public MagicLeapOpenXRSpatialMeshObserver(
            IMixedRealitySpatialAwarenessSystem spatialAwarenessSystem,
            string name = null,
            uint priority = DefaultPriority,
            BaseMixedRealityProfile profile = null) : base(spatialAwarenessSystem, name, priority, profile)
        {
        }

        public override void Enable()
        {
            base.Enable();
            Profile = ConfigurationProfile as MagicLeapOpenXRSpatialMeshObserverProfile;
            if (Profile == null)
            {
                Debug.LogWarning($"Use the `MagicLeapOpenXRSpatialMeshObserverProfile` configuration to set Magic Leap specific meshing settings. Default settings will be used.");
            
                Profile = ScriptableObject.CreateInstance<MagicLeapOpenXRSpatialMeshObserverProfile>();
            }

            meshingFeature = OpenXRSettings.Instance.GetFeature<MagicLeapMeshingFeature>();
            if (!meshingFeature.enabled)
            {
                Debug.LogError($"{nameof(MagicLeapMeshingFeature)} was not enabled.");
            }
        }

        public override void Update()
        {
            if (Profile == null)
            {
                return;
            }

            if (Profile != null && !permissionRequested)
            {
                permissionRequested = true;

                Permissions.RequestPermissions(new string[] { Permissions.SpatialMapping }, OnPermissionGranted,
                    OnPermissionDenied, OnPermissionDenied);
            }
        }

        private void OnPermissionDenied(string permission)
        {
            if (permission == Permissions.SpatialMapping)
            {
                Debug.LogError($"Failed to create Meshing Subsystem due to missing or denied {permission} permission. Please add to manifest and grant permissions.");
            }
        }

        private void UpdateSettings()
        {
            if (!meshingFeature.enabled)
            {
                Debug.LogError($"Unable to set values for {nameof(MagicLeapMeshingFeature)}.");
                return;
            }
            meshManager.transform.localScale = Profile.MeshBoundsScale;
            meshManager.transform.rotation = Quaternion.Euler(Profile.MeshBoundsRotation);
            meshManager.transform.localPosition = Profile.MeshBoundsOrigin;

            if (Profile.MeshMode == MeshRenderMode.Triangles || Profile.MeshMode == MeshRenderMode.Occlusion)
            {
                meshManager.density = Profile.Density;
                meshManager.meshPrefab = Profile.MeshPrefab.GetComponent<MeshFilter>();
                meshManager.normals = Profile.ComputeNormals;
                meshManager.tangents = Profile.ComputeTangents;
                meshManager.textureCoordinates = Profile.TextureCoordinates;
                meshManager.colors = Profile.ComputeColors;
                meshManager.concurrentQueueSize = Profile.ConcurrentQueueSize;
                if (Profile.MeshMode == MeshRenderMode.Occlusion)
                {
                    var renderer = meshManager.meshPrefab.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = Profile.MeshOcclusionMaterial;
                    }
                }
            }
            meshingFeature.MeshDensity = Profile.Density;
            meshingFeature.MeshBoundsOrigin = Profile.MeshBoundsOrigin;
            meshingFeature.MeshBoundsRotation = Quaternion.Euler(Profile.MeshBoundsRotation);
            meshingFeature.MeshBoundsScale = Profile.MeshBoundsScale;
            var querySettings = new MeshingQuerySettings();
            querySettings.fillHoleLength = Profile.FillHoleLength;
            querySettings.appliedDisconnectedComponentArea = Profile.AppliedDisconnectedComponentArea;
            querySettings.meshDetectorFlags = Profile.MeshDetectorFlags;
            querySettings.useIonAllocator = Profile.UseIonAllocator;
            meshingFeature.UpdateMeshQuerySettings(in querySettings);
            meshingFeature.InvalidateMeshes();
            
            meshManager.DestroyAllMeshes();
            meshManager.enabled = false;
            pointCloudManager.SetTrackablesActive(false);
            pointCloudManager.enabled = false;
            switch (Profile.MeshMode)
            {
                case MeshRenderMode.Triangles:
                case MeshRenderMode.Occlusion:
                    meshingFeature.MeshRenderMode = MeshingMode.Triangles;
                    break;
                case MeshRenderMode.PointCloud:
                    meshingFeature.MeshRenderMode = MeshingMode.PointCloud;
                    break;
            }

            EnableManager();
        }

        private void EnableManager()
        {
            if (Profile.MeshMode == MeshRenderMode.PointCloud)
            {
                meshManager.enabled = false;
                pointCloudManager.enabled = true;
                pointCloudManager.SetTrackablesActive(true);
            }
            else
            {
                pointCloudManager.SetTrackablesActive(false);
                pointCloudManager.enabled = false;
                meshManager.enabled = true;
            }
        }

        private void OnPermissionGranted(string permission)
        {
            if (permission == Permissions.SpatialMapping)
            {
                permissionsGranted = true;
                XROrigin xrOrigin = GameObject.FindObjectOfType<XROrigin>();
            
                if (xrOrigin != null)
                {
                    arSession = xrOrigin.gameObject.AddComponent<ARSession>();
                    pointCloudManager = xrOrigin.gameObject.AddComponent<ARPointCloudManager>();
                    pointCloudManager.pointCloudPrefab = Profile.PointCloudPrefab;
                    pointCloudManager.enabled = false;
                    arSession.attemptUpdate = true;
                    arSession.matchFrameRateRequested = true;
                    arSession.requestedTrackingMode = TrackingMode.PositionAndRotation;
                    Transform transform = xrOrigin.transform;
                    meshManagerParent = new GameObject("MeshManager");
                    meshManagerParent.transform.SetParent(transform);
                    meshManager = meshManagerParent.AddComponent<ARMeshManager>();
                    meshManager.enabled = false;
                    UpdateSettings();
                }
                else
                {
                    Debug.LogError("XR Origin not found in the scene.");
                }
            }
        }
    }
}
