$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot = Join-Path $root 'Temp/BuffAuditCheck'
New-Item -ItemType Directory -Force $testRoot | Out-Null
$buffRoot = Join-Path $root 'Assets/Script/Card/Buff'
$names = @('FeatherEnhanceBuffScript','DoubleFeatherBuffScript','FeatherStackBoostBuffScript','FeatherAutoTriggerBuffScript','BlindFeatherBuffScript','LifeLinkBuffScript','GlacierBondBuffScript','FrailBuffScript','FeatherBuffScript','FeatherCycleBuffScript','DeadlyAmbushBuffScript','StrengthBuffScript','AttackBoostBuffScript','PlayerBuffScript','MonsterBuffScript','PlayerBuffRuntimeService','MonsterBuffRuntimeService','GrowingFeatherBuffScript','GlacierShapeOnHitBuffScript','RepeatNextPowerCardBuffScript','RepeatNextCardBuffScript','HighCostRepeatBuffScript','LastStandBuffScript','StrengthContractBuffScript','WuppiAttackBuffScript','PotionCycleBuffScript','DrawLockBuffScript','OverheatBuffScript','WeakBuffScript','RegenerationBuffScript','BurnBuffScript','DoubleActionBuffScript','DrowningOnDebuffBuffScript','RisingWaterBuffScript','UnderwaterBreathingBuffScript','NextCardFreeBuffScript','ComboBuffScript','Monster/StrengthDecayBuffScript','Monster/FrailBuffScript','Monster/GlacierBondBuffScript','Monster/MirrorBuffScript','Monster/LifeLinkBuffScript','Monster/ThornBuffScript','Monster/StrengthBuffScript','Monster/LifeStealBuffScript')
$compile = @()
$actualClasses = @{}
foreach ($name in $names) {
    $path = Join-Path $buffRoot ($name + '.cs')
    $compile += '<Compile Include="' + [Security.SecurityElement]::Escape($path) + '" />'
    $className = $name.Replace('Monster/', 'MonsterBuffs.')
    $actualClasses[$className] = $true
}

foreach ($relative in @('Card/Effects/TriggeredCardExecutionUtility.cs','Card/Effects/CardEffectSequence.cs','Card/Effects/BuffEffect.cs','Data/BuffData.cs','Card/Effects/AttackEffect.cs','Card/Effects/DamageEffect.cs','Card/Effects/ICardEffect.cs')) {
    $compile += '<Compile Include="' + [Security.SecurityElement]::Escape((Join-Path $root ('Assets/Script/' + $relative))) + '" />'
}
# 계산과 비용 지불 경로도 복제 구현 대신 실제 소스의 메서드를 컴파일
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
    return $source.Substring($start, $end-$start)
}
$methods = 'using UnityEngine; using System.Collections; using System.Collections.Generic; using static BattleRuntimeDefinitions; public partial class PlayerData {' + (Read-SourceMethod 'Battle/PlayerData.cs' 'public int CalculateCardDamage(') + (Read-SourceMethod 'Battle/PlayerData.cs' 'public int AddDefense(') + '}'
$methods += 'public partial class PlayerData {'
foreach ($signature in @('public void AddBuff(', 'public int GetBuffStack(', 'public void SetBuffStack(', 'private void DecreaseBuffStack(', 'public void ConsumeBuffStack(', 'public void RemoveBuffStack(', 'private void RemoveBuff(', 'public int TakeDamage(', 'public int LoseHp(', 'private void TryConsumeSoulProtection(')) {
    $methods += Read-SourceMethod 'Battle/PlayerData.cs' $signature
}
$methods += '}'
$methods += 'public partial class TrainingBattleManager {' + (Read-SourceMethod 'Battle/TrainingBattleManager.cs' 'public bool TryPayCardCost(') + (Read-SourceMethod 'Battle/TrainingBattleManager.cs' 'public void SetState(') + (Read-SourceMethod 'Battle/TrainingBattleManager.cs' 'public int ResolvePlayerBarrierGain(') + '}'
$methods += 'public partial class TurnSystem {' + (Read-SourceMethod 'Battle/Systems/TurnSystem.cs' 'private IEnumerator DiscardRemainingHandCards(') + (Read-SourceMethod 'Battle/Systems/TurnSystem.cs' 'private void ExecuteAdditionalTurnEndTriggers(') + '}'
$methods += 'public partial class BattleBuffController {' + (Read-SourceMethod 'Card/Buff/BattleBuffController.cs' 'public int ResolvePlayerBarrierGain(') + (Read-SourceMethod 'Card/Buff/BattleBuffController.cs' 'public int GetAdditionalBarrierGain(') + '}'
$methods += 'public partial class TrainingBattleManager {' + (Read-SourceMethod 'Battle/TrainingBattleManager.cs' 'public Card ApplyHandCardUpgrades(') + (Read-SourceMethod 'Battle/TrainingBattleManager.cs' 'public Card ApplyPersistentUpgradeToCard(Card card, int sourceBuffId)') + '}'
$methods += 'public partial class BattleBuffController {'
foreach ($signature in @('public int ResolvePersistentUpgradeCardId(', 'public void HandleBuffApplied(', 'private static bool IsPersistentUpgradeBuff(', 'private bool ApplyPersistentCardBuffChanges(', 'public Card ApplyPersistentUpgradeToCard(')) {
    $methods += Read-SourceMethod 'Card/Buff/BattleBuffController.cs' $signature
}
$methods += '}'
[IO.File]::WriteAllText((Join-Path $testRoot 'ProductionMethods.cs'), $methods, [Text.UTF8Encoding]::new($false))

$stubs = ''
foreach ($service in @('PlayerBuffRuntimeService','MonsterBuffRuntimeService')) {
    $baseType = if ($service.StartsWith('Player')) { 'PlayerBuffScript' } else { 'MonsterBuffScript' }
    $source = Get-Content (Join-Path $buffRoot ($service + '.cs')) -Raw -Encoding UTF8
    foreach ($match in [regex]::Matches($source, 'Register\(new ([\w.]+)\(\)\)')) {
        $name = $match.Groups[1].Value
        if ($actualClasses.ContainsKey($name)) { continue }
        if ($name.StartsWith('MonsterBuffs.')) {
            $stubs += 'namespace MonsterBuffs { public class ' + $name.Substring(13) + ' : MonsterBuffScript { public override int BuffId => 0; } }' + "`n"
        } else {
            $stubs += 'public class ' + $name + ' : PlayerBuffScript { public override int BuffId => 0; }' + "`n"
        }
    }
}
# 이번 회귀 검증에서 사용하지 않는 버프만 대체하며 검증 대상과 두 실행 서비스는 실제 소스를 사용
[IO.File]::WriteAllText((Join-Path $testRoot 'UnusedBuffStubs.cs'), $stubs, [Text.UTF8Encoding]::new($false))
$definitions = Get-Content (Join-Path $root 'Assets/Script/Battle/BattleRuntimeDefinitions.cs') -Raw -Encoding UTF8
$constants = 'public static class BattleRuntimeDefinitions {'
foreach ($match in [regex]::Matches($definitions, 'Register\(nameof\((\w+)\),\s*"[^"]+",\s*(\d+)\)')) {
    $constants += 'public const int ' + $match.Groups[1].Value + '=' + $match.Groups[2].Value + ';'
}
$constants += '}'
[IO.File]::WriteAllText((Join-Path $testRoot 'Definitions.cs'), $constants, [Text.UTF8Encoding]::new($false))
$escapedRoot = [Security.SecurityElement]::Escape($root)
$project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="' + $escapedRoot + '/Tests/BuffAudit/*.cs" /><Compile Include="' + [Security.SecurityElement]::Escape($testRoot) + '/*.cs" />' + ($compile -join "`n") + '</ItemGroup></Project>'
$projectPath = Join-Path $testRoot 'Check.csproj'
[IO.File]::WriteAllText($projectPath, $project, [Text.UTF8Encoding]::new($false))
$configPath = Join-Path $testRoot 'NuGet.Config'
[IO.File]::WriteAllText($configPath, '<configuration><packageSources><clear /></packageSources></configuration>', [Text.UTF8Encoding]::new($false))
dotnet restore $projectPath --configfile $configPath
if ($LASTEXITCODE -ne 0) { throw '버프 검증 복원 실패' }
dotnet run --project $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw '버프 동작 검증 실패' }
