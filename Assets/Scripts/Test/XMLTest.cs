using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.Xml;
using System.IO;
using System.Security;

using System;

using Mogo.Util;
using UnityEditor;

public class XMLTest : MonoBehaviour {

	// Use this for initialization
	void Start () {

        //Debug.Log(Application.dataPath);
        //LoadXmlTest(Application.dataPath + "/booksData.xml");
        
        string file1 = Application.dataPath + "/Meta2.xml";
        string file2 = Application.dataPath + "/Meta1.xml";
        string resultfile = Application.dataPath + "/result.xml";

        MergeXml(file1, file2, resultfile);

        AssetDatabase.Refresh();
	}

    private static Dictionary<string, ResourceMetaData> metaOfResource = new Dictionary<string, ResourceMetaData>();

    public static void MergeXml(string file1, string file2, string resultfile)
    {
        var file1Data = Utils.LoadFile(file1);
        //Debug.Log(file1Data);
        var file2Data = Utils.LoadFile(file2);

        var xml1 = XMLParser.LoadXML(file1Data);
        var xml2 = XMLParser.LoadXML(file2Data);

        if (xml1 == null || xml2 == null)
        {
            Debug.LogError("can not find meta file.");
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

        foreach( var dependency in metaOfResource[relativePath].Dependencies)
        {
            var child = doc.CreateElement("dependency");
            child.SetAttribute("path", dependency);
            node.AppendChild(child);
        }
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

    public void LoadXmlTest(string xmlPath)
    {
        XmlDocument doc = new XmlDocument();
        doc.PreserveWhitespace = true;
        //Debug.Log(xmlPath);
        try
        {
            doc.Load(xmlPath);
        }
        catch(Exception ex)
        {
            Debug.Log(ex.ToString());
            doc.LoadXml("<?xml version=\"1.0\"?> \n" +
            "<books xmlns=\"http://www.contoso.com/books\"> \n" +
            "  <book genre=\"novel\" ISBN=\"1-861001-57-8\" publicationdate=\"1823-01-28\"> \n" +
            "    <title>Pride And Prejudice</title> \n" +
            "    <price>24.95</price> \n" +
            "  </book> \n" +
            "  <book genre=\"novel\" ISBN=\"1-861002-30-1\" publicationdate=\"1985-01-01\"> \n" +
            "    <title>The Handmaid's Tale</title> \n" +
            "    <price>29.95</price> \n" +
            "  </book> \n" +
            "</books>"); 
        }
    }



    private static void WriteMetaData(string rootPath, string relativePath)
    {
        var xmlFileName = GetXmlFileName(relativePath);
        var xmlPath = String.Concat(rootPath, "/", xmlFileName);

        var doc = GetXMLDocument(xmlPath);

        //var node = 
    }

    // 拼接meta文件相对路径
    private static string GetXmlFileName(string relativePath)
    {
        var xmlFileName = Path.GetDirectoryName(relativePath);

        if (xmlFileName != null && xmlFileName.Length > 0)
            xmlFileName += "/";

        xmlFileName += ResourceManager.MetaFileName;

        return xmlFileName;
    }

    private static XmlDocument GetXMLDocument(string xmlPath)
    {
        XmlDocument doc = new XmlDocument();
        if (File.Exists(xmlPath))
            doc.Load(xmlPath);

        return doc;
    }

    private static XmlElement InitXmlNode(XmlDocument xmlDoc, string relativePath)
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

        return node;
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
}
