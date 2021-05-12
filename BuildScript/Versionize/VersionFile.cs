
using System;
using System.Text.Json.Serialization;

namespace BuildScript
{

    public class VersionFile
    {
        [JsonPropertyName("version")]
        public string VersionString { get; private set; }

        [JsonPropertyName("scopeName")]
        public string ScopeName { get; private set; }

        [JsonPropertyName("parentScopes")]
        public string[] ParentScopes { get; private set; } = { };

        [JsonIgnore]
        public Version Version { get; private set; }

        public VersionFile(
            string versionString,
            string scopeName,
            string[] parentScopes)
        {
            this.VersionString = versionString;
            this.ScopeName = scopeName;
            this.ParentScopes = parentScopes;
        }

        internal void SetVersion() => Version = new Version(VersionString);
    }
}
