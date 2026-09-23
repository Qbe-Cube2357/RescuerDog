using System.IO;
using UnityEngine;

public static class ExternalPaths
{
    public static string ExternalDirectory
    {
        get
        {
#if UNITY_EDITOR
            return Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "external")
            );
#else
            return Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "..", "external")
            );
#endif
        }
    }

    public static string SensorExecutablePath =>
        Path.Combine(ExternalDirectory, "m5_sensor");

    public static string SensorDataFilePath =>
        Path.Combine(ExternalDirectory, "data.txt");
}