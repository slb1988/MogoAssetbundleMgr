using UnityEngine;
using System.Collections;

using Mogo.Util;

using System.Security.Cryptography;
using System.IO;
using UnityEditor;

public class CheckHashTest
{

    //[MenuItem("Tools/CheckSampleShaderHash")]
    public static void CheckSampleShaderHash()
    {

        //ExportScenesManager.GetFileMD5("");/Resources/Shaders/Single.shader

        string file1 = Application.dataPath + "/Resources/Shaders/builtin_shaders-4.6.9/DefaultResourcesExtra/Unlit/Unlit-Normal.shader";
        string file2 = Application.dataPath + "/Resources/Single.shader";
        Debug.Log(GetFileMD5(file1));

        Debug.Log(GetFileMD5(file2));

        Debug.Log(GetFileMD5_2(file1));
        Debug.Log(GetFileMD5_2(file2));

        string file3 = "D:/workshop/My GitHub/MogoAssetbundleMgr/Export/0.0.0.1/ExportedFiles/Resources/Shaders/builtin_shaders-4.6.9/DefaultResourcesExtra/Unlit/Unlit-Normal.shader.u";

        Debug.Log(GetFileMD5(file3));
        Debug.Log(GetFileMD5_2(file3));

    }
    private static string GetFileMD5_2(string path)
    {
        FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
        var md5 = Eddy.MD5Hash.Get(stream);
        stream.Close();
        return md5;
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

}
