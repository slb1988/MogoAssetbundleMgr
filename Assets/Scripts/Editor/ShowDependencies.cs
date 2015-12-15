using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

using Mogo.Util;

public class ShowDependencies : EditorWindow
{
    private Vector2 scrollPos;

    [MenuItem("Assets/Show Dependencies")]
    public static void Init()
    {
        GetWindow<ShowDependencies>();
    }

    [MenuItem("Tools/Check Use BuildinShader")]
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
