
using System;
using System.IO;
using System.Linq;
using FlubuCore.Context;
using LibGit2Sharp;

namespace BuildScript
{

    public class WorkingCopy
    {
        private readonly DirectoryInfo _directory;
        private readonly ITaskContext context;

        private WorkingCopy(DirectoryInfo directory, ITaskContext context)
        {
            _directory = directory;
            this.context = context;
        }

        public Projects Versionize()
        {
            var workingDirectory = _directory.FullName;

            using (var repo = new Repository(workingDirectory))
            {
                var projects = Projects.Discover(workingDirectory);

                if (projects.IsEmpty())
                {
                    context.LogError(
                        $"Could not find any projects files in {workingDirectory} that have a <Version> defined in their csproj file.");
                    return projects;
                }

                var versionTag = repo.SelectLatestVersionTag();
                var commitsInVersion = repo.GetCommitsSinceLastVersion(versionTag);

                var commitParser = new ConventionalCommitParser();
                var conventionalCommits = commitParser.Parse(commitsInVersion);
                projects.GroupCommitsAndReleases(
                    conventionalCommits,
                    repo.Tags.Select(x => x.FriendlyName));

                var versionTime = DateTimeOffset.Now;

                projects.WriteVersion(versionTag != null, ignoreInsignificant: true);

                if (!projects.UpdatedProjects.Any())
                {
                    context.LogInfo(
                        $"Version was not affected by commits since last release ({versionTag?.FriendlyName}), since you specified to ignore insignificant changes, no action will be performed.");
                    return projects;
                }

                foreach (var projectFile in projects.GetVersionFilesPath())
                {
                    Commands.Stage(repo, projectFile);
                }

                var changelog = Changelog.Discover(workingDirectory);

                foreach (var project in projects.UpdatedProjects)
                {
                    context.LogInfo(
                        $"bumping version from {project.VersionFile.Version} to {project.NewVersion} in projects");

                    changelog.Write(
                        project.VersionFile.ScopeName,
                        project.NewVersion,
                        versionTime,
                        project.ConventionalCommits);
                }

                Commands.Stage(repo, changelog.FilePath);

                var author = repo.Config.BuildSignature(versionTime);
                var committer = author;
                var commitMessages = projects.UpdatedProjects.Select(x =>
                    $"{x.VersionFile.ScopeName}/ {x.NewVersion}");
                var releaseCommitMessage = $"chore(release): {string.Join(Environment.NewLine, commitMessages)}";
                var versionCommit = repo.Commit(releaseCommitMessage, author, committer);
                foreach (var project in projects.UpdatedProjects)
                {
                    // TODO: Check if tag exists before commit
                    repo.Tags.Add($"{project.VersionFile.ScopeName}/v{project.NewVersion}", versionCommit, author, $"{project.NewVersion}");
                    context.LogInfo($"tagged release as {project.NewVersion}");
                }

                return projects;
            }
        }

        public static WorkingCopy Discover(string workingDirectory, ITaskContext context)
        {
            var workingCopyCandidate = new DirectoryInfo(workingDirectory);

            if (!workingCopyCandidate.Exists)
            {
                context.LogError($"Directory {workingDirectory} does not exist");
            }

            do
            {
                var isWorkingCopy = workingCopyCandidate.GetDirectories(".git").Any();

                if (isWorkingCopy)
                {
                    return new WorkingCopy(workingCopyCandidate, context);
                }

                workingCopyCandidate = workingCopyCandidate.Parent;
            }
            while (workingCopyCandidate.Parent != null);

            context.LogError($"Directory {workingDirectory} or any parent directory do not contain a git working copy");

            return null;
        }
    }
}
