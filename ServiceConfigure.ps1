param(
  [Parameter(Mandatory = $true)]
  [string] $configFile,

  [string] $teamCityDataDir = $null,

  [Parameter(Mandatory = $true)]
  [string] $gitConfigName,

  [Parameter(Mandatory = $true)]
  [string] $gitConfigEmail,

  [string] $gitRemoteRepository = $null
)
$xml = (Get-Content $configFile) -as [xml]
$xml.SelectSingleNode('/configuration/appSettings/add[@key="GitConfigName"]/@value').set_InnerXML($gitConfigName)
$xml.SelectSingleNode('/configuration/appSettings/add[@key="GitConfigEmail"]/@value').set_InnerXML($gitConfigEmail)
if($teamCityDataDir) {
    $xml.SelectSingleNode('/configuration/appSettings/add[@key="TeamCityDataDir"]/@value').set_InnerXML($teamCityDataDir)
}
if($gitRemoteRepository) {
    $xml.SelectSingleNode('/configuration/appSettings/add[@key="GitRemoteRepository"]/@value').set_InnerXML($gitRemoteRepository)
}
$xml.Save($configFile)