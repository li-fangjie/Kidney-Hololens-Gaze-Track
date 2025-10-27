using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
#if XR_HANDS
using UnityEngine.XR.Hands;
#endif

namespace MagicLeap.MRTK.DeviceManagement.Input
{
    public struct InvalidHands
    {
        public bool isTracked;
    }

    public class MagicLeapOpenXRHandProcessor
    {
#if XR_HANDS
        XRHandSubsystem HandSubsystem;
        List<XRHandSubsystem> SubsystemsReuse = new List<XRHandSubsystem>();

        public XRHand LeftHand
        {
            get
            {
                return ((HandSubsystem != null) ? HandSubsystem.leftHand : default);
            }
        }
        public XRHand RightHand
        {
            get
            {
                return ((HandSubsystem != null) ? HandSubsystem.rightHand : default);
            }
        }

        public int callbackOrder => 0;
#else
        public InvalidHands LeftHand;
        public InvalidHands RightHand;
#endif
        public MagicLeapOpenXRHandProcessor()
        {
#if XR_HANDS
            SynchronizationContext mainSyncContext = SynchronizationContext.Current;
            System.Timers.Timer timer = new System.Timers.Timer(1000);
            timer.Start();
            timer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
            {
                mainSyncContext.Post(_ =>
                {
                    // Check subsystem valid
                    SubsystemManager.GetSubsystems(SubsystemsReuse);
                    if (SubsystemsReuse.Count > 0)
                    {
                        timer.Stop();
                        HandSubsystem = SubsystemsReuse[0];
                    }
                }, null);
            };
#else
            Debug.LogError("MagicLeapOpenXRHandProcessor was created without the Hands Subsystem within the com.unity.xr.hands package and will not work.");
#endif
        }
    }
}