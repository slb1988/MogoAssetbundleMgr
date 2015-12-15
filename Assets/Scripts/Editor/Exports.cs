using UnityEngine;
using System.Collections;

using UnityEditor;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Security;
using System.Security.Cryptography;

using Debug = UnityEngine.Debug;
using Object= UnityEngine.Object;

using Mogo.Util;

public class VersionInfo
{
	public string Path{ get; set; }
	public string MD5{ get; set; }
	public string Version{get;set;}
	public Object Asset{get;set;}
}

public class ExportScenesManager
{
	public const string ExportPath = "Export";
	public const string SubVersion = "Version";
	public const string SubMogoResources = "MogoResources";
	public const string SubExportedFileList = "SubExportedFileList";
	private const string ExportedFileList = ExportPath + "/" + SubExportedFileList;
	
	/// <summary>
	/// 记录需要被删除的多余的祖先关系。
	/// </summary>
	private static Queue<KeyValuePair<Dictionary<string, MogoResourceInfo>, string>> m_deleteQueue = new Queue<KeyValuePair<Dictionary<string, MogoResourceInfo>, string>>();
	/// <summary>
	/// 在导出时排列好导出顺序的资源。
	/// </summary>
	private static Stack<KeyValuePair<MogoResourceInfo, int>> m_resourceStack = new Stack<KeyValuePair<MogoResourceInfo, int>>();
	/// <summary>
	/// 一次导出中选中的资源。
	/// </summary>
	private static Dictionary<string, MogoResourceInfo> m_allResources = new Dictionary<string, MogoResourceInfo>();
	/// <summary>
	/// 临时存放没选中但有被依赖的资源。
	/// </summary>
	private static Dictionary<string, MogoResourceInfo> m_leftResources = new Dictionary<string, MogoResourceInfo>();
	private static Dictionary<string, VersionInfo> m_fileVersions = new Dictionary<string, VersionInfo>();
	private static Dictionary<string, VersionInfo> m_updatedFiles = new Dictionary<string, VersionInfo>();

    public static Dictionary<string, VersionInfo> FileVersions
    {
        get { return ExportScenesManager.m_fileVersions; }
        set { ExportScenesManager.m_fileVersions = value; }
    }

    public static Dictionary<string, VersionInfo> UpdatedFiles
    {
        get { return ExportScenesManager.m_updatedFiles; }
        set { ExportScenesManager.m_updatedFiles = value; }
    }

	/// <summary>
	/// 全局导出选项。（一般不需修改）
	/// </summary>
	private static readonly BuildAssetBundleOptions options =
		BuildAssetBundleOptions.CollectDependencies |
			BuildAssetBundleOptions.CompleteAssets |
			BuildAssetBundleOptions.DeterministicAssetBundle;
	
	public static BuildAssetBundleOptions Options
	{
		get { return ExportScenesManager.options; }
	}
	
	/// <summary>
	/// 标记导出目标平台。
	/// </summary>
	private static BuildTarget m_currentBuildTarget = BuildTarget.StandaloneWindows64;
	
	public static BuildTarget CurrentBuildTarget
	{
		get { return ExportScenesManager.m_currentBuildTarget; }
		set { ExportScenesManager.m_currentBuildTarget = value; }
	}

    /// <summary>
    /// 存放打包后输出的资源包。
    /// </summary>
    public const string SubUpdatePackage = "UpdatePackage";
    /// <summary>
    /// 存放打包后输出的资源包。
    /// </summary>
    private const string UpdatePackage = ExportPath + "/" + SubUpdatePackage;


	[MenuItem("Mogo/Build Item")]
	public static void BuildItem()
	{
		var root = Application.dataPath + "/Resources/Characters/Item";
		var af = Directory.GetFiles (root, "*.prefab", SearchOption.AllDirectories);

		BuildResourceManullyEx (new VersionCodeInfo("0.0.0.2"), "prefab", af.ToList(), ".prefab");

	}

	[MenuItem("Mogo/Test")]
	public static void Test()
	{
		Debug.Log (GetFolderPath (ExportPath));	

	}

	
	#region 切换输出平台

	[MenuItem("BuildTarget/Android")]
	public static void BuildTargetAndroid()
	{
		m_currentBuildTarget = BuildTarget.Android;
	}
	
	[MenuItem("BuildTarget/iOS")]
	public static void BuildTargetiOS()
	{
		m_currentBuildTarget = BuildTarget.iPhone;
	}
	
	[MenuItem("BuildTarget/PC")]
	public static void BuildTargetPC()
	{
		m_currentBuildTarget = BuildTarget.StandaloneWindows64;
	}
	
	public static void AutoSwitchTarget()
	{
		Debug.Log(EditorUserBuildSettings.activeBuildTarget);
		switch (EditorUserBuildSettings.activeBuildTarget)
		{
		case BuildTarget.Android:
			CurrentBuildTarget = BuildTarget.Android;
			break;
		case BuildTarget.StandaloneWindows:
			CurrentBuildTarget = BuildTarget.StandaloneWindows;
			break;
		case BuildTarget.StandaloneWindows64:
			CurrentBuildTarget = BuildTarget.StandaloneWindows64;
			break;
		case BuildTarget.iPhone:
			CurrentBuildTarget = BuildTarget.iPhone;
			break;
		default:
			break;
		}
	}
	
	#endregion

	public static void LogDebug(object str)
	{
		Debug.Log (str);
	}
    public static void LogWarning(object str)
	{
		Debug.LogWarning (str);
	}
    public static void LogError(object str)
	{
		Debug.LogError (str);
	}

	private static void BuildResourceManullyEx(VersionCodeInfo currentVersion, string resFolder, List<string> targets, params string[] extentions)
	{
		ExportScenesManager.LoadVersion ();

		var versionFolder = Path.Combine (GetFolderPath (ExportPath), currentVersion.ToString ());
		var folder = Path.Combine (versionFolder, SubMogoResources);	// huoqu shuchu mulu
		if (folder.Length == 0)
			return;
		Stopwatch sw = new Stopwatch ();
		sw.Start ();

		var exportedFiles = new List<string> ();
		foreach (var item in targets) {
			var path = "Assets" + item.ReplaceFirst(Application.dataPath, "");
			Debug.Log(path);
			var o = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
			Debug.Log(o);
			var target = folder + "/" + Path.GetFileNameWithoutExtension(item);
			if (!Directory.Exists(target))
				exportedFiles.AddRange(ExportResourcesEx(new Object[]{o}, currentVersion.ToString(), target, resFolder, false, extentions));
		}

		SaveExportedFileList (exportedFiles, SubExportedFileList, versionFolder, versionFolder);	//jilu daochu wenjian xinxi
		//LogError ("SaveExportedFileList time: " + sw.ElapsedMilliseconds);

		//LogError ("FildUpdatedFiles time: " + sw.ElapsedMilliseconds);
		//LogError ("PackUpdatedFiles time: " + sw.ElapsedMilliseconds);
		LogError ("Total time: " + sw.ElapsedMilliseconds);

		sw.Stop ();
//		Debug.Log (sw.ElapsedMilliseconds);
		SaveVersion ();
	}
	
	private static VersionCodeInfo GetLastVersion()
	{
		var currentVersion = new VersionCodeInfo("0.0.0.0");
		var folders = Directory.GetDirectories(GetFolderPath(ExportPath));
		foreach (var item in folders)
		{
			var version = new VersionCodeInfo(item);
			if (version.Compare(currentVersion) > 0)
				currentVersion = version;
		}
		return currentVersion;
	}
	
	private static VersionCodeInfo GetUpperVersion(VersionCodeInfo currentVersion)
	{
		var nextVersion = currentVersion.GetUpperVersion();
		var targetPath = Path.Combine(ExportPath, nextVersion);
		if (!Directory.Exists(targetPath))
			Directory.CreateDirectory(targetPath);
		else
			LogWarning(string.Format("varsion {0} already exist.", nextVersion));
		return new VersionCodeInfo(nextVersion);
	}

	public static void SaveVersion()
	{
		var curVersion = GetLastVersion().ToString();
		var curVersions = from t in m_fileVersions
			where t.Value.Version == curVersion
				select t;
		SaveVersionFile(SubVersion, Path.Combine(GetFolderPath(ExportPath), curVersion), curVersions);
	}
	
	public static void SaveVersionFile(string fileName, string path, IEnumerable<KeyValuePair<string, VersionInfo>> versions, string root = "")
	{
		var sb = new StringBuilder ();
		foreach (var item in versions) {
			string tempPath = string.IsNullOrEmpty (root) ? item.Value.Path : item.Value.Path.Replace (root, "");
			sb.Append (tempPath);
			sb.Append ("\n");
			sb.Append (item.Value.MD5);
			sb.Append ("\n");
			sb.Append (item.Value.Version);
			sb.Append ("\n");
		}
		var fileVersion = Path.Combine (path, fileName) + ".txt";
		XMLParser.SaveText (fileVersion.Replace ("\\", "/"), sb.ToString ());
		Debug.Log ("Save Version finished. " + fileVersion + " " + path);
	}

			/// <summary>
			/// 记录此次导出的文件信息。
			/// </summary>
			/// <param name="list">文件路径信息。</param>
			/// <param name="fileName">文件名。</param>
			/// <param name="folderPath">资源存放目录，用于替换掉文件路径的根目录信息，只保存相对目录。</param>
	private static void SaveExportedFileList(List<string> list, string fileName, string folderPath)
	{
		SaveExportedFileList (list, fileName, folderPath, GetFolderPath (ExportedFileList));
	}
		
		/// <summary>
		/// 记录此次导出的文件信息。
		/// </summary>
		/// <param name="list">文件路径信息。</param>
		/// <param name="fileName">文件名。</param>
		/// <param name="folderPath">资源存放目录，用于替换掉文件路径的根目录信息，只保存相对目录。</param>
	public static void SaveExportedFileList(List<string> list, string fileName, string folderPath, string fileFolder)
	{
		var sb = new StringBuilder ();
		foreach (var item in list) {
			sb.Append (item.Replace (folderPath, ""));
			sb.Append ("\n");
		}
		var fileVersion = Path.Combine (fileFolder, fileName) + ".txt";
		fileVersion = fileVersion.Replace ("\\", "/");
		if (!File.Exists (fileVersion)) {
			XMLParser.SaveText (fileVersion, sb.ToString ());
			Debug.Log ("Save ExportedFileList finished. " + fileVersion);
		} else {
			var text = Utils.LoadFile (fileVersion);
			XMLParser.SaveText (fileVersion, text + "\n" + sb.ToString ());
			Debug.Log ("File exist: " + fileVersion);
		}
	}

	/// <summary>
	/// 导出资源。
	/// </summary>
	/// <param name="selection">待导出的资源。</param>
	/// <param name="parentFolder">资源存放目录。</param>
	/// <param name="folder">资源类型目录。</param>
	/// <param name="extentions">目标导出资源后缀。</param>
	/// <returns>所有导出出来的资源的路径。</returns>
	public static List<string> ExportResourcesEx(Object[] selection, string newVersion, string parentFolder, string folder, bool isPopInBuild = false, params string[] extentions)
	{
		var prefabsFolder = Path.Combine(parentFolder, folder);
		var roots = GetRootEx(selection);
		var list = new List<string>();
		//BeginBuildAssetBundles();
		foreach (var item in roots)
		{
			foreach (var extention in extentions)
			{
				if (item.Path.EndsWith(extention, System.StringComparison.OrdinalIgnoreCase))
				{
					Debug.Log("root: " + item.Path);
					var assets = GetResourceAssets(item);
					Debug.Log("assets.Count: " + assets.Count);
					List<Object> updatedObj = UpdateVersion(newVersion, assets);//比对资源版本，过滤出有更新的资源。
					if (updatedObj.Count != 0)//若无更新，则跳过导出该资源
						list.AddRange(BuildAssetBundles(parentFolder, item, isPopInBuild));
					
					var path = Path.Combine(prefabsFolder, Utils.GetFileNameWithoutExtention(item.Path) + ".xml");
					if (File.Exists(path) && updatedObj.Count == 0)//资源依赖信息存在且资源无更新，则不更新资源依赖信息
						continue;
					SecurityElement se = new SecurityElement("r");
					se.AddChild(BuildResourceInfoXML(item));
					var directory = Path.GetDirectoryName(path);
					if (!Directory.Exists(directory))
						Directory.CreateDirectory(directory);
					XMLParser.SaveText(path, se.ToString());
					Debug.Log(string.Format("Build {0}: {1}", folder, item.Path));
					break;
				}
			}
		}
		//EndBuildAssetBundles();
		Debug.Log("Export resources finished.");
		return list;
	}

	
	/// <summary>
	/// 构造资源信息配置。
	/// </summary>
	/// <param name="info">资源信息实例。</param>
	/// <returns>资源信息配置。</returns>
	private static SecurityElement BuildResourceInfoXML(MogoResourceInfo info)
	{
		SecurityElement se = new SecurityElement("k");
		se.AddChild(new SecurityElement("p", info.Path));
		foreach (var item in info.SubResource.Values)
		{
			var child = BuildResourceInfoXML(item);
			se.AddChild(child);
		}
		return se;
	}


	#region Build Assetbundle
	
	/// <summary>
	/// 用于导出时记录已经入栈的文件。
	/// </summary>
	private static HashSet<string> stackFile = new HashSet<string>();
	/// <summary>
	/// 记录管线堆栈入栈深度，用于出栈计数。
	/// </summary>
	private static int currentStackDeep;
	
	/// <summary>
	/// 开始导出一批资源，初始化资源。
	/// </summary>
	private static void BeginBuildAssetBundles()
	{
		m_resourceStack.Clear();
		stackFile.Clear();
		currentStackDeep = 0;
	}
	
	/// <summary>
	/// 导出一批资源结束，退出资源管线堆栈。
	/// </summary>
	private static void EndBuildAssetBundles()
	{
		LogWarning("stackFile count: " + stackFile.Count);
		while (currentStackDeep > 0)
		{
			BuildPipeline.PopAssetDependencies();
			popCount++;
			currentStackDeep--;
		}
		BuildPipeline.PopAssetDependencies();
		popCount++;
	}
	
	/// <summary>
	/// 将资源推入堆栈。
	/// </summary>
	/// <param name="info">资源实例</param>
	/// <param name="deep">资源深度</param>
	private static void PushToStack(MogoResourceInfo info, int deep)
	{
		if (stackFile.Contains(info.Path))//重复资源不入盏
		{
			return;
		}
		stackFile.Add(info.Path);
		m_resourceStack.Push(new KeyValuePair<MogoResourceInfo, int>(info, deep));//深度优先入栈
		deep++;
		foreach (var item in info.SubResource)
		{
			PushToStack(item.Value, deep);
		}
	}

	public static int pushCount;
	public static int popCount;
	
	/// <summary>
	/// 生成资源文件
	/// </summary>
	/// <param name="saveFolder">资源保存路径</param>
	/// <param name="info">资源实例</param>
	private static List<string> BuildAssetBundles(string saveFolder, MogoResourceInfo info, bool isPopInBuild = false)
	{
		BeginBuildAssetBundles();
		PushToStack(info, 0);
		List<string> exportedList = new List<string>();
		bool isFirstRes = true;
		int lastDeep = -1;
		BuildPipeline.PushAssetDependencies();
		pushCount++;
		while (m_resourceStack.Count != 0)
		{
			var resource = m_resourceStack.Pop();
			var path = Path.Combine(saveFolder, string.Concat(resource.Key.Path, SystemConfig.ASSET_FILE_EXTENSION));//.unity3d
			var directory = Path.GetDirectoryName(path);
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);
			
			if (isFirstRes)
			{
				lastDeep = resource.Value;
				isFirstRes = false;
			}
			var deepDistance = lastDeep - resource.Value;
			if (deepDistance == 1)//父子关系
			{
				//Debug.Log(string.Concat("BuildAssetBundles: PushAssetDependencies parent", lastDeep, " ", resource.Value));
				BuildPipeline.PushAssetDependencies();
				pushCount++;
				currentStackDeep++;
			}
			else if (deepDistance == 0)//同级关系
			{
			}
			else//子树结束
			{
				if (isPopInBuild)
				{
					Debug.Log(string.Concat("BuildAssetBundles: PopAssetDependencies end", lastDeep, " ", resource.Value));
					BuildPipeline.PopAssetDependencies();
					popCount++;
					currentStackDeep--;
				}
			}
			lastDeep = resource.Value;
			
			if (resource.Key.Path.EndsWith(".unity"))
			{
				var scene = BuildPipeline.BuildStreamedSceneAssetBundle(new string[1] { resource.Key.Path }, path, m_currentBuildTarget);
				Debug.Log("BuildStreamedSceneAssetBundle: " + scene);
				BuildPipeline.PushAssetDependencies();
				currentStackDeep++;
			}
			else
			{
				var res = BuildPipeline.BuildAssetBundleExplicitAssetNames(new Object[1] { resource.Key.Asset }, new string[1] { resource.Key.Path }, path, options, m_currentBuildTarget);
				if (!res)
					LogWarning("BuildAssetBundle error: " + resource.Key.Path);
				exportedList.Add(path);
			}
			Debug.Log(string.Concat("BuildAssetBundles: ", resource.Key, " ", resource.Value));
		}
		EndBuildAssetBundles();
		return exportedList;
	}

	#endregion

	/// <summary>
	/// 比对资源版本，过滤出有更新的资源。
	/// </summary>
	/// <param name="version">目标版本。</param>
	/// <param name="selection">待过滤的资源。</param>
	/// <returns>过滤后的资源。</returns>
	public static List<Object> UpdateVersion(string version, List<Object> selection)
	{
		var root = Application.dataPath.Replace("Assets", "");
		var updatedFilesAsset = new List<Object>();
		foreach (var go in selection)
		{
			var path = AssetDatabase.GetAssetPath(go);
			if (IsIngoreResource(path))
				continue;
			var fileName = string.Concat(root, path);
			if (File.Exists(fileName))
			{
				var md5 = GetFileMD5(fileName);
				if (m_fileVersions.ContainsKey(path) && m_fileVersions[path].MD5 == md5)
					continue;//没有变化的文件忽略
				var vi = new VersionInfo() { Path = path, MD5 = md5, Version = version, Asset = go };
				m_fileVersions[path] = vi;
				m_updatedFiles[path] = vi;
				updatedFilesAsset.Add(go);
			}
			else
			{
				LogWarning("file not exist: " + fileName);
			}
		}
		
		Debug.Log("updatedFilesAsset count: " + updatedFilesAsset.Count);
		return updatedFilesAsset;
	}

	private static List<Object> GetResourceAssets(MogoResourceInfo info)
	{
		var result = new List<Object>();
		result.Add(info.Asset);
		foreach (var item in info.SubResource)
		{
			result.AddRange(GetResourceAssets(item.Value));
		}
		return result;
	}

	/// <summary>
	/// 构造资源树型结构
	/// </summary>
	/// <param name="resource">资源根节点</param>
	/// <returns>带子节点的树（有的话）</returns>
	private static MogoResourceInfo BuildResourceTreeEx(MogoResourceInfo resource)
	{
		var subResources = AssetDatabase.GetDependencies(new string[] { resource.Path });
		if (subResources.Length <= 1)//一条数据时是只包含自己
			return resource;
		//Debug.Log("subResources.Length: " + resource.MogoResourceInfo.Path + subResources.Length);
		var resDic = new Dictionary<string, MogoResourceInfo>();
		for (int i = 0; i < subResources.Length; i++)
		{
			var subPath = subResources[i];
			//空字符代表已经被移除，还有在字符串为自身和资源为忽略资源时跳过
			if (string.IsNullOrEmpty(subPath) || subPath == resource.Path || IsIngoreResource(subPath))
				continue;
			
			//构造资源对象
			MogoResourceInfo info;
			if (!m_leftResources.ContainsKey(subPath))
			{
				info = new MogoResourceInfo();
				info.Path = subPath;
				info.Asset = AssetDatabase.LoadAssetAtPath(subPath, typeof(Object));
				m_leftResources.Add(subPath, info);
			}
			else
			{
				info = m_leftResources[subPath];
			}
			var ex = info.GetCopy();
			
			var res = BuildResourceTreeEx(ex);//构造子节点的子树
			resDic[subPath] = res;//缓存子树数据
			var list = res.GetSonRecursively();//获取子节点所有节点信息（即所有子孙）
			
			foreach (var item in list)//在父的工作列表中删除自己的子孙
			{
				if (item == subPath)
					continue;
				
				for (int j = 0; j < subResources.Length; j++)
				{
					if (subResources[j] == item)
					{
						subResources[j] = string.Empty;
						break;
					}
				}
			}
		}
		
		foreach (var item in subResources)
		{
			//空字符代表已经被移除，还有在字符串为自身和资源为忽略资源时跳过
			if (string.IsNullOrEmpty(item) || item == resource.Path || IsIngoreResource(item))
			{
				continue;
			}
			//剩下的为直属孩子资源
			resDic[item].Parents.Add(resource.Path, resource);
			resource.SubResource.Add(item, resDic[item]);
		}
		return resource;
	}
	
	/// <summary>
	/// 判断该资源是否不独立导出
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	private static bool IsIngoreResource(string path)
	{
		var filter = new List<string>() { ".cs" };
		foreach (var item in filter)
		{
			if (path.EndsWith(item, System.StringComparison.OrdinalIgnoreCase))
				return true;
		}
		return false;
	}

	private static List<MogoResourceInfo> GetRootEx(Object[] selection)
	{
		Stopwatch sw = new Stopwatch();
		sw.Start();
		m_allResources.Clear();
		m_leftResources.Clear();
		var res = LoadResourcesToDic(selection);
		
		Debug.Log("res " + res.Count);
		
		var list = new List<MogoResourceInfo>();
		foreach (var item in res)
		{
			var ex = item.Value.GetCopy();
			list.Add(BuildResourceTreeEx(ex));
		}
		
		//foreach (var item in list)
		//{
		//    Debug.Log(item.Print());
		//}
		var roots = new List<MogoResourceInfo>();
		foreach (var item in list)//找出所有根资源
		{
			if (File.Exists(item.Path))//不是文件不打包
			{
				roots.Add(item);
			}
		}
		Debug.Log("Find root..." + roots.Count);
		sw.Stop();
		Debug.Log("GetRootEx time..." + sw.ElapsedMilliseconds);
		return roots;
	}

	/// <summary>
	/// 初始化资源实体。
	/// </summary>
	/// <param name="selection"></param>
	private static Dictionary<string, MogoResourceInfo> LoadResourcesToDic(Object[] selection)
	{
		Dictionary<string, MogoResourceInfo> allResources = new Dictionary<string, MogoResourceInfo>();
		foreach (var go in selection)
		{
			MogoResourceInfo info = new MogoResourceInfo();
			info.Path = AssetDatabase.GetAssetPath(go);//.Replace("Assets/", "");
			info.Asset = go;
			if (allResources.ContainsKey(info.Path))
				LogWarning("Info exist: " + info.Path);
			else
				allResources.Add(info.Path, info);
		}
		return allResources;
	}

	
	public static string GetFileMD5(string filename)
	{
		using (var md5 = MD5.Create())
		{
			using (var stream = File.OpenRead(filename))
			{
				return System.BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
			}
		}
	}

	public static string GetFolderPath(string folder="")
	{
		var root = new DirectoryInfo (Application.dataPath);
		var path = Path.Combine (root.Parent.FullName, folder).Replace ("\\", "/");
		if (!Directory.Exists (path))
			Directory.CreateDirectory (path);

		return path;
	}

    /// <summary>
    /// 拷贝目录资源。
    /// </summary>
    /// <param name="targetPath"></param>
    /// <param name="sourcePath"></param>
    /// <param name="extention"></param>
    public static void CopyFolder(string targetPath, string sourcePath, string extention)
    {
        if (!Directory.Exists(targetPath))
            Directory.CreateDirectory(targetPath);
        var files = Directory.GetFiles(sourcePath);
        foreach (var item in files)
        {
            if (item.EndsWith(extention, System.StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(item, Path.Combine(targetPath, Path.GetFileName(item)), true);
            }
        }
    }

    public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs, bool overwrite = false)
    {
        // Get the subdirectories for the specified directory.
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);
        DirectoryInfo[] dirs = dir.GetDirectories();

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                "Source directory does not exist or could not be found: "
                + sourceDirName);
        }

        // If the destination directory doesn't exist, create it. 
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string temppath = Path.Combine(destDirName, file.Name);
            try
            {
                file.CopyTo(temppath, overwrite);
            }
            catch (Exception e)
            {
                continue;
            }
        }

        // If copying subdirectories, copy them and their contents to new location. 
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath, copySubDirs, overwrite);
            }
        }
    }

	public static void LoadVersionFile(string fileName, string path, Dictionary<string, VersionInfo> versions, string root = "")
	{
		var fileVersion = Path.Combine (path, fileName) + ".txt";
		if (File.Exists (fileVersion)) {
			var filesInfo = Utils.LoadFile(fileVersion);
			var files = filesInfo.Split('\n');
			var total = files.Length -1;
			for(int i=0;i<total;i+=3)
			{
				var v = new VersionInfo(){Path = string.Concat(root, files[i]), MD5=files[i+1], Version=files[i+2]};
				versions.Add(v.Path, v);
			}
		}
	}

	public static void LoadVersion()
	{
		m_fileVersions.Clear ();
		m_updatedFiles.Clear ();
		var folders = Directory.GetDirectories (GetFolderPath (ExportPath));
		foreach (var item in folders) {
			var fileVersions = new Dictionary<string, VersionInfo>();
			LoadVersionFile(SubVersion, Path.Combine(GetFolderPath(ExportPath), item), fileVersions);
			foreach( var version in fileVersions)
			{
				m_fileVersions[version.Key] = version.Value;
			}
		}
		Debug.Log ("Load Version finished.");
	}

    /// <summary>
    /// 获取拷贝资源信息。
    /// </summary>
    /// <returns></returns>
    public static List<CopyResourcesInfo> LoadCopyResourcesInfo()
    {
        var path = string.Concat(GetFolderPath("ResourceDef"), "\\ForCopy.xml").PathNormalize();
        //Debug.Log(path);
        var xml = XMLParser.LoadXML(Utils.LoadFile(path));
        if (xml == null)
        {
            EditorUtility.DisplayDialog("Error", "Load Copy Resources Info Error.", "ok");
            return null;
        }
        var result = new List<CopyResourcesInfo>();

        if (xml.Children != null)
        {
            foreach (SecurityElement item in xml.Children)
            {
                var info = new CopyResourcesInfo();
                info.check = true;
                info.targetPath = (item.Children[0] as SecurityElement).Text;
                info.sourcePath = (item.Children[1] as SecurityElement).Text;
                info.extention = (item.Children[2] as SecurityElement).Text;
                result.Add(info);
            }
        }

        return result;
    }


    public static List<BuildResourcesInfo> LoadBuildResourcesInfo(int xmlindex = 0)
    {
        string path = string.Concat(GetFolderPath("ResourceDef"), "\\ForBuild.xml").PathNormalize();
        switch(xmlindex)
        {
            case 0:
                {
                    path = string.Concat(GetFolderPath("ResourceDef"), "\\ForBuild.xml").PathNormalize();
                    break;
                }
            case 1:
                {
                    path = string.Concat(GetFolderPath("ResourceDef"), "1.xml").PathNormalize();
                    break;
                }
            case 2:
                {
                    path = string.Concat(GetFolderPath("ResourceDef"), "2.xml").PathNormalize();
                    break;
                }
            case 3:
                {
                    path = string.Concat(GetFolderPath("ResourceDef"), "3.xml").PathNormalize();
                    break;
                }
            case 4:
                {
                    path = string.Concat(GetFolderPath("ResourceDef"), "4.xml").PathNormalize();
                    break;
                }
        }

        var xml = XMLParser.LoadXML(Utils.LoadFile(path));
        if (xml == null)
        {
            EditorUtility.DisplayDialog("Error", "Load Build Resource Info Error.", "ok");
            return null;
        }
        var result = new List<BuildResourcesInfo>();

        foreach(SecurityElement item in xml.Children)
        {
            var info = new BuildResourcesInfo();
            info.check = true;
            info.name = (item.Children[0] as SecurityElement).Text;
            info.type = (item.Children[1] as SecurityElement).Text;
            info.packLevel = (item.Children[2] as SecurityElement).Text.Split(' ');
            info.isMerge = int.Parse((item.Children[3] as SecurityElement).Text);
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


    public static void ZIPFile(string sourcePath, string currentVersion, string newVersion)
    {
        var targetPath = GetFolderPath(UpdatePackage);
        ZIPFile(sourcePath, targetPath, currentVersion, newVersion);
    }

    public static void ZIPFile(string sourcePath, string targetPath, string currentVersion, string newVersion)
    {
        ZIPFileWithFileName(sourcePath, targetPath, VersionManager.Instance.GetPackageName(currentVersion, newVersion));
    }

    public static void ZIPFileWithFileName(string sourcePath, string targetPath, string fileName, int zipLevel = 5)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        LogDebug("ZIPFile sourcePath: " + sourcePath);
        LogDebug("ZIPFile targetPath: " + targetPath);
        LogDebug("ZIPFile fileName: " + fileName);
        var zipPath = Path.Combine(targetPath, fileName);
        if (File.Exists(zipPath))
            File.Delete(zipPath);
        Utils.CompressDirectory(sourcePath, zipPath, zipLevel);

        sw.Stop();
        var t = sw.ElapsedMilliseconds;
        LogDebug(t);
    }
}

public class MogoResourceInfoRoot
{
    public string Version { get; set; }
    public int Deep { get; set; }
    public string Path { get; set; }
    public Object Asset { get; set; }
}

public class MogoResourceInfo : MogoResourceInfoRoot
{
    public Dictionary<string, MogoResourceInfo> Parents { get; set; }
    public Dictionary<string, MogoResourceInfo> SubResource { get; set; }

    public MogoResourceInfo()
    {
        Parents = new Dictionary<string, MogoResourceInfo>();
        SubResource = new Dictionary<string, MogoResourceInfo>();
    }

    public override string ToString()
    {
        return Path;
    }

    public MogoResourceInfo GetCopy()
    {
        var info = new MogoResourceInfo();
        info.Path = Path;
        info.Asset = Asset;
        return info;
    }

    public List<string> GetSonRecursively()
    {
        var list = new List<string>();
        list.Add(Path);
        foreach (var item in SubResource)
        {
            list.AddRange(item.Value.GetSonRecursively());
        }
        return list;
    }

    public List<MogoResourceInfo> GetSonInfoRecursively()
    {
        var list = new List<MogoResourceInfo>();
        list.AddRange(SubResource.Values);
        foreach (var item in SubResource)
        {
            list.AddRange(item.Value.GetSonInfoRecursively());
        }
        return list;
    }

    public string Print()
    {
        return Print(this);
    }

    private string Print(MogoResourceInfo info)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(string.Concat(info.Path, " ", info.Deep));
        if (info.SubResource != null && info.SubResource.Count != 0)
        {
            sb.AppendLine("push");
            foreach (var item in info.SubResource)
            {
                sb.Append(item.Value.Print());
            }
            sb.AppendLine("pop");
        }

        return sb.ToString();
    }
}

public class BuildResourcesInfo
{
    public string name { get; set; }
    public string type { get; set; }
    public string[] packLevel { get; set; }
    public int isMerge { get; set; }
    public bool isPopInBuild { get; set; }
    public string[] extentions { get; set; }
    public bool check { get; set; }
    public List<BuildResourcesSubInfo> folders { get; set; }
    //默认构造
    public BuildResourcesInfo()
    { }
    //拷贝构造
    public BuildResourcesInfo(BuildResourcesInfo bri)
    {
        name = bri.name;
        type = bri.type;
        packLevel = bri.packLevel;
        isMerge = bri.isMerge;
        isPopInBuild = bri.isPopInBuild;
        extentions = bri.extentions;
        check = bri.check;
        folders = bri.folders;
    }
}

public class BuildResourcesSubInfo
{
    public string path { get; set; }
    public bool deep { get; set; }
    public bool check { get; set; }
}

public class CopyResourcesInfo
{
    public string targetPath { get; set; }
    public string sourcePath { get; set; }
    public string extention { get; set; }
    public bool check { get; set; }
}