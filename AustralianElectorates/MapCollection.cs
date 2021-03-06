﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace AustralianElectorates
{
    public class MapCollection
    {
        string prefix;
        ConcurrentDictionary<string, ElectorateMap> electoratesCache = new ConcurrentDictionary<string, ElectorateMap>(StringComparer.OrdinalIgnoreCase);
        ConcurrentDictionary<State, StateMap> statesCache = new ConcurrentDictionary<State, StateMap>();
        string australia;
        static Assembly assembly;

        public IReadOnlyDictionary<string, ElectorateMap> LoadedElectorates => electoratesCache;
        public IReadOnlyDictionary<State, StateMap> LoadedStates => statesCache;

        internal MapCollection(string prefix)
        {
            this.prefix = prefix;
        }

        static MapCollection()
        {
            assembly = typeof(DataLoader).Assembly;
        }

        public ElectorateMap GetElectorate(string electorateName)
        {
            electorateName = GetElectorateShortName(electorateName);
            Guard.AgainstNullWhiteSpace(electorateName, nameof(electorateName));
            return electoratesCache.GetOrAdd($@"{prefix}\Electorates\{electorateName}",
                s =>
                {
                    var geoJson = Inner(s);
                    var electorate = DataLoader.Electorates.Single(x => x.ShortName == electorateName);
                    return new ElectorateMap
                    {
                        Electorate = electorate,
                        GeoJson = geoJson
                    };
                });
        }

        public StateMap GetState(State state)
        {
            var key = $@"{prefix}\{state.ToString().ToLowerInvariant()}";
            return statesCache.GetOrAdd(state, s =>
            {
                var geoJson = Inner(key);
                return new StateMap
                {
                    State = state,
                    GeoJson = geoJson
                };
            });
        }

        public string GetAustralia()
        {
            if (australia == null)
            {
                australia = Inner($@"{prefix}\australia");
            }

            return australia;
        }

        static string Inner(string path)
        {
            using (var stream = assembly.GetManifestResourceStream("Maps.zip"))
            using (var archive = new ZipArchive(stream))
            {
                var entry = archive.GetEntry($"{path}.geojson");
                if (entry == null)
                {
                    throw new Exception($"Could not find data for '{path}'.");
                }

                return ReadString(entry);
            }
        }

        public static string GetElectorateShortName(string electorate)
        {
            return electorate.Replace(" ", "-").Replace("'", "").ToLowerInvariant();
        }

        public void LoadAll()
        {
            using (var stream = assembly.GetManifestResourceStream("Maps.zip"))
            using (var archive = new ZipArchive(stream))
            {
                foreach (var entry in archive.Entries.Where(x => x.FullName.StartsWith(prefix)))
                {
                    var key = entry.FullName.Split('.').First();
                    var mapString = ReadString(entry);

                    if (key.Contains("Electorates"))
                    {
                        var shortName = Path.GetFileName(key);
                        var electorate = DataLoader.Electorates.Single(x => x.ShortName == shortName);
                        electoratesCache[key] = new ElectorateMap
                        {
                            Electorate = electorate,
                            GeoJson = mapString
                        };
                        continue;
                    }

                    if (key.Contains("australia"))
                    {
                        australia = mapString;
                        continue;
                    }

                    var state = ParseState(key);
                    statesCache[state] = new StateMap
                    {
                        GeoJson = mapString,
                        State = state
                    };
                }
            }
        }

        static State ParseState(string key)
        {
            return (State) Enum.Parse(typeof(State), key.Split('\\')[1], true);
        }

        static string ReadString(ZipArchiveEntry entry)
        {
            using (var entryStream = entry.Open())
            using (var reader = new StreamReader(entryStream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}