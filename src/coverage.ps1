# Run the tests, generating the coverage report.
dotnet test --collect:"XPlat Code Coverage" "$PSScriptRoot/DotNetWindowsRegistry.sln"

# Create a local HTML report, copy over the xml report.
$coverageArtifacts = "$PSScriptRoot/../artifacts/coverage" 
$coverageReportsGlob = "$PSScriptRoot/*.Tests/TestResults/**/coverage.cobertura.xml"
reportgenerator "-reports:$coverageReportsGlob" "-targetdir:$coverageArtifacts/html"
Copy-Item "$coverageReportsGlob" "$coverageArtifacts"

# Upload the report to codecov. The CODECOV_TOKEN env var must be set.
codecov -f "$coverageArtifacts/coverage.cobertura.xml"
