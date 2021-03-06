﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using AustralianElectorates;
using GeoJSON.Net.Feature;
using Xunit;

public class Sync
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task SyncData()
    {
        IoHelpers.PurgeDirectory(DataLocations.MapsPath);
        IoHelpers.PurgeDirectory(DataLocations.TempPath);

        await GetCurrent();

        await FutureToCountry.Run();

        ProcessYear(DataLocations.MapsCurrentPath, electoratesCurrent);
        ProcessYear(DataLocations.MapsFuturePath, electoratesFuture);

        var electorates = await WriteElectoratesMetaData();
        WriteNamedCs(electorates);
        Export.ExportElectorates();
        Zipper.ZipDir(DataLocations.MapsCuratedZipPath, DataLocations.MapsCuratedPath);
    }

    static List<int> percents;

    static List<string> electoratesFuture = new List<string>();
    static List<string> electoratesCurrent = new List<string>();

    static Dictionary<State, HashSet<string>> electorateNames = new Dictionary<State, HashSet<string>>
    {
        {State.ACT, new HashSet<string>()},
        {State.TAS, new HashSet<string>()},
        {State.SA, new HashSet<string>()},
        {State.VIC, new HashSet<string>()},
        {State.QLD, new HashSet<string>()},
        {State.NT, new HashSet<string>()},
        {State.NSW, new HashSet<string>()},
        {State.WA, new HashSet<string>()},
    };

    static List<State> states = new List<State>
    {
        State.ACT,
        State.TAS,
        State.SA,
        State.VIC,
        State.QLD,
        State.NT,
        State.NSW,
        State.WA,
    };

    static Sync()
    {
        percents = new List<int> {20, 10, 5, 1};
    }

    static void ProcessYear(string yearPath, List<string> electorates)
    {
        WriteOptimised(yearPath);

        foreach (var australiaPath in Directory.EnumerateFiles(yearPath, "australia*"))
        {
            var australiaFeatures = JsonSerializer.DeserializeGeo(australiaPath);

            var electoratesDirectory = Path.Combine(yearPath, "Electorates");
            Directory.CreateDirectory(electoratesDirectory);
            foreach (var state in states)
            {
                var lower = state.ToString().ToLower();
                var featureCollectionForState = australiaFeatures.FeaturesCollectionForState(state);
                var suffix = Path.GetFileName(australiaPath).Replace("australia", "");
                var stateJson = Path.Combine(yearPath, $"{lower}{suffix}");
                JsonSerializer.SerializeGeo(featureCollectionForState, stateJson);

                foreach (var electorateFeature in featureCollectionForState.Features)
                {
                    var electorate = (string) electorateFeature.Properties["electorateShortName"];
                    var electorateNameList = electorateNames[state];
                    electorateNameList.Add(electorate);
                    electorates.Add(electorate);

                    var electorateJsonPath = Path.Combine(electoratesDirectory, $"{electorate}{suffix}");
                    JsonSerializer.SerializeGeo(electorateFeature.ToCollection(), electorateJsonPath);
                }
            }
        }
    }

    static async Task GetCurrent()
    {
        var zip = Path.Combine(DataLocations.TempPath, "current.zip");
        await Downloader.DownloadFile(zip, "https://www.aec.gov.au/Electorates/gis/files/national-midmif-09052016.zip");
        var targetPath = Path.Combine(DataLocations.MapsCurrentPath, "australia.geojson");

        var extractDirectory = Path.Combine(DataLocations.TempPath, "australiaCurrent_extract");
        ZipFile.ExtractToDirectory(zip, extractDirectory);

        MapToGeoJson.ConvertTab(targetPath, Path.Combine(extractDirectory, "COM_ELB.tab"));

        var featureCollection = JsonSerializer.Deserialize<FeatureCollection>(targetPath);
        featureCollection.FixBoundingBox();
        MetadataCleaner.CleanMetadata(featureCollection);
        JsonSerializer.SerializeGeo(featureCollection, targetPath);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void AssertAllContainBbox()
    {
        foreach (var file in Directory.EnumerateFiles(DataLocations.MapsPath, "*.geojson", SearchOption.AllDirectories))
        {
            if (!File.ReadAllText(file).Contains("bbox"))
            {
                throw new Exception(file);
            }
        }
    }

    static void WriteOptimised(string directory)
    {
        var jsonPath = Path.Combine(directory, "australia.geojson");
        var raw = JsonSerializer.DeserializeGeo(jsonPath);
        raw.FixBoundingBox();
        foreach (var percent in percents)
        {
            var percentJsonPath = Path.Combine(directory, $"australia_{percent:D2}.geojson");
            MapToGeoJson.ConvertShape(percentJsonPath, jsonPath, percent);
            var featureCollection = JsonSerializer.DeserializeGeo(percentJsonPath);
            featureCollection.FixBoundingBox();

            JsonSerializer.SerializeGeo(featureCollection, percentJsonPath);
        }
    }

    static async Task<List<Electorate>> WriteElectoratesMetaData()
    {
        var electorates = new List<Electorate>();
        foreach (var electoratePair in electorateNames)
        {
            foreach (var electorateName in electoratePair.Value)
            {
                var existInCurrent = electoratesCurrent.Contains(electorateName);
                var existInFuture = electoratesFuture.Contains(electorateName);
                var electorate = await ElectoratesScraper.ScrapeElectorate(electorateName, electoratePair.Key);
                electorate.ExistInCurrent = existInCurrent;
                electorate.ExistInFuture = existInFuture;
                electorates.Add(electorate);
            }
        }

        var combine = Path.Combine(DataLocations.DataPath, "electorates.json");
        File.Delete(combine);
        JsonSerializer.Serialize(electorates, combine);
        return electorates;
    }

    static void WriteNamedCs(List<Electorate> electorates)
    {
        var namedData = Path.Combine(DataLocations.AustralianElectoratesProjectPath, "DataLoader_named.cs");
        File.Delete(namedData);

        using (var writer = File.CreateText(namedData))
        {
            writer.WriteLine(@"
// ReSharper disable IdentifierTypo
using System.Linq;

namespace AustralianElectorates
{
    public static partial class DataLoader
    {");

            writer.WriteLine(@"
        static void InitNamed()
        {");
            foreach (var electorate in electorates)
            {
                var name = GetCSharpName(electorate);
                writer.WriteLine($@"
            {name} = Electorates.Single(x => x.Name == ""{electorate.Name}"");");
            }
            writer.WriteLine("        }");

            foreach (var electorate in electorates)
            {
                var name = GetCSharpName(electorate);
                writer.WriteLine($@"
        public static Electorate {name} {{ get; private set;}}");
            }

            writer.WriteLine("    }");
            writer.WriteLine("}");
        }


        var namedBogusData = Path.Combine(DataLocations.BogusProjectPath, "ElectorateDataSet_named.cs");
        File.Delete(namedBogusData);
        using (var writer = File.CreateText(namedBogusData))
        {
            writer.WriteLine(@"
// ReSharper disable IdentifierTypo
using Bogus;

namespace AustralianElectorates.Bogus
{
    public partial class ElectorateDataSet : DataSet
    {");
            foreach (var electorate in electorates)
            {
                var name = GetCSharpName(electorate);
                writer.WriteLine($@"
        public Electorate {name}()
        {{
            return DataLoader.{name};
        }}");
            }
            writer.WriteLine("    }");
            writer.WriteLine("}");
        }
    }

    private static string GetCSharpName(Electorate electorate)
    {
        return electorate.Name.Replace(" ", "").Replace("-", "").Replace("'", "");
    }
}