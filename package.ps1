param (
    [string]$modName,
    [string]$modVersion
)

# Configuration
$packageDir = '.\Package'
$artifactDir = '.\bin\Package'
$clientArtifactDir = '.\ClientMod\bin\Package'

# Make sure our CWD is where the script lives
Set-Location $PSScriptRoot

Write-Host ('Packaging {0} v{1}' -f $modName, $modVersion)

# Create the package structure
$sptDir = '{0}\SPT' -f $packageDir 
$modsDir = '{0}\user\mods' -f $sptDir
$bepInExDir = '{0}\BepInEx' -f $packageDir
$pluginsDir = '{0}\plugins' -f $bepInExDir
Remove-Item -Path $sptDir -Recurse -Force
Remove-Item -Path $bepInExDir -Recurse -Force
$null = mkdir $modsDir -ea 0
$null = mkdir $pluginsDir -ea 0

# Copy server mod to the package folder
$artifactPath = ('{0}\{1}' -f $artifactDir, $modName)
Copy-Item -Path $artifactPath -Destination $modsDir -Recurse

# Copy client mod to the package folder
$clientArtifactDir = ('{0}\{1}-Client.dll' -f $clientArtifactDir, $modName)
Copy-Item $clientArtifactDir -Destination $pluginsDir

# Create the archive
$archivePath = '{0}\{1}-{2}.7z' -f $packageDir, $modName, $modVersion
if (Test-Path $archivePath)
{
    Remove-Item $archivePath
}
7z a $archivePath $sptDir $bepInExDir

Write-Host ('Mod packaging complete')