$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$buffs = (Get-Content -LiteralPath (Join-Path $projectRoot 'Assets/Resources/JsonData/buffs.json') -Raw -Encoding UTF8 | ConvertFrom-Json).buffs
$futurePredation = @($buffs | Where-Object { $_.name -eq '미래 포식' })
if ($futurePredation.Count -ne 1 -or $futurePredation[0].buffId -ne 5010) { throw '미래 포식 버프 ID는 5010이어야 합니다' }
$harmony = @($buffs | Where-Object { $_.name -eq '조화' })
if ($harmony.Count -ne 1 -or $harmony[0].buffId -ne 5014) { throw '조화 버프 ID는 5014이어야 합니다' }
$testRoot = Join-Path $projectRoot 'Temp/BossPatternCheck'
New-Item -ItemType Directory -Force $testRoot | Out-Null
$jsonDll = Get-ChildItem (Join-Path $projectRoot 'Library/PackageCache/com.unity.nuget.newtonsoft-json*/Runtime/Newtonsoft.Json.dll') | Select-Object -First 1
if (!$jsonDll) { throw 'Unity에서 패키지 복원을 먼저 완료해야 합니다' }
$escapedRoot = [Security.SecurityElement]::Escape($projectRoot)
$escapedDll = [Security.SecurityElement]::Escape($jsonDll.FullName)
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>
  <ItemGroup>
    <Compile Include="$escapedRoot/Tests/BossPatterns/*.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Monster/Boss/*.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Monster/DataMonster.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Monster/MonsterIntentLine.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Localization/Core/*.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Monster/AI/*.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Monster/Data/*.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Card/Buff/MonsterBuffScript.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Card/Buff/Monster/FuturePredationBuffScript.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Card/Buff/Monster/EightLegsBuffScript.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Card/Buff/Monster/VoidShellBuffScript.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Card/Buff/Monster/HarmonyBuffScript.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Card/Buff/Monster/AttackBoostBuffScript.cs" />
    <Reference Include="Newtonsoft.Json"><HintPath>$escapedDll</HintPath></Reference>
  </ItemGroup>
</Project>
"@
$projectPath = Join-Path $testRoot 'Check.csproj'
[IO.File]::WriteAllText($projectPath, $project, [Text.UTF8Encoding]::new($false))
$configPath = Join-Path $testRoot 'NuGet.Config'
[IO.File]::WriteAllText($configPath, '<configuration><packageSources><clear /></packageSources></configuration>', [Text.UTF8Encoding]::new($false))
dotnet restore $projectPath --configfile $configPath
if ($LASTEXITCODE -ne 0) { throw '보스모드 검증 프로젝트 복원 실패' }
dotnet run --project $projectPath --no-restore -- $projectRoot
if ($LASTEXITCODE -ne 0) { throw '보스모드 동작 검증 실패' }
