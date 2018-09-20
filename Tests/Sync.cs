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
    static List<int> percents;

    private static Dictionary<string, List<string>> electoratesByYear = new Dictionary<string, List<string>>();

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

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SyncData()
    {
        IoHelpers.PurgeDirectory(DataLocations.MapsPath);
        IoHelpers.PurgeDirectory(DataLocations.TempPath);

        await Get2016();

        await FutureToCountry.Run();

        foreach (var yearPath in Directory.EnumerateDirectories(DataLocations.MapsPath))
        {
            var electorates = new List<string>();
            electoratesByYear.Add(Path.GetFileName(yearPath), electorates);
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

        await WriteElectoratesMetaData();
        Export.ExportElectorates();
        Zipper.ZipDir(DataLocations.MapsCuratedZipPath, DataLocations.MapsCuratedPath);
    }

    static async Task Get2016()
    {
        var australia2016zip = Path.Combine(DataLocations.TempPath, "2016.zip");
        await Downloader.DownloadFile(australia2016zip, "https://www.aec.gov.au/Electorates/gis/files/national-midmif-09052016.zip");
        var australia2016 = Path.Combine(DataLocations.MapsPath, "2016", "australia.geojson");

        var extractDirectory = Path.Combine(DataLocations.TempPath, "australia2016_extract");
        ZipFile.ExtractToDirectory(australia2016zip, extractDirectory);

        MapToGeoJson.ConvertTab(australia2016, Path.Combine(extractDirectory, "COM_ELB.tab"));

        var featureCollection = JsonSerializer.Deserialize<FeatureCollection>(australia2016);
        featureCollection.FixBoundingBox();
        MetadataCleaner.CleanMetadata(featureCollection);
        JsonSerializer.SerializeGeo(featureCollection, australia2016);
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

    static async Task WriteElectoratesMetaData()
    {
        var electorates = new List<Electorate>();
        var electorates2016 = electoratesByYear["2016"];
        var electoratesFuture = electoratesByYear["Future"];
        foreach (var electoratePair in electorateNames)
        {
            foreach (var electorateName in electoratePair.Value)
            {
                var existIn2016 = electorates2016.Contains(electorateName);
                var existInFuture = electoratesFuture.Contains(electorateName);
                var electorate = await ElectoratesScraper.ScrapeElectorate(electorateName, electoratePair.Key);
                electorate.ExistIn2016 = existIn2016;
                electorate.ExistInFuture = existInFuture;
                electorates.Add(electorate);
            }
        }

        var combine = Path.Combine(DataLocations.DataPath, "electorates.json");
        File.Delete(combine);
        JsonSerializer.Serialize(electorates, combine);
    }
}