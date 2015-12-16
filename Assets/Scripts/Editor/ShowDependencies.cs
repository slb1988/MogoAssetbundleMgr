using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

using Mogo.Util;

using Object = UnityEngine.Object;
using System.IO;

public class ShowDependencies : EditorWindow
{
    private Vector2 scrollPos;

    [MenuItem("Assets/Show Dependencies")]
    public static void Init()
    {
        GetWindow<ShowDependencies>();
    }
	
	public static bool CheckHasChinese(string str)
	{
		for(int i = 0; i < str.Length;i++)
		{
			char ch = str[i];
			if (ch >= 0x4e00 && ch <= 0xFEFE)
				return true;
		}
		
		return false;
	}

	//[MenuItem("Tools/TestChinese")]
	public static void TestChinese()
	{
		var testStr = "阿斯蒂芬";
		Debug.Log (CheckHasChinese (testStr));

		testStr = "_22";
		Debug.Log (CheckHasChinese (testStr));
	}

	[MenuItem("Tools/Find Chinese")]
	public static void FindChinese()
	{
		var files = AssetDatabase.GetAllAssetPaths();
		string lastErrFile = string.Empty;
		foreach(var f in files)
		{
			if (CheckHasChinese(Utils.GetFileName(f)))
			{
				lastErrFile = f;
				Debug.LogError(f);
			}
		}


		if (!string.IsNullOrEmpty (lastErrFile)) {
			var o = AssetDatabase.LoadAssetAtPath (lastErrFile, typeof(Object));
			Selection.objects = new Object[]{o};
		}

	}


    static string[] WhiteShaderList = new string[] { "Shader Forge" };

    [MenuItem("Tools/Check Buildin Shader")]
    public static void ChangeBuildinShader()
    {
        var files = Directory.GetFiles(Application.dataPath + "/Resources/", "*.prefab", SearchOption.AllDirectories);

        var errObjs = new List<Object>();

        foreach (var f in files)
        {
            var resFilePath = "Assets" + f.ReplaceFirst(Application.dataPath, "");
            resFilePath = resFilePath.Replace("\\", "/");
            var o = AssetDatabase.LoadAssetAtPath(resFilePath, typeof(Object));

			if (!IsUseLocalBuildinShader(o))
            {
                errObjs.Add(o);
            }
        }

//        foreach (var o in errObjs)
//        {
//            Debug.LogError(o);
//        }

        Selection.objects = errObjs.ToArray();
    }

    public static bool IsUseLocalBuildinShader(Object obj)
    {
        //Debug.Log(obj);
        var assetPath = AssetDatabase.GetAssetPath(obj);
        var dependencies = AssetDatabase.GetDependencies(new string[] { assetPath });

        bool hasMat = false;
        bool hasBuildinShader = false;
		bool isOnlyUseWhiteListShader = true;

        List<Object> selectObjs = new List<Object>();

        foreach (var d in dependencies)
        {
            if (Utils.GetFileExtention(d) == ".mat")
            {
                hasMat = true;
                
                var r = AssetDatabase.LoadAssetAtPath(d, typeof(Material));

                var mat = r as Material;
                //Debug.Log(mat);
				/*if (mat.shader.name.Contains("Shader Forge") == false)
				{
					Debug.Log(mat.shader);
					Debug.Log(d);
				}
				*/
				string shaderName = mat.shader.name;
				foreach(var whiteStr in WhiteShaderList)
				{
					if (shaderName.Contains(whiteStr))
					{
						continue;
					}
					else
					{
						isOnlyUseWhiteListShader = false;
						break;
					}
				}
            }

            if (d.IndexOf("builtin_shaders-4.6.9") >= 0)
            {
                hasBuildinShader = true;
            }
        }

		if (isOnlyUseWhiteListShader)
			return true;

        if (hasMat && !hasBuildinShader)
		{
			foreach (var d in dependencies)
			{
				if (Utils.GetFileExtention(d) == ".mat")
				{
					var r = AssetDatabase.LoadAssetAtPath(d, typeof(Material));
					
					var mat = r as Material;
					string shaderName = mat.shader.name;
					Debug.Log(shaderName);
					Debug.Log(r);
				}
			}
			Debug.LogError(obj);
            //Debug.LogWarning("this material has no buildin shader " + obj.name);
            return false;
        }

        return true;
    }
    //[MenuItem("Tools/Check Use BuildinShader")]
    public static void CheckUseBuildinShader()
    {
        var files = AssetDatabase.GetAllAssetPaths();
        foreach (var f in files)
        {
            if (Utils.GetFileExtention(f) == ".prefab")
            {
                bool isUseLocalBuildinShader = false;
                var dependends = AssetDatabase.GetDependencies(new string[] { f });
                StringBuilder sb = new StringBuilder();
                foreach (var d in dependends)
                {
                    sb.Append(d);
                    sb.Append("\n");
                    if (d.IndexOf("builtin_shaders-4.6.9") >= 0)
                    {
                        isUseLocalBuildinShader = true;
                    }
                }

                if (!isUseLocalBuildinShader)
                    Debug.Log(sb.ToString());
            }
        }
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.BeginVertical();
        DrawDependencies();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    private void DrawDeepHierarchy()
    {
        GUILayout.Label("DeepHierarchy:");
        var dependencies = EditorUtility.CollectDeepHierarchy(new[] { Selection.activeObject });
        foreach (var obj in dependencies)
        {
            GUILayout.Label(AssetDatabase.GetAssetPath(obj) + "  :  " + obj);
        }
    }

    private void DrawDependencies()
    {
        GUILayout.Label("Dependencies:");
        var path = AssetDatabase.GetAssetPath(Selection.activeObject);
        var dependencies =AssetDatabase.GetDependencies(new string[] { path }).Where(x=>x.EndsWith(".cs")==false);
        foreach (var obj in dependencies)
            GUILayout.Label(obj);
    }
}
