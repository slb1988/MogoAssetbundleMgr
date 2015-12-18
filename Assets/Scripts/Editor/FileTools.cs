using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System;
using System.Diagnostics;

using System.IO;

using System.Net;
using System.Net.Sockets;

using Object = UnityEngine.Object;

using Mogo.Util;

public class FileTools
{
    private static List<string> drives = new List<string>() { "c:", "d:", "e:", "f:" };
    private static string notepadPath = @"\Program Files (x86)\Notepad++\notepad++.exe";
    
    public static string GetNotePadPath()
    {
        foreach (var item in drives)
        {
            var path = notepadPath;
            if (File.Exists(notepadPath))
            {
                return path;
            }
        }

        notepadPath = EditorUtility.OpenFilePanel("Select NotePad++.exe", "d:\\", "exe");
        return notepadPath;
    }
}
