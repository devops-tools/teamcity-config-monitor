param(
  [Parameter(Mandatory = $true)]
  [string] $configFile,
  
  [string] $teamCityDataDir = $null,
  [string] $teamCityAuditLog = $null,
  [string] $teamCityUrl = $null,
  [string] $teamCityUsername = $null,
  [string] $teamCityPassword = $null,

  [Parameter(Mandatory = $true)]
  [string] $gitConfigName,

  [Parameter(Mandatory = $true)]
  [string] $gitConfigEmail,

  [string] $gitRemoteRepository = $null
)
$xml = (Get-Content $configFile) -as [xml]
$xml.SelectSingleNode('/configuration/appSettings/add[@key="GitConfigName"]/@value').set_InnerXML($gitConfigName)
$xml.SelectSingleNode('/configuration/appSettings/add[@key="GitConfigEmail"]/@value').set_InnerXML($gitConfigEmail)

$xml.SelectSingleNode('/configuration/appSettings/add[@key="TeamCityAuditLog"]/@value').set_InnerXML($teamCityAuditLog)
$xml.SelectSingleNode('/configuration/appSettings/add[@key="TeamCityUrl"]/@value').set_InnerXML($teamCityUrl)
$xml.SelectSingleNode('/configuration/appSettings/add[@key="TeamCityUsername"]/@value').set_InnerXML($teamCityUsername)
$xml.SelectSingleNode('/configuration/appSettings/add[@key="TeamCityPassword"]/@value').set_InnerXML($teamCityPassword)
if($teamCityDataDir) {
    $xml.SelectSingleNode('/configuration/appSettings/add[@key="TeamCityDataDir"]/@value').set_InnerXML($teamCityDataDir)
}
if($gitRemoteRepository) {
    $xml.SelectSingleNode('/configuration/appSettings/add[@key="GitRemoteRepository"]/@value').set_InnerXML($gitRemoteRepository)
}
$xml.Save($configFile)