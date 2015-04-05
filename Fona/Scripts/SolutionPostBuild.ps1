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
	Write-Verbose "nuspec file $nuspec"

	$nugetBuildDir = $SolutionDir + "nuget\" + $ConfigurationName + "\" + $ProjectName + "\"
	$libDir = $nugetBuildDir + "lib\"

	Write-Verbose "Nuget build dir is $nugetBuildDir"
	$nuget = $SolutionDir + ".nuget\nuget.exe"

	if (test-path $nugetBuildDir) { ri -r -fo $nugetBuildDir }
	mkdir $libDir | out-null
	if ($IsNetMF)
	{
		mkdir $libDir"\netmf\be" | out-null
		Copy-Item -Path $TargetDir"be\*" -Destination $libDir"\netmf\be" -Include "*.dll","*.pdb","*.xml","*.pdbx","*.pe"
		mkdir $libDir"\netmf\le" | out-null
		Copy-Item -Path $TargetDir"le\*" -Destination $libDir"\netmf\le" -Include "*.dll","*.pdb","*.xml","*.pdbx","*.pe"
		Copy-Item -Path $TargetDir"*" -Destination $libDir"\netmf" -Include "*.dll","*.pdb","*.xml"
	}
	else
	{
		mkdir $libDir"\net40" | out-null
		Copy-Item -Path $TargetDir"*" -Destination $libDir"\net40" -Include "*.dll","*.pdb","*.xml"
	}

	$output = $repoDir + $ConfigurationName
	if (-not (test-path $output)) { mkdir $output | out-null }

	$args = 'pack', $nuspec, '-basepath', $nugetBuildDir, '-OutputDirectory', $output
	& $nuget $args
}