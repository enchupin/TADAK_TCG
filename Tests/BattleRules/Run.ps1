$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot = Join-Path $root 'Temp/BattleRulesCheck'
New-Item -ItemType Directory -Force $testRoot | Out-Null

# 손패 상한과 드로우 경로는 복제 구현 대신 실제 소스의 메서드를 추출해 컴파일
function Read-SourceMethod([string]$relative, [string]$signature) {
    $source = Get-Content (Join-Path $root ('Assets/Script/' + $relative)) -Raw -Encoding UTF8
    $start = $source.IndexOf($signature)
    if ($start -lt 0) { throw "검증 메서드를 찾을 수 없습니다: $signature" }
    $open = $source.IndexOf('{', $start)
    $depth = 1
    $end = $open + 1
    while ($depth -gt 0) {
        if ($source[$end] -eq '{') { $depth++ }
        if ($source[$end] -eq '}') { $depth-- }
        $end++
    }
    return $source.Substring($start, $end - $start)
}
function Read-SourceLine([string]$relative, [string]$prefix) {
    $line = Get-Content (Join-Path $root ('Assets/Script/' + $relative)) -Encoding UTF8 | Where-Object { $_.Trim().StartsWith($prefix) } | Select-Object -First 1
    if (!$line) { throw "검증 속성을 찾을 수 없습니다: $prefix" }
    return $line
}

$manager = 'Battle/TrainingBattleManager.cs'
$methods = 'using System; using System.Collections.Generic; using UnityEngine; public partial class TrainingBattleManager {'
$methods += Read-SourceLine $manager 'public int MaxHandCardCount =>'
$methods += Read-SourceLine $manager 'public int DrawCardCount =>'
$methods += Read-SourceLine $manager 'public int PlayerBaseEnergyPerTurn =>'
$methods += Read-SourceLine $manager 'public bool IsHandFull =>'
foreach ($signature in @('private List<Card> DrawCardsSequentially(', 'private void CompleteDrawSequence(', 'private bool CanStartDraw(', 'private bool HasRootAbsorptionInHand(', 'public bool HasCardInHand(')) {
    $methods += Read-SourceMethod $manager $signature
}
$methods += '} public partial class HandManager {' + (Read-SourceMethod 'Battle/HandManager.cs' 'private bool TryRouteOverflowToDiscard(') + '}'
[IO.File]::WriteAllText((Join-Path $testRoot 'ProductionMethods.cs'), $methods, [Text.UTF8Encoding]::new($false))

$escapedRoot = [Security.SecurityElement]::Escape($root)
$escapedTestRoot = [Security.SecurityElement]::Escape($testRoot)
$project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><Nullable>disable</Nullable></PropertyGroup><ItemGroup>' +
    '<Compile Include="' + $escapedRoot + '/Tests/BattleRules/*.cs" />' +
    '<Compile Include="' + $escapedTestRoot + '/ProductionMethods.cs" />' +
    '<Compile Include="' + $escapedRoot + '/Assets/Script/Data/GameRules.cs" />' +
    '<Compile Include="' + $escapedRoot + '/Assets/Script/Character/Character.cs" />' +
    '<Compile Include="' + $escapedRoot + '/Assets/Script/Card/CardKeywordIds.cs" />' +
    '<Compile Include="' + $escapedRoot + '/Assets/Script/Character/Identity/CharacterIdentityDefinition.cs" />' +
    '<Compile Include="' + $escapedRoot + '/Assets/Script/Character/Identity/CharacterIdentityState.cs" />' +
    '<Compile Include="' + $escapedRoot + '/Assets/Script/Character/Identity/CharacterIdentityService.cs" />' +
    '<Compile Include="' + $escapedRoot + '/Assets/Script/TrainingMode/TrainingDeckRewardService.cs" />' +
    '</ItemGroup></Project>'
$projectPath = Join-Path $testRoot 'Check.csproj'
[IO.File]::WriteAllText($projectPath, $project, [Text.UTF8Encoding]::new($false))
$configPath = Join-Path $testRoot 'NuGet.Config'
[IO.File]::WriteAllText($configPath, '<configuration><packageSources><clear /></packageSources></configuration>', [Text.UTF8Encoding]::new($false))
dotnet restore $projectPath --configfile $configPath
if ($LASTEXITCODE -ne 0) { throw '전투 규칙 검증 복원 실패' }
dotnet run --project $projectPath --no-restore -- $root
if ($LASTEXITCODE -ne 0) { throw '전투 규칙 검증 실패' }
