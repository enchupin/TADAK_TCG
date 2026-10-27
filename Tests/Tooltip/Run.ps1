$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot = Join-Path $projectRoot 'Temp/TooltipCheck'
New-Item -ItemType Directory -Force $testRoot | Out-Null
$escapedRoot = [Security.SecurityElement]::Escape($projectRoot)
# Unity 의존이 없는 툴팁 계산 · 포맷 소스만 컴파일
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>
  <ItemGroup>
    <Compile Include="$escapedRoot/Tests/Tooltip/*.cs" />
    <Compile Include="$escapedRoot/Assets/Script/UI/Tooltip/TooltipLayout.cs" />
    <Compile Include="$escapedRoot/Assets/Script/UI/IconRowLayout.cs" />
    <Compile Include="$escapedRoot/Assets/Script/UI/Tooltip/TooltipRichText.cs" />
    <Compile Include="$escapedRoot/Assets/Script/UI/Tooltip/BuffTooltipFormatter.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Localization/Core/KoreanJosa.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Localization/Core/LocalizedTextFormatter.cs" />
    <Compile Include="$escapedRoot/Assets/Script/Localization/Core/PluralRules.cs" />
    <Compile Include="$escapedRoot/Assets/Script/UI/Tooltip/IntentTooltipFormatter.cs" />
  </ItemGroup>
</Project>
"@
$projectPath = Join-Path $testRoot 'Check.csproj'
[IO.File]::WriteAllText($projectPath, $project, [Text.UTF8Encoding]::new($false))
$configPath = Join-Path $testRoot 'NuGet.Config'
[IO.File]::WriteAllText($configPath, '<configuration><packageSources><clear /></packageSources></configuration>', [Text.UTF8Encoding]::new($false))
dotnet restore $projectPath --configfile $configPath
if ($LASTEXITCODE -ne 0) { throw '툴팁 검증 복원 실패' }
dotnet run --project $projectPath --no-restore -- $projectRoot
if ($LASTEXITCODE -ne 0) { throw '툴팁 검증 실패' }
