$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot = Join-Path $projectRoot 'Temp/BossModeCheck'
New-Item -ItemType Directory -Force $testRoot | Out-Null
$escapedRoot = [Security.SecurityElement]::Escape($projectRoot)
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>
  <ItemGroup>
    <Compile Include="$escapedRoot/Tests/BossMode/*.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Battle/BossMode/BossModeSession.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Lobby/BossMode/BossModeDeckSelection.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Save/CharacterDeckSave.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Character/Character.cs" />
  </ItemGroup>
</Project>
"@
$projectPath = Join-Path $testRoot 'Check.csproj'
[IO.File]::WriteAllText($projectPath, $project, [Text.UTF8Encoding]::new($false))
$configPath = Join-Path $testRoot 'NuGet.Config'
[IO.File]::WriteAllText($configPath, '<configuration><packageSources><clear /></packageSources></configuration>', [Text.UTF8Encoding]::new($false))
dotnet restore $projectPath --configfile $configPath
if ($LASTEXITCODE -ne 0) { throw '보스모드 검증 프로젝트 복원 실패' }
dotnet run --project $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw '보스모드 동작 검증 실패' }
