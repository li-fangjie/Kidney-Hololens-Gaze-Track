#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildIncrementorPostProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        IncrementBuild();
    }

    public static void IncrementBuild()
    {
        string path = "Assets/Resources/" + BuildAsset.BuildAssetPath + ".asset";
        var asset = AssetDatabase.LoadAssetAtPath<BuildAsset>(path);
        if (asset == null)
        {
            Debug.LogError("Could not find build asset " + BuildAsset.BuildAssetPath);
            return;
        }

        asset.BuildNumber++;
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(path));
        Debug.Log("Incremented build to " + asset.BuildNumber);
    }
}
#endif