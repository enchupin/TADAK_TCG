param([switch]$Write)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot = Join-Path $projectRoot 'Temp/SpawnLayoutCheck'
New-Item -ItemType Directory -Force $testRoot | Out-Null
$escapedRoot = [Security.SecurityElement]::Escape($projectRoot)
# Unity 의존이 없는 스폰 좌표 서식 · 해석 소스와 조우 데이터 정의만 컴파일
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><Nullable>disable</Nullable></PropertyGroup>
  <ItemGroup>
    <Compile Include="$escapedRoot/Tests/SpawnLayout/*.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Battle/SpawnLayout.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Monster/Data/MonsterDefinition.cs" />
  </ItemGroup>
</Project>
"@
$projectPath = Join-Path $testRoot 'Check.csproj'
[IO.File]::WriteAllText($projectPath, $project, [Text.UTF8Encoding]::new($false))
$configPath = Join-Path $testRoot 'NuGet.Config'
[IO.File]::WriteAllText($configPath, '<configuration><packageSources><clear /></packageSources></configuration>', [Text.UTF8Encoding]::new($false))
dotnet restore $projectPath --configfile $configPath
if ($LASTEXITCODE -ne 0) { throw '스폰 좌표 검증 복원 실패' }
# -Write: 빠진 조우 항목을 씬 기본 좌표로 채워 저장
$extra = @()
if ($Write) { $extra = @('--write') }
dotnet run --project $projectPath --no-restore -- $projectRoot @extra
if ($LASTEXITCODE -ne 0) { throw '스폰 좌표 검증 실패' }
