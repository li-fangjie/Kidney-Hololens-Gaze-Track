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
using Microsoft.MixedReality.Toolkit.CameraSystem;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace MagicLeap.MRTK.DeviceManagement.Input
{
    /// <summary>
    /// Configuration profile for the Windows Mixed Reality camera settings provider.
    /// </summary>
    [CreateAssetMenu(
        menuName =
            "Mixed Reality/Toolkit/Providers/Magic Leap/Magic Leap Camera Settings Profile",
        fileName = "MagicLeapCameraSettingsProfile", order = 100)]
    [MixedRealityServiceProfile(typeof(MagicLeapCameraSettings))]
    public class MagicLeapCameraSettingsProfile : BaseCameraSettingsProfile
    {
        public enum MLOpenXRTrackingOriginMode { None, Device, Floor, LocalFloor, Unbounded }

        [SerializeField]
        [Tooltip("Transform you want to be the focus point of the camera. Can help improve alignment when performing Mixed Reality Capture. Leave empty to use default value")]
        private Transform _stereoConvergencePoint;
        public Transform StereoConvergencePoint => _stereoConvergencePoint;

        [SerializeField]
        [Tooltip("Content for this app is protected and should not be recorded or captured")]
        private bool _protectedSurface;
        public bool ProtectedSurface => _protectedSurface;

        [SerializeField]
        [Tooltip("Whether to instruct the Unity XR plugin to keep the rendered alpha and pass it along " +
                 "to Magic Leap 2 for further processing, resulting in better quality photo/video captures " +
                 "and segmented dimmer results. " +
                 "Note, this feature may not work well when used in combination with any post-processing " +
                 "effects, like HDR, which may need to be disabled to use this setting.")]
        private bool _keepRenderedAlpha;
        public bool KeepRenderedAlpha => _keepRenderedAlpha;

        [SerializeField]
        [Tooltip("Choose the Origin Mode for OpenXR Scenes.")]
        private MLOpenXRTrackingOriginMode _openXRTrackingOriginModeOverride = MLOpenXRTrackingOriginMode.Device;
        public MLOpenXRTrackingOriginMode OpenXRTrackingOriginMode => _openXRTrackingOriginModeOverride;
    }
}