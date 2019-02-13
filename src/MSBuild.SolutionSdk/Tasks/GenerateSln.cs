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
                var projects = Projects.Select(proj =>
                {
                    var guid = Guid.NewGuid();
                    var typeguid = SlnProject.GetKnownProjectTypeGuid(Path.GetExtension(proj.ItemSpec), true, new Dictionary<string, Guid>());
                    Log.LogMessage("typeguid={0}", typeguid);
                    return new SlnProject(
                        proj.ItemSpec,
                        Path.GetFileNameWithoutExtension(proj.ItemSpec),
                        guid,
                        typeguid,
                        configurations,
                        platforms,
                        false);
                });
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