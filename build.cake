#tool "nuget:?package=GitVersion.CommandLine&version=4.0.0"
#tool "nuget:?package=vswhere&version=2.6.7"

var configuration = Argument("configuration", "Release");
var isPreRelease = Argument("isPreRelease", true);
var output = Argument("output", "artifacts");

var sln = "Postgres.sln";
var nupkgs = "Postgres/**/*.nupkg";

// Determine package version
var gv = GitVersion();
var branch = gv.BranchName;
if (branch.Contains("/")) branch = branch.Substring(branch.LastIndexOf('/') + 1);

var version = XmlPeek(GetFiles("**/*.csproj").First(), "//Version");
var versionPkg = !isPreRelease ? version : $"{version}-{branch}-{DateTime.Now.ToString("MMddHHmm")}";

Task("Clean").Does(() =>
{
	CleanDirectory(output);
});

Task("Build").Does(() =>
{
	MSBuild(sln, new MSBuildSettings
	{
		Configuration = configuration,
		MaxCpuCount = 0, // As many as available
		NodeReuse = false, // Required to prevent build task dll's from being locked
		Restore = true,
		ToolPath = GetFiles(VSWhereLatest() + "/**/MSBuild.exe").FirstOrDefault()
	}
		.WithProperty("AssemblyVersion", version)
		.WithProperty("FileVersion", versionPkg)
		.WithProperty("InformationalVersion", versionPkg)
		.WithProperty("PackageVersion", versionPkg)
	);
});

Task("Test").Does(() =>
{
	VSTest("./**/bin/**/*.UnitTest.dll", new VSTestSettings
	{
		ToolPath = GetFiles(VSWhereLatest() + "/**/vstest.console.exe").FirstOrDefault()
	});
});

Task("Artifact.NuGet").Does(() =>
{
	MoveFiles(nupkgs, output);
});

Task("Default")
	.IsDependentOn("Clean")
	.IsDependentOn("Build")
	.IsDependentOn("Test")
	.IsDependentOn("Artifact.NuGet")
	.Does(() => {})
;

RunTarget("Default");
