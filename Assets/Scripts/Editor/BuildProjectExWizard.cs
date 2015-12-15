using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mogo.Util;
using UnityEditor;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;

public class BuildProjectExWizard : EditorWindow
{
    public const string ExportFilesPath = "/ExportedFiles";
    public Vector2 scrollPosition;
    public string m_currentVersion = "0.0.0.0";
    public string m_newVersion = "0.0.0.1";
    public string m_newVersionFolder;
    private bool m_selectAll = true;
    /// <summary>
    /// 导出资源配置数据
    /// </summary>
    private List<BuildResourcesInfo> m_buildResourcesInfoList;
    private List<CopyResourcesInfo> m_copyResourcesInfoList;
    private Dictionary<string, VersionInfo> m_fileVersions
    {
        get
        {
            return ExportScenesManager.FileVersions;
        }
        set
        {
            ExportScenesManager.FileVersions = value;
        }
    }

    private Dictionary<string, VersionInfo> m_updatedFiles = new Dictionary<string, VersionInfo>();

    [MenuItem("MogoEx/Build Resources")]
    public static void ShowWindow()
    {
        var wizard = EditorWindow.GetWindow(typeof(BuildProjectExWizard)) as BuildProjectExWizard;
        ExportScenesManager.AutoSwitchTarget();
        wizard.m_buildResourcesInfoList = ExportScenesManager.LoadBuildResourcesInfo();
        wizard.m_copyResourcesInfoList = ExportScenesManager.LoadCopyResourcesInfo();
        if (wizard.m_buildResourcesInfoList == null || wizard.m_copyResourcesInfoList == null)
            return;
        var currentVersion = GetNewVersionCode();
        wizard.m_currentVersion = currentVersion.GetLowerVersion();
        wizard.m_newVersion = currentVersion.ToString();
        wizard.m_newVersionFolder = GetVersionFolder(wizard.m_newVersion);
    }

    /// <summary>
    /// Refresh the window on selection.
    /// </summary>
    void OnSelectionChange() { Repaint(); }

    /// <summary>
    /// Returns a blank usable 1x1 white texture.
    /// </summary>

    static public Texture2D blankTexture
    {
        get
        {
            return EditorGUIUtility.whiteTexture;
        }
    }

    /// <summary>
    /// Draw a visible separator in addition to adding some padding.
    /// </summary>

    static public void DrawSeparator()
    {
        GUILayout.Space(12f);

        if (Event.current.type == EventType.Repaint)
        {
            Texture2D tex = blankTexture;
            Rect rect = GUILayoutUtility.GetLastRect();
            GUI.color = new Color(0f, 0f, 0f, 0.25f);
            GUI.DrawTexture(new Rect(0f, rect.yMin + 6f, Screen.width, 4f), tex);
            GUI.DrawTexture(new Rect(0f, rect.yMin + 6f, Screen.width, 1f), tex);
            GUI.DrawTexture(new Rect(0f, rect.yMin + 9f, Screen.width, 1f), tex);
            GUI.color = Color.white;
        }
    }

    void OnGUI()
    {
        EditorGUIUtility.LookLikeControls(80f);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        GUILayout.BeginHorizontal();
        GUILayout.Label("比对版本号：", GUILayout.Width(120f));
        m_currentVersion = GUILayout.TextField(m_currentVersion);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("目标版本号：", GUILayout.Width(120f));
        m_newVersion = GUILayout.TextField(m_newVersion);
        GUILayout.EndHorizontal();
        if (GUILayout.Button("提升版本", GUILayout.Width(120f)))
        {
            BundleExporter.ClearDependenciesDic();
            m_currentVersion = new VersionCodeInfo(m_currentVersion).GetUpperVersion();
            m_newVersion = new VersionCodeInfo(m_newVersion).GetUpperVersion();
            var curVersionFolder = Path.Combine(ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath), m_currentVersion).Replace("\\", "/");
            m_newVersionFolder = Path.Combine(ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath), m_newVersion).Replace("\\", "/");
            ExportScenesManager.DirectoryCopy(curVersionFolder, m_newVersionFolder, true);
        }
        
        DrawSeparator();
        GUILayout.BeginHorizontal();
        GUILayout.Label("选择打包资源：", GUILayout.Width(120f));
        bool tempAll = m_selectAll;
        m_selectAll = GUILayout.Toggle(tempAll, "全选");
        GUILayout.EndHorizontal();

        if(m_buildResourcesInfoList != null)
        {
            foreach (var item in m_buildResourcesInfoList)
            {
                DrawSeparator();
                GUILayout.BeginHorizontal();
                bool temp = item.check;
                bool hasChanged = false;
                if (m_selectAll != tempAll)
                    item.check = m_selectAll;
                item.check = GUILayout.Toggle(item.check, item.name);
                GUILayout.Label(item.type, GUILayout.Width(100f));
                foreach (var ex in item.extentions)
                {
                    GUILayout.Label(ex, GUILayout.Width(100f));
                }
                hasChanged = temp != item.check;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                foreach (var folder in item.folders)
                {
                    if (hasChanged)
                        folder.check = item.check;
                    folder.check = GUILayout.Toggle(folder.check, folder.path);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("导出资源EX", GUILayout.Width(120f)))
                {
                    var targetPath = m_newVersionFolder + ExportFilesPath;
                    BuildAssetBundleMainAsset(item, targetPath);
                }
                if (GUILayout.Button("生成资源版本", GUILayout.Width(120f)))
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    var rootPath = Path.Combine(m_newVersionFolder, "version").Replace("\\", "/");
                    if (item.check)
                        BuildAssetVersion(item, rootPath);
                    sw.Stop();
                    LoggerHelper.Debug("BuildAssetVersion time: " + sw.ElapsedMilliseconds);
                }

                GUILayout.EndHorizontal();
            }

        }

        DrawSeparator();
        GUILayout.BeginHorizontal();
        GUILayout.Label("选择拷贝资源：", GUILayout.Width(120f));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (m_copyResourcesInfoList != null)
        {
            foreach(var item in m_copyResourcesInfoList)
            {
                item.check = GUILayout.Toggle(item.check, item.sourcePath);
                if (item.check && GUILayout.Button("导出", GUILayout.Width(120f)))
                {
                    var targetPath = m_newVersionFolder + ExportFilesPath;
                    ExportScenesManager.CopyFolder(Path.Combine(targetPath, item.targetPath), Application.dataPath + item.sourcePath, item.extention);
                }
            }
        }
        GUILayout.EndHorizontal();

        DrawSeparator();
        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        //if (GUILayout.Button("MogoLib", GUILayout.Width(120f)))
        //{            
        //}
        if (GUILayout.Button("压缩", GUILayout.Width(120f)))
        {
            Zip();
        }
        if (GUILayout.Button("完整打包", GUILayout.Width(120f)))
        {
            var sw = new Stopwatch();
            sw.Start();
            var targetPath = m_newVersionFolder + ExportFilesPath;
            foreach (var item in m_buildResourcesInfoList)
            {
                if (item.check)
                    BuildAssetBundleMainAsset(item, targetPath);
            }
            foreach (var item in m_copyResourcesInfoList)
            {
                if (item.check)
                {
                    ExportScenesManager.CopyFolder(Path.Combine(targetPath, item.targetPath), Application.dataPath + item.sourcePath, item.extention);
                }
            }
            sw.Stop();
            LoggerHelper.Debug("完整打包 time: " + sw.ElapsedMilliseconds);
        }
        if (GUILayout.Button("生成资源版本", GUILayout.Width(120f)))
        {
            var root = Application.dataPath;
            var dataPath = new DirectoryInfo(Application.dataPath).Parent.FullName.Replace("\\", "/") + "/";
            System.Action action = () =>
            {
                BuildAssetVersion(root, dataPath, m_newVersion, m_newVersionFolder);
            };
            action.BeginInvoke(null, null);
        }
        if (GUILayout.Button("比对资源版本", GUILayout.Width(120f)))
        {
            var currentVersionFolder = GetVersionFolder(m_currentVersion);
            var diff = FindVersionDiff(currentVersionFolder, m_newVersionFolder, "version");
            LogDebug(diff.PackArray('\n'));
            diff = BundleExporter.FindDependencyRoot(diff);
            LogDebug("Root resource:\n" + diff.PackArray('\n'));
            var targetPath = m_newVersionFolder + ExportFilesPath;
            BundleExporter.BuildBundleWithRoot(diff, targetPath);
        }
        if (GUILayout.Button("清理MR", GUILayout.Width(120f)))
        {
            if (!EditorUtility.DisplayDialog("Conform", "确认清理？", "yes", "no"))
                return;
            var rootPath = ExportScenesManager.GetFolderPath(ExportScenesManager.SubMogoResources);
            Directory.Delete(rootPath, true);
            LogDebug("clean success.");
        }
        if (GUILayout.Button("拷贝MR", GUILayout.Width(120f)))
        {
            var targetPath = ExportScenesManager.GetFolderPath(ExportScenesManager.SubMogoResources);
            var sourcePath = m_newVersionFolder + ExportFilesPath;
            ExportScenesManager.DirectoryCopy(sourcePath, targetPath, true);
            LogDebug(string.Format("copy success. from: \n" + sourcePath + "\nto " + targetPath));
        }

        GUILayout.EndHorizontal();
    }

    private static void BuildAssetBundleMainAsset(BuildResourcesInfo item, string exportRootPath, bool isMerge = false)
    {
        BundleExporter.BuildBundleWithRoot(GetAssetList(item).ToArray(), exportRootPath, isMerge);
    }

    public static IEnumerable<string> GetAssetList(BuildResourcesInfo item)
    {
        List<string> list = new List<string>();
        foreach (var folder in item.folders)
        {
            var root = Application.dataPath + "/" + folder.path;
            Debug.Log("root: " + root);
            foreach (var extention in item.extentions)
            {
                var sp = "*" + extention;
                Debug.Log("sp: " + sp);
                list.AddRange(Directory.GetFiles(root, sp, folder.deep ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
            }
        }

        var newList = from fileName in list
                      select fileName.Replace("\\", "/").ReplaceFirst(Application.dataPath, "Assets");
        Debug.Log("list.Count: " + list.Count);
        return newList;
    }
    private void Zip()
    {
        m_updatedFiles.Clear();
        ExportScenesManager.LoadVersionFile("ExportFileVersion", ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath), m_updatedFiles);

        var versionFolder = Path.Combine(ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath), m_newVersion);
        var sourcePath = versionFolder + ExportFilesPath;
        var newFiles = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);
        LogDebug(sourcePath);
        var updatedFiles = new List<string>();
        LogDebug(newFiles.Length);
        foreach (var item in newFiles)
        {
            var md5 = ExportScenesManager.GetFileMD5(item);
            var path = item.ReplaceFirst(sourcePath, "");
            if (m_updatedFiles.ContainsKey(path) && m_updatedFiles[path].MD5 == md5)
                continue;//没有变化的文件忽略
            m_updatedFiles[path] = new VersionInfo() { Path = path, MD5 = md5, Version = m_currentVersion };
            updatedFiles.Add(item);
        }
        LogDebug(m_updatedFiles.Count);
        ExportScenesManager.SaveVersionFile("ExportFileVersion", ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath), m_updatedFiles, sourcePath);
        var targetPath = versionFolder;
        var tempExport = ExportScenesManager.GetFolderPath("tempExport");
        PackUpdatedFiles(sourcePath, tempExport, targetPath, updatedFiles, m_currentVersion, m_newVersion);
    }

    /// <summary>
    /// 打包更新的文件。
    /// </summary>
    /// <param name="saveFolder">存放文件夹。</param>
    /// <param name="fileList">更新文件列表。</param>
    /// <param name="currentVersion">当前版本号。</param>
    /// <param name="newVersion">目标版本号。</param>
    public static void PackUpdatedFiles(string saveFolder, string tempExport, string targetPath, List<string> fileList, string currentVersion, string newVersion)
    {
        foreach (var item in fileList)
        {
            var path = item.Replace(saveFolder, "");
            var newPath = string.Concat(tempExport, path);
            var di = Path.GetDirectoryName(newPath);
            if (!Directory.Exists(di))
                Directory.CreateDirectory(di);
            if (File.Exists(newPath))
                continue;
            File.Copy(item, newPath);
        }
        ExportScenesManager.ZIPFile(tempExport, targetPath, currentVersion, newVersion);
        Directory.Delete(tempExport, true);
    }

    private void BuildAssetVersion(BuildResourcesInfo item, string rootPath)
    {
        BundleExporter.BuildAssetVersion(GetAssetList(item).ToArray(), rootPath);
    }
    /// <summary>
    /// 生成源资源版本
    /// </summary>
    /// <param name="root"></param>
    /// <param name="dataPath"></param>
    /// <param name="newVersion"></param>
    /// <param name="newVersionPath"></param>
    private static void BuildAssetVersion(string root, string dataPath, string newVersion, string newVersionPath)
    {
        var sw = new Stopwatch();
        sw.Start();
        var allFiles = Directory.GetFiles(root, "*.*", SearchOption.AllDirectories);
        ExportScenesManager.LogDebug("total files count: " + allFiles.Length);
        var files = new Dictionary<string, VersionInfo>();
        foreach (var item in allFiles)
        {
            if (item.EndsWith(".meta") || item.EndsWith(".cs") || item.EndsWith(".xml") || item.EndsWith(".DS_Store")
                || item.Contains(".svn") || item.Contains("SpawnPointGearAgent") || item.Contains("TrapStudio")
                || item.Contains(@"Gear\Agent") || item.Contains(@"Gear\Example") || item.Contains(@"Assets\Scenes")
                || item.Contains(@"_s.unity"))
                continue;
            var fileName = item.Replace("\\", "/");
            var md5 = ExportScenesManager.GetFileMD5(fileName);
            var path = fileName.ReplaceFirst(dataPath, "");
            files.Add(fileName, new VersionInfo() { Path = path, MD5 = md5, Version = newVersion });
        }

        ExportScenesManager.SaveVersionFile("version", newVersionPath, files, "");
        sw.Stop();
        ExportScenesManager.LogDebug("BuildAssetVersion time: " + sw.ElapsedMilliseconds);
    }


    /// <summary>
    /// 比对源资源变化
    /// </summary>
    /// <param name="currentVersionFolder"></param>
    /// <param name="newVersionFolder"></param>
    /// <returns></returns>
    private static string[] FindVersionDiff(string currentVersionFolder, string newVersionFolder, string versionFileName)
    {
        var currentVersionInfos = new Dictionary<string, VersionInfo>();
        var newVersionInfos = new Dictionary<string, VersionInfo>();

        ExportScenesManager.LoadVersionFile(versionFileName, currentVersionFolder, currentVersionInfos, "");
        ExportScenesManager.LoadVersionFile(versionFileName, newVersionFolder, newVersionInfos, "");

        var diff = (from newV in newVersionInfos
                    where !(currentVersionInfos.ContainsKey(newV.Key) && currentVersionInfos[newV.Key].MD5 == newV.Value.MD5)
                    select newV.Key).ToArray();

        return diff;
    }

    public static string GetNewVersion()
    {
        return GetNewVersionCode().ToString();
    }
    public static string GetVersionFolder(string version)
    {
        return Path.Combine(ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath), version).Replace("\\", "/");
    }
    public static VersionCodeInfo GetNewVersionCode()
    {
        var dirs = Directory.GetDirectories(ExportScenesManager.GetFolderPath(ExportScenesManager.ExportPath));
        var currentVersion = new VersionCodeInfo("0.0.0.1");
        foreach (var item in dirs)
        {
            var fileVersion = new VersionCodeInfo(new DirectoryInfo(item).Name);
            if (fileVersion.Compare(currentVersion) > 0)
                currentVersion = fileVersion;
        }
        return currentVersion;
    }

    public void LogDebug(object content)
    {
        ExportScenesManager.LogDebug(content);
    }

    public void LogWarning(object content)
    {
        ExportScenesManager.LogWarning(content);
    }

    public void LogError(object content)
    {
        ExportScenesManager.LogError(content);
    }
}
