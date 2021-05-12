using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BuildScript
{
    public class Changelog
    {
        private const string Preamble = @"# Changelog";

        private Changelog(string file)
        {
            FilePath = file;
        }

        public string FilePath { get; }

        public void Write(string scopeName, Version version, DateTimeOffset versionTime,
            IEnumerable<ConventionalCommit> commits,
            bool includeAllCommitsInChangelog = false)
        {
            var versionTagLink = $"{scopeName}: {version}";

            var markdown = $"{Environment.NewLine}## {versionTagLink} ({versionTime.Year}-{versionTime.Month}-{versionTime.Day})";
            markdown += Environment.NewLine;

            var bugFixes = BuildBlock("Bug Fixes", commits.Where(commit => commit.IsFix));

            if (!string.IsNullOrWhiteSpace(bugFixes))
            {
                markdown += bugFixes;
                markdown += Environment.NewLine;
            }

            var features = BuildBlock("Features", commits.Where(commit => commit.IsFeature));

            if (!string.IsNullOrWhiteSpace(features))
            {
                markdown += features;
                markdown += Environment.NewLine;
            }

            var breaking = BuildBlock("Breaking Changes", commits.Where(commit => commit.IsBreakingChange));

            if (!string.IsNullOrWhiteSpace(breaking))
            {
                markdown += breaking;
                markdown += Environment.NewLine;
            }

            if (includeAllCommitsInChangelog)
            {
                var other = BuildBlock("Other", commits.Where(commit => !commit.IsFix && !commit.IsFeature && !commit.IsBreakingChange));

                if (!string.IsNullOrWhiteSpace(other))
                {
                    markdown += other;
                    markdown += Environment.NewLine;
                }
            }

            if (File.Exists(FilePath))
            {
                var contents = File.ReadAllText(FilePath);

                contents = contents.Replace(Preamble + Environment.NewLine, "");
                markdown = Preamble + Environment.NewLine + contents.Insert(0, markdown);
                File.WriteAllText(FilePath, markdown);
            }
            else
            {
                File.WriteAllText(FilePath, Preamble + Environment.NewLine + markdown);
            }
        }

        public static string BuildBlock(string header, IEnumerable<ConventionalCommit> commits)
        {
            if (!commits.Any())
            {
                return null;
            }

            var block = $"#### {header} {Environment.NewLine}";

            return commits
                .OrderBy(c => c.Scope)
                .ThenBy(c => c.Subject)
                .Aggregate(block, (current, commit) => current + BuildCommit(commit) + Environment.NewLine);
        }

        public static string BuildCommit(ConventionalCommit commit)
        {
            var sb = new StringBuilder("* ");

            if (!string.IsNullOrWhiteSpace(commit.Scope))
            {
                sb.Append($"**{commit.Scope}:** ");
            }
#pragma warning disable CA1304 // Specify CultureInfo
            var formattedSubject = char.ToUpper(commit.Subject[0]) +
                ((commit.Subject.Length > 1) ? commit.Subject.Substring(1) : string.Empty);
#pragma warning restore CA1304 // Specify CultureInfo

            sb.Append(formattedSubject);

            //var commitLink = linkBuilder.BuildCommitLink(commit);

            //if (!string.IsNullOrWhiteSpace(commitLink))
            //{
            //    sb.Append($" ([{commit.Sha.Substring(0, 7)}]({commitLink}))");
            //}

            return sb.ToString();
        }

        public static Changelog Discover(string directory)
        {
            var changelogFile = Path.Combine(directory, "CHANGELOG.md");

            return new Changelog(changelogFile);
        }
    }
}
