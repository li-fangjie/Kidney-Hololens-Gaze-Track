# Eye Gaze Metric Analysis – Unity Integration

This repository contains the Unity-based application used in the **Eye Gaze Share** system described in the MICCAI 2025 accepted paper:

**From Sight to Skill: A Surgeon-Centered Augmented Reality System for Ureteroscopy Training**

## Overview

This Unity project is a key component of the Eye Gaze Share platform. It enables real-time integration of eye gaze data into an augmented reality (AR) surgical training simulation for ureteroscopy.

## Repository Structure

- `Assets/`: Unity assets for the AR simulation
- `Scripts/`: Custom C# scripts for gaze integration, visualization, and interaction
- `Scenes/`: Unity scenes used for the training environment
- `Prefabs/`: Reusable UI and tool components
- `Plugins/`: Third-party or platform-specific dependencies (e.g., NetMQ, MRTK, etc.)

## Related Repository

If you are looking for the **data processing and gaze metrics computation scripts**, please see:

- **[Gaze Data Processing & Metric Analysis](https://github.com/jatoum/Eye_Gaze_Metric_Analysis)  
(This companion repository includes Python scripts for analyzing gaze patterns, calculating fixation metrics, and visualizing gaze behavior.)

- **[Kidney Gaze User Study Tools](https://github.com/li-fangjie/Kidney-Gaze-User-Study-Tools):**  
  Tools used in the user study for collecting gaze data during studies.


## Setup Instructions

1. Open this project with **Unity 2021.3 LTS** or newer.
2. Make sure you have the following Unity packages installed:
   - Mixed Reality Toolkit (MRTK)
   - NetMQ or equivalent messaging system
3. Connect the system to real-time gaze input or replay pre-recorded gaze data.
4. Build for the appropriate platform (e.g., HoloLens or PC).
