using UnityEngine;

[CreateAssetMenu(menuName = "Build Asset")]
public class BuildAsset : ScriptableObject
{
    public static string BuildAssetPath = "BuildInfo";

    public static BuildAsset LoadBuildInfo()
    {
        return Resources.Load<BuildAsset>(BuildAssetPath);
    }

    public int BuildNumber;
}