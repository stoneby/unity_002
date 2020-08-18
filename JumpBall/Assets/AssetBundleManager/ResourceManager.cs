using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

public class ResourceManager : SingletonUnity<ResourceManager>
{
    [Serializable]
    public class BundleCounter
    {
        public int counter;
        public AssetBundle assetBundle;
    }

    public static string BundleBase = "AssetBundles";
    public static string BundleManifestName = "AssetBundleManifest";

    private readonly string logHead = ("ResourceManager");

    /// <summary>
    /// AssetBundle Url, empty using StreamingAssetPath.
    /// </summary>
    public string AssetBundleUrl
    {
        get => assetBundleUrl;
        set => assetBundleUrl = new Uri($"{value}").AbsoluteUri;
    }

    public string CurrentPlatform => Application.platform == RuntimePlatform.IPhonePlayer ? "IOS" : "Android"; 

    [SerializeField]
    private string assetBundleUrl;

    /// <summary>
    /// AssetBundles/AssetBundles.manifest file.
    /// </summary>
    private AssetBundleManifest manifest;

    private readonly Dictionary<string, BundleCounter> assetBundleMap = new Dictionary<string, BundleCounter>();

    public IEnumerator LoadManifest(Action<bool> callback)
    {
        if (manifest == null)
        {
            var manifestUri = GetBundleUri(BundleBase);
            yield return StartCoroutine(LoadAsset(manifestUri, (bundle, error) =>
            {
                manifest = bundle.LoadAsset<AssetBundleManifest>(BundleManifestName);

                if (manifest == null)
                {
                    Debug.LogError($"{logHead} Init fails to fetch manifest in {BundleManifestName}");
                    callback(false);
                }

                Add(BundleBase, bundle);
                callback(true);
            }));
        }
    }

    /// <summary>
    /// Adding assetbundle to map, bundle name lowercase as key.
    /// </summary>
    /// <param name="bundleName">Bundle name in lower case.</param>
    /// <param name="assetBundle">Asset bundle.</param>
    private void Add(string bundleName, AssetBundle assetBundle)
    {
        Debug.Log($"{logHead} Adding asset bundle:{bundleName}, {assetBundle}");

        var abCounter = new BundleCounter
        {
            assetBundle = assetBundle
        };
        assetBundleMap.Add(bundleName, abCounter);
    }

    private BundleCounter GetBundleCounter(string bundleName)
    {
        if (assetBundleMap.ContainsKey(bundleName))
        {
            return assetBundleMap[bundleName];
        }
        return null;
    }

    /// <summary>
    /// Load all asset bundles according to manifest file.
    /// </summary>
    public IEnumerator LoadAllBundles(Action<bool> callback)
    {
        var bundles = manifest.GetAllAssetBundles();
        var result = true;
        foreach (var bundleName in bundles)
        {
            yield return StartCoroutine(LoadAsset(bundleName, flag => result = result && flag));
        }
        callback(result);
    }

    private IEnumerator LoadAsset(string bundleName, Action<bool> callback)
    {
        Debug.Log($"{logHead} LoadBundle {bundleName}");

        if (GetBundleCounter(bundleName) == null)
        {
            var dependencies = manifest.GetAllDependencies(bundleName);
            foreach (var depend in dependencies)
            {
                if (GetBundleCounter(depend) == null)
                    yield return StartCoroutine(LoadAsset(depend, callback));
            }

            var uri = GetBundleUri(bundleName);
            yield return StartCoroutine(LoadAsset(uri, (bundle, error) =>
            {
                var success = string.IsNullOrEmpty(error);
                if (success)
                    Add(bundleName, bundle);
                else
                    Debug.LogError($"Error loading asset bundle {bundleName} from uri {uri}");
                callback(success);
            }));
        }
    }

    public T LoadAsset<T>(string pair)
        where T : Object
    {
        var index = pair.LastIndexOf('/');
        var bundleName = pair.Substring(0, index);
        var assetName = pair.Substring(index + 1);
        return LoadAsset<T>(bundleName, assetName);
    }

    public T LoadAsset<T>(string bundleName, string assetName)
        where T : Object
    {
        Debug.Log($"{logHead} LoadAsset with bundle:{bundleName} assetName:{assetName}");

        var bundle = GetBundleCounter(bundleName);
        return bundle?.assetBundle.LoadAsset<T>(assetName);
    }

    public void UnloadAsset(string bundleName, bool unloadAllLoadedObjects, bool considerCounerFlag = false)
    {
        var abCounter = GetBundleCounter(bundleName);
        if (abCounter != null && abCounter.assetBundle != null)
        {
            var flag = true;
            if (considerCounerFlag)
            {
                abCounter.counter--;
                flag = abCounter.counter <= 0;
            }
            if (flag)
            {
                abCounter.assetBundle.Unload(unloadAllLoadedObjects);
                assetBundleMap.Remove(bundleName);
            }
        }
    }

    /// <summary>
    /// Bundle file full path.
    /// </summary>
    /// <param name="relativePath">Relative path with upper case.</param>
    /// <returns>The bundle full path.</returns>
    private string GetBundleUri(string relativePath)
    {
        return Path.Combine(AssetBundleUrl, relativePath);
    }

    /// <summary>
    /// Load asset from url, callback when assetbundle is loaded.
    /// </summary>
    /// <param name="uri">Asset bundle uri.</param>
    /// <param name="callback">Callback after loading.</param>
    /// <returns></returns>
    private IEnumerator LoadAsset(string uri, Action<AssetBundle, string> callback)
    {
        Debug.Log($"{logHead} LoadBundle from {uri}");

        using (var uwr = UnityWebRequestAssetBundle.GetAssetBundle(uri))
        {
            yield return uwr.SendWebRequest();

            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Debug.Log(uwr.error);
                callback(null, uwr.error);
            }
            else
            {
                // Get downloaded asset bundle
                var bundle = DownloadHandlerAssetBundle.GetContent(uwr);
                callback(bundle, uwr.error);
            }
        }
    }
}