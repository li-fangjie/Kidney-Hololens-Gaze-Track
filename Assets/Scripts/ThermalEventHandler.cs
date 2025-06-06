using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.PowerThermalNotification;
using System.Threading;
using UnityEngine.UI;

public class ThermalEventHandler : MonoBehaviour
{

    [SerializeField] public Text thermalText;
    private int nSkipFrame = 3;
    private int nFrame = 0;

    // Start is called before the first frame update
    void Start()
    {
        InitializeThermalNotifications();
        
    }

    // Update is called once per frame
    void Update()
    {
        nFrame++;
        if (nFrame > nSkipFrame)
        {
            timerCallback(null);
            nFrame = 0;
        }
    }

    private void NotificationHandler(object sender, PowerThermalEventArgs args)
    {
        //  Notification handling can be done here using information contained in args
        Debug.Log(args.MitigationLevel);
    }

    private void InitializeThermalNotifications()
    {
        PowerThermalNotification p = PowerThermalNotification.GetForCurrentProcess();

        PowerThermalPeripheralFlags requestedFlags = PowerThermalPeripheralFlags.Cpu | PowerThermalPeripheralFlags.PhotoVideoCamera;
        if (PowerThermalNotification.IsSupported(requestedFlags))
        {
            Debug.Log("The requested thermal flags are supported");
            if (thermalText != null)
            {
                thermalText.text = "The requested thermal flags are supported";
            }
            //At least one of these peripherals is supported by the system
            p.PeripheralsOfInterest = requestedFlags;
            //p.PowerThermalMitigationLevelChanged += NotificationHandler;
        } else
        {
            Debug.Log("The requested thermal flags are not supported");
            if (thermalText != null)
            {
                thermalText.text = "The requested thermal flags are not supported";
            }
        }
    }

    private void timerCallback(object state)
    {
        PowerThermalNotification p = PowerThermalNotification.GetForCurrentProcess();

        PowerThermalPeripheralState CpuState = p.GetLatestPeripheralState(PowerThermalPeripheralFlags.Cpu);
        PowerThermalPeripheralState PhotoVideoCameraState = p.GetLatestPeripheralState(PowerThermalPeripheralFlags.PhotoVideoCamera);
        if (thermalText != null)
        {
            thermalText.text = "CPU Stat: " + CpuState.IsSupportedPeripheral.ToString() + " " + CpuState.ThermalScore.ToString() + " " + CpuState.MitigationLevel.ToString() + "\nPhoto Video Stat: " + PhotoVideoCameraState.IsSupportedPeripheral.ToString() + " " + PhotoVideoCameraState.ThermalScore.ToString() + " " + PhotoVideoCameraState.MitigationLevel.ToString();
        }
        Debug.Log(thermalText.text);
    }

    private void InitializeThermalNotificationsPolling()
    {
        PowerThermalNotification p = PowerThermalNotification.GetForCurrentProcess();

        PowerThermalPeripheralFlags requestedFlags = PowerThermalPeripheralFlags.Cpu | PowerThermalPeripheralFlags.PhotoVideoCamera;
        p.SuppressedPlatformMitigationForPeripherals = requestedFlags;//Suppress any platform mitigation on CPU or PhotoVideoCamera

        if (PowerThermalNotification.IsSupported(requestedFlags))
        {
            p.PeripheralsOfInterest = requestedFlags;
        }
        else
        {
        }
    }
}
