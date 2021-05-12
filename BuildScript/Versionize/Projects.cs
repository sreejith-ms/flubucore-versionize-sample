using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BuildScript
{

    public class Projects
    {
        private readonly List<Project> projects;

        private Projects(List<Project> projects)
        {
            this.projects = projects;
        }

        public bool IsEmpty()
        {
            return projects.Count == 0;
        }

        public IEnumerable<Project> UpdatedProjects => projects.Where(x => x.NewVersion != null);

        //public Version Version { get => _projects.First().Version; }

        public static Projects Discover(string workingDirectory)
        {
            var projects = Directory
                .GetFiles(workingDirectory, "version.json", SearchOption.AllDirectories)
                .Where(Project.IsVersionable)
                .Select(Project.Create)
                .ToList();

            return new Projects(projects);
        }

        public void WriteVersion(Version nextVersion)
        {
            foreach (var project in projects)
            {
                project.WriteVersion(nextVersion);
            }
        }

        public void WriteVersion(bool hasVersionTag, bool ignoreInsignificant)
        {
            for (var index = 0; index < projects.Count; index++)
            {
                var project = projects[index];
                var versionIncrement = VersionIncrementStrategy.CreateFrom(project.ConventionalCommits);
                var isInitialVersion = !hasVersionTag || !project.Releases.Any();
                var nextVersion = !isInitialVersion
                    ? versionIncrement.NextVersion(project.VersionFile.Version, ignoreInsignificant)
                    : project.VersionFile.Version;
                if (nextVersion != project.VersionFile.Version || isInitialVersion)
                {
                    project.WriteVersion(nextVersion);
                }
            }
        }

        public IEnumerable<string> GetVersionFilesPath()
        {
            return projects.Select(project => project.VersionFilePath);
        }

        public void GroupCommitsAndReleases(
            List<ConventionalCommit> conventionalCommits,
            IEnumerable<string> tags)
        {
            if (conventionalCommits.Count == 0)
            {
                return;
            }

            for (var index = 0; index < projects.Count; index++)
            {
                var project = projects[index];
                project.ConventionalCommits =
                    conventionalCommits.Where(commit =>
                        commit.Scope == project.VersionFile.ScopeName ||
                        project.VersionFile.ParentScopes.Contains(commit.Scope));
                // TODO: multiple scopes in a commit
                project.Releases = tags.Where(t =>
                    project.VersionFile.ScopeName == t.Split('/')?[0]);
            }
        }
    }
}
