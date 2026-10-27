param([switch]$Write)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot = Join-Path $projectRoot 'Temp/MonsterSpriteCheck'
New-Item -ItemType Directory -Force $testRoot | Out-Null
$escapedRoot = [Security.SecurityElement]::Escape($projectRoot)
# Unity 의존이 없는 몬스터 이미지 경로 · 여백 측정 소스만 컴파일
# 배치 계산 · 배치 설정 서식 소스도 함께 컴파일
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><Nullable>disable</Nullable></PropertyGroup>
  <ItemGroup>
    <Compile Include="$escapedRoot/Tests/MonsterSprite/*.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Monster/MonsterImagePaths.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Monster/MonsterSpriteBounds.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Monster/MonsterDisplayLayout.cs" />
  </ItemGroup>
</Project>
"@
$projectPath = Join-Path $testRoot 'Check.csproj'
[IO.File]::WriteAllText($projectPath, $project, [Text.UTF8Encoding]::new($false))
$configPath = Join-Path $testRoot 'NuGet.Config'
[IO.File]::WriteAllText($configPath, '<configuration><packageSources><clear /></packageSources></configuration>', [Text.UTF8Encoding]::new($false))
dotnet restore $projectPath --configfile $configPath
if ($LASTEXITCODE -ne 0) { throw '몬스터 이미지 검증 복원 실패' }
# -Write: 측정 데이터 다시 생성
$extra = @()
if ($Write) { $extra = @('--write') }
dotnet run --project $projectPath --no-restore -- $projectRoot @extra
if ($LASTEXITCODE -ne 0) { throw '몬스터 이미지 검증 실패' }
