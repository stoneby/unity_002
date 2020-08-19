using System;
using System.IO;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class LoadScene : MonoBehaviour
{
    public string BundleName;
    public string SceneName;

    void Start()
    {
        var config = JsonUtility.FromJson<GameConfig>(Resources.Load<TextAsset>("GameConfig").text);
        var sceneUrl = new Uri(Path.Combine(Application.streamingAssetsPath, config.SceneName)).AbsoluteUri;
        var bundleUrl = new Uri($"{Application.streamingAssetsPath}/{config.BundleName}/{config.AssetName}").AbsoluteUri;

        StartCoroutine(DoLoadScene(bundleUrl));
        StartCoroutine(DoLoadScene(sceneUrl));
    }

    IEnumerator DoLoadScene(string url)
    {
        Debug.Log(url);

        using (UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Debug.Log(uwr.error);
            }
            else
            {
                // Get downloaded asset bundle
                var bundle = DownloadHandlerAssetBundle.GetContent(uwr);
                if (bundle.isStreamedSceneAssetBundle)
                {
                    var scenePath = bundle.GetAllScenePaths()[0];
                    Debug.Log(scenePath);
                    SceneManager.LoadScene(scenePath); 
                }
                else
                {
                    var asset = bundle.LoadAllAssets();
                    foreach (var a in asset)
                    {
                        Debug.Log(a.name);
                    }
                }
            }
        }
    }
}
