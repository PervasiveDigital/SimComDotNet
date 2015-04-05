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
	$srcDir = $nugetBuildDir + "src\"

	Write-Verbose "Nuget build dir is $nugetBuildDir"
	$nuget = $SolutionDir + ".nuget\nuget.exe"

	if (test-path $nugetBuildDir) { ri -r -fo $nugetBuildDir }
	mkdir $libDir | out-null
	mkdir $srcDir | out-null

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

	# Copy source files
#	$source = $SolutionDir + "Fona.Shared"
#	Get-ChildItem $source -Recurse -Filter "*.cs" | Copy-Item -Destination {Join-Path $srcDir $_.FullName.Substring($source.length)}
#	$source = $ProjectDir
#	Get-ChildItem $source -Recurse -Filter "*.cs" -Exclude "obj","bin" | Copy-Item -Destination {Join-Path $srcDir $_.FullName.Substring($source.length)}

	Copy-Item -Recurse -Path $SolutionDir"Fona.Shared" -Destination $srcDir -Filter "*.cs"
	$target = $srcDir + "Fona.Shared"
	if (test-path $target"\obj") { Remove-Item -Recurse $target"\obj" | out-null }
	if (test-path $target"\bin") { Remove-Item -Recurse $target"\bin" | out-null }
	
	Copy-Item -Recurse -Path $ProjectDir -Destination $srcDir -Filter "*.cs"
	$target = $srcDir + $ProjectName
	if (test-path $target"\obj") { Remove-Item -Recurse $target"\obj" | out-null }
	if (test-path $target"\bin") { Remove-Item -Recurse $target"\bin" | out-null }

	$output = $repoDir + $ConfigurationName
	if (-not (test-path $output)) { mkdir $output | out-null }

	$args = 'pack', $nuspec, '-Symbols', '-basepath', $nugetBuildDir, '-OutputDirectory', $output
	& $nuget $args
}
