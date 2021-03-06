﻿using System;
using System.Diagnostics;

public class MapToGeoJson
{
    public static void ConvertShape(string targetFile, string shpFile, int? percent = null)
    {
        //mapshaper C:\Code\AustralianElectorates\Data\ElectoratesByState\act.geojson -simplify dp 20% -o format=geojson C:\Code\AustralianElectorates\Data\ElectoratesByState\temp.json
        var arguments = $@"/C mapshaper ""{shpFile}"" ";
        if (percent != null)
        {
            arguments += $" -simplify {percent}%";
        }
        arguments += $@" -o format=geojson ""{targetFile}"" ";
        Run("cmd.exe", arguments);
    }

    public static void ConvertTab(string targetFile, string tabFile)
    {
        var arguments = $@"-f GeoJSON {targetFile} {tabFile}";
        Run("ogr2ogr", arguments);
    }

    static void Run(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using (var process = Process.Start(startInfo))
        {
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                var readToEnd = process.StandardError.ReadToEnd();
                if (readToEnd.Contains("Error"))
                {
                    throw new Exception($"Failed to run: {arguments}. Output: {readToEnd}");
                }
            }
        }
    }
}