var target = Argument("target", "Default");
var tag = Argument("tag", "cake");

Task("Restore")
  .Does(() =>
{
    DotNetCoreRestore(".");
});

Task("Build")
  .Does(() =>
{
    DotNetCoreBuild(".");
});

Task("Test")
  .Does(() =>
{
    var files = GetFiles("tests/**/*.csproj");
    foreach(var file in files)
    {
        DotNetCoreTest(file.ToString());
    }
});

Task("Publish")
  .Does(() =>
{
    var settings = new DotNetCorePublishSettings
    {
        Framework = "netcoreapp1.1",
        Configuration = "Release",
        OutputDirectory = "./publish",
        VersionSuffix = tag
    };
                
    DotNetCorePublish("src/RealWorld", settings);
});

Task("Push")
    .Does(() =>
{
    var packages = GetFiles(nupkgs + "/*.nupkg");
    foreach(var package in packages) 
    {
        if (package.ToString().Contains("symbols"))
        {
            Warning("Skipping Symbols package " + package);
            continue;
        }
        if (IsNuGetPublished(package, sources[1]))
        {
            throw new InvalidOperationException(package + " is already published.");
        }
        NuGetPush(package, new NuGetPushSettings{
            ApiKey = apiKey,
            Verbosity = NuGetVerbosity.Detailed,
            Source = publishTarget
        });     
    }         
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Publish");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);