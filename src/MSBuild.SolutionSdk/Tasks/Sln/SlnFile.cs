// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MSBuild.SolutionSdk.Tasks.Sln
{
    internal sealed class SlnFile
    {
        /// <summary>
        /// The solution header
        /// </summary>
        private const string Header = "Microsoft Visual Studio Solution File, Format Version {0}";

        /// <summary>
        /// The file format version
        /// </summary>
        private readonly string _fileFormatVersion;

        /// <summary>
        /// Gets the projects.
        /// </summary>
        private readonly List<SlnProject> _projects = new List<SlnProject>();

        /// <summary>
        /// A list of absolute paths to include as Solution Items.
        /// </summary>
        private readonly List<string> _solutionItems = new List<string>();

        private Dictionary<Guid, Dictionary<string, string>> _configurationMap = new Dictionary<Guid, Dictionary<string, string>>();
        private Dictionary<Guid, Dictionary<string, string>> _platformMap = new Dictionary<Guid, Dictionary<string, string>>();

        private string[] _solutionConfigurations;
        private string[] _solutionPlatforms;

        /// <summary>
        /// Initializes a new instance of the <see cref="SlnFile" /> class.
        /// </summary>
        /// <param name="projects">The project collection.</param>
        /// <param name="fileFormatVersion">The file format version.</param>
        public SlnFile(string fileFormatVersion, string[] configurations, string[] platforms)
        {
            _fileFormatVersion = fileFormatVersion;
            _solutionConfigurations = configurations;
            _solutionPlatforms = platforms;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SlnFile" /> class.
        /// </summary>
        /// <param name="projects">The projects.</param>
        public SlnFile()
            : this("12.00", null, null)
        {
        }

        /// <summary>
        /// Gets a list of solution items.
        /// </summary>
        public IReadOnlyCollection<string> SolutionItems => _solutionItems;

        /// <summary>
        /// Adds the specified projects.
        /// </summary>
        /// <param name="projects">An <see cref="IEnumerable{SlnProject}"/> containing projects to add to the solution.</param>
        public void AddProjects(IEnumerable<SlnProject> projects)
        {
            _projects.AddRange(projects);
        }

        public void UpdateConfigurationMap(IEnumerable<KeyValuePair<Guid, Dictionary<string, string>>> configurationMap)
        {
            foreach (var map in configurationMap)
            {
                _configurationMap[map.Key] = map.Value;
            }
        }
        public void UpdatePlatformMap(IEnumerable<KeyValuePair<Guid, Dictionary<string, string>>> maps)
        {
            foreach (var map in maps)
            {
                _platformMap[map.Key] = map.Value;
            }
        }

        /// <summary>
        /// Adds the specified solution items.
        /// </summary>
        /// <param name="items">An <see cref="IEnumerable{String}"/> containing items to add to the solution.</param>
        public void AddSolutionItems(IEnumerable<string> items)
        {
            _solutionItems.AddRange(items);
        }

        public void Save(string path, bool folders)
        {
            Save(path, folders, null, null);
        }
        /// <summary>
        /// Saves the Visual Studio solution to a file.
        /// </summary>
        /// <param name="path">The full path to the file to write to.</param>
        /// <param name="folders">Specifies if folders should be created.</param>
        public void Save(string path, bool folders, IReadOnlyDictionary<string, string> configurationMap, IReadOnlyDictionary<string, string> platformMap)
        {
            string directoryName = Path.GetDirectoryName(path);

            if (!String.IsNullOrWhiteSpace(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            using (StreamWriter writer = File.CreateText(path))
            {
                Save(writer, folders, configurationMap, platformMap);
            }
        }

        public void Save(TextWriter writer, bool folders)
        {
            Save(writer, folders, null, null);
        }

        public void Save(TextWriter writer, bool folders, IReadOnlyDictionary<string, string> configurationMap, IReadOnlyDictionary<string, string> platformMap)
        {
            configurationMap = configurationMap == null ? new Dictionary<string, string>() : configurationMap;
            platformMap = platformMap == null ? new Dictionary<string, string>() : platformMap;
            writer.WriteLine(Header, _fileFormatVersion);

            foreach (SlnProject project in _projects)
            {
                writer.WriteLine($@"Project(""{project.ProjectTypeGuid.ToSolutionString()}"") = ""{project.Name}"", ""{project.FullPath}"", ""{project.ProjectGuid.ToSolutionString()}""");
                writer.WriteLine("EndProject");
            }

            if (SolutionItems.Count > 0)
            {
                writer.WriteLine($@"Project(""{SlnFolder.FolderProjectTypeGuid.ToSolutionString()}"") = ""Solution Items"", ""Solution Items"", ""{Guid.NewGuid().ToSolutionString()}"" ");
                writer.WriteLine("	ProjectSection(SolutionItems) = preProject");
                foreach (string solutionItem in SolutionItems)
                {
                    writer.WriteLine($"		{solutionItem} = {solutionItem}");
                }

                writer.WriteLine("	EndProjectSection");
                writer.WriteLine("EndProject");
            }

            SlnHierarchy hierarchy = null;

            if (folders)
            {
                hierarchy = SlnHierarchy.FromProjects(_projects);

                if (hierarchy.Folders.Count > 0)
                {
                    foreach (SlnFolder folder in hierarchy.Folders)
                    {
                        writer.WriteLine($@"Project(""{folder.ProjectTypeGuid.ToSolutionString()}"") = ""{folder.Name}"", ""{folder.FullPath}"", ""{folder.FolderGuid.ToSolutionString()}""");
                        writer.WriteLine("EndProject");
                    }
                }
            }

            writer.WriteLine("Global");

            writer.WriteLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");

            IEnumerable<string> globalConfigurations = Enumerable.Empty<string>();
            if (_solutionConfigurations != null && _solutionConfigurations.Length != 0)
            {
                globalConfigurations = _solutionConfigurations;
            }
            else
            {
                globalConfigurations = _projects.SelectMany(p => p.Configurations).Distinct().ToArray();
            }
            IEnumerable<string> globalPlatforms = Enumerable.Empty<string>();
            if (_solutionPlatforms != null && _solutionPlatforms.Length != 0)
            {
                globalPlatforms = _solutionPlatforms;
            }
            else
            {
                globalPlatforms = _projects.SelectMany(p => p.Platforms).Distinct().ToArray();
            }
            foreach (string configuration in globalConfigurations)
            {
                foreach (string platform in globalPlatforms)
                {
                    if (!string.IsNullOrWhiteSpace(configuration) && !string.IsNullOrWhiteSpace(platform))
                    {
                        writer.WriteLine($"\t\t{configuration}|{platform} = {configuration}|{platform}");
                    }
                }
            }

            writer.WriteLine("\tEndGlobalSection");

            writer.WriteLine("	GlobalSection(ProjectConfigurationPlatforms) = preSolution");
            foreach (SlnProject project in _projects)
            {
                var mappedConfigurationMap = new Dictionary<string, string>();
                if (configurationMap.TryGetValue(project.FullPath, out var configurationMapString))
                {
                    mappedConfigurationMap = configurationMapString.Split(';').Select(x => x.Split(new[] { '=' }, 2)).Where(x => x.Length == 2).ToDictionary(x => x[0], x => x[1]);
                }
                var mappedPlatformMap = new Dictionary<string, string>();
                if (platformMap.TryGetValue(project.FullPath, out var projectPlatformMapString))
                {
                    mappedPlatformMap = projectPlatformMapString.Split(';').Select(x => x.Split(new[] { '=' }, 2)).Where(x => x.Length == 2).ToDictionary(x => x[0], x => x[1]);
                }
                foreach (var (configuration, mappedName) in globalConfigurations.Select(x =>
                {
                    var mapped = mappedConfigurationMap.FirstOrDefault(y => y.Value == x);
                    if(mapped.Value != null)
                    {
                        return (configuration: mapped.Key, mappedName: mapped.Value);
                    }
                    else
                    {
                        if(project.Configurations.Contains(x))
                        {
                            return (configuration: x, mappedName: x);
                        }
                        else
                        {
                            return (configuration: null, mappedName: null);
                        }
                    }
                }).Where(x => x.configuration != null))
                {
                    foreach (string platform in project.Platforms)
                    {
                        if (!string.IsNullOrWhiteSpace(configuration) && !string.IsNullOrWhiteSpace(platform))
                        {
                            string mappedConfiguration = mappedName;
                            string mappedPlatform;
                            if (!mappedPlatformMap.TryGetValue(platform, out mappedPlatform))
                            {
                                mappedPlatform = platform;
                            }
                            if (globalConfigurations.Any(x => x == mappedConfiguration) && globalPlatforms.Any(x => x == mappedPlatform))
                            {
                                writer.WriteLine($@"		{project.ProjectGuid.ToSolutionString()}.{mappedConfiguration}|{mappedPlatform}.ActiveCfg = {configuration}|{platform}");
                                writer.WriteLine($@"		{project.ProjectGuid.ToSolutionString()}.{mappedConfiguration}|{mappedPlatform}.Build.0 = {configuration}|{platform}");
                            }
                        }
                    }
                }
            }

            writer.WriteLine("\tEndGlobalSection");

            if (folders
                && _projects.Count > 1)
            {
                writer.WriteLine(@"	GlobalSection(NestedProjects) = preSolution");
                foreach (KeyValuePair<Guid, Guid> nestedProject in hierarchy.Hierarchy)
                {
                    writer.WriteLine($@"		{nestedProject.Key.ToSolutionString()} = {nestedProject.Value.ToSolutionString()}");
                }

                writer.WriteLine("	EndGlobalSection");
            }

            writer.WriteLine("EndGlobal");
        }
    }
}