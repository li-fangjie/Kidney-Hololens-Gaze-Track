using Microsoft.MixedReality.Toolkit.UI;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using UnityEngine.Assertions;
using UnityEngine.UI;


/* 
 * In charge of the visualization styling control of the cursors, in conjunction with NetMQ Manager and the Control Menu.
 * Also In Charge of General Frame Rate settings.
 * NOTE: Actual cursor position control is achievved via `GenericNetSync.cs` of the player prefab at `Assets/MRTK.Tutorials.MultiUserCapabilities/Resources/PhotonUserGaze.prefab`
 */

public class GazeCursorController : MonoBehaviour
{
    [SerializeField] public float cursorScaleMax = 2.5f;
    [SerializeField] public float cursorScaleMin = 0.5f;
    [SerializeField] public GameObject slider;
    [SerializeField] public Material myMaterial;
    [SerializeField] public GameObject recordButton = null;
    [SerializeField] public GameObject screenObj = null;
    [SerializeField] public GameObject controlMenu = null;
    [SerializeField] public Text dispText = null;
    public AppConfig appConfig;
    //public ExtendedEyeGazeDataProvider extendedEyeGazeDataProvider;

    public bool amIPrimaryUser = true;
    public int myUserIdx = -1;
    private float cursorScaleGradient;
    private bool isOtherCursorVisible = true;
    private int otherCursorStyle = 0;
    private GameObject otherPhotoViewObj = null;
    private GameObject buttonTMP = null;

    private bool isMyCursorVisible = false;
    private int myCursorStyle = 0;
    private GameObject myPhotoViewObj = null;
    [HideInInspector] public bool isRecording;
    [HideInInspector] public int recordingTrialCount;
    private List<GameObject> recordingGameObjs = null;

    private DateTime curRecordStartTime;

    private StreamWriter myGazeWriter = null;
    private StreamWriter otherGazeWriter = null;
    private StreamWriter screenPosWriter = null;

    //private Thread listenerThread;
    //private bool listenerRunning = true;
    private SubscriberSocket subscriberSocket;

    //private Coroutine recordingCoroutine;
    private float dataRecordingRate = 90.0f;

    private int _frameCount = 0;
    private int userCheckSkipFrame = 5;


    // Start is called before the first frame update
    void Start()
    {
        AsyncIO.ForceDotNet.Force();

        Time.fixedDeltaTime = 0.015f; //0.016667f; // 0.011111f;
        // Application.targetFrameRate = -1;
        cursorScaleGradient = cursorScaleMax - cursorScaleMin;
        float curScaleValue = (1 - cursorScaleMin) / cursorScaleGradient;
        slider.GetComponent<PinchSlider>().SliderValue = curScaleValue;
        myPhotoViewObj = null;
        otherPhotoViewObj = null;
        //extendedEyeGazeDataProvider = gameObject.GetComponent<ExtendedEyeGazeDataProvider>();

        isOtherCursorVisible = true;
        isMyCursorVisible = true;
        onToggleMyCursorVisibility();

        if (recordButton != null)
        {
            buttonTMP = recordButton.transform.Find("IconAndText").Find("TextMeshPro").gameObject;
        }
        recordingGameObjs = new List<GameObject> { myPhotoViewObj, otherPhotoViewObj, screenObj };

        //NetMQConfig.Linger = new TimeSpan(0, 0, 1);

        //// InitializeSubscriber();
        //subscriberSocket = new SubscriberSocket();
        //subscriberSocket.Options.Linger = new TimeSpan(0, 0, 1);
        //subscriberSocket.Connect("tcp://localhost:5555"); // Replace with the publisher's IP and port
        //subscriberSocket.SubscribeToAnyTopic();
        //Assert.IsNotNull(subscriberSocket);
        //Debug.Log("Finished Settingup Socket");

        hideMenu();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (appConfig != null && (appConfig.appOperation == false || appConfig.gazeSaveOperation == false))
        {
            //Debug.Log("Not saving gaze");
            return;
        }
        //string message = subscriberSocket.ReceiveFrameString();
        //ProcessMessage(message);

        _frameCount += 1;
        if (_frameCount == userCheckSkipFrame)
        {
            _frameCount = 0;
            // Retrieve (both) photonViews in the scene
            // Assumptions: 1. Only 2 photonViews are present. 2. One is my gaze, the other is the other's gaze.
            // TODO: very inefficient code, try improve it in the future
            var photonViews = FindObjectsOfType<PhotonView>();
            //int curLowestIdx = System.Int32.MaxValue;
            string curLowestNickName = null;
            foreach (PhotonView view in photonViews)
            {
                if (view.IsMine)
                {
                    if (myPhotoViewObj == null)
                    {
                        myPhotoViewObj = view.gameObject;
                        for (int i = 0; i < myPhotoViewObj.transform.childCount; i++)
                        {
                            myPhotoViewObj.transform.GetChild(i).gameObject.GetComponent<Renderer>().material = myMaterial;
                        }
                    }
                }
                else
                {
                    otherPhotoViewObj = view.gameObject;
                    if (curLowestNickName == null || StringComparerHelper.CompareHashedStrings(view.Owner.NickName, curLowestNickName) < 0)
                    {
                        curLowestNickName = view.Owner.NickName;
                    }
                    //if (int.TryParse(otherPhotoViewObj.name.Substring(4), out int otherUserId))
                    //{
                    //    if (curLowestNickName == null || StringComparerHelper.CompareHashedStrings(view.Owner.NickName, curLowestNickName) < 0)
                    //    {
                    //        curLowestIdx = otherUserId;
                    //        curLowestNickName = view.Owner.NickName;
                    //    }
                    //    //Debug.Log("Extracted otherUserIdx: " + otherUserId);
                    //}
                }
            }
            if (myPhotoViewObj != null)
            {
                if (curLowestNickName == null || StringComparerHelper.CompareHashedStrings(myPhotoViewObj.GetComponent<PhotonView>().Owner.NickName, curLowestNickName) < 0)
                {
                    curLowestNickName = myPhotoViewObj.GetComponent<PhotonView>().Owner.NickName;
                    amIPrimaryUser = true;
                }
                else
                {
                    amIPrimaryUser = false;
                }
                //if (myPhotoViewObj != null)
                //{
                //    Debug.Log("My name is: " + myPhotoViewObj.name);
                //}
                ////Debug.Log(myUserIdx);
                //Debug.Log("cur lowest NickName: " + curLowestNickName);
                //Debug.Log("Am I primary User " + amIPrimaryUser);
            }
            //if (myUserIdx == -1  && myPhotoViewObj != null)
            //{
            //    string numberPart = myPhotoViewObj.name.Substring(4);
            //    Debug.Log("Processing my user id " + numberPart);
            //    if (int.TryParse(numberPart, out int userId))
            //    {
            //        myUserIdx = userId;
            //        if (myUserIdx < curLowestIdx)
            //        {
            //            curLowestIdx = myUserIdx;
            //        }
            //        //Debug.Log("extracted myuseridx: " + userId);
            //    }
            //}

            //if (myUserIdx <= curLowestIdx)
            //{
            //    amIPrimaryUser = true;
            //} else
            //{
            //    amIPrimaryUser = false;
            //}
        }


        RecordData();

        //if (dispText != null && myPhotoViewObj != null)
        //{
        //    DateTime timestamp = DateTime.Now;
        //    //var combinedGazereadingInWorldSpace = extendedEyeGazeDataProvider.GetWorldSpaceGazeReading(ExtendedEyeGazeDataProvider.GazeType.Combined, timestamp);
        //    //var combinedGazeReadingInWorldSpace = extendedEyeGazeDataProvider.GetCameraSpaceGazeReading(ExtendedEyeGazeDataProvider.GazeType.Combined, timestamp);
        //    //Debug.Log(combinedGazeReadingInWorldSpace.IsValid);
        //    //Debug.Log(combinedGazeReadingInWorldSpace.EyePosition);
        //    //Debug.Log(combinedGazeReadingInWorldSpace.GazeDirection);
        //    // string curMsg = dispText.text;
        //    //dispText.text = "";
        //    //curMsg = curMsg + "\nRecording";
        //    //dispText.text = combinedGazeReadingInWorldSpace.IsValid.ToString() + " " + combinedGazeReadingInWorldSpace.EyePosition.ToString();
        //    //dispText.text += "\n";
        //    //dispText.text += combinedGazeReadingInWorldSpace.GazeDirection.ToString();
        //    dispText.text = myPhotoViewObj.transform.localPosition.ToString() ;
        //    dispText.text += "\n";
        //    dispText.text += myPhotoViewObj.transform.localRotation.ToString();
        //}
    }

    public string getMyName()
    {
        if (myPhotoViewObj == null)
        {
            return "User1";
        }
        else
        {
            return myPhotoViewObj.name;
        }
    }

    //void ondisable()
    //{
    //    subscribersocket?.dispose();
    //    netmqconfig.cleanup(false);
    //}

    //private void ondestroy()
    //{

    //    subscribersocket?.close();
    //    netmqconfig.cleanup(false);
    //}

    // Not used; Don't Use; This function will create a file descriptor each time it is called, which is hyper-inefficient
    public void saveAllTransformData(List<GameObject> gameObjs, DateTime recordStartTime, string fileNamePrefix = "combined_track_coordinate")
    {
        if (!isRecording)
        {
            return;
        }
        string timeStamp = recordStartTime.ToLocalTime().ToString("yyyyMMdd_HHmmss");

        DateTime curTime = DateTime.Now;
        long curUnixTime = ((DateTimeOffset)curTime).ToUnixTimeMilliseconds();
        string curUnixTimeString = curUnixTime.ToString();

        string filepath_in_function2 = Application.persistentDataPath + "/" + fileNamePrefix + "_" + timeStamp + "_" + recordingTrialCount + ".csv";
        string curLine = "";
        foreach (GameObject obj in gameObjs)
        {
            if (obj != null)
            {
                Transform T = obj.transform;
                curLine += T.localPosition.x + "," + T.localPosition.y + "," + T.localPosition.z + "," + T.localRotation.w + "," + T.localRotation.x + "," + T.localRotation.y + "," + T.localRotation.z + ",";
            }
            else
            {
                curLine += -1 + "," + -1 + "," + -1 + "," + -1 + "," + -1 + "," + -1 + "," + -1 + ",";
            }
        }
        curLine += timeStamp + "," + curUnixTimeString;
        try
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@filepath_in_function2, true))
            {
                file.WriteLine(curLine);
            }
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Failed to save:  ", ex);
        }
    }

    public void saveTransformData(float px, float py, float pz, float rw, float rx, float ry, float rz, DateTime recordStartTime, DateTime curTime, ref StreamWriter writer, string fileNamePrefix = "my_Eye_Gaze_Coordinate")
    {
        // long startUnixTime = ((DateTimeOffset)recordStartTime).ToUnixTimeMilliseconds();
        string timeStamp = recordStartTime.ToLocalTime().ToString("yyyyMMdd_HHmmss");

        long curUnixTime = ((DateTimeOffset)curTime).ToUnixTimeMilliseconds();
        string curUnixTimeString = curUnixTime.ToString();


        if (isRecording)
        {
            //Debug.Log("filepath" + filepath_in_function2);
            //string test_filePath = "U:/Users/yizhou.li@vanderbilt.edu/AppData/Local/Packages/Eyerecorder_pzq3xp76mxafg/LocalState/position_Cursor_IO.csv"; 
            try
            {
                string curFilePath = Application.persistentDataPath + "/" + fileNamePrefix + "_" + timeStamp + "_" + recordingTrialCount + ".csv";
                writer ??= new System.IO.StreamWriter(@curFilePath, true);
                writer.WriteLine(px + "," + py + "," + pz + "," + rw + "," + rx + "," + ry + "," + rz + "," + timeStamp + "," + curUnixTimeString);
            }

            catch (Exception ex)
            {
                writer?.Dispose();
                throw new ApplicationException("Failed to save:  ", ex);
            }
        }
    }

    public void hideMyCursor()
    {
        if (myPhotoViewObj == null)
        {
            return;
        }
        hideCursor(myPhotoViewObj);
        isMyCursorVisible = false;
    }

    public void showMyCursor(int visibleChildIdx)
    {
        if (myPhotoViewObj == null)
        {
            return;
        }
        updateMyCursorStyle(visibleChildIdx);
    }

    public void hideOtherCursor()
    {
        if (otherPhotoViewObj == null)
        {
            return;
        }
        hideCursor(otherPhotoViewObj);
        isOtherCursorVisible = false;
    }

    public void showOtherCursor(int visibleChildIdx)
    {
        if (otherPhotoViewObj == null)
        {
            return;
        }
        updateOtherCursorStyle(visibleChildIdx);
    }


    public void onToggleMyCursorVisibility()
    {
        if (myPhotoViewObj == null)
        {
            return;
        }

        onToggleVisibility(myPhotoViewObj, isMyCursorVisible, myCursorStyle);
        isMyCursorVisible = !isMyCursorVisible;

        Debug.Log("Toggled My Cursor's Visibility to " + isMyCursorVisible.ToString());
    }

    public void onToggleOtherCursorVisibility()
    {
        if (otherPhotoViewObj == null)
        {
            return;
        }

        onToggleVisibility(otherPhotoViewObj, isOtherCursorVisible, otherCursorStyle);
        isOtherCursorVisible = !isOtherCursorVisible;

        Debug.Log("Toggled Other Cursor's Visibility to " + isOtherCursorVisible.ToString());
    }


    public void updateMyCursorStyle(int newIdx)
    {
        if (myPhotoViewObj == null)
        {
            return;
        }

        isMyCursorVisible = true;
        myCursorStyle = newIdx % myPhotoViewObj.transform.childCount;
        showCursorWReset(myPhotoViewObj, myCursorStyle);

        Debug.Log("Updated My Cursor's Visibility to " + myCursorStyle.ToString());
    }

    public void onCycleMyCursorStyle()
    {
        if (myPhotoViewObj == null)
        {
            return;
        }
        updateMyCursorStyle((myCursorStyle + 1) % myPhotoViewObj.transform.childCount);
    }

    public void updateOtherCursorStyle(int newIdx)
    {
        if (otherPhotoViewObj == null)
        {
            return;
        }

        isOtherCursorVisible = true;
        otherCursorStyle = newIdx % otherPhotoViewObj.transform.childCount;
        showCursorWReset(otherPhotoViewObj, otherCursorStyle);

        Debug.Log("Updated Other Cursor's Visibility to " + myCursorStyle.ToString());
    }

    public void onCycleOtherCursorStyle()
    {
        if (otherPhotoViewObj == null)
        {
            return;
        }
        updateOtherCursorStyle((otherCursorStyle + 1) % otherPhotoViewObj.transform.childCount);
    }

    public void setCursorScale(float scale)
    {
        float convertedScale = scale * cursorScaleGradient + cursorScaleMin;
        if (myPhotoViewObj != null)
        {
            myPhotoViewObj.transform.localScale = new Vector3(convertedScale, convertedScale, convertedScale);
        }
        if (otherPhotoViewObj != null)
        {
            otherPhotoViewObj.transform.localScale = new Vector3(convertedScale, convertedScale, convertedScale);
        }
        Debug.Log("Updated Cursors' Scale to" + convertedScale.ToString());
    }

    private void onToggleVisibility(GameObject cursorObj, bool curVisibility, int visibleChildIdx = 0)
    {
        if (curVisibility == true)
        {
            hideCursor(cursorObj);
        }
        else
        {
            showCursorWReset(cursorObj, visibleChildIdx);
        }
    }

    private void hideCursor(GameObject cursorObj)
    {
        for (int childIdx = 0; childIdx < cursorObj.gameObject.transform.childCount; childIdx++)
        {
            cursorObj.transform.GetChild(childIdx).gameObject.GetComponent<Renderer>().enabled = false;
        }
    }

    private void showCursorNoReset(GameObject cursorObj, int visibleChildIdx)
    {
        cursorObj.transform.GetChild(visibleChildIdx).gameObject.GetComponent<Renderer>().enabled = true;
    }

    public void onUpdateCursorScale()
    {
        float scale = slider.GetComponent<PinchSlider>().SliderValue;
        setCursorScale(scale);
    }

    private void showCursorWReset(GameObject cursorObj, int newIdx)
    {
        hideCursor(cursorObj);
        showCursorNoReset(cursorObj, newIdx);
    }

    private void RecordData()
    {
        if (isRecording)
        {
            // Data to Save:
            // My Gaze, local to screen
            // Other's Gaze, local to screen
            // My Screen Position

            DateTime curTime = DateTime.Now;
            saveTransformData(
                myPhotoViewObj.transform.localPosition.x,
                myPhotoViewObj.transform.localPosition.y,
                myPhotoViewObj.transform.localPosition.z,
                myPhotoViewObj.transform.localRotation.w,
                myPhotoViewObj.transform.localRotation.x,
                myPhotoViewObj.transform.localRotation.y,
                myPhotoViewObj.transform.localRotation.z,
                curRecordStartTime,
                curTime,
                ref myGazeWriter,
                "my_Eye_Gaze_Transforms"
                );
            if (otherPhotoViewObj != null)
            {
                saveTransformData(
                    otherPhotoViewObj.transform.localPosition.x,
                    otherPhotoViewObj.transform.localPosition.y,
                    otherPhotoViewObj.transform.localPosition.z,
                    otherPhotoViewObj.transform.localRotation.w,
                    otherPhotoViewObj.transform.localRotation.x,
                    otherPhotoViewObj.transform.localRotation.y,
                    otherPhotoViewObj.transform.localRotation.z,
                    curRecordStartTime,
                    curTime,
                    ref otherGazeWriter,
                    "other_Eye_Gaze_Transforms"
                );
            }

            if (screenObj != null)
            {
                saveTransformData(
                    screenObj.transform.localPosition.x,
                    screenObj.transform.localPosition.y,
                    screenObj.transform.localPosition.z,
                    screenObj.transform.localRotation.w,
                    screenObj.transform.localRotation.x,
                    screenObj.transform.localRotation.y,
                    screenObj.transform.localRotation.z,
                    curRecordStartTime,
                    curTime,
                    ref screenPosWriter,
                    "screen_Track_Transforms"
                );
            }
        }
    }

    IEnumerator SampleAndWriteData()
    {
        float interval = 1.0f / dataRecordingRate; //

        while (true)
        {
            RecordData(); // Your method to sample and buffer data

            yield return new WaitForSecondsRealtime(interval);
        }
    }

    public void startRecording()
    {
        if (isRecording)
        {
            return;
        }

        isRecording = true;
        if (buttonTMP != null)
        {
            buttonTMP.GetComponent<TextMeshPro>().text = "Stop Recording Data";
        }
        myGazeWriter?.Dispose();
        otherGazeWriter?.Dispose();
        screenPosWriter?.Dispose();

        curRecordStartTime = DateTime.Now;
        string timeStamp = curRecordStartTime.ToLocalTime().ToString("yyyyMMdd_HHmmss");
        string myGazeFilePath = Application.persistentDataPath + "/" + "my_Eye_Gaze_Transforms" + "_" + timeStamp + "_" + recordingTrialCount + ".csv";
        string otherGazeFilePath = Application.persistentDataPath + "/" + "other_Eye_Gaze_Transforms" + "_" + timeStamp + "_" + recordingTrialCount + ".csv";
        string screenPoseFilePath = Application.persistentDataPath + "/" + "screen_Track_Transforms" + "_" + timeStamp + "_" + recordingTrialCount + ".csv";
        myGazeWriter = new System.IO.StreamWriter(myGazeFilePath, true);
        otherGazeWriter = new System.IO.StreamWriter(otherGazeFilePath, true);
        screenPosWriter = new System.IO.StreamWriter(screenPoseFilePath, true);

        if (dispText != null)
        {
            // string curMsg = dispText.text;
            //dispText.text = "";
            //curMsg = curMsg + "\nRecording";
            dispText.text = "Recording Gaze";
        }

        //recordingCoroutine = StartCoroutine(SampleAndWriteData());
    }

    public void stopRecording()
    {
        if (!isRecording)
        {
            return;
        }

        //StopCoroutine(recordingCoroutine);
        recordingTrialCount++;
        isRecording = false;
        if (buttonTMP != null)
        {
            buttonTMP.GetComponent<TextMeshPro>().text = "Record Gaze Data";
        }
        myGazeWriter?.Dispose();
        otherGazeWriter?.Dispose();
        screenPosWriter?.Dispose();

        if (dispText != null)
        {
            // string curMsg = dispText.text;

            // dispText.text = "";
            // curMsg = curMsg.Split('\n')[0] + "\nRecording";
            dispText.text = "Not Recording Gaze";
        }
    }

    public void onToggleGazeRecording()
    {
        if (isRecording)
        {
            stopRecording();
        }
        else
        {
            startRecording();
        }
    }


    //private void InitializeSubscriber()
    //{
    //    listenerThread = new Thread(ListenerLoop);
    //    listenerThread.IsBackground = true;
    //    listenerThread.Start();
    //}


    //private void ListenerLoop()
    //{
    //    AsyncIO.ForceDotNet.Force(); // Ensure proper cleanup of sockets
    //    using (subscriberSocket = new SubscriberSocket())
    //    {
    //        subscriberSocket.Connect("tcp://localhost:5555"); // Replace with the publisher's IP and port
    //        subscriberSocket.SubscribeToAnyTopic();

    //        while (listenerRunning)
    //        {
    //            try
    //            {
    //                string message = subscriberSocket.ReceiveFrameString();
    //                ProcessMessage(message);
    //            }
    //            catch (NetMQException)
    //            {
    //                // Handle socket closure or other issues gracefully
    //            }
    //        }
    //    }
    //    NetMQConfig.Cleanup();
    //}

    private void ProcessMessage(string message)
    {
        Debug.Log($"Received message: {message}");

        // Parse topic and payload
        string[] parts = message.Split(':');
        if (parts.Length < 2) return; // Ignore invalid messages

        string topic = parts[0].Trim();
        string payload = parts[1].Trim();

        switch (topic)
        {
            case "DataCollection":
                if (payload == "StartRecording")
                {
                    startRecording();
                }
                if (payload == "StopRecording")
                {
                    stopRecording();
                }
                break;
            case "CursorVisual/User1":
                if (int.TryParse(payload, out int user1Style))
                    updateMyCursorStyle(user1Style);
                break;

            case "CursorVisual/User2":
                if (int.TryParse(payload, out int user2Style))
                    updateOtherCursorStyle(user2Style);
                break;

            case "CursorSize":
                if (float.TryParse(payload, out float cursorSize))
                    setCursorScale(cursorSize);
                break;

            default:
                Debug.LogWarning($"Unknown topic: {topic}");
                break;
        }
    }

    public void hideMenu()
    {
        if (controlMenu != null)
        {
            controlMenu.SetActive(false);
            Debug.Log("Menu Hidden!");
        }
        else
        {
            Debug.Log("Menu is Null, can't be hidden!");
        }
    }

    public void showMenu()
    {
        if (controlMenu != null)
        {
            controlMenu.SetActive(true);
            Debug.Log("Menu Shown!");
        }
        else
        {
            Debug.Log("Menu is Null, can't be shown!");
        }
    }
}


public static class StringComparerHelper
{
    public static byte[] HashStringSHA256(string input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        }
    }

    public static int CompareHashedStrings(string a, string b)
    {
        byte[] hashA = HashStringSHA256(a);
        byte[] hashB = HashStringSHA256(b);

        for (int i = 0; i < hashA.Length; i++)
        {
            if (hashA[i] < hashB[i]) return -1;
            if (hashA[i] > hashB[i]) return 1;
        }
        return 0;
    }
}
