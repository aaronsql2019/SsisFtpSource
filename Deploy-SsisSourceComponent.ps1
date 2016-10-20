# 2016.10.18 - Gjermund Skobba
# RUN AS ADMINISTRATOR!!!

# PATH - CHANGE BEFORE RUN!!!
$vsPath = "C:\Program Files (x86)\Microsoft Visual Studio 12.0\VC\"
$componentsPath = "C:\Program Files (x86)\Microsoft SQL Server\120\DTS\PipelineComponents\"

# Add TO gac with gacutil
[Reflection.Assembly]::LoadWithPartialName("System.EnterpriseServices")
[System.EnterpriseServices.Internal.Publish] $publish = new-object System.EnterpriseServices.Internal.Publish;


# FTP SOURCE
$ftpSourceFile = "SsisFtpSource.dll"
$ftpSourceFilePath = "C:\Github\SsisFtpSource\bin\Debug\" + $ftpSourceFile
cp $ftpSourceFilePath $componentsPath -Force
$publish.UnRegisterAssembly($componentsPath + $ftpSourceFile); 
$publish.GacInstall($componentsPath + $ftpSourceFile); 
cp $ftpSourceFilePath $vsPath
cp $ftpSourceFilePath $remoteServerPath


#Open project in VS - After copying files open your test-project
$devenv = "C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\devenv.exe"
Invoke-Expression  "C:\Github\SsisFtpSource\SsisFtpSource_Test\SsisFtpSource_Test.sln"