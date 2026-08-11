#:sdk Cake.Sdk
#:property EnableDefaultEmbeddedResourceItems=false
#:property EnableDefaultContentItems=false

// glaa-trips build orchestration (Cake.Sdk).
//
// Targets:
//   Clean         - remove build outputs (bin/obj) and dotnet-clean the solution
//   VerifyFormat  - fail if dotnet-format would change anything (formatting gate)
//   Build         - verify formatting, then build the solution
//   UnitTest      - build, then run the fast unit tests (no server, no browser)
//   Test          - unit tests + install the Playwright browser + run the UI suite
//   Format        - apply dotnet-format (whitespace + style + analyzers)
//
// Usage:
//   dotnet cake.cs --target=Build
//   dotnet cake.cs --target=UnitTest
//   dotnet cake.cs --target=Test
//   dotnet cake.cs --target=Format
//
// Default target is Test.

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var solution = "./AalgTrips.slnx";
var unitTestProject = "./tests/AalgTrips.UnitTests/AalgTrips.UnitTests.csproj";
var uiTestProject = "./tests/AalgTrips.UITests/AalgTrips.UITests.csproj";
var testTfm = "net10.0";

Task("Clean")
    .Does(() =>
    {
        CleanDirectories("./**/bin");
        CleanDirectories("./**/obj");
        DotNetClean(solution, new DotNetCleanSettings { Configuration = configuration });
    });

Task("Build")
    .IsDependentOn("VerifyFormat")
    .Does(() =>
    {
        DotNetBuild(solution, new DotNetBuildSettings { Configuration = configuration });
    });

Task("Install-Playwright")
    .IsDependentOn("Build")
    .Does(() =>
    {
        var script = $"./tests/AalgTrips.UITests/bin/{configuration}/{testTfm}/playwright.ps1";
        if (!FileExists(script))
        {
            throw new Exception(
                $"Playwright bootstrap script not found at '{script}'. "
                + "Ensure the test project built successfully first.");
        }

        var exit = StartProcess("pwsh", new ProcessSettings
        {
            Arguments = $"-NoProfile -File \"{script}\" install chromium"
        });

        if (exit != 0)
        {
            throw new Exception($"Playwright browser install failed (exit code {exit}).");
        }
    });

Task("UnitTest")
    .IsDependentOn("Build")
    .Does(() =>
    {
        DotNetTest(unitTestProject, new DotNetTestSettings
        {
            Configuration = configuration,
            NoBuild = true,
        });
    });

Task("Test")
    .IsDependentOn("UnitTest")
    .IsDependentOn("Install-Playwright")
    .Does(() =>
    {
        DotNetTest(uiTestProject, new DotNetTestSettings
        {
            Configuration = configuration,
            NoBuild = true,
        });
    });

Task("Format")
    .Does(() =>
    {
        DotNetFormat(solution);
    });

Task("VerifyFormat")
    .Does(() =>
    {
        DotNetFormat(solution, new DotNetFormatSettings { VerifyNoChanges = true });
    });

Task("Default")
    .IsDependentOn("Test")
    .Description("Default target - runs the unit tests and the Playwright UI suite.");

RunTarget(target);
