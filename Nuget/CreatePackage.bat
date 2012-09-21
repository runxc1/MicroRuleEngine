mkdir content
mkdir content\MRE

copy ..\MicroRuleEngine\MRE.cs content\MRE
NuGet pack MRE.nuspec -Exclude *.bat
Nuget push MRE.1.0.1.nupkg

pause