using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using Version = System.Version;

namespace BuildScript
{

    public static class RespositoryExtensions
    {
        public static Tag SelectLatestVersionTag(this Repository repository)
        {
            try
            {
                var latestTag = repository.Describe(repository.Head.Tip, new DescribeOptions
                {
                    AlwaysRenderLongFormat = false,
                    MinimumCommitIdAbbreviatedSize = 0,
                    Strategy = DescribeStrategy.Tags
                });
                // TODO: Check describe options ie git describe --tags
                // https://gist.github.com/rponte/fdc0724dd984088606b0
                return repository.Tags.SingleOrDefault(t => t.FriendlyName == latestTag);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Tag SelectVersionTag(this Repository repository, Version version)
        {
            return repository.Tags.SingleOrDefault(t => t.FriendlyName == $"v{version}");
        }

        public static List<Commit> GetCommitsSinceLastVersion(this Repository repository, Tag versionTag)
        {
            if (versionTag == null)
            {
                return repository.Commits.ToList();
            }

            var filter = new CommitFilter()
            {
                ExcludeReachableFrom = versionTag
            };

            return repository.Commits.QueryBy(filter).ToList();
        }
    }
}
