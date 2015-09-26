param($installPath, $toolsPath, $package, $project)

$file1 = $project.ProjectItems.Item("FeldSpar.Console.exe.config")

# set 'Copy To Output Directory' to 'Copy if newer'
$copyToOutput = $file1.Properties.Item("CopyToOutputDirectory")
$copyToOutput.Value = 1
$buildAction = $file1.Properties.Item("BuildAction")
$buildAction.Value = 0