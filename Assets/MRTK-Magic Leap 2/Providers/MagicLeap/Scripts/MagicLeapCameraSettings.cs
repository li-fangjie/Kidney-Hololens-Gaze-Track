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
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.Threading;


namespace MagicLeap.MRTK.DeviceManagement.Input
{
    /// <summary>
    /// Camera settings provider for use with XR SDK.
    /// </summary>
    [MixedRealityDataProvider(
        typeof(IMixedRealityCameraSystem),
        SupportedPlatforms.Android,
        "MagicLeap Camera Settings")]
    public class MagicLeapCameraSettings : BaseCameraSettingsProvider
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cameraSystem">The instance of the camera system which is managing this provider.</param>
        /// <param name="name">Friendly name of the provider.</param>
        /// <param name="priority">Provider priority. Used to determine order of instantiation.</param>
        /// <param name="profile">The provider's configuration profile.</param>
        public MagicLeapCameraSettings(
            IMixedRealityCameraSystem cameraSystem,
            string name = null,
            uint priority = DefaultPriority,
            BaseCameraSettingsProfile profile = null) : base(cameraSystem, name, priority, profile)
        {
            ReadProfile();
        }

        private UnityEngine.SpatialTracking.TrackedPoseDriver trackedPoseDriver = null;

        private MLAudioOutputPluginBehavior mlAudioOutputPluginBehavior = null;
        private UnityEngine.XR.MagicLeap.MagicLeapCamera magicLeapCamera = null;

#if UNITY_ANDROID && !UNITY_EDITOR
        [DllImport("UnityMagicLeap", CallingConvention = CallingConvention.Cdecl, EntryPoint = "UnityMagicLeap_SegmentedDimmer_KeepAlpha")]
        private static extern void SetSegmentedDimmerKeepAlpha(bool status);
#endif


        #region IMixedRealityCameraSettings

        private Transform _stereoConvergencePoint;
        private bool _protectedSurface;
        private bool _keepRenderedAlpha;
        private MagicLeapCameraSettingsProfile.MLOpenXRTrackingOriginMode openXRTrackingOriginMode;
        private bool openXROriginModeSet;

        private XRInputSubsystem inputSubsystem = null;

        /// <inheritdoc/>
        public override bool IsOpaque => XRSubsystemHelpers.DisplaySubsystem?.displayOpaque ?? true;
        public MagicLeapCameraSettingsProfile SettingsProfile => ConfigurationProfile as MagicLeapCameraSettingsProfile;

        private void ReadProfile()
        {
            if (SettingsProfile == null)
            {
                Debug.LogWarning("A profile was not specified for the Unity AR Camera Settings provider.\nUsing Microsoft Mixed Reality Toolkit default options.");
                return;
            }

            _stereoConvergencePoint = SettingsProfile.StereoConvergencePoint;
            _protectedSurface = SettingsProfile.ProtectedSurface;
            _keepRenderedAlpha = SettingsProfile.KeepRenderedAlpha;
            openXRTrackingOriginMode = SettingsProfile.OpenXRTrackingOriginMode;
        }

        /// <inheritdoc/>
        public override void Enable()
        {
            // Only track the TrackedPoseDriver if we added it ourselves.
            // There may be a pre-configured TrackedPoseDriver on the camera.
            if (!CameraCache.Main.GetComponent<TrackedPoseDriver>())
            {
                trackedPoseDriver = CameraCache.Main.gameObject.AddComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
            }
            if (MLDevice.IsMagicLeapLoaderActive())
            {
                if (!CameraCache.Main.GetComponent<MLAudioOutputPluginBehavior>())
                {
                    mlAudioOutputPluginBehavior = CameraCache.Main.gameObject.AddComponent<MLAudioOutputPluginBehavior>();
                }
                if (!CameraCache.Main.GetComponent<UnityEngine.XR.MagicLeap.MagicLeapCamera>())
                {
                    magicLeapCamera = CameraCache.Main.gameObject.AddComponent<UnityEngine.XR.MagicLeap.MagicLeapCamera>();
                    magicLeapCamera.StereoConvergencePoint = _stereoConvergencePoint;
                    magicLeapCamera.ProtectedSurface = _protectedSurface;

#if UNITY_ANDROID && !UNITY_EDITOR
                if (_keepRenderedAlpha)
                {
                    bool dllLoadSuccess = false;
                    try
                    {
                        SetSegmentedDimmerKeepAlpha(true);
                        dllLoadSuccess = true;
                    }
                    catch (DllNotFoundException)
                    {
                        Debug.LogError("Unable to load UnityMagicLeap DLL for MRTK2.8 Magic Leap Camera Settings 'Keep Rendered Alpha'.");
                    }

                    if (dllLoadSuccess) {
                        // In order for the SetSegmentedDimmerKeepAlpha(true) call to work, the camera's
                        // clear color must be ensured to be set to zero (0) alpha.
                        // Verify the main camera's background color alpha is zero.
                        var mainCamera = CameraCache.Main;
                        if (mainCamera != null)
                        {
                            var backgroundColor = mainCamera.backgroundColor;
                            backgroundColor.a = 0;
                            mainCamera.backgroundColor = backgroundColor;
                        }
                    }
                }
#endif
                    // MagicLeapCamera will override some camera settings on Awake(), so re-apply them after adding it.
                    ApplyConfiguration();
                }

                base.Enable();
            }

            if (MLDevice.IsOpenXRLoaderActive() && !openXROriginModeSet)
            {
                if (openXRTrackingOriginMode == MagicLeapCameraSettingsProfile.MLOpenXRTrackingOriginMode.None)
                {
                    openXROriginModeSet = true;
                }
                else
                {
                    TrackingOriginModeFlags setFlag = TrackingOriginModeFlags.Device;

                    switch (openXRTrackingOriginMode)
                    {
                        case MagicLeapCameraSettingsProfile.MLOpenXRTrackingOriginMode.Device:
                            {
                                setFlag = TrackingOriginModeFlags.Device;
                                break;
                            }
                        case MagicLeapCameraSettingsProfile.MLOpenXRTrackingOriginMode.Floor:
                            {
                                setFlag = TrackingOriginModeFlags.Floor;
                                break;
                            }
                        case MagicLeapCameraSettingsProfile.MLOpenXRTrackingOriginMode.LocalFloor:
                            {
                                setFlag = TrackingOriginModeFlags.Floor;
                                OpenXRSettings.SetAllowRecentering(true);
                                break;
                            }
                        case MagicLeapCameraSettingsProfile.MLOpenXRTrackingOriginMode.Unbounded:
                            {
                                setFlag = TrackingOriginModeFlags.Unbounded;
                                break;
                            }
                    }

                    if (XRGeneralSettings.Instance != null &&
                                XRGeneralSettings.Instance.Manager != null &&
                                XRGeneralSettings.Instance.Manager.activeLoader != null &&
                                XRGeneralSettings.Instance.Manager.activeLoader.GetLoadedSubsystem<XRInputSubsystem>() != null)
                    {
                        setTrackingOriginMode(setFlag);
                    }
                    else
                    {
                        SynchronizationContext mainSyncContext = SynchronizationContext.Current;
                        System.Timers.Timer timer = new System.Timers.Timer(500);
                        timer.Start();
                        timer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
                        {
                            mainSyncContext.Post(_ =>
                            {
                                if (XRGeneralSettings.Instance != null &&
                                    XRGeneralSettings.Instance.Manager != null &&
                                    XRGeneralSettings.Instance.Manager.activeLoader != null &&
                                    XRGeneralSettings.Instance.Manager.activeLoader.GetLoadedSubsystem<XRInputSubsystem>() != null)
                                {
                                    timer.Stop();
                                    setTrackingOriginMode(setFlag);
                                }
                            }, null);
                        };
                    }
                }

                base.Enable();
            }            
        }

        /// <inheritdoc />
        public override void Disable()
        {
            if (trackedPoseDriver != null)
            {
                UnityObjectExtensions.DestroyObject(trackedPoseDriver);
                trackedPoseDriver = null;
            }

            if (magicLeapCamera != null)
            {
                UnityObjectExtensions.DestroyObject(magicLeapCamera);
                magicLeapCamera = null;
            }

            if (mlAudioOutputPluginBehavior != null)
            {
                UnityObjectExtensions.DestroyObject(mlAudioOutputPluginBehavior);
                mlAudioOutputPluginBehavior = null;
            }

            openXROriginModeSet = false;

            base.Disable();
        }

        private void setTrackingOriginMode(TrackingOriginModeFlags setFlag)
        {
            if (inputSubsystem == null)
            {
                inputSubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRInputSubsystem>();
            }

            if (inputSubsystem != null)
            {
                if (!inputSubsystem.TrySetTrackingOriginMode(setFlag))
                {
                    Debug.LogError("MagicLeapCameraSettings failed to set Tracking Mode Origin to " + openXRTrackingOriginMode.ToString());
                }
                else
                {
                    inputSubsystem.TryRecenter();
                    openXROriginModeSet = true;
                }
            }
            else
            {
                Debug.LogError("MagicLeapCameraSettings Cannot set the Tracking Origin Mode without a valid XRInputSubsystem.");
            }
        }

        #endregion IMixedRealityCameraSettings
    }
}
