using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// 한국어 원문(JsonData) → Localization/ko 동기화, 번역 상태 · CSV 입출력 (UnityEditor 의존 없음)
/// </summary>
// 생성 표(cards · buffs · keywords · characters · monsters)는 JsonData가 원문이라 동기화 때 덮어씀
// 그 밖의 ko 표(ui · intents 등)는 직접 작성하는 원문이라 건드리지 않음
// 번역 상태: 번역할 때의 원문 해시를 TranslationState/<코드>.json에 기록해 원문이 바뀐 키를 찾음
public static class LocalizationSourceSync
{
    public const string SourceLanguage = "ko";
    public const string JsonDataFolder = "Assets/Resources/JsonData";
    public const string LocalizationFolder = "Assets/Resources/Localization";
    public const string TranslationStateFolder = "Assets/Editor/Localization/TranslationState";
    public const string ExportFolder = "LocalizationExport";

    public const string CardsTable = "cards";
    public const string BuffsTable = "buffs";
    public const string KeywordsTable = "keywords";
    public const string CharactersTable = "characters";
    public const string MonstersTable = "monsters";
    public const string IntentsTable = "intents";
    public const string UiTable = "ui";

    // 동기화 도구가 생성하는 ko 표
    public static readonly string[] GeneratedTables = { CardsTable, BuffsTable, KeywordsTable, CharactersTable, MonstersTable };

    // 기존 원문의 동적 이름 표기 → 영문 인자 이름
    private static readonly Dictionary<string, string> DynamicNameKeys = new(StringComparer.Ordinal)
    {
        { "동력", "runeBuff" }
    };

    private static readonly Regex ParenthesizedNamePattern = new(@"\((?<name>[^()]+)\)", RegexOptions.Compiled);
    private static readonly Regex PlaceholderPattern = new(@"\{(?<name>[^{},]+)\}", RegexOptions.Compiled);

    // ── 원문 표 생성 ─────────────────────────────────────────────

    public static Dictionary<string, SortedDictionary<string, string>> BuildSourceTables(string projectRoot, LocalizationSyncReport report)
    {
        report ??= new LocalizationSyncReport();
        string jsonData = Path.Combine(projectRoot, JsonDataFolder);
        Dictionary<string, int> buffIdByName = LoadBuffIdsByName(jsonData, report);

        Dictionary<string, SortedDictionary<string, string>> tables = new(StringComparer.Ordinal);
        foreach (string table in GeneratedTables)
        {
            tables[table] = new SortedDictionary<string, string>(StringComparer.Ordinal);
        }

        AddCards(jsonData, tables[CardsTable], buffIdByName, report);
        AddBuffs(jsonData, tables[BuffsTable], buffIdByName, report);
        AddKeywords(jsonData, tables[KeywordsTable], buffIdByName, report);
        AddCharacters(jsonData, tables[CharactersTable], buffIdByName, report);
        AddMonsters(jsonData, tables[MonstersTable], buffIdByName, report);
        return tables;
    }

    // ko 생성 표를 다시 씀, 바뀐 표 이름 목록을 보고서에 기록
    public static LocalizationSyncReport SyncKorean(string projectRoot)
    {
        LocalizationSyncReport report = new();
        Dictionary<string, SortedDictionary<string, string>> tables = BuildSourceTables(projectRoot, report);
        string folder = GetLanguageFolder(projectRoot, SourceLanguage);
        foreach (KeyValuePair<string, SortedDictionary<string, string>> table in tables)
        {
            string path = Path.Combine(folder, table.Key + ".json");
            if (WriteTableIfChanged(path, table.Value))
            {
                report.ChangedTables.Add(table.Key);
            }

            report.EntryCount += table.Value.Count;
        }

        return report;
    }

    // 원문 문구 변환: (버프 이름) → [buff:id], {동력} → {runeBuff}
    public static string ConvertSourceText(string text, IReadOnlyDictionary<string, int> buffIdByName, LocalizationSyncReport report, string context)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        string converted = ParenthesizedNamePattern.Replace(text, match =>
        {
            string name = match.Groups["name"].Value.Trim();
            if (buffIdByName != null && buffIdByName.TryGetValue(name, out int buffId))
            {
                return $"[buff:{buffId.ToString(CultureInfo.InvariantCulture)}]";
            }

            // 버프가 아닌 괄호 문구는 그대로 (예: 턴당 1회)
            report?.KeptParentheses.Add($"{context}: ({name})");
            return match.Value;
        });

        return PlaceholderPattern.Replace(converted, match =>
            DynamicNameKeys.TryGetValue(match.Groups["name"].Value, out string key) ? "{" + key + "}" : match.Value);
    }

    // ── 표 입출력 ────────────────────────────────────────────────

    public static string GetLanguageFolder(string projectRoot, string languageCode)
    {
        return Path.Combine(projectRoot, LocalizationFolder, languageCode);
    }

    // 키 접두어로 표 파일 결정
    public static string GetTableName(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return UiTable;
        }

        if (key.StartsWith("card.", StringComparison.Ordinal))
        {
            return CardsTable;
        }

        if (key.StartsWith("buff.", StringComparison.Ordinal))
        {
            return BuffsTable;
        }

        if (key.StartsWith("keyword.", StringComparison.Ordinal))
        {
            return KeywordsTable;
        }

        if (key.StartsWith("character.", StringComparison.Ordinal))
        {
            return CharactersTable;
        }

        if (key.StartsWith("monster.", StringComparison.Ordinal))
        {
            return MonstersTable;
        }

        if (key.StartsWith("intent.", StringComparison.Ordinal))
        {
            return IntentsTable;
        }

        return UiTable;
    }

    public static SortedDictionary<string, string> ReadTable(string path)
    {
        SortedDictionary<string, string> entries = new(StringComparer.Ordinal);
        if (!File.Exists(path))
        {
            return entries;
        }

        Dictionary<string, string> parsed = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path, Encoding.UTF8));
        if (parsed == null)
        {
            return entries;
        }

        foreach (KeyValuePair<string, string> entry in parsed)
        {
            entries[entry.Key] = entry.Value ?? string.Empty;
        }

        return entries;
    }

    // 언어 폴더의 모든 표를 합침
    public static SortedDictionary<string, string> ReadLanguage(string projectRoot, string languageCode)
    {
        SortedDictionary<string, string> entries = new(StringComparer.Ordinal);
        string folder = GetLanguageFolder(projectRoot, languageCode);
        if (!Directory.Exists(folder))
        {
            return entries;
        }

        foreach (string path in Directory.GetFiles(folder, "*.json").OrderBy(p => p, StringComparer.Ordinal))
        {
            foreach (KeyValuePair<string, string> entry in ReadTable(path))
            {
                entries[entry.Key] = entry.Value;
            }
        }

        return entries;
    }

    // 키 접두어별 표 파일에 병합 기록 (빈 문자열이면 키 삭제)
    public static void WriteLanguageEntries(string projectRoot, string languageCode, IEnumerable<KeyValuePair<string, string>> entries)
    {
        string folder = GetLanguageFolder(projectRoot, languageCode);
        Dictionary<string, SortedDictionary<string, string>> tables = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> entry in entries)
        {
            string tableName = GetTableName(entry.Key);
            if (!tables.TryGetValue(tableName, out SortedDictionary<string, string> table))
            {
                table = ReadTable(Path.Combine(folder, tableName + ".json"));
                tables.Add(tableName, table);
            }

            if (string.IsNullOrEmpty(entry.Value))
            {
                table.Remove(entry.Key);
            }
            else
            {
                table[entry.Key] = entry.Value;
            }
        }

        foreach (KeyValuePair<string, SortedDictionary<string, string>> table in tables)
        {
            WriteTableIfChanged(Path.Combine(folder, table.Key + ".json"), table.Value);
        }
    }

    // UTF-8(BOM 없음) · LF · 키 정렬로 기록, 내용이 같으면 쓰지 않음
    public static bool WriteTableIfChanged(string path, IDictionary<string, string> entries)
    {
        SortedDictionary<string, string> sorted = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> entry in entries)
        {
            sorted[entry.Key] = entry.Value ?? string.Empty;
        }

        string json = SerializeTable(sorted);
        if (File.Exists(path) && File.ReadAllText(path, Encoding.UTF8) == json)
        {
            return false;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json, new UTF8Encoding(false));
        return true;
    }

    public static string SerializeTable(IDictionary<string, string> entries)
    {
        StringBuilder builder = new();
        using (StringWriter stringWriter = new(builder, CultureInfo.InvariantCulture))
        using (JsonTextWriter writer = new(stringWriter))
        {
            writer.Formatting = Formatting.Indented;
            writer.Indentation = 4;
            stringWriter.NewLine = "\n";
            writer.WriteStartObject();
            foreach (KeyValuePair<string, string> entry in entries)
            {
                writer.WritePropertyName(entry.Key);
                writer.WriteValue(entry.Value);
            }

            writer.WriteEndObject();
        }

        return builder.Replace("\r\n", "\n").Append('\n').ToString();
    }

    // ── 번역 상태 ────────────────────────────────────────────────

    public static string ComputeHash(string text)
    {
        using SHA256 sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? string.Empty));
        StringBuilder builder = new(12);
        for (int i = 0; i < 6; i++)
        {
            builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    public static string GetStatePath(string projectRoot, string languageCode)
    {
        return Path.Combine(projectRoot, TranslationStateFolder, languageCode + ".json");
    }

    public static SortedDictionary<string, string> ReadState(string projectRoot, string languageCode)
    {
        return ReadTable(GetStatePath(projectRoot, languageCode));
    }

    // 지금 번역이 현재 원문 기준이라고 기록 (keys가 null이면 번역된 모든 키)
    public static int MarkTranslated(string projectRoot, string languageCode, IEnumerable<string> keys = null)
    {
        SortedDictionary<string, string> source = ReadLanguage(projectRoot, SourceLanguage);
        SortedDictionary<string, string> translation = ReadLanguage(projectRoot, languageCode);
        SortedDictionary<string, string> state = ReadState(projectRoot, languageCode);
        int marked = 0;
        foreach (string key in keys ?? translation.Keys)
        {
            if (!translation.ContainsKey(key) || !source.TryGetValue(key, out string sourceText))
            {
                continue;
            }

            state[key] = ComputeHash(sourceText);
            marked++;
        }

        WriteTableIfChanged(GetStatePath(projectRoot, languageCode), state);
        return marked;
    }

    public static List<TranslationRow> BuildRows(string projectRoot, string languageCode)
    {
        SortedDictionary<string, string> source = ReadLanguage(projectRoot, SourceLanguage);
        SortedDictionary<string, string> translation = ReadLanguage(projectRoot, languageCode);
        SortedDictionary<string, string> state = ReadState(projectRoot, languageCode);
        List<TranslationRow> rows = new();
        foreach (KeyValuePair<string, string> entry in source)
        {
            translation.TryGetValue(entry.Key, out string translated);
            state.TryGetValue(entry.Key, out string recordedHash);
            rows.Add(new TranslationRow(entry.Key, entry.Value, translated ?? string.Empty, ResolveStatus(entry.Value, translated, recordedHash)));
        }

        // 원문에 없는 번역 키 (카드 삭제 등)
        foreach (KeyValuePair<string, string> entry in translation)
        {
            if (!source.ContainsKey(entry.Key))
            {
                rows.Add(new TranslationRow(entry.Key, string.Empty, entry.Value, TranslationStatus.Orphan));
            }
        }

        return rows;
    }

    public static TranslationStatus ResolveStatus(string sourceText, string translatedText, string recordedHash)
    {
        if (string.IsNullOrEmpty(translatedText))
        {
            return TranslationStatus.Missing;
        }

        if (string.IsNullOrEmpty(recordedHash))
        {
            return TranslationStatus.Unverified;
        }

        return recordedHash == ComputeHash(sourceText) ? TranslationStatus.UpToDate : TranslationStatus.Stale;
    }

    // ── CSV ─────────────────────────────────────────────────────

    // key, status, source, translation (엑셀 호환 UTF-8 BOM)
    public static string ExportCsv(string projectRoot, string languageCode, string path = null)
    {
        path ??= Path.Combine(projectRoot, ExportFolder, $"{languageCode}.csv");
        StringBuilder builder = new();
        builder.Append("key,status,source,translation\r\n");
        foreach (TranslationRow row in BuildRows(projectRoot, languageCode))
        {
            builder.Append(EscapeCsv(row.Key)).Append(',')
                .Append(EscapeCsv(row.Status.ToString())).Append(',')
                .Append(EscapeCsv(row.Source)).Append(',')
                .Append(EscapeCsv(row.Translation)).Append("\r\n");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(true));
        return path;
    }

    // translation 열을 표에 반영하고 원문 해시를 기록, 반영한 행 수를 돌려줌
    public static int ImportCsv(string projectRoot, string languageCode, string path, LocalizationSyncReport report = null)
    {
        List<List<string>> records = ParseCsv(File.ReadAllText(path, Encoding.UTF8));
        if (records.Count == 0)
        {
            return 0;
        }

        List<string> header = records[0];
        int keyColumn = header.IndexOf("key");
        int translationColumn = header.IndexOf("translation");
        if (keyColumn < 0 || translationColumn < 0)
        {
            throw new InvalidDataException("CSV 첫 줄에 key, translation 열이 필요합니다");
        }

        SortedDictionary<string, string> source = ReadLanguage(projectRoot, SourceLanguage);
        SortedDictionary<string, string> current = ReadLanguage(projectRoot, languageCode);
        Dictionary<string, string> changed = new(StringComparer.Ordinal);
        List<string> imported = new();
        for (int i = 1; i < records.Count; i++)
        {
            List<string> record = records[i];
            if (record.Count <= Math.Max(keyColumn, translationColumn))
            {
                continue;
            }

            string key = record[keyColumn].Trim();
            string translated = record[translationColumn];
            if (key.Length == 0 || string.IsNullOrEmpty(translated))
            {
                continue;
            }

            if (!source.ContainsKey(key))
            {
                report?.Warnings.Add($"원문에 없는 키를 건너뜀: {key}");
                continue;
            }

            if (!current.TryGetValue(key, out string existing) || existing != translated)
            {
                changed[key] = translated;
            }

            imported.Add(key);
        }

        WriteLanguageEntries(projectRoot, languageCode, changed);
        MarkTranslated(projectRoot, languageCode, imported);
        return imported.Count;
    }

    public static string EscapeCsv(string value)
    {
        value ??= string.Empty;
        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    // RFC 4180 (따옴표 안 줄바꿈 · "" 이스케이프)
    public static List<List<string>> ParseCsv(string text)
    {
        List<List<string>> records = new();
        if (string.IsNullOrEmpty(text))
        {
            return records;
        }

        if (text[0] == '﻿')
        {
            text = text.Substring(1);
        }

        List<string> record = new();
        StringBuilder field = new();
        bool quoted = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else
                {
                    field.Append(c);
                }

                continue;
            }

            switch (c)
            {
                case '"':
                    quoted = true;
                    break;
                case ',':
                    record.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                    break;
                case '\n':
                    record.Add(field.ToString());
                    field.Clear();
                    records.Add(record);
                    record = new List<string>();
                    break;
                default:
                    field.Append(c);
                    break;
            }
        }

        if (field.Length > 0 || record.Count > 0)
        {
            record.Add(field.ToString());
            records.Add(record);
        }

        return records;
    }

    // ── 원문 읽기 ────────────────────────────────────────────────

    private static Dictionary<string, int> LoadBuffIdsByName(string jsonData, LocalizationSyncReport report)
    {
        Dictionary<string, int> buffIdByName = new(StringComparer.Ordinal);
        foreach (JObject buff in ReadArray(Path.Combine(jsonData, "buffs.json"), "buffs"))
        {
            int buffId = buff.Value<int?>("buffId") ?? 0;
            string name = buff.Value<string>("name")?.Trim();
            if (buffId <= 0 || string.IsNullOrEmpty(name))
            {
                continue;
            }

            if (buffIdByName.ContainsKey(name))
            {
                report.Warnings.Add($"버프 이름 중복: {name} ({buffIdByName[name]}, {buffId})");
                continue;
            }

            buffIdByName.Add(name, buffId);
        }

        return buffIdByName;
    }

    private static void AddCards(string jsonData, SortedDictionary<string, string> table, Dictionary<string, int> buffIdByName, LocalizationSyncReport report)
    {
        foreach (string path in Directory.GetFiles(jsonData, "*Cards.json").OrderBy(p => p, StringComparer.Ordinal))
        {
            JToken root = ReadJson(path);
            JArray cards = root as JArray ?? (root as JObject)?.Properties().Select(p => p.Value).OfType<JArray>().FirstOrDefault();
            if (cards == null)
            {
                report.Warnings.Add($"카드 배열이 없습니다: {Path.GetFileName(path)}");
                continue;
            }

            foreach (JObject card in cards.OfType<JObject>())
            {
                int cardId = card.Value<int?>("cardId") ?? 0;
                if (cardId <= 0)
                {
                    continue;
                }

                string prefix = $"card.{cardId.ToString(CultureInfo.InvariantCulture)}";
                AddEntry(table, prefix + ".name", card.Value<string>("name"), report);
                AddEntry(table, prefix + ".desc", ConvertSourceText(card.Value<string>("description"), buffIdByName, report, prefix), report);
            }
        }
    }

    private static void AddBuffs(string jsonData, SortedDictionary<string, string> table, Dictionary<string, int> buffIdByName, LocalizationSyncReport report)
    {
        foreach (JObject buff in ReadArray(Path.Combine(jsonData, "buffs.json"), "buffs"))
        {
            int buffId = buff.Value<int?>("buffId") ?? 0;
            if (buffId <= 0)
            {
                continue;
            }

            string prefix = $"buff.{buffId.ToString(CultureInfo.InvariantCulture)}";
            AddEntry(table, prefix + ".name", buff.Value<string>("name"), report);
            AddEntry(table, prefix + ".desc", ConvertSourceText(buff.Value<string>("description"), buffIdByName, report, prefix), report);
        }
    }

    private static void AddKeywords(string jsonData, SortedDictionary<string, string> table, Dictionary<string, int> buffIdByName, LocalizationSyncReport report)
    {
        foreach (JObject keyword in ReadArray(Path.Combine(jsonData, "keywords.json"), "keywords"))
        {
            int keywordId = keyword.Value<int?>("keywordId") ?? 0;
            if (keywordId <= 0)
            {
                continue;
            }

            string prefix = $"keyword.{keywordId.ToString(CultureInfo.InvariantCulture)}";
            AddEntry(table, prefix + ".name", keyword.Value<string>("name"), report);
            AddEntry(table, prefix + ".desc", ConvertSourceText(keyword.Value<string>("description"), buffIdByName, report, prefix), report);
        }
    }

    private static void AddCharacters(string jsonData, SortedDictionary<string, string> table, Dictionary<string, int> buffIdByName, LocalizationSyncReport report)
    {
        foreach (JObject character in ReadArray(Path.Combine(jsonData, "characters.json"), "characters"))
        {
            int characterId = character.Value<int?>("characterId") ?? 0;
            if (characterId <= 0)
            {
                continue;
            }

            string prefix = $"character.{characterId.ToString(CultureInfo.InvariantCulture)}";
            AddEntry(table, prefix + ".name", character.Value<string>("name"), report);
            AddEntry(table, prefix + ".identity", ConvertSourceText(character.Value<string>("identityDescription"), buffIdByName, report, prefix), report);
        }
    }

    // 몬스터 이름(monster.<id>.name)과 화면에 나오는 특수 패턴 문구(monster.pattern.<패턴 id>.text)
    // 일반 패턴 문구는 행동으로 의도 문장을 만들므로 표에 넣지 않음
    private static void AddMonsters(string jsonData, SortedDictionary<string, string> table, Dictionary<string, int> buffIdByName, LocalizationSyncReport report)
    {
        foreach (JObject monster in ReadArray(Path.Combine(jsonData, "monsters.json"), "monsters"))
        {
            int monsterId = monster.Value<int?>("id") ?? 0;
            if (monsterId <= 0)
            {
                continue;
            }

            string prefix = $"monster.{monsterId.ToString(CultureInfo.InvariantCulture)}";
            AddEntry(table, prefix + ".name", monster.Value<string>("name"), report);
            if (monster["patterns"] is not JArray patterns)
            {
                continue;
            }

            foreach (JObject pattern in patterns.OfType<JObject>())
            {
                int patternId = pattern.Value<int?>("id") ?? 0;
                bool hasSpecial = pattern.Descendants().OfType<JObject>().Any(action => action.Value<string>("type") == "special");
                if (patternId <= 0 || !hasSpecial)
                {
                    continue;
                }

                string patternPrefix = $"monster.pattern.{patternId.ToString(CultureInfo.InvariantCulture)}";
                AddEntry(table, patternPrefix + ".text", ConvertSourceText(pattern.Value<string>("text"), buffIdByName, report, patternPrefix), report);
            }
        }
    }

    // 빈 원문은 표에 넣지 않음
    private static void AddEntry(SortedDictionary<string, string> table, string key, string value, LocalizationSyncReport report)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (table.ContainsKey(key))
        {
            report.Warnings.Add($"키 중복: {key}");
        }

        table[key] = value;
    }

    private static IEnumerable<JObject> ReadArray(string path, string propertyName)
    {
        JToken root = ReadJson(path);
        JArray array = root as JArray ?? root?[propertyName] as JArray;
        return array != null ? array.OfType<JObject>() : Enumerable.Empty<JObject>();
    }

    private static JToken ReadJson(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        string text = File.ReadAllText(path, Encoding.UTF8);
        return JToken.Parse(text.Length > 0 && text[0] == '﻿' ? text.Substring(1) : text);
    }
}

public enum TranslationStatus
{
    // 원문 해시와 일치
    UpToDate,
    // 번역은 있지만 기록된 원문 해시가 없음 (기존 번역 이전분 등)
    Unverified,
    // 번역 후 원문이 바뀜
    Stale,
    Missing,
    // 원문에 없는 키
    Orphan
}

public readonly struct TranslationRow
{
    public string Key { get; }
    public string Source { get; }
    public string Translation { get; }
    public TranslationStatus Status { get; }

    public TranslationRow(string key, string source, string translation, TranslationStatus status)
    {
        Key = key;
        Source = source;
        Translation = translation;
        Status = status;
    }
}

public sealed class LocalizationSyncReport
{
    public readonly List<string> Warnings = new();
    // 버프 이름이 아니라 그대로 둔 괄호 문구
    public readonly List<string> KeptParentheses = new();
    public readonly List<string> ChangedTables = new();
    public int EntryCount;

    public string ToSummary()
    {
        StringBuilder builder = new();
        builder.Append($"원문 {EntryCount}개, 갱신된 표 {ChangedTables.Count}개");
        if (ChangedTables.Count > 0)
        {
            builder.Append($" ({string.Join(", ", ChangedTables)})");
        }

        foreach (string kept in KeptParentheses)
        {
            builder.Append("\n괄호 유지: ").Append(kept);
        }

        foreach (string warning in Warnings)
        {
            builder.Append("\n경고: ").Append(warning);
        }

        return builder.ToString();
    }
}
