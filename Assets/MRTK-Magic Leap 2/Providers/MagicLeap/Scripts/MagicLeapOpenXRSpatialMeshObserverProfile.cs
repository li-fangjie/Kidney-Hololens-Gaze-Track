// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2018-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using MagicLeap.OpenXR.Features.Meshing;

namespace MagicLeap.MRTK.SpatialAwareness
{
    /// <summary>
    /// Configuration profile settings for spatial awareness mesh observers.
    /// </summary>
    [CreateAssetMenu(
        menuName =
            "Mixed Reality/Toolkit/Profiles/Magic Leap OpenXR Spatial Awareness Mesh Observer Profile",
        fileName = "MagicLeapOpenXRSpatialMeshObserverProfile", order = (int)CreateProfileMenuItemIndices.SpatialAwarenessMeshObserver)]
    [MixedRealityServiceProfile(typeof(MagicLeapOpenXRSpatialMeshObserver))]
    public class MagicLeapOpenXRSpatialMeshObserverProfile : MixedRealitySpatialAwarenessMeshObserverProfile
    {
        [Header("OpenXR Feature Settings")]
        [Tooltip("Bounding Box Origin")]
        public Vector3 MeshBoundsOrigin;
        
        [Tooltip("Bounding Box Rotation")]
        public Vector3 MeshBoundsRotation;
        
        [Tooltip("Bounding Box Scale")]
        public Vector3 MeshBoundsScale;
        
        [Tooltip("Whether to generate a triangle mesh or point cloud points.")]
        public MeshRenderMode MeshMode = MeshRenderMode.Triangles;

        [Tooltip("The material to apply for occlusion.")]
        public Material MeshOcclusionMaterial;

        [Tooltip("Boundary distance (in meters) of holes you wish to have filled.")]
        public float FillHoleLength = 0.25f;
        
        [Tooltip("Disconnected Areas Length.")]
        public float AppliedDisconnectedComponentArea = 0.25f;
        
        [Tooltip("Disconnected Areas Length.")]
        public MeshDetectorFlags MeshDetectorFlags = MeshDetectorFlags.Planarize | MeshDetectorFlags.ComputeNormals;
        
        public bool UseIonAllocator = false;

        [Header("PointCloud Manager Settings")]
        [Tooltip("Disconnected Areas Length.")]
        public GameObject PointCloudPrefab;

        [Header("Mesh Manager Settings")]
        [Tooltip("Mesh Prefab Filter")]
        public GameObject MeshPrefab;

        [Tooltip("Level of detail, ranges from 0.0f to 1.0f")]
        [Range(0f, 1f)]
        public float Density = 0.5f;

        [Tooltip("When enabled, the system will compute the normals for the triangle vertices.")]
        public bool ComputeNormals = true;

        public bool ComputeTangents;

        public bool ComputeColors;

        public bool TextureCoordinates;

        public int ConcurrentQueueSize = 4;
    }
}