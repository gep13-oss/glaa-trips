#:sdk Cake.Sdk
#:property EnableDefaultEmbeddedResourceItems=false
#:property EnableDefaultContentItems=false

// glaa-trips build orchestration (Cake.Sdk).
//
// Targets:
//   Clean         - remove build outputs (bin/obj) and dotnet-clean the solution
//   Build         - build the solution
//   Test          - build, install the Playwright browser, run the Playwright suite
//   Format        - apply dotnet-format (whitespace + style + analyzers)
//   VerifyFormat  - fail if dotnet-format would change anything (for CI)
//
// Usage:
//   dotnet cake.cs --target=Build
//   dotnet cake.cs --target=Test
//   dotnet cake.cs --target=Format
//
// Default target is Test.

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var solution = "./GlaaTrips.sln";
var testProject = "./tests/GlaaTrips.Tests/GlaaTrips.Tests.csproj";
var testTfm = "net10.0";

Task("Clean")
    .Does(() =>
    {
        CleanDirectories("./**/bin");
        CleanDirectories("./**/obj");
        DotNetClean(solution, new DotNetCleanSettings { Configuration = configuration });
    });

Task("Build")
    .Does(() =>
    {
        DotNetBuild(solution, new DotNetBuildSettings { Configuration = configuration });
    });

Task("Install-Playwright")
    .IsDependentOn("Build")
    .Does(() =>
    {
        var script = $"./tests/GlaaTrips.Tests/bin/{configuration}/{testTfm}/playwright.ps1";
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

Task("Test")
    .IsDependentOn("Install-Playwright")
    .Does(() =>
    {
        DotNetTest(testProject, new DotNetTestSettings
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
    .Description("Default target - runs the Playwright test suite.");

RunTarget(target);
