using UnityEngine;
using System.Collections;
using Mogo.Util;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Linq;

public class TestAssetbundleDownLoad : MonoBehaviour {
    /// <summary>
    /// 存放被掉用资源信息。(name为主键)
    /// </summary>
    private Dictionary<string, AssetBundleInfo> m_assetBundleDic = new Dictionary<string, AssetBundleInfo>();
    /// <summary>
    /// 存放所有资源信息。(path为主键)
    /// </summary>
    private Dictionary<string, AssetBundleInfo> m_allResources = new Dictionary<string, AssetBundleInfo>();
    private bool m_isLoading;

    public string prefabName = "boxbrass.prefab";

    void OnGUI()
    {
        prefabName = GUI.TextField(new Rect(0, 60, 200, 40), prefabName);

        if (GUI.Button(new Rect(100, 100, 100, 100), "load info"))
        {
            LoadAssetBundleInfo();
        }
        if (GUI.Button(new Rect(0, 100, 100, 100), "create"))
        {
            LoadAssetBundleInfo(prefabName, (abi) =>
                {
                    abi.GetInstance();
                });
        }
    }

    private Dictionary<string, string> m_dicMap = new Dictionary<string, string>();
	
    void LoadAssetBundleInfo()
    {
        var buildInfo = LoadBuildResourcesInfo();

        foreach (var build in buildInfo)
        {
            var root = new DirectoryInfo(Application.dataPath);
            var path = Path.Combine(root.Parent.FullName, "Export\\0.0.0.1\\MogoResources");
            var dirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
            foreach(var item in dirs)
            {
                var folder = Path.Combine(item, build.type);
                var defFiles = Directory.GetFiles(folder);
                
                var fileName = Utils.GetFileNameWithoutExtention(defFiles[0], '\\') + build.extentions[0];
                if (!m_dicMap.ContainsKey(fileName))
                    m_dicMap.Add(fileName, item);
                else
                    LoggerHelper.Debug("fileName exist: " + fileName);

                BuildMogoAssetBundleInfo(folder);
            }
        }

        LoggerHelper.Debug("m_allResources.Count: " + m_allResources.Count);
        LoggerHelper.Debug("m_assetBundleDic.Count: " + m_assetBundleDic.Count);
    }

    private void BuildMogoAssetBundleInfo(string folder)
    {
        var defFiles = Directory.GetFiles(folder);
        foreach(var item in defFiles)
        {
            var xmlText = Utils.LoadFile(item);
            var xml = XMLParser.LoadXML(xmlText);

            foreach(SecurityElement se in xml.Children)
            {
                var asset = GetAssetBundleInfo(se, string.Empty);
                var key = Utils.GetFileName(asset.Path.Replace("\\", "/"));
                if (!m_assetBundleDic.ContainsKey(key))
                {
                    m_assetBundleDic[key] = asset;
                }
            }
        }
    }

    private AssetBundleInfo GetAssetBundleInfo(SecurityElement se, string parentFolder)
    {
        var path = string.Concat(parentFolder, (se.Children[0] as SecurityElement).Text);
        Debug.Log("GetAssetBundleInfo " + path);
        if (m_allResources.ContainsKey(path))
        {
            return m_allResources[path];
        }
        else
        {
            var ab = new AssetBundleInfo();
            ab.Path = path;
            for (int i=1;i<se.Children.Count;i++)
            {
                var child = se.Children[i] as SecurityElement;
                ab.SubResource.Add(GetAssetBundleInfo(child, parentFolder));
            }
            m_allResources.Add(path, ab);
            return ab;
        }
    }

    public static List<BuildResourcesInfo> LoadBuildResourcesInfo()
    {
        var path = string.Concat(GetFolderPath("ResourceDef"), "/ForBuild.xml");

        var xml = XMLParser.LoadXML(Utils.LoadFile(path));
        if (xml == null)
        {
            return null;
        }
        var result = new List<BuildResourcesInfo>();
        
        foreach(SecurityElement item in xml.Children)
        {
            var info = new BuildResourcesInfo();
            //info.check = true;
            info.name = (item.Children[0] as SecurityElement).Text;
            info.type = (item.Children[1] as SecurityElement).Text;
            //info.copyOrder = (item.Children[2] as SecurityElement).Text;
            //string isMerge = (item.Children[3] as SecurityElement).Text;
            //info.isPopInBuild = bool.Parse((item.Children[4] as SecurityElement).Text);
            info.extentions = (item.Children[5] as SecurityElement).Text.Split(' ');
            var folders = item.Children[6] as SecurityElement;
            info.folders = new List<BuildResourcesSubInfo>();
            foreach(SecurityElement folder in folders.Children)
            {
                var sub = new BuildResourcesSubInfo()
                {
                    path = (folder.Children[0] as SecurityElement).Text,
                    deep = bool.Parse((folder.Children[1] as SecurityElement).Text),
                    check = true
                };
                info.folders.Add(sub);
            }
            result.Add(info);
        }

        return result;
    }

    public static string GetFolderPath(string folder)
    {
        var root = new DirectoryInfo(Application.dataPath);
        var path = Path.Combine(root.Parent.FullName, folder);
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }

    private HashSet<string> GetAllFile(AssetBundleInfo info)
    {
        HashSet<string> list = new HashSet<string>();
        if (info.Asset == null)
        {
            foreach(var item in info.SubResource)
            {
                var subRes = GetAllFile(item);
                foreach(var set in subRes)
                {
                    if (!list.Add(set))
                        LoggerHelper.Debug("exist 1: " + set);
                }
            }
            if (!list.Add(info.Path))
                LoggerHelper.Debug("exist 2: " + info.Path);
        }

        return list;
    }


    private Queue<System.Action> m_resourceLoadQueue = new Queue<System.Action>();
    void LoadAssetBundleInfo(string prefab, System.Action<AssetBundleInfo> loaded)
    {
        AssetBundleInfo assetBundleInfo;
        var flag = m_assetBundleDic.TryGetValue(prefab, out assetBundleInfo);
        if (flag)
        {
            if (assetBundleInfo.Asset != null)
            {
                loaded(assetBundleInfo);
            }
            else
            {
                LoadListInfo loadList = new LoadListInfo();
                loadList.Root = prefab;
                loadList.LoadList = new List<string>();
                loadList.LoadList.AddRange(GetAllFile(assetBundleInfo));    // 获取加载资源列表

                LoggerHelper.Debug("FileCount: " + loadList.LoadList.Count);
                if (m_isLoading)
                {
                    m_resourceLoadQueue.Enqueue(()=>{StartCoroutine(StartLoadAsset(loadList, loaded));});
                }
                else
                {
                    m_isLoading = true;
                    StartCoroutine(StartLoadAsset(loadList, loaded));
                }
            }
        }
        else
        {
            LoggerHelper.Warning(prefab + " does not exist.");
        }
    }

    private IEnumerator StartLoadAsset(LoadListInfo loadList, System.Action<AssetBundleInfo> loaded)
    {
        while (true)
        {
            if (loadList.Download != null)
                loadList.Download.Dispose();
            string path = loadList.Next;
            LoggerHelper.Debug("path" + path);
            var key = Utils.GetFileName(path);
            LoggerHelper.Debug("key" + key);
            var info = m_allResources[path];
            if (info.Asset == null)
            {
                var temp = string.Concat(SystemConfig.ASSET_FILE_HEAD, m_dicMap.Get(Utils.GetFileName(loadList.Root)), "/", path, SystemConfig.ASSET_FILE_EXTENSION);
                temp = temp.Replace("\\", "/");
                LoggerHelper.Debug(temp);
                loadList.Download = new WWW(temp);
                yield return loadList.Download;

                info.Asset = loadList.Download.assetBundle;
            }

            if (!loadList.HasNext)
            {
                var assetInfo = m_assetBundleDic[loadList.Root];
                try
                {
                    if (loaded != null)
                        loaded(assetInfo);
                }
                catch (System.Exception ex)
                {
                    LoggerHelper.Except(ex);
                }
                ContinueLoad();
                yield break;
            }
        }
    }

    private void ContinueLoad()
    {
        if (m_resourceLoadQueue.Count != 0)
        {
            System.Action action = m_resourceLoadQueue.Dequeue();
            action();
        }
        else
            m_isLoading = false;
    }

    // 资源加载控制索引器
    public class LoadListInfo
    {
        public string Root { get; set; }
        public WWW Download { get; set; }
        public List<string> LoadList { get; set; }
        public int Index { get; set; }
        public bool HasNext
        {
            get { return Index < LoadList.Count; }
        }
        public string Next
        {
            get { return LoadList[Index++]; }
        }
    }
    public class BuildResourcesInfo
    {
        public string name { get; set; }
        public string type { get; set; }
        public string copyOrder { get; set; }
        public bool isPopInBuild { get; set; }
        public string[] extentions { get; set; }
        public bool check { get; set; }
        public List<BuildResourcesSubInfo> folders { get; set; }
    }

    public class BuildResourcesSubInfo
    {
        public string path { get; set; }
        public bool deep { get; set; }
        public bool check { get; set; }
    }

    public class AssetBundleInfo
    {
        private Object m_gameObject;

        public string Path { get; set; }
        public int ReferenceCount { get; set; }
        public AssetBundle Asset { get; set; }
        public bool IsGameObjectLoaded { get { return m_gameObject; } }
        public Object GameObject
        {
            get
            {
                if (!m_gameObject)
                {
                    m_gameObject = Asset.Load(Path);
                }
                return m_gameObject;
            }
        }
        public List<AssetBundleInfo> SubResource { get; set; }

        public Object GetInstance()
        {
            try
            {
                if (!Path.EndsWith(".unity"))
                    return UnityEngine.GameObject.Instantiate(GameObject);
                else
                    return null;
            }
            catch(System.Exception ex)
            {
                LoggerHelper.Error("AssetBundle get instance: '" + Path + "' error: " + ex.Message);
                return null;
            }
        }

        public AssetBundleInfo()
        {
            SubResource = new List<AssetBundleInfo>();
        }
    }
}
