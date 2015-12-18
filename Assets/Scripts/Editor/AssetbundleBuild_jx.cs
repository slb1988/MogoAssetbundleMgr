/// <summary> 
/// Exports a player compatible AssetBundle containing the selected objects, including dependencies. 
/// </summary> 
/// <remarks>Editor scripts are removed from the exported Assets.</remarks> 
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

class AssetbundleEditor
{
    public static string output_iphone = "output_iphone/";
    public static string output_android = "output_android/";
    public static string output_pc = "output_pc/";
    public static readonly string ABDirPC = "output_pc/";
    public static readonly string ABDirAndroid = "output_android/";
    public static readonly string ABDirIphone = "output_iphone/";


	//[MenuItem("Assets/Build AssetBundle")] 
	public static void ExportPlayerAssetBundle() 
	{ 
		string tempRelativePath = "Assets/Temp/"; 
		string tempAbsolutePath = Application.dataPath + "/../" + tempRelativePath; 

		// Bring up save dialog. 
		string path = EditorUtility.SaveFilePanel("Save AssetBundle", "", "AssetBundle", "unity3d"); 
		if (path.Length > 0) 
		{ 
			// Get all selected objects. 

			Object[] selection = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets); 
			//Object[] processedSelection = new Object[selection.Length]; 
			Object[] processedSelection=selection;
			for (int i = 0; i < selection.Length; i++) 
			{ 
				// Clone the original object. 

				Object currentObject = selection[i]; 
				bool isPrefab = currentObject != null && currentObject.GetType() == typeof(GameObject); 
				if (isPrefab) 
				{ 
					if (!Directory.Exists(tempAbsolutePath)) 
						Directory.CreateDirectory(tempAbsolutePath); 

					// Remove unneeded scripts from the prefab. 
                    /*
					Object clonedPrefab = EditorUtility.CreateEmptyPrefab(string.Format("{0}{1}.prefab", tempRelativePath, currentObject.name)); 
					if (clonedPrefab != null) 
					{ 
						clonedPrefab = EditorUtility.ReplacePrefab((GameObject)currentObject, clonedPrefab); 
						//EditorBase component = ((GameObject)clonedPrefab).GetComponent(typeof(EditorBase)) as EditorBase; 
						UnityEditor component = ((GameObject)clonedPrefab).GetComponent(typeof(UnityEditor)) as UnityEditor; 
						if (component != null) 
							GameObject.DestroyImmediate(component, true); 
						EditorUtility.SetDirty(clonedPrefab); 
						processedSelection[i] = clonedPrefab; 
					} 
					*/
				} 
			} 

			// Save changes to AssetDatabase and import processed prefabs. 

			EditorApplication.SaveAssets(); 
			AssetDatabase.Refresh(); 

			// Export the processed AssetBundle. 

			BuildPipeline.BuildAssetBundle(Selection.activeObject, processedSelection, path, BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets); 
			Selection.objects = selection; 

			// Remove all cloned objects from the project. 

			for (int i = 0; i < processedSelection.Length; i++) 
			{ 
				if (processedSelection[i] != null) 
					AssetDatabase.DeleteAsset(string.Format("{0}{1}.prefab", tempRelativePath, processedSelection[i].name)); 
			} 
			if (Directory.Exists(tempAbsolutePath)) 
				Directory.Delete(tempAbsolutePath, false); 
		} 
	}

    //[MenuItem("Assets/Build Multiple AssetBundles")]
    public static void ExportMultipleAssetBundles()
    {
        List<GameObject> golist = new List<GameObject>();
        golist.AddRange(Selection.gameObjects);

        foreach (var go in golist)
        {
            Selection.activeGameObject = go;
            ExportAB(output_pc + go.name, BuildTarget.WebPlayer);
        }
    }

    //[MenuItem("Assets/Build Multiple AssetBundles iPhone")]
    public static void ExportMultipleAssetBundlesIPhone()
    {
        List<GameObject> golist = new List<GameObject>();
        golist.AddRange(Selection.gameObjects);

        foreach (var go in golist)
        {
            Selection.activeGameObject = go;
            ExportAB(output_iphone + go.name, BuildTarget.iPhone);
        }
    }

    //[MenuItem("Assets/Build Multiple AssetBundles android")]
    public static void ExportMultipleAssetBundlesAndroid()
    {
        List<GameObject> golist = new List<GameObject>();
        golist.AddRange(Selection.gameObjects);

        foreach (var go in golist)
        {
            Selection.activeGameObject = go;
            ExportAB(output_android + go.name, BuildTarget.Android);
        }
    }

    private static void ExportAB(string path, BuildTarget platform)
    {
        string tempRelativePath = "Assets/Temp/";
        string tempAbsolutePath = Application.dataPath + "/../" + tempRelativePath; 

        if (path.Length > 0)
        {
            // Get all selected objects. 

            Object[] selection = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
            //Object[] processedSelection = new Object[selection.Length]; 
            Object[] processedSelection = selection;
            for (int i = 0; i < selection.Length; i++)
            {
                // Clone the original object. 
                
                Object currentObject = selection[i];
                bool isPrefab = currentObject != null && currentObject.GetType() == typeof(GameObject);
                if (isPrefab)
                {
                    if (!Directory.Exists(tempAbsolutePath))
                        Directory.CreateDirectory(tempAbsolutePath);

                    // Remove unneeded scripts from the prefab. 
                    /*
					Object clonedPrefab = EditorUtility.CreateEmptyPrefab(string.Format("{0}{1}.prefab", tempRelativePath, currentObject.name)); 
					if (clonedPrefab != null) 
					{ 
						clonedPrefab = EditorUtility.ReplacePrefab((GameObject)currentObject, clonedPrefab); 
						//EditorBase component = ((GameObject)clonedPrefab).GetComponent(typeof(EditorBase)) as EditorBase; 
						UnityEditor component = ((GameObject)clonedPrefab).GetComponent(typeof(UnityEditor)) as UnityEditor; 
						if (component != null) 
							GameObject.DestroyImmediate(component, true); 
						EditorUtility.SetDirty(clonedPrefab); 
						processedSelection[i] = clonedPrefab; 
					} 
					*/
                }
            }

            // Save changes to AssetDatabase and import processed prefabs. 

            EditorApplication.SaveAssets();
            AssetDatabase.Refresh();

            // Export the processed AssetBundle. 

            BuildPipeline.BuildAssetBundle(Selection.activeObject, processedSelection, path + ".unity3d", 
                BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets, platform);
            Selection.objects = selection;

            // Remove all cloned objects from the project. 

            for (int i = 0; i < processedSelection.Length; i++)
            {
                if (processedSelection[i] != null)
                    AssetDatabase.DeleteAsset(string.Format("{0}{1}.prefab", tempRelativePath, processedSelection[i].name));
            }
            if (Directory.Exists(tempAbsolutePath))
                Directory.Delete(tempAbsolutePath, false);
        } 
    }

    //[MenuItem("Assets/Export Textures PC")]
    public static void GenerateTextures()
    {
        if (Selection.objects == null)
            return;

        string path = ABDirPC;
        foreach (var obj in Selection.objects)
        {
            string fullpath = path + obj.name + ".unity3d";
            Debug.Log(fullpath);
            BuildPipeline.BuildAssetBundle(obj, null, fullpath, BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets, BuildTarget.WebPlayer);
        }
    }

    //[MenuItem("Assets/Export Textures iPhone")]
    public static void GenerateTexturesIphone()
    {
        if (Selection.objects == null)
            return;

        string path = ABDirIphone;
        foreach (var obj in Selection.objects)
        {
            string fullpath = path + obj.name + ".unity3d";
            Debug.Log(fullpath);
            BuildPipeline.BuildAssetBundle(obj, null, fullpath, BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets, BuildTarget.iPhone);
        }
    }

    //[MenuItem("Assets/Export Textures android")]
    public static void GenerateTexturesAndroid()
    {
        if (Selection.objects == null)
            return;

        string path = ABDirAndroid;
        foreach (var obj in Selection.objects)
        {
            string fullpath = path + obj.name + ".unity3d";
            Debug.Log(fullpath);
            BuildPipeline.BuildAssetBundle(obj, null, fullpath, BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets, BuildTarget.Android);
        }
    }
}

