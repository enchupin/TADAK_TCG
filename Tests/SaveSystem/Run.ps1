$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot = Join-Path $projectRoot 'Temp/SaveSystemCheck'
New-Item -ItemType Directory -Force $testRoot | Out-Null
$jsonDll = Get-ChildItem (Join-Path $projectRoot 'Library/PackageCache/com.unity.nuget.newtonsoft-json*/Runtime/Newtonsoft.Json.dll') | Select-Object -First 1
if (!$jsonDll) { throw 'Unity에서 패키지 복원을 먼저 완료해야 합니다' }
$escapedRoot = [System.Security.SecurityElement]::Escape($projectRoot)
$escapedDll = [System.Security.SecurityElement]::Escape($jsonDll.FullName)
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>
  <ItemGroup>
    <Compile Include="$escapedRoot/Tests/SaveSystem/*.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Save/*.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Character/Character.cs" />
    <Reference Include="Newtonsoft.Json"><HintPath>$escapedDll</HintPath></Reference>
  </ItemGroup>
</Project>
"@
[IO.File]::WriteAllText((Join-Path $testRoot 'Check.csproj'), $project, [Text.UTF8Encoding]::new($false))
dotnet run --project (Join-Path $testRoot 'Check.csproj') -- $testRoot
if ($LASTEXITCODE -ne 0) { throw '저장 시스템 검증 실패' }
