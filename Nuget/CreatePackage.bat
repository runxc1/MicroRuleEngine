mkdir content
mkdir content\MRE

copy ..\MicroRuleEngine\MRE.cs content\MRE
nuget pack MRE.nuspec -Exclude *.bat
nuget push MRE.1.0.2.nupkg -Source https://api.nuget.org/v3/index.json

pause