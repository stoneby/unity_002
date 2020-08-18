using System.IO;
using UnityEngine;
using UnityEditor;

public class CreatAssetsBundle : Editor
{
    [MenuItem("Tools/IOS/Build AssetBundle Scene")]
    static void MyBuildIOS()
    {
        Build(BuildTarget.iOS);
    }

    [MenuItem("Tools/Android/Build AssetBundle Scene")]
    static void MyBuildAndroid()
    {
        Build(BuildTarget.Android);
    }

    static void Build(BuildTarget target)
    {
        var config = JsonUtility.FromJson<Config>(Resources.Load<TextAsset>("Config").text);

        BuildPipeline.BuildAssetBundles($"Assets/StreamingAssets/{config.BundleName}", BuildAssetBundleOptions.None, target);

        string[] path = { "Assets/Scene/game.unity" };
        BuildPipeline.BuildPlayer(path, Path.Combine(Application.streamingAssetsPath, config.SceneName), target, BuildOptions.BuildAdditionalStreamedScenes);

        AssetDatabase.Refresh();
    }
}