using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using MSBuild.SolutionSdk.Tasks.Sln;

namespace MSBuild.SolutionSdk.Tasks
{
    public class GenerateSln : Task
    {
        [Required]
        public ITaskItem[] Projects { get; set; }
        [Required]
        public ITaskItem ProjectName { get; set; }
        [Required]
        public ITaskItem ProjectDirectory { get; set; }
        [Required]
        public ITaskItem[] ProjectMetaData { get; set; }
        // [Required]
        public ITaskItem[] Configurations { get; set; }
        // [Required]
        public ITaskItem[] Platforms { get; set; }
        public ITaskItem[] AdditionalProperties { get; set; }
        static Dictionary<string, string> ExtractMap(string str, char delimiterElement, char delimiterKeyValue)
        {
            if(string.IsNullOrEmpty(str))
            {
                return new Dictionary<string, string>();
            }
            return str.Split(delimiterElement)
                .Select(x => x.Split(new char[1]{ delimiterKeyValue }, 2))
                .Where(x => x.Length == 2)
                .ToDictionary(x => x[0], x => x[1])
                ;
        }
        public override bool Execute()
        {
            if (ProjectName == null)
            {
                Log.LogMessage($"ProjectName is null");
            }
            else
            {
                Log.LogMessage($"ProjectName is {ProjectName.ItemSpec}");
            }
            if (ProjectDirectory == null)
            {
                Log.LogMessage($"ProjectDirectory is null");
            }
            else
            {
                Log.LogMessage($"ProjectDirectory is {ProjectDirectory.ItemSpec}");
            }
            if (Projects != null)
            {
                var slnFile = new SlnFile();
                var slnFileName = Path.GetFileNameWithoutExtension(ProjectName.ItemSpec) + ".sln";
                Log.LogMessage("project num = {0}", Projects.Length);
                var configurations = new string[] { "Debug", "Release" };
                var platforms = new string[] { "Any CPU", "x86", "x64" };
                if(Platforms != null && Platforms.Length != 0)
                {
                    platforms = Platforms.Select(x => x.ItemSpec).ToArray();
                }
                if(Configurations != null && Configurations.Length != 0)
                {
                    configurations = Configurations.Select(x => x.ItemSpec).ToArray();
                }
                var projectConfigurationMap = new Dictionary<Guid, Dictionary<string, string>>();
                var projectPlatformMap = new Dictionary<Guid, Dictionary<string, string>>();
                var projects = Projects.Select(proj =>
                {
                    var guid = Guid.NewGuid();
                    var typeguid = SlnProject.GetKnownProjectTypeGuid(Path.GetExtension(proj.ItemSpec), true, new Dictionary<string, Guid>());
                    Log.LogMessage("typeguid={0}", typeguid);
                    var configurationMap = ExtractMap(proj.GetMetadata("ConfigurationMap"), ';', '=');
                    if(configurationMap != null && configurationMap.Count != 0)
                    {
                        projectConfigurationMap[guid] = configurationMap;
                    }
                    var platformMap = ExtractMap(proj.GetMetadata("PlatformMap"), ';', '=');
                    if(platformMap != null && platformMap.Count != 0)
                    {
                        projectPlatformMap[guid] = platformMap;
                    }
                    var projectConfigurations = configurations.Select(x => configurationMap.ContainsKey(x) ? configurationMap[x] : x)
                        .OrderBy(x => x).Distinct().ToArray();
                    var projectPlatforms = platforms.Select(x => platformMap.ContainsKey(x) ? platformMap[x] : x)
                        .OrderBy(x => x).Distinct().ToArray();
                    return new SlnProject(
                        proj.ItemSpec,
                        Path.GetFileNameWithoutExtension(proj.ItemSpec),
                        guid,
                        typeguid,
                        projectConfigurations,
                        projectPlatforms,
                        false);
                });
                slnFile.UpdateConfigurationMap(projectConfigurationMap);
                slnFile.UpdatePlatformMap(projectPlatformMap);
                slnFile.AddProjects(projects);
                slnFile.Save(Path.Combine(ProjectDirectory.ItemSpec, slnFileName), false);
                foreach (var proj in Projects)
                {
                    Log.LogMessage("itemspec = {0}, metadatanames = {1}", proj.ItemSpec, string.Join("|", proj.MetadataNames.Cast<string>()));
                    foreach (var metaDataName in proj.MetadataNames.Cast<string>())
                    {
                        var metaData = proj.GetMetadata(metaDataName);
                        Log.LogMessage("{0} = {1}", metaDataName, metaData);
                    }
                }
            }
            else
            {
                Log.LogWarning("Projects is null");
            }
            if (ProjectMetaData != null)
            {
                Log.LogMessage("projectmetadata num = {0}", ProjectMetaData.Length);
                foreach (var proj in ProjectMetaData)
                {
                    // var guid = Guid.NewGuid();
                    // var projectSlnFileName = Path.GetFileNameWithoutExtension(proj.ItemSpec) + ".sln";
                    // var slnProj = new SlnProject(Path.GetFullPath(proj.ItemSpec), Path.GetFileNameWithoutExtension(projectSlnFileName), guid, SlnProject.GetKnownProjectTypeGuid(Path.GetExtension(proj.ItemSpec), true, null), null, null, false);
                    Log.LogMessage("meta: itemspec = {0}, metadatanames = {1}", proj.ItemSpec, string.Join("|", proj.MetadataNames.Cast<string>()));
                    foreach (var metaDataName in proj.MetadataNames.Cast<string>())
                    {
                        var metaData = proj.GetMetadata(metaDataName);
                        Log.LogMessage("meta: {0} = {1}", metaDataName, metaData);
                    }
                }
            }
            else
            {
                Log.LogWarning("Projects is null");
            }
            return true;
        }
    }
}