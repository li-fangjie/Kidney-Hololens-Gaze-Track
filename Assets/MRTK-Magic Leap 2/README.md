
# MRTK Magic Leap 2

This setup guide assumes you have already configured your Unity Project for ML2. (https://developer.magicleap.cloud/learn/docs/guides/unity/getting-started/configure-unity-settings)

## Current Status

| Feature | Status |
|--|--|
| Controller | Release |
| Eye Tracking | Release |
| Hand Tracking | Release |
| Voice Commands | Release |
| Spatial Meshing | Release |

## Prerequisites

- Magic Leap Unity SDK v2.6.0
- MRTK Foundations v2.8
- MRTK Examples v2.8
- A configured Magic Leap 2 Unity project
- [Recommended] For OpenXR Projects use the 1.13+ OpenXR Package as previous versions had known graphical issues.
- [Established projects] Magic Leap XR Plugin 7.0.0 or later. Preview Packages are not supported. Unity 6 does require 7.1.1+ to continue support.

## Getting Started

### Configure Your Project (In Review for OpenXR)

1. In Player Settings set the **Active Input Handling** to **Both**. Restarting the editor may be required.
2. Import the  **TMP Essential Resources**  by selecting  **Window > TextMeshPro > Import TMP Essential Resources**.
3. Open the Unity Preferences window and set the **Script Changes While Playing** setting to **Stop Playing and Recompile** or **Recompile and Continue Playing** .
4. Install Universal RP from the Package Manager and set a **UniversalRenderPipelineAsset** in the **Graphics Settings**
5. Make sure **Custom Main Manifest** is selected under **Publishing Settings** so MagicLeap Manifest Settings can be set.
6. [OpenXR] The 2.4.0+ Magic Leap Unity SDK should install the OpenXR Plugin automatically, but confirm it is present in the project **com.unity.xr.openxr**.
7. [OpenXR Hands] Hand Tracking in OpenXR requires the base Unity XR Hands package **com.unity.xr.hands**.

### How to Use OpenXR

1. In Player Settings' **XR Plugin-in Management** make sure Magic Leap is not checked, and select **OpenXR**. Checking the Magic Leap 2 feature group and Experimental feature group is not needed as only certain features are used in MRTK.
2. [If Switching from Magic Leap Provider] It is generally recommended to close the project and delete the project's library folder after switching Providers to make sure there are no longer assets that could cause issues.
3. [Project Validation Error] After switching providers if a Project Validation error triggers, but no error is listed in **XR Plug-in Management > Project Validation**. This is a known Unity issue and can be resolved by switching to **Windows, Mac, Linux** Platform in the Build settings and then back to **Android** so the interface recompiles. A common reason for an error when switching is the **Target Devices** in Player Settings needs to be **All Devices**.
4. [Optional] OpenXR uses what is called Reference Spaces to determine the origin of the scene. For convenience a dropdown has been added to the camera settings with all supported reference spaces, some will require the Magic leap 2 Reference Spaces Feature, but this will be set at runtime and may cause content to visibly shift once loaded. To avoid this, and if the default Floor origin space is not desired, an **XR Origin** component can be added to the Main Camera Parent, usually **MixedRealityPlayspace**. Please note this should be disabled if returning to the old Magic Leap Provider if meshing is in the scene.
5. In **XR Plug-in Management > OpenXR** Specific features and interaction profiles can be added to work with the MRTK 2.8 implementations without making any scene changes.

#### Interaction profiles:
1. Magic Leap 2 Controller Interaction Profile
2. Base Unity Eye Gaze Interaction profile
3. Base Unity Hand Interaction Profile. [Works with base Unity Hand Tracking Subsystem Feature]

#### Magic Leap 2 (Experimental means the extension is still in Khronos review to be a part of base OpenXR)
1. [Required for all Applications] Magic Leap 2 support
2. Magic leap 2 Meshing Subsystem - Currently works through the already established profile, but will be expanded to support additional OpenXR Settings.
3. [Optional] Magic leap 2 Reference Spaces - Adds support for Unbounded and Local Floor Reference Spaces.
4. [Optional] Magic Leap 2 Secondary View - Improves alignment when making recordings on device.
5. [Optional] Magic Leap 2 Rendering Extensions - Dimming and Blend Mode settings.

#### Base Unity OpenXR features
1. Hand Tracking Subsystem, Hand Interaction Poses, Palm Pose - All needed for accurate Hand Tracking in OpenXR, these features are from the Unity XR Hands package.


### Import MRTK

1. Download version 2.8 (Latest at this time is 2.8.3) of  **MRTK Foundation**  and  **MRTK Examples**  from the MRTK [GitHub](https://github.com/Microsoft/MixedRealityToolkit-Unity/releases).
2. Import the  **MRTK Foundation 2.8**  package into your Unity project. Apply the recommended settings from the popup window that appears after doing so.
3. It will ask for Script Updating Consent, select **Yes, for these and other files that might be found later**.
4. Next, import the  **MRTK Examples 2.8**  package into your project. Note: Some Oculus prefabs may log an error, these can be cleared.
5. Note that the MRTK Tools package is now required for future upgrading to a newer version of MRTK according to the releases page linked to in #1.  
6. From the top menu Select **Mixed Reality > Toolkit > Utilities> Upgrade MRTK Standard Shader for Universal Render Pipeline**. This will be greyed out unless a URP pipeline asset is set in the Graphics Settings

### Edit MRTK Standard Shader

The standard shader in MRTK may give an error in 2022.2 when building and may block Build and Run. Make these edits in the shader to fix this if this happens.

1. `fixed facing : VFACE` to `bool facing : SV_IsFrontFace` at line 775
2. `* facing` to `* (facing ? 1.0 : -1.0)` at lines 956 & 959

### Import MRTK Magic Leap 2

1. Download the MRTK Magic Leap 2 Unity Asset.
2. Import the asset into your project.

### (Potentially needed) Tracked Pose Driver

It may be needed for Users to add the tracked pose driver to the camera themselves.

### MRTK Magic Leap 2 Examples

You can test the MRTK Magic Leap 2 implementation using the Example scenes located under `MRTK-Magic Leap 2/Samples/`

Make sure the `MixedRealityToolkit's` configuration profile is set to `MagicLeap2 ...`

#### Example Features

- Voice Intents - Refer to SpeechCommandsDemoMagicLeap scene for usage.
- Control - Refer to ControlMagicLeapDemo scene for usage.
- HandTracking - Refer to HandInteractionExamplesMagicLeap for usage. Only Partially implemented on platform currently.
- EyeTracking - Refer to EyeTrackingDemoMagicLeap for usage.
- Meshing - Refer to MeshingDemoMagicLeap for usage.
- AllInteractions - Refer to InteractionsDemoMagicLeap for HandTracking, Control, HeadTracking, and Voice usage together.

## Troubleshooting

### Eye Tracking Calibration

- If EyeTracking has a noticeable offset please run through the device's Eye Tracking Calibration.

### Configure Hand Tracking Settings

#### Track a Single Hand

This can be done by setting the following setting on `Start()`

```csharp
        MagicLeapHandTrackingInputProvider.Instance.CurrentHandSettings = MagicLeapHandTrackingInputProvider.HandSettings.Left;
```

#### Disable Hand Tracking

Hand tracking can be disabled by setting the MagicLeapHandTrackingInputProvider's `CurrentHandSettings` to `HandSettings.None`

```csharp
        MagicLeapHandTrackingInputProvider.Instance.CurrentHandSettings = MagicLeapDeviceManager.HandSettings.None;
```

The Magic Leap HandTracking Input Data Provider has Magic leap Settings to easily control these and set them in editor.

## Known Issues

- Hand tracking Performance issues when interacting with other objects. Continuous Improvements made each Sprint.
- To use the simulator when running MRTK, you must set the **Script Changes While Playing** setting to **Stop Playing and Recompile** or **Recompile and Continue Playing** in the Unity **Preferences**.
- Occasionally the controller may not connect properly when launching a new application. Pausing and resuming the application resolves this.
- [OpenXR] Performance issues with MRTK shaders at startup causing delays resulting in a few seconds of no content. This is noticeable with multiple Data providers or especially scenes with Eye Tracking. Temporarily the Interactions Example uses HeadTracking as a fallback instead of EyeTracking while this problem is looked into.

## Important Notes

- Instead of copying a configuration file, clone the DefaultMixedReality version and make adjustments. We have found copying an MRTK configuration file can cause issues such as Input Data Providers not loading or visualizers not attaching properly.
- Controller Visualizer sometimes stops positioning and the logs say: Left_ControllerModel(Clone) is missing a IMixedRealityControllerVisualizer component! This happens sporadically, we have found adding the MixedRealityControllerVisualizer component to the model itself resolves this.
- If your application builds and results in a blank/empty scene, you must adjust your project's quality settings. (Known issue in editor. This will be resolved in future 2022.2 editor releases) To resolve this, remove all but one of the quality presets in your projects quality settings (**Edit>Player Settings>Quality**)
- Users may need to add the tracked pose driver to the camera themselves.