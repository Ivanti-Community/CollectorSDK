<#
	Builds the SDK
#>

param
(
[String] $config = "Debug"
)

$lastDir = Get-Location

$workingDir = $PSSCRIPTROOT

Import-Module -Name "$workingDir\SdkFunctions.psm1"
Write-Host "Resolving MsBuild..."
$msbuild = Resolve-MsBuild

$nugetExe = "$workingDir\..\nuget.exe"
$workingDir = "$workingDir\..\Collector.SDK\Src"

Set-Location -Path "$workingDir"

[Environment]::CurrentDirectory = $workingDir

Start-Process "$nugetExe" "restore" -Wait -NoNewWindow

$solutionFile = "$workingDir\Collector.SDK.sln"
Write-Host "Solution file: " $solutionFile

Write-Host "Building Collector SDK"
Write-Host $msbuild $solutionFile /t:Rebuild /fl /clp:"v=m;Summary" /p:Configuration=$config /p:Platform="Any CPU"
& $msbuild $solutionFile /t:Rebuild /fl /clp:"v=m;Summary" /p:Configuration=$config /p:Platform="Any CPU"

Set-Location $lastDir

Write-Host "Done..."