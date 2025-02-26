#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CodeBase
{
    public class FilesMap
    {
        private const string RootFolder = @"Assets/";

        public List<string> AllFiles = new();

        /// <summary>
        /// List of files and assets they depend on
        /// </summary>
        private Dictionary<string, string[]> _allDependencies;

        /// <summary>
        /// List of files and assets they are used by
        /// </summary>
        private Dictionary<string, List<string>> _allUsages;

        public void PrepareFiles(string dir = null)
        {
            GetFilesWithoutMeta(dir ?? RootFolder, out var clientFiles, filesMask: new List<string> { ".cs", ".shader" });
            AllFiles = clientFiles;

            Debug.LogWarning($"Files count: {AllFiles.Count}");
        }

        public void CollectAllDependenciesAndUsages()
        {
            _allDependencies = new Dictionary<string, string[]>(100000);
            _allUsages = new Dictionary<string, List<string>>(100000);

            foreach (var file in AllFiles)
            {
                var dependencies = AssetDatabase.GetDependencies(file);
                dependencies.NormalizeRelativePaths();

                var duplicateIndex = Array.IndexOf(dependencies, file); // do not store reference to itself
                if (duplicateIndex >= 0)
                {
                    dependencies[duplicateIndex] = null;
                    dependencies = dependencies.Where(d => d != null).ToArray();
                }

                foreach (var dependency in dependencies)
                {
                    if (file == dependency) continue;

                    if (!_allUsages.ContainsKey(dependency))
                    {
                        _allUsages.Add(dependency, new List<string>());
                    }

                    _allUsages[dependency].Add(file);
                }

                _allDependencies[file] = dependencies;
            }
        }

        private static void GetFilesWithoutMeta(
            string directoryPath,
            out List<string> output,
            List<string> exceptDirsList = null,
            List<string> filesMask = null)
        {
            output = new List<string>();
            GetAllFiles(directoryPath, output, exceptDirsList, filesMask);
            output = output.Where(f => !f.EndsWith(".meta")).ToList();
            output.NormalizeRelativePaths();
        }

        private static void GetAllFiles(
            string directoryPath,
            List<string> output,
            List<string> exceptDirsList = null,
            List<string> filesMask = null)
        {
            if (output == null) output = new List<string>();

            var files = Directory.GetFiles(directoryPath);
            var dirs = Directory.GetDirectories(directoryPath);

            if (files.Length > 0)
            {
                output.AddRange(filesMask == null
                    ? files
                    : files.Where(file => filesMask.All(mask => !file.Contains(mask))));
            }

            if (dirs.Length <= 0) return;

            foreach (var dir in dirs)
            {
                if (exceptDirsList != null && exceptDirsList.Count > 0 &&
                    exceptDirsList.Any(excDir => dir.Contains(excDir)))
                {
                    continue;
                }

                GetAllFiles(dir, output, exceptDirsList);
            }
        }
    }
}
#endif