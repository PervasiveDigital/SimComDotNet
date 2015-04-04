[CmdletBinding()]

param($RepoDir, $SolutionDir, $ProjectDir, $ProjectName, $TargetDir, $TargetFileName, $ConfigurationName, $nuspec, [switch]$IsNetMF, [switch]$Disable)

if(-not ($RepoDir -and $SolutionDir -and $ProjectDir -and $ProjectName -and $TargetDir -and $TargetFileName -and $ConfigurationName))
{
	Write-Error "RepoDir, SolutionDir, ProjectDir, TargetDir, TargetFileName and ConfigurationName are all required"
	exit 1
}

if ($PSBoundParameters.ContainsKey('Disable'))
{
	Write-Verbose "Script disabled; no actions will be taken on the files."
}

if ($nuspec)
{
	$nugetBuildDir = $SolutionDir + "nuget\" + $ConfigurationName + "\" + $ProjectName + "\"
	Write-Verbose "Nuget build dir is $nugetBuildDir"
	$nuget = $SolutionDir + "\.nuget\nuget.exe"
}