using System.Collections.Generic;
using System.Linq;

namespace BuildScript
{
    public class ConventionalCommit
    {
        public string Sha { get; set; }

        public string Scope { get; set; }

        public string Type { get; set; }

        public string Subject { get; set; }

        public List<ConventionalCommitNote> Notes { get; set; } = new List<ConventionalCommitNote>();

        public bool IsFeature => Type == "feat";
        public bool IsFix => Type == "fix";
#pragma warning disable CA1309 // Use ordinal string comparison
        public bool IsBreakingChange => Notes.Any(note => "BREAKING CHANGE".Equals(note.Title));
#pragma warning restore CA1309 // Use ordinal string comparison
    }

    public class ConventionalCommitNote
    {
        public string Title { get; set; }

        public string Text { get; set; }
    }
}
