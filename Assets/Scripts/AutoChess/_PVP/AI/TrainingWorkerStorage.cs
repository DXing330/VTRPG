using System.IO;
using UnityEngine;

public static class TrainingWorkerStorage
{
    private static int workerId = 0;
    public static void SetWorker(int id)
    {
        workerId = id;
    }
    public static bool IsWorkerMode =>
        workerId > 0;
    public static string GetWorkerPath(int newWorker)
    {
        string trainingPath = Path.Combine(
            Application.persistentDataPath,
            "Training"
        );
        string workerPath = Path.Combine(
            trainingPath,
            $"Worker_{newWorker}"
        );
        Directory.CreateDirectory(workerPath);
        return workerPath;
    }
    public static string GetWorkerFilePath(int newWorker, string filename)
    {
         return Path.Combine(GetWorkerPath(newWorker), filename);
    }
    public static string GetPersistentPath()
    {
        if (!IsWorkerMode)
            return Application.persistentDataPath;
        string trainingPath = Path.Combine(
            Application.persistentDataPath,
            "Training"
        );
        string workerPath = Path.Combine(
            trainingPath,
            $"Worker_{workerId}"
        );
        Directory.CreateDirectory(workerPath);
        return workerPath;
    }
    public static string GetFilePath(string filename)
    {
        return Path.Combine(
            GetPersistentPath(),
            filename
        );
    }
}