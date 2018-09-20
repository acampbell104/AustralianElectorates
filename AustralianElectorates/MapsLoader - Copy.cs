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
        ConcurrentDictionary<string, string> electoratesCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ConcurrentDictionary<State, string> statesCache = new ConcurrentDictionary<State, string>();
        string australia;
        static Assembly assembly;

        public IReadOnlyDictionary<string, string> LoadedElectorateMaps => electoratesCache;
        public IReadOnlyDictionary<State, string> LoadedStateMaps => statesCache;

        public MapCollection(string prefix)
        {
            this.prefix = prefix;
        }

        static MapCollection()
        {

            assembly = typeof(MapsLoader).Assembly;
        }

        public string LoadElectorateMap(string electorateName)
        {
            Guard.AgainstNullWhiteSpace(electorateName, nameof(electorateName));
            return electoratesCache.GetOrAdd($@"{prefix}\Electorates\{electorateName}", Inner);
        }

        public string LoadStateMap(State state)
        {
            return electoratesCache.GetOrAdd($@"{prefix}\{state.ToString().ToLowerInvariant()}", Inner);
        }

        public string LoadAustraliaMap()
        {
            if (australia == null)
            {
                australia = Inner(@"{prefix}\australia");
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

        public  void LoadAll()
        {
            using (var stream = assembly.GetManifestResourceStream("Maps.zip"))
            using (var archive = new ZipArchive(stream))
            {
                foreach (var entry in archive.Entries)
                {
                    var key = entry.FullName.Split('.').First();
                    var mapString = ReadString(entry);

                    if (key.StartsWith(@"{prefix}\Electorates"))
                    {
                        electoratesCache[key] = mapString;
                        continue;
                    }
                    if (key==@"{prefix}\australia")
                    {
                        australia = mapString;
                        continue;
                    }
                    if (key.StartsWith(prefix))
                    {
                        var state = ParseState(key);
                        statesCache[state] = mapString;
                        continue;
                    }
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