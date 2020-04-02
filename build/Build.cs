using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion] readonly GitVersion GitVersion;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath PublishDirectory => OutputDirectory / "publish";
    AbsolutePath ArtifactsDirectory => OutputDirectory / "artifacts";

    string[] ProjectNames => new string[] {"Ldv.Scrappy.ConsoleApp"};

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj", "output").ForEach(DeleteDirectory);
            EnsureCleanDirectory(OutputDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(_ => _
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(_ => _
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.MajorMinorPatch)
                .EnableNoRestore());
        });

    Target Publish => _ => _
        .DependsOn(Clean, Restore)
        .Executes(() =>
        {
            foreach (var ProjectName in ProjectNames)
            {
                var project = Solution.GetProject(ProjectName);
                DotNetPublish(_ => _
                    .SetOutput(PublishDirectory / ProjectName)
                    .SetProject(project)
                    .SetConfiguration(Configuration)
                    .SetAssemblyVersion(GitVersion.AssemblySemVer)
                    .SetFileVersion(GitVersion.AssemblySemFileVer)
                    .SetInformationalVersion(GitVersion.MajorMinorPatch)
                    .EnableNoRestore());
                var configurationFiles = Directory.GetFiles(project.Directory, "appsettings*");
                var dockerfiles = Directory.GetFiles(project.Directory, "Dockerfile*");
                foreach (var c in configurationFiles.Concat(dockerfiles))
                {
                    File.Copy(c, Path.Combine(PublishDirectory / ProjectName, new FileInfo(c).Name));
                }
            }
        });

    Target Package => _ => _
        .DependsOn(Publish)
        .Produces(ProjectNames
            .Select((ProjectName) => (ArtifactsDirectory / ProjectName / $"{ProjectName}*.zip").ToString()).ToArray())
        .Executes(() =>
        {
            foreach (var ProjectName in ProjectNames)
            {
                Directory.CreateDirectory(ArtifactsDirectory / ProjectName);
                Utils.CreateTarGz(
                    PublishDirectory / ProjectName, 
                    ArtifactsDirectory / ProjectName / $"{ProjectName}-{GitVersion.MajorMinorPatch}.tar.gz", 
                    $"{ProjectName}-{GitVersion.MajorMinorPatch}");
            }
        });
}