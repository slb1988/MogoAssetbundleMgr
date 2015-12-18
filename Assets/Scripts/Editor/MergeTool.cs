using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using System;
using System.Xml;
using System.IO;
using System.Security;

using Mogo.Util;

using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class MergeToolExWizard : EditorWindow
{

    private static Dictionary<string, ResourceMetaData> metaOfResource = new Dictionary<string, ResourceMetaData>();

    string m_targetPath = "";
    string m_sourcePath = "";

    static Stopwatch sw = new Stopwatch();

    [MenuItem("MogoEx/Merge Folder")]
    public static void ShowWindow()
    {
        var wizard = EditorWindow.GetWindow(typeof(MergeToolExWizard)) as MergeToolExWizard;
        wizard.m_targetPath = Application.dataPath + "/../MogoResources/";
        wizard.minSize = new Vector2(400, 100);
    }
    
    void OnGUI()
    {

        EditorGUIUtility.LookLikeControls(80f);
        GUILayout.BeginHorizontal();
        GUILayout.Label("目标路径：", GUILayout.Width(120f));
        m_targetPath = GUILayout.TextField(m_targetPath);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("来源路径：", GUILayout.Width(120f));
        
        m_sourcePath = GUILayout.TextField(m_sourcePath);
        GUILayout.EndHorizontal();
        if (GUILayout.Button("选择来源", GUILayout.Width(120f)))
        {
            m_sourcePath = EditorUtility.OpenFolderPanel("select source Folder", m_sourcePath, "") + "/";
             //    EditorUtility.OpenFilePanel("Select NotePad++.exe", "d:\\", "exe");
        }
        if (GUILayout.Button("合并", GUILayout.Width(120f)))
        {
            sw.Reset();
            sw.Start();

            CopyFolder(m_targetPath, m_sourcePath, true);

            sw.Stop();
            UnityEngine.Debug.Log("elapsed time: " + sw.ElapsedMilliseconds);

        }
    }
    public static void CopyFolder(string targetPath, string sourcePath, bool isMergeXML)
    {
        // 大于20秒退出
        if (sw.ElapsedMilliseconds > 20 * 1000)
            return;

        if (!Directory.Exists(targetPath))
            Directory.CreateDirectory(targetPath);
        
        var dirs = Directory.GetDirectories(sourcePath, "*.*", SearchOption.TopDirectoryOnly);

        foreach (var dir in dirs)
        {
            string normalizeDir = dir.Replace('\\', '/');
            string d2 = normalizeDir.Replace(sourcePath, "");
            string targetDir = Path.Combine(targetPath, d2) + "/";
            CopyFolder(targetDir, normalizeDir + "/", isMergeXML);
        }
        
        var files = Directory.GetFiles(sourcePath, "*.*", SearchOption.TopDirectoryOnly);
        foreach (var srcFile in files)
        {
            string normalizeSrcFile = srcFile.Replace('\\', '/');
            string filedir = normalizeSrcFile.Replace(sourcePath, "");
            string targetFile = Path.Combine(targetPath, filedir);

            if (isMergeXML && File.Exists(targetFile)
                && srcFile.EndsWith("xml", System.StringComparison.OrdinalIgnoreCase))
            {
                MergeXml(srcFile, targetFile, targetFile);
            }
            else
            {
                try
                {
                    File.Copy(normalizeSrcFile, targetFile, true);
                }
                catch(Exception ex)
                {
                    Debug.LogError(ex.ToString());
                }
            }
            //File.Copy(item, targetFile, true);
        }

    }

    public static void MergeXml(string file1, string file2, string resultfile)
    {
        metaOfResource.Clear();

        Debug.Log(string.Format("MergeXml " + file1 + "file2"));
        var file1Data = Utils.LoadFile(file1);
        //Debug.Log(file1Data);
        var file2Data = Utils.LoadFile(file2);

        var xml1 = XMLParser.LoadXML(file1Data);
        var xml2 = XMLParser.LoadXML(file2Data);

        if (xml1 == null || xml2 == null)
        {
            Debug.LogError(string.Format("can not find meta file: \n" + file1 + "\n" + file2));
            return;
        }

        StoreMetaResource(xml1);
        StoreMetaResource(xml2);

        var doc = GetXMLDocument(resultfile);

        foreach (var meta in metaOfResource)
        {
            WriteMetaData(doc, meta.Key);
        }

        doc.Save(resultfile);
    }

    public static void WriteMetaData(XmlDocument xmlDoc, string relativePath)
    {
        var root = GetXmlRoot(xmlDoc);

        var node = (root.SelectSingleNode(string.Concat("file[@path = '", relativePath, "']")) as XmlElement);

        if (node == null)
        {
            node = xmlDoc.CreateElement("file");

            root.AppendChild(node);
        }
        else
        {
            node.RemoveAll();
        }

        SetXmlNodeFilePath(node, relativePath);
        SetXmlNodeMD5(node, relativePath);
        SetXmlNodeDependencies(xmlDoc, node, relativePath);
    }

    private static void SetXmlNodeFilePath(XmlElement node, string relativePath)
    {
        node.SetAttribute("path", relativePath);
    }

    private static void SetXmlNodeMD5(XmlElement node, string relativePath)
    {
        node.SetAttribute("md5", metaOfResource[relativePath].MD5);
    }

    private static void SetXmlNodeDependencies(XmlDocument doc, XmlElement node, string relativePath)
    {
        if (metaOfResource[relativePath].Dependencies == null)
            return;

        foreach (var dependency in metaOfResource[relativePath].Dependencies)
        {
            var child = doc.CreateElement("dependency");
            child.SetAttribute("path", dependency);
            node.AppendChild(child);
        }
    }

    private static XmlDocument GetXMLDocument(string xmlPath)
    {
        XmlDocument doc = new XmlDocument();
        if (File.Exists(xmlPath))
            doc.Load(xmlPath);

        return doc;
    }

    private static XmlNode GetXmlRoot(XmlDocument xmlDoc)
    {
        var root = xmlDoc.SelectSingleNode("root");

        if (root == null)
        {
            root = xmlDoc.CreateElement("root");
            xmlDoc.AppendChild(root);
        }

        return root;
    }

    public static void StoreMetaResource(SecurityElement se)
    {
        for (int i = 0; i < se.Children.Count; i++)
        {
            SecurityElement item = se.Children[i] as SecurityElement;
            ResourceMetaData meta = new ResourceMetaData();
            meta.RelativePath = item.Attribute("path");
            meta.MD5 = item.Attribute("md5");

            var dependencies = item.Children;
            if (dependencies != null && dependencies.Count > 0)
            {
                meta.Dependencies = new List<string>();

                foreach (SecurityElement dependency in dependencies)
                {
                    meta.Dependencies.Add(dependency.Attribute("path"));
                }
            }

            bool isNeedReplace = false;

            if (metaOfResource.ContainsKey(meta.RelativePath) == false)
            {
                isNeedReplace = true;
            }
            else
            {
                if (metaOfResource[meta.RelativePath].Dependencies == null)
                {
                    if (dependencies != null)
                    {
                        Debug.Log("replace " + meta.RelativePath);
                        isNeedReplace = true;
                    }
                }
                else if (dependencies != null)
                {
                    if (metaOfResource[meta.RelativePath].Dependencies.Count < dependencies.Count)
                    {
                        Debug.Log("replace " + meta.RelativePath);
                        isNeedReplace = true;
                    }
                }
            }

            if (isNeedReplace)
            {
                metaOfResource[meta.RelativePath] = meta;
            }

        }
    }

}
