ApiKey=$1
Source=$2

nuget pack ./InfiniMap/InfiniMap.nuspec -Verbosity detailed

nuget push ./InfiniMap.*.nupkg -Verbosity detailed -ApiKey $ApiKey -Source $Source
