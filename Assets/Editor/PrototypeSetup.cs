using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

// Batch-mode helper for the prototype setup (plan step 5).
// Run with: Tuanjie.exe -batchmode -quit -projectPath <project> -executeMethod PrototypeSetup.InstallPackagesBatch
public static class PrototypeSetup
{
    static readonly string[] Wanted = { "com.unity.inputsystem", "com.unity.cinemachine" };

    static SearchRequest searchReq;
    static AddRequest addReq;
    static int step;
    static int index;

    public static void InstallPackagesBatch()
    {
        searchReq = Client.SearchAll(true);
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        switch (step)
        {
            case 0:
                if (!searchReq.IsCompleted) return;
                if (searchReq.Status == StatusCode.Success)
                {
                    foreach (var p in searchReq.Result.Where(p => Wanted.Contains(p.name)))
                        Debug.Log("[PrototypeSetup] available: " + p.name + " " + p.version);
                }
                else
                {
                    Debug.LogError("[PrototypeSetup] search failed: " + searchReq.Error.message);
                }
                addReq = Client.Add(Wanted[index]);
                step = 1;
                break;

            case 1:
                if (!addReq.IsCompleted) return;
                if (addReq.Status == StatusCode.Success)
                    Debug.Log("[PrototypeSetup] added: " + addReq.Result.name + " " + addReq.Result.version);
                else
                    Debug.LogError("[PrototypeSetup] add failed: " + Wanted[index] + " -> " + addReq.Error.message);

                index++;
                if (index < Wanted.Length)
                {
                    addReq = Client.Add(Wanted[index]);
                }
                else
                {
                    step = 2;
                }
                break;

            case 2:
                Debug.Log("[PrototypeSetup] package step finished");
                EditorApplication.update -= Tick;
                EditorApplication.Exit(0);
                break;
        }
    }
}
