$targetDir = ".\nuget"
$targetDirWin = $targetDir + "\Win"
$targetDirRpi = $targetDir + "\RPi"
$targetDirNETMF = $targetDir + "\NETMF"
$libDirWin = $targetDirWin + "\lib"
$libDirRPi = $targetDirRpi + "\lib"
$libDirNETMF = $targetDirNETMF + "\lib"

$repoDir = "..\..\repo"

# doesn't work - doesn't seem to do anything
#pushd $env:VS120COMNTOOLS
#Invoke-BatchFile vsvars32.bat
#popd

.\Invoke-Environment '"%VS120COMNTOOLS%\vsvars32.bat"' 

if (test-path $repoDir) { ri -r -fo $repoDir }
mkdir $repoDir | out-null
mkdir $repoDir"\Debug" | out-null
mkdir $repoDir"\Release" | out-null

$nuspecWin = $targetDir + "\FonaWin.nuspec"
[xml] $nuspecSourceWin = gc $nuspecWin
$nuspecRPi = $targetDir + "\FonaRPi.nuspec"
[xml] $nuspecSourceRPi = gc $nuspecRPi
$nuspecNETMF = $targetDir + "\FonaNETMF.nuspec"
[xml] $nuspecSourceNETMF = gc $nuspecNETMF

#
# Debug
#

if (test-path $libDirWin) { ri -r -fo $libDirWin }
mkdir $libDirWin | out-null
mkdir $libDirWin"\net40" | out-null
if (test-path $libDirRPi) { ri -r -fo $libDirRPi }
mkdir $libDirRPi | out-null
mkdir $libDirRPi"\net40" | out-null
if (test-path $libDirNETMF) { ri -r -fo $libDirNETMF }
mkdir $libDirNETMF | out-null
mkdir $libDirNETMF"\netmf" | out-null
mkdir $libDirNETMF"\netmf\be" | out-null
mkdir $libDirNETMF"\netmf\le" | out-null

msbuild /m /target:rebuild /p:Configuration=Debug Fona.sln
#$versionFile = Get-Item .\PemlSVG.Portable.78\bin\Debug\PemlSVG.Portable.78.dll
#$version = [Reflection.Assembly]::LoadFile($versionFile.FullName).GetName().Version
#TODO: edit the xml like http://social.technet.microsoft.com/Forums/windowsserver/en-US/3775b488-d1d6-4c61-a2bd-411b7966ff09/edit-xml-with-powershell?forum=winserverpowershell
#TODO: write to temp location and set $nuspec to the temp location
.\.nuget\nuget.exe pack $nuspecWin -basepath $targetDirWin -o $repoDir"\Debug"
.\.nuget\nuget.exe pack $nuspecRPi -basepath $targetDirRPi -o $repoDir"\Debug"
.\.nuget\nuget.exe pack $nuspecNETMF -basepath $targetDirRPi -o $repoDir"\Debug"

#
# Release
#

if (test-path $libDirWin) { ri -r -fo $libDirWin }
mkdir $libDirWin | out-null
mkdir $libDirWin"\net40" | out-null
if (test-path $libDirRPi) { ri -r -fo $libDirRPi }
mkdir $libDirRPi | out-null
mkdir $libDirRPi"\net40" | out-null
if (test-path $libDirNETMF) { ri -r -fo $libDirNETMF }
mkdir $libDirNETMF | out-null
mkdir $libDirNETMF"\netmf" | out-null
mkdir $libDirNETMF"\netmf\be" | out-null
mkdir $libDirNETMF"\netmf\le" | out-null

msbuild /m /target:rebuild /p:Configuration=Release Fona.sln
#$versionFile = Get-Item .\PemlSVG.Portable.78\bin\Release\PemlSVG.Portable.78.dll
#$version = [Reflection.Assembly]::LoadFile($versionFile.FullName).GetName().Version
#TODO: edit the xml like http://social.technet.microsoft.com/Forums/windowsserver/en-US/3775b488-d1d6-4c61-a2bd-411b7966ff09/edit-xml-with-powershell?forum=winserverpowershell
#TODO: write to temp location and set $nuspec to the temp location
.\.nuget\nuget.exe pack $nuspecWin -basepath $targetDirWin -o $repoDir"\Release"
.\.nuget\nuget.exe pack $nuspecRPi -basepath $targetDirRPi -o $repoDir"\Release"
.\.nuget\nuget.exe pack $nuspecNETMF -basepath $targetDirNETMF -o $repoDir"\Release"

