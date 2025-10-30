# Changelog

## [2.00.0 Preview 3]

### Features

- Added a MagicLeapOpenXRSpatialMeshObserver to allow for OpenXR specific settings if desired that will allow for 
normal Mesh Manager and Point Cloud Manager workflow.

### Misc.

- Updated to MagicLeap SDK 2.6.0.

## [2.00.0 Preview 2]

### Features

- Update MagicLeapEyeGazeDataProvider to use base OpenXR Eye Gaze Interaction Profile instead of Magic Leap 2 Eye Tracker.

### Misc.

- Updated to MagicLeap SDK 2.5.0.

## [2.00.0 Preview 1]

### Features

- Added OpenXR Provider Support.
- Added dropdown for OpenXR Origin Modes in the Camera Settings Profile. Only valid with OpenXR Provider.

### Misc.

- Updated to MagicLeap SDK 2.4.0.

## [1.12.3]

### Misc.

- Updated LICENSE.

## [1.12.2]

### Features

- Added a Keep Rendered Alpha setting in the Camera Profile to provide better photo & video capture capabilities,
along with better segmented dimmer support.

### Bugfixes

- Fixed an issue with the Magic Leap Controller and near interactions not working.

## [1.12.1]

### Features

- Added support for a new minimum near clip distance of 25cm when adjusted in Settings.

### Bugfixes

- Fixed an issue with the pinch logic not correctly observing the Pinch Trigger and Maintain values.

## [1.12.0]

### Features

- Made hand gesture pinch detection more responsive.

### Misc.

- Updated to MagicLeap Unity SDK 1.12.0.

## [1.11.0]

### Features

- Meshing - The ML SpatialMeshObserverProfile has been simplified in the inspector.
- Hand Tracking - Now using the default MRTK hand ray.
- Hand Tracking - Added an experimental improvement to pinching interactions.
- Changed the default Hand Ray pointer to use MRTK logic.

### Misc.

- Updated to MagicLeap SDK 1.4.0-dev2 and MagicLeap Unity SDK 1.11.0.

## [1.7.0]

### Features

- Added an additional option in the Hand Tracking Input provider Settings to allow developers to choose between the MRTK or Magic Leap logic for the Hand Ray Pointers.

### Misc.

- Updated to MagicLeap SDK 1.3.0-Dev1 and MagicLeap Unity SDK 1.7.0.

## [1.6.0]

### Features

- Exposed the values to determine if the controller is being held in hand. These settings only appear in the Magic Leap Device Manager Input Data Provider if the Disable Controller When Not in Hand option is selected.
- Examples with HandTracking, Meshing, and Controller now print settings on the UI panel to show which settings have been selected for the scene.

### Bugfixes

- Fixed false Permission Denied Log for Meshing.

### Misc.

- Updated to MagicLeap SDK 1.2.0 and MagicLeap Unity SDK 1.6.0.

## [1.5.0]

### Features

- In the Speech Commands Magic leap Profile added support for Voice Intent Slots.
- Begin updating UI look and feel to better match Magic Leap aesthetic.

### Bugfixes

- Fixed issue when both hand tracking and controller are tracked at once and added an option to disable hand tracking for the hand holding the controller.

### Misc.

- Updated to MagicLeap SDK 1.2.0-Dev2 and MagicLeap Unity SDK 1.5.0.

## [1.4.0]

### Features

- **Important** HandTracking Smoothing has been removed as smoothing is now done on the platform and no longer needed in the MRTK layer.
- Added a Wireframe General Rendering option to the MagicLeap Spatial Mesh Observer.
- Additional Logic to better support the "Air-Tap" Gesture.
- Added Support for System Voice Commands in the Speech Input Profile Important Scenes using Speech Commands will need to update their profiles from the DefaultMixedRealitySpeechCommandsProfile to the Magic Leap Speech profile located at Assets/Create/Mixed Reality/Toolkit/Profiles.

### Misc.

- Updated to MagicLeap SDK 1.2.0-Dev1 and MagicLeap Unity SDK 1.4.0.
- In the Magic Leap HandTracking Input Data Provider if Both or MRTK KeyPoints are selected as the Gesture Interaction Type, the MRTK Pinch values to trigger or maintain a pinch gesture are customizable.

## [1.2.0]

### Features

- **Important** MRTK versioning will match Magic Leap Unity SDK versions from this release onwards.
- Updated the Pointer logic for the Interactions scene to properly fall back to Eye Tracking when controller is off and hands are not visible.
- Implemented a new MagicLeapHapticsHandler to more easily trigger haptic events for the different types of haptic events Magic Leap supports.
- Changed the Keycodes on the Speech Commands sample as the num pad keycodes were not getting triggered in the input simulator.
- Removed unnecessary directives to better support cross platform.

### Bugfixes

- Fixed General Rendering Occlusion material not being referenced properly in the Spatial Mesh Observer.
- Fixed an issue with Eye Tracking where the fixation point may be incorrect if the Mixed Reality Playspace is not at origin.
- Corrected a misspelling in the MagicLeapTouchInputHandler. "EvenActions" has been renamed to "ActionEvents".

### Misc.

- Updated to MagicLeap SDK 1.1.0-Dev1 and MagicLeap Unity SDK 1.2.0.

## [1.1.0]

### Features

- **Important** Updated Controller Touchpad Interactions to be the proper type to get data from.
- Added a MagicLeapTouchInputHandler to be able to get data from Input Action Events for the touchpad. An Example has been added to the Control Sample.
- Modified Robust smoothing option in HandTracking. This is still experimental and being worked on for improvements to the HandTracking experience.
- Modified HandTracking Pointer to improve selection behavior.
- HandTracking Sample cleanup to remove unusable components.

### Misc.

- Updated to MagicLeap SDK 1.1.0-Dev1.

## [1.0.0]

### Features

- Modified Robust smoothing option in HandTracking. This is still experimental and being worked on for improvements to the HandTracking experience.
- Added Follow option to MagicLeapSpatialMeshObserver's profile so meshing can move with the device.
- Made MagicLeapSpatialMeshObserver's profile public so it can be changed at runtime. ForceUpdateMeshData may need to be called after changes.

### Bugfixes

- Fixed issue with Meshing Scaling and Position.
- Fixed issue with Controller pointer disappearing after pause.

### Misc.

- Updated to MagicLeap SDK 1.0.0.

## [0.53.3]

### Features

- Added a More Robust smoothing option to HandTracking. This is still experimental and being worked on for improvements to the HandTracking experience.
- Added Magic Leap Haptics functions to the Controller. An example can be found in the Control Example with Menu button press.
- Updated Magic Leap HandTracking Input Data Provider settings for new HandTracking options.

### Deprecations & Removals

- SetHandTrackingSettings is removed as it was deprecated last release since there is an Input Provider profile now. Settings can still be modified at Runtime the same way.

### Bugfixes

- Fixed Bug where importing the Magic Leap MRTK package into a project with Magic Leap Unity Examples would overwrite some material assets.
- Fixed issues with Hand Menus.

### Misc.

- Updated to MagicLeap SDK 0.53.3.

## [0.53.2]

### Features

- **Important** HandTracking and Controller Tracking have been separated into two separate Input Providers. Magic Leap Device Manager remains the input provider for the Controller and a new Magic Leap HandTracking Input provider has been added.
- Added Meshing, Spatial Awareness, implementation and example. This includes a Meshing Profile specifically for Magic Leap to be used in the Toolkit.
- Moved HandTracking Settings into a profile for the new HandTracking Input Provider and included more options.
- Added Gestures Classification API to HandTracking.

### Deprecations & Removals

- SetHandTrackingSettings is being deprecated as there is an Input Provider profile now. Settings can still be modified at Runtime the same way.

### Misc.

- Updated to MagicLeap SDK 0.53.2.

## [0.53.0]

### Features

- Permission Logic Updated to use Callbacks.
- MagicLeapCameraSettings have changed as MLCamera has moved from the XR package into the SDK package and some features have been altered or removed.
- Updated pointer logic for HandTracking to improve Grab logic.

### Bugfixes

- Fixed issue where trigger would also cause vibration.

### Misc.

- Updated to MagicLeap SDK 0.53.0.

## [0.52.2]

### Features

- Updated to MRTK 2.8.
- Upgraded to Unity 2022.2 where Magic Leap builds are now Pure Android and no longer "Relish".
- Added Magic Leap Camera Settings.
- Added base controller haptics implementation, will be expanded on in future releases.
- Adjusted Handtracking Pointer position based on aligning with other platforms.
- Added logic to Handtracking to improve smoothness.

### Bugfixes

- Resolved issue with Controller Events on a Canvas.

### Misc.

- Updated to MagicLeap SDK 0.52.2.

## [0.52.1]

### Misc.

- Updated to MagicLeap SDK 0.52.1.

## [0.52.0]

### Features

- Initial Magic Leap 2 support for MRTK.
- Migration from earlier Magic Leap 1 support.

### Deprecations & Removals

- Previous configuration files have been put in a deprecated folder to resolve any upgrade issues.

### Misc.

- Updated to MagicLeap SDK 0.52.0.



