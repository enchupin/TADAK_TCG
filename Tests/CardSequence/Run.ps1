$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot = Join-Path $projectRoot 'Temp/CardSequenceCheck'
New-Item -ItemType Directory -Force $testRoot | Out-Null
$sources = @(
    'Card/Card.cs', 'Card/CardKeywordIds.cs', 'Character/Character.cs',
    'Battle/BattleContext.cs', 'Battle/Systems/CombatResolver.cs',
    'Card/Data/MoveZoneType.cs', 'Card/Data/MovePositionType.cs',
    'Card/Effects/ICardEffect.cs', 'Card/Effects/CardEffectSequence.cs', 'Card/Effects/CardPlayCompletion.cs',
    'Card/Effects/SelectCardEffect.cs', 'Card/Effects/RepeatEffect.cs',
    'Card/Effects/ConditionalEffect.cs', 'Card/Effects/TriggeredCardExecutionUtility.cs',
    'Card/Effects/UseTopDeckCardsEffect.cs', 'Card/Effects/ReplayExhaustedCardsEffect.cs',
    'Card/Effects/MoveEffect.cs', 'Card/Effects/DrawEffect.cs', 'Card/Effects/RandGenerateEffect.cs'
)
$includes = ($sources | ForEach-Object {
    '<Compile Include="' + [Security.SecurityElement]::Escape("$projectRoot/Assets/Script/$_") + '" />'
}) -join "`n"
$escapedRoot = [Security.SecurityElement]::Escape($projectRoot)
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>
  <ItemGroup>
    <Compile Include="$escapedRoot/Tests/CardSequence/*.cs" />
    $includes
  </ItemGroup>
</Project>
"@
[IO.File]::WriteAllText((Join-Path $testRoot 'Check.csproj'), $project, [Text.UTF8Encoding]::new($false))
dotnet run --project (Join-Path $testRoot 'Check.csproj') -- $projectRoot
if ($LASTEXITCODE -ne 0) { throw '카드 효과 순서 검증 실패' }
