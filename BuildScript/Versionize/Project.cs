
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BuildScript
{

    public class Project
    {
        public string ProjectFilePath { get; }

        public string VersionFilePath { get; }

        public VersionFile VersionFile { get; }

        public IEnumerable<ConventionalCommit> ConventionalCommits { get; set; }

        public Version NewVersion { get; private set; }

        public IEnumerable<string> Releases { get; set; }

        public string ProjectFile => Path.GetFileName(ProjectFilePath);

        private Project(
            string projectFilePath,
            string versionFilePath,
            VersionFile version)
        {
            ProjectFilePath = projectFilePath;
            VersionFilePath = versionFilePath;
            VersionFile = version;
        }

        public static Project Create(string versionFilePath)
        {
            var version = ReadVersion(versionFilePath);
            var projectFilePath = GetProjectFile(versionFilePath);
            return new Project(projectFilePath, versionFilePath, version);
        }

        public static bool IsVersionable(string versionFilePath)
        {
            try
            {
                ReadVersion(versionFilePath);
                GetProjectFile(versionFilePath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string GetProjectFile(string path)
        {
            var dirctory = Path.GetDirectoryName(path);
            var projectFile = Directory.GetFiles(dirctory, "*.csproj", SearchOption.TopDirectoryOnly);
            if (projectFile.Length == 0)
            {
                throw new InvalidOperationException($"Unable to locate .csproj for version path ${path}");
            }
            return projectFile[0];
        }

        private static VersionFile ReadVersion(string versionFilePath)
        {
            VersionFile versionFile;
            try
            {
                var jsonString = File.ReadAllText(versionFilePath);
                versionFile = JsonSerializer.Deserialize<VersionFile>(jsonString);
            }
            catch (Exception)
            {
                throw new InvalidOperationException($"Project {versionFilePath} is not a valid csproj file. Please make sure that you have a valid csproj file in place!");
            }

            if (string.IsNullOrWhiteSpace(versionFile.VersionString))
            {
                throw new InvalidOperationException($"Project {versionFilePath} contains no or an empty <Version> XML Element. Please add one if you want to version this project - for example use <Version>1.0.0</Version>");
            }

            try
            {
                versionFile.SetVersion();
                return versionFile;
            }
            catch (Exception)
            {
                throw new InvalidOperationException($"Project {versionFilePath} contains an invalid version {versionFile.VersionString}. Please fix the currently contained version - for example use <Version>1.0.0</Version>");
            }
        }

        public void WriteVersion(Version nextVersion)
        {
            var version = new VersionFile(
                nextVersion.ToString(),
                VersionFile.ScopeName,
                VersionFile.ParentScopes);

            var jsonString = JsonSerializer.Serialize(version, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(VersionFilePath, jsonString);
            NewVersion = nextVersion;
        }
    }
}
