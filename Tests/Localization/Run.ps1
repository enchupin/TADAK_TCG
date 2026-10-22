$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot = Join-Path $projectRoot 'Temp/LocalizationCheck'
New-Item -ItemType Directory -Force $testRoot | Out-Null
$escapedRoot = [Security.SecurityElement]::Escape($projectRoot)
# Unity 의존이 없는 로컬라이징 코어 · 동기화 도구 소스만 컴파일 (Newtonsoft는 패키지 캐시의 DLL 사용)
$newtonsoft = Get-ChildItem (Join-Path $projectRoot 'Library/PackageCache') -Recurse -Filter Newtonsoft.Json.dll | Where-Object { $_.FullName -notmatch 'AOT' } | Select-Object -First 1
if (-not $newtonsoft) { throw 'Newtonsoft.Json.dll을 찾을 수 없습니다 (Unity에서 프로젝트를 한 번 열어야 함)' }
$escapedNewtonsoft = [Security.SecurityElement]::Escape($newtonsoft.FullName)
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><Nullable>disable</Nullable></PropertyGroup>
  <ItemGroup>
    <Compile Include="$escapedRoot/Tests/Localization/*.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Localization/Core/*.cs" />
    <Compile Include="$escapedRoot/Assets/Editor/Localization/LocalizationSourceSync.cs" />
    <Reference Include="Newtonsoft.Json"><HintPath>$escapedNewtonsoft</HintPath></Reference>
  </ItemGroup>
</Project>
"@
$projectPath = Join-Path $testRoot 'Check.csproj'
[IO.File]::WriteAllText($projectPath, $project, [Text.UTF8Encoding]::new($false))
$configPath = Join-Path $testRoot 'NuGet.Config'
[IO.File]::WriteAllText($configPath, '<configuration><packageSources><clear /></packageSources></configuration>', [Text.UTF8Encoding]::new($false))
dotnet restore $projectPath --configfile $configPath
if ($LASTEXITCODE -ne 0) { throw '로컬라이징 검증 복원 실패' }
dotnet run --project $projectPath --no-restore -- $projectRoot
if ($LASTEXITCODE -ne 0) { throw '로컬라이징 검증 실패' }
