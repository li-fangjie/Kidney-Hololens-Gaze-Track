using MathNet.Numerics.LinearAlgebra.Factorization;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Unity.VisualScripting.Antlr3.Runtime;
using ArUcoDetectionHoloLensUnity;

public class ScreenTracker : MonoBehaviour
{
    public GameObject arucoMarkerDetectorObj = default;
    public GameObject bottomLeftMarker = default;
    public GameObject bottomRightMarker = default;
    public GameObject topLeftMarker = default;
    public GameObject topRightMarker = default;
    public GameObject screenObj = null;
    public GameObject screenDispObj = null;
    public GameObject screenFrame = null;
    [SerializeField] Material screenMaterialGreen = null;
    [HideInInspector] public List<ArucoMarker> markers; // Bottom Left, Bottom Right, Top Left, Top Right
    [HideInInspector] public List<GameObject> markerObjs; // Bottom Left, Bottom Right, Top Left, Top Right
    [SerializeField] AppConfig appConfig;
    public Text debugText = null;

    public bool screenCenterToMarkersDetermined = false;
    public int screenCenterToMarkersDetermineFrames = 100;

    public float RSmoothFactor = 0.8f; // The positions
    public float VSmoothFactor = 0.8f;

    public float screenWidth = 0;
    public float screenHeight = 0;

    private bool trackStarted = false;
    private int trackStartCount = 0;
    public int initTrackTotal = 30;

    public int skipFrames = 3;
    private int _frameCount = 0;

    // Start is called before the first frame update
    void Start()
    {
        trackStarted = false;
        trackStartCount = 0;
        markerObjs = new List<GameObject> { bottomLeftMarker, bottomRightMarker, topLeftMarker, topRightMarker };
        markers = new List<ArucoMarker>();
        
        foreach (var markerObj in markerObjs)
        {
            markers.Add(markerObj.GetComponent<ArucoMarker>());
        }

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
    }

    private void Update()
    { // Using Update() instead of fixedUpdate, because arucoMarkerDetection also uses it.
        if (appConfig != null && (appConfig.appOperation == false || appConfig.arUcoOperation == false))
        {
            //Debug.Log("Not tracking screen");
            return;
        }
        _frameCount++;

        if (_frameCount == skipFrames)
        {
            // Initialize Screen Center transform relative to other coordinates
            if ((!screenCenterToMarkersDetermined) & screenCenterToMarkersDetermineFrames > 0 & AllMarkersTracked())
            {
                Debug.Log("All markers visible, determining marker to screen T");
                if (debugText != null)
                {
                    debugText.text = string.Format("Registering Screen. Keep the screen in view until the virtual screen frame disappears.");
                }
                

                DetermineCenterOnAllMarkersVisible();
                DetermineScreenSizeOnAllMarkersVisible();
                UpdateScreenObjScale();

                foreach (var marker in markers)
                {
                    marker.newCoordTracked = false;
                }

                screenCenterToMarkersDetermineFrames--;
            }
            else if (screenCenterToMarkersDetermineFrames <= 0)
            {
                screenCenterToMarkersDetermined = true;
                // Finished Initialization - marker to screen center transform available
                List<GameObject> curTrackedMarkerToScreenObjs = GetAllTrackedMarkersToScreenObjs();
                if (curTrackedMarkerToScreenObjs.Count > 0) 
                {
                    GameObject THolder = TransformUtils.AverageTransforms(curTrackedMarkerToScreenObjs);
                    TransformUtils.LerpTransforms(screenObj, THolder, RSmoothFactor, VSmoothFactor, screenObj);
                    Destroy(THolder);

                    foreach (var marker in markers)
                    {
                        marker.GetComponent<ArucoMarker>().newCoordTracked = false;
                    }
                    if (screenFrame != null)
                    {
                        Debug.Log("Updating Frame Materials");
                        for (int i = 0; i < screenFrame.transform.childCount; i++)
                        {
                            //screenFrame.transform.GetChild(i).gameObject.GetComponent<Renderer>().material = screenMaterialGreen;
                            screenFrame.transform.GetChild(i).gameObject.GetComponent<Renderer>().enabled = false;
                        }
                    }
                
                    // Debug.Log("Marker to Screen T determined, now normal tracking...");
                    // debugText.text = string.Format("Marker to Screen T determined, now normal tracking...\n{0}\n{1}", screenObj.transform.position, screenObj.transform.eulerAngles);
                    if (debugText != null)
                    {
                        debugText.text = string.Format("Screen Registered.");
                    }
                }
            }
            _frameCount = 0;
        }
    }

    private bool AllMarkersTracked()
    {
        foreach (var marker in markers)
        {
            if (!marker.newCoordTracked)
            {
                return false;
            }
        }
        return true;
    }

    private List<ArucoMarker> GetAllTrackedMarkers()
    {
        List<ArucoMarker> trackedMarkers = new List<ArucoMarker>();
        foreach (var marker in markers)
        {
            if (marker.newCoordTracked)
            {
                trackedMarkers.Add(marker);
            }
        }
        return trackedMarkers;
    }
    private List<GameObject> GetAllTrackedMarkersObjs()
    {
        List<GameObject> trackedMarkersObj = new List<GameObject>();
        foreach (var marker in markers)
        {
            if (marker.newCoordTracked)
            {
                trackedMarkersObj.Add(marker.markerObj);
            }
        }
        return trackedMarkersObj;
    }

    private List<GameObject> GetAllTrackedMarkersToScreenObjs()
    {
        List<GameObject> trackedMarkersObj = new List<GameObject>();
        foreach (var marker in markers)
        {
            if (marker.newCoordTracked)
            {
                trackedMarkersObj.Add(marker.transformToScreenObj);
            }
        }
        return trackedMarkersObj;
    }


    // All four markers are tracked
    void DetermineCenterOnAllMarkersVisible()
    {
        // Compute a "good" estimate of the current screen coordinate,
        // then computes the relative transform between each marker and the screen coordinate

        // Assumptions:
        // The markers are at the four corners of the screen ;
        // The markers have the same orientation (xyz)

        // Compute current screen center transform
        if (trackStartCount >= initTrackTotal)
        {
            trackStarted = true;
            GameObject curTHolder = TransformUtils.AverageTransforms(markerObjs);
            TransformUtils.LerpTransforms(screenObj, curTHolder, RSmoothFactor, VSmoothFactor, screenObj);
            Destroy(curTHolder);
        } else
        {
            TransformUtils.AverageTransforms(markerObjs, screenObj);
            trackStartCount++;
        }
        
        // screenObj.transform.rotation = Quaternion.Slerp(screenObj.transform.rotation, curTHolder.transform.rotation, RSmoothFactor);
        // screenObj.transform.position = Vector3.Lerp(screenObj.transform.position, curTHolder.transform.position, VSmoothFactor);

        // Compute transform from each marker to center
        foreach (var marker in markers)
        {
            // marker.transformToScreenObj.transform.position = screenObj.transform.InverseTransformPoint(marker.markerObj.transform.position);
            marker.transformToScreenObj.transform.localPosition = marker.markerObj.transform.InverseTransformPoint(screenObj.transform.position);
            // marker.transformToScreenObj.transform.rotation = Quaternion.Inverse(screenObj.transform.rotation) * marker.markerObj.transform.rotation;
            marker.transformToScreenObj.transform.localRotation = Quaternion.Inverse(marker.markerObj.transform.rotation) * screenObj.transform.rotation;
        }
    }

    void DetermineScreenSizeOnAllMarkersVisible()
    {
        Vector3 BLScreenPos = screenObj.transform.InverseTransformPoint(bottomLeftMarker.transform.position);
        Vector3 BRScreenPos = screenObj.transform.InverseTransformPoint(bottomRightMarker.transform.position);
        Vector3 TLScreenPos = screenObj.transform.InverseTransformPoint(topLeftMarker.transform.position);
        Vector3 TRScreenPos = screenObj.transform.InverseTransformPoint(topRightMarker.transform.position);
        
        /*
        List <Vector3> markerPositionsOnScreens = new List<Vector3>();
        foreach (var marker in markers)
        {
            // Note: The marker and screen definition of "up" is z
            markerPositionsOnScreens.Add(screenObj.transform.TransformPoint(marker.markerObj.transform.position));
        }
        */
        // Bottom Left, Bottom Right, Top Left, Top Right
        screenWidth = (Mathf.Abs(BRScreenPos.x - BLScreenPos.x) + Mathf.Abs(TRScreenPos.x - TLScreenPos.x)) / 2 + arucoMarkerDetectorObj.GetComponent<ArUcoMarkerDetection>().markerSize * 2.1f;
        screenHeight = (Mathf.Abs(TLScreenPos.y - BLScreenPos.y) + Mathf.Abs(TRScreenPos.y - BRScreenPos.y)) / 2 + arucoMarkerDetectorObj.GetComponent<ArUcoMarkerDetection>().markerSize * 2.1f;
        // debugText.text += string.Format("\nWidth {0}, Height {1}", screenWidth, screenHeight);
    }

    void UpdateScreenObjScale()
    {
        screenDispObj.transform.localScale = new Vector3(screenWidth, screenHeight, 1f); ;
    }
}


public static class TransformUtils
{

    public static GameObject LerpTransforms(GameObject a, GameObject b, float RSmoothFactor, float VSmoothFactor, GameObject outputObj = null)
    {
        if (outputObj  == null)
        {
            outputObj = new GameObject();
        }
        outputObj.transform.rotation = Quaternion.Slerp(a.transform.rotation, b.transform.rotation, RSmoothFactor);
        outputObj.transform.position = Vector3.Lerp(a.transform.position, b.transform.position, VSmoothFactor);

        return outputObj;
    }

    public static GameObject AverageTransforms(List<GameObject> transformObjs, GameObject outputTHolder=null)
    {

        if (transformObjs.Count == 0)
        {
            throw new System.ArgumentException("Empty Transforms list provided");
        }
        List<Quaternion> markersR = new List<Quaternion>();
        List<Vector3> markersV = new List<Vector3>();
        foreach (var obj in transformObjs)
        {
            markersR.Add(obj.transform.rotation);
            markersV.Add(obj.transform.position);
        }
        if (outputTHolder == null)
        {
            outputTHolder = new GameObject();
        }
        outputTHolder.transform.rotation = QuaternionAverage(markersR);
        outputTHolder.transform.position = VectorAverage(markersV);

        return outputTHolder;
    }

    public static Quaternion QuaternionAverage(List<Quaternion> quats, List<double> weights = null)
    {
        if (weights != null && quats.Count != weights.Count)
        {
            throw new System.ArgumentException("Arguments are of different lengths.");
        }

        if (quats.Count == 0)
        {
            throw new System.ArgumentException("Empty vector list provided");
        }


        // Initialize average quaternion with zero components
        Vector<double> qAvg = Vector<double>.Build.Dense(4, 0.0);
        Vector<double> q0 = DenseVector.OfArray(new double[] { quats[0].x, quats[0].y, quats[0].z, quats[0].w });

        for (int i = 0; i < quats.Count; i++)
        {
            Quaternion q = quats[i];
            double weight = weights == null ? 1.0 : weights[i];
            Vector<double> qVec = DenseVector.OfArray(new double[] { q.x, q.y, q.z, q.w });
            
            Vector<double> qi = DenseVector.OfArray(new double[] { quats[i].x, quats[i].y, quats[i].z, quats[i].w });
            // Correct for double cover, ensuring dot product of quats[i] and quats[0] is positive
            if (i > 0 && qi.DotProduct(q0)< 0.0)
            {
                weight = -weight;
            }

            // Weighted sum of quaternion components
            qAvg[0] += weight * q.x;
            qAvg[1] += weight * q.y;
            qAvg[2] += weight * q.z;
            qAvg[3] += weight * q.w;
        }

        // Normalize the resulting quaternion
        // double magnitude = qAvg.L2Norm();
        // qAvg = qAvg / magnitude;
        qAvg = qAvg.Normalize(2);

        return new Quaternion((float)qAvg[0], (float)qAvg[1], (float)qAvg[2], (float)qAvg[3]);
    }

    // Approach: https://www.acsu.buffalo.edu/%7Ejohnc/ave_quat07.pdf
    // Or: https://math.stackexchange.com/questions/61146/averaging-quaternions
    public static Quaternion QuaternionAverageEig(List<Quaternion> quats, List<double> weights = null)
    {
        if (weights != null && quats.Count != weights.Count)
        {
            throw new System.ArgumentException("Args are of different length");
        }

        if (quats.Count == 0)
        {
            throw new System.ArgumentException("Empty quaternions list provided");
        }
        

        // Initialize a 4x4 matrix for accumulation
        Matrix<double> accum = DenseMatrix.OfArray(new double[4, 4]);

        for (int i = 0; i < quats.Count; i++)
        {
            Quaternion q = quats[i];
            double weight = weights == null ? 1.0 : weights[i];
            Vector<double> qVec = DenseVector.OfArray(new double[] { q.x, q.y, q.z, q.w });

            Matrix<double> qOuterProdWeighted = qVec.OuterProduct(qVec).Multiply(weight);
            accum = accum.Add(qOuterProdWeighted);
        }

        Evd<double> eigDecomp = accum.Evd(Symmetricity.Symmetric);
        Vector<double> ev0 = eigDecomp.EigenVectors.Column(0);
        Debug.Log(string.Format("{0}", eigDecomp.EigenValues));
        Debug.Log(string.Format("{0}", accum));
        Debug.Log(string.Format("{0}", ev0));

        if (ev0.DotProduct(DenseVector.OfArray(new double[] { quats[0].x, quats[0].y, quats[0].z, quats[0].w,})) < 0.0) {
            ev0 = ev0.Multiply(-1.0);
            Debug.Log("Inverting Quat Vector");
        }
        Quaternion result = new Quaternion((float)ev0[0], (float)ev0[1], (float)ev0[2], (float)ev0[3]);
        result.Normalize();

        return result;
    }

    public static Vector3 VectorAverage(List<Vector3> vectors)
    {
        if (vectors == null || vectors.Count == 0)
        {
            throw new System.ArgumentException("The list of vectors is either null or empty.");
        }

        Vector3 sum = Vector3.zero;
        foreach (Vector3 vec in vectors)
        {
            sum += vec;
        }
        Vector3 average = sum / vectors.Count;

        return average;
    }
}

