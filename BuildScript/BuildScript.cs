
using System.Collections.Generic;
using FlubuCore.Context;
using FlubuCore.Context.Attributes.BuildProperties;
using FlubuCore.Context.FluentInterface.Interfaces;
using FlubuCore.IO;
using FlubuCore.Scripting;
using FlubuCore.Scripting.Attributes;

namespace BuildScript
{

    [IncludeFromDirectory(@"./BuildScript/Versionize", true)]
    //[IncludeFromDirectory(@"./BuildScript/Test", true)]
    public class BuildScript : DefaultBuildScript
    {
        [FromArg("c|configuration")]
        [BuildConfiguration]
        public string Configuration { get; set; } = "Release";

        [FromArg("vs|versionsuffix")]
        public string VersionSuffix { get; set; } = string.Empty;

        [SolutionFileName]
        public string SolutionFileName { get; set; } = "ConsoleApp.sln";

        protected string ArtifactsDir => RootDirectory.CombineWith("artifacts");

        protected List<FileFullPath> ProjectFiles { get; set; }

        protected Projects Projects { get; set; }

        protected override void BeforeBuildExecution(ITaskContext context)
        {
            ProjectFiles = context.GetFiles(RootDirectory, "**/*.csproj");
            //Projects = WorkingCopy
            //    .Discover(RootDirectory.ToFileFullPath(), context)
            //    .Versionize();
        }

        protected override void ConfigureTargets(ITaskContext context)
        {
            var compile = context.CreateTarget("compile")
                .SetDescription("Compiles the solution.")
                .AddCoreTask(x => x.Build());

            var clean = context.CreateTarget("Clean")
                .SetDescription("Cleans the output of all projects in the solution.")
                .AddCoreTask(x => x.Clean()
                    .AddDirectoryToClean(ArtifactsDir, true));

            var restore = context.CreateTarget("Restore")
                .SetDescription("Restores the dependencies and tools of all projects in the solution.")
                .DependsOn(clean)
                .AddCoreTask(x => x.Restore());

            var targets = new List<ITarget>
            {
                compile,
                clean,
                restore
            };

            foreach (var project in ProjectFiles)
            {
                var build = context.CreateTarget($"Build{project.FileName}")
                    .SetDescription($"Builds {project.FileName} in the solution.")
                    .DependsOn(restore)
                    .AddCoreTask(x => x.Build()
                        .Project(project.ToFullPath())
                        .InformationalVersion("1.0.0"));

                var pack = context.CreateTarget($"Pack{project.FileName}")
                    .SetDescription($"Creates nuget package for {project.FileName}")
                    .DependsOn(build)
                    .AddCoreTask(x => x.Pack()
                        .NoBuild()
                        .IncludeSymbols()
                        .VersionSuffix(VersionSuffix)
                        .OutputDirectory(ArtifactsDir));

                targets.Add(build);
                targets.Add(pack);
            }

            context.CreateTarget("Default")
              .SetDescription("Runs all targets.")
              .SetAsDefault()
              .DependsOn(targets.ToArray());
        }
    }
}
