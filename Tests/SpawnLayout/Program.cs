using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
// 조우별 스폰 좌표 검증: 서식 왕복 · 조우 ID 찾기 · 칸 해석 · spawnLayouts.json 항목 규칙
// --write 인자를 주면 빠진 조우 항목을 씬 기본 좌표로 채워 저장
class Program {
 static int count;
 static readonly List<string> warnings=new();
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
 // CombatScene MonsterSpawner (1)~(4) 위치
 static readonly SpawnSlot[] SceneDefaults={new(-125,0),new(-285,0),new(-445,0),new(-605,0)};
 // 1920 기준 화면 가로 중앙 (우측 끝 기준 좌표)
 const float ScreenCenterX=-960f;
 // 체력 바 폭
 const float HealthBarWidth=150f;
 static readonly JsonSerializerOptions Options=new(){IncludeFields=true};

 static int Main(string[] args){
  string root=args[0];
  bool write=args.Contains("--write");
  FormatRule();
  FindRule();
  ResolveRule();
  List<EncounterDefinition> encounters=LoadEncounters(root);
  HashSet<int> summoners=LoadSummoners(root);
  int maxEnemies=LoadMaxEnemies(root);
  string jsonPath=Path.Combine(root,"Assets/Resources",SpawnLayoutData.ResourcePath+".json");
  if(write){
   SpawnLayoutData filled=File.Exists(jsonPath)?Parse(File.ReadAllText(jsonPath)):new SpawnLayoutData();
   int added=Fill(filled,encounters,summoners,maxEnemies);
   File.WriteAllText(jsonPath,filled.ToJson(),new UTF8Encoding(false));
   Console.WriteLine($"스폰 좌표 저장: 항목 {filled.layouts.Count}개 (새로 채움 {added}개) → {jsonPath}");
   return 0;
  }
  Stored(jsonPath,encounters,summoners,maxEnemies);
  foreach(string warning in warnings)Console.WriteLine("경고: "+warning);
  Console.WriteLine($"스폰 좌표 검증 {count}개 통과");
  return 0;
 }

 static void FormatRule(){
  var data=new SpawnLayoutData();
  data.layouts.Add(new SpawnLayoutEntry{id="E1",slots=new(){new(-125,0),new(-300.36f,12.5f)}});
  data.layouts.Add(new SpawnLayoutEntry{id="E2",slots=new()});
  string json=data.ToJson();
  Check(json.Contains("{ \"id\": \"E1\", \"slots\": [ { \"x\": -125, \"y\": 0 }, { \"x\": -300.4, \"y\": 12.5 } ] },\n"),"서식 오류\n"+json);
  Check(json.Contains("{ \"id\": \"E2\", \"slots\": [ ] }\n"),"빈 칸 서식 오류\n"+json);
  var parsed=Parse(json);
  Check(parsed.layouts.Count==2&&parsed.Find("E1").slots.Count==2&&parsed.Find("E2").slots.Count==0,"서식 왕복 오류");
  Check(Near(parsed.Find("E1").slots[1].x,-300.4f)&&Near(parsed.Find("E1").slots[1].y,12.5f),"좌표 왕복 오류");
  Check(parsed.Find("E3")==null&&parsed.Find(null)==null,"없는 ID 처리 오류");
 }

 static void FindRule(){
  var list=new List<EncounterDefinition>{
   new(){id="A",monsters=new(){1,2}},
   new(){id="B",monsters=new(){2,1,1}},
   new(){id="C",monsters=new(){2,1}}};
  Check(SpawnLayoutResolver.FindEncounterId(new List<int>{2,1},list)=="C","순서 일치 우선 오류");
  Check(SpawnLayoutResolver.FindEncounterId(new List<int>{1,2},list)=="A","순서 일치 오류");
  Check(SpawnLayoutResolver.FindEncounterId(new List<int>{1,1,2},list)=="B","순서 무시 일치 오류");
  Check(SpawnLayoutResolver.FindEncounterId(new List<int>{1},list)==null,"없는 조합 처리 오류");
  Check(SpawnLayoutResolver.FindEncounterId(new List<int>(),list)==null&&SpawnLayoutResolver.FindEncounterId(null,list)==null,"빈 목록 처리 오류");
 }

 static void ResolveRule(){
  var entry=new SpawnLayoutEntry{id="A",slots=new(){new(-200,0),new(-500,40)}};
  var slots=SpawnLayoutResolver.ResolveSlots(entry,SceneDefaults);
  Check(slots.Count==4,"칸 수 오류");
  Check(Near(slots[0].x,-200)&&Near(slots[1].x,-500)&&Near(slots[1].y,40),"조우 좌표 적용 오류");
  Check(Near(slots[2].x,-445)&&Near(slots[3].x,-605),"부족한 칸 기본 좌표 오류");
  slots[0].x=0;
  Check(Near(entry.slots[0].x,-200),"원본 좌표가 바뀜");
  var none=SpawnLayoutResolver.ResolveSlots(null,SceneDefaults);
  Check(none.Select(s=>s.x).SequenceEqual(SceneDefaults.Select(s=>s.x)),"항목 없음 기본 좌표 오류");
  Check(SpawnLayoutResolver.SummonSlotOrder(2,4).SequenceEqual(new[]{2,3,0,1}),"소환 순서 오류");
  Check(SpawnLayoutResolver.SummonSlotOrder(4,4).SequenceEqual(new[]{0,1,2,3}),"가득 찬 조우 소환 순서 오류");
  Check(SpawnLayoutResolver.SummonSlotOrder(0,3).SequenceEqual(new[]{0,1,2}),"빈 조우 소환 순서 오류");
 }

 static void Stored(string jsonPath,List<EncounterDefinition> encounters,HashSet<int> summoners,int maxEnemies){
  Check(File.Exists(jsonPath),"spawnLayouts.json 없음 (Run.ps1 -Write로 생성)");
  string text=File.ReadAllText(jsonPath);
  byte[] bytes=File.ReadAllBytes(jsonPath);
  Check(!(bytes.Length>=3&&bytes[0]==0xEF&&bytes[1]==0xBB&&bytes[2]==0xBF),"spawnLayouts.json BOM 있음");
  SpawnLayoutData data=Parse(text);
  Check(data.version==SpawnLayoutData.CurrentVersion,$"버전 오류 {data.version}");
  Check(data.ToJson()==text.Replace("\r\n","\n"),"spawnLayouts.json 서식이 ToJson과 다름");
  var ids=data.layouts.Select(l=>l.id).ToList();
  Check(ids.Count==ids.Distinct().Count(),"중복 ID: "+string.Join(",",ids.GroupBy(i=>i).Where(g=>g.Count()>1).Select(g=>g.Key)));
  var expected=ExpectedSlotCounts(encounters,summoners,maxEnemies);
  foreach(var pair in expected){
   SpawnLayoutEntry entry=data.Find(pair.Key);
   Check(entry!=null,$"{pair.Key} 항목 없음 (Run.ps1 -Write로 채움)");
   Check(entry.slots.Count==pair.Value,$"{pair.Key} 칸 수 {entry.slots.Count} (필요 {pair.Value})");
   for(int i=0;i<entry.slots.Count;i++){
    SpawnSlot s=entry.slots[i];
    Check(s.x>ScreenCenterX&&s.x<=0,$"{pair.Key} {i}번 칸 x {s.x}가 화면 오른쪽 절반 밖");
    for(int j=0;j<i;j++){
     SpawnSlot o=entry.slots[j];
     if(Math.Abs(o.y-s.y)<20f&&Math.Abs(o.x-s.x)<HealthBarWidth)warnings.Add($"{pair.Key} {j}번 · {i}번 칸 체력 바 겹침 (x 간격 {Math.Abs(o.x-s.x)})");
    }
   }
  }
  foreach(string id in ids)Check(expected.ContainsKey(id),$"모르는 ID {id}");
  Check(summoners.Count>0,"소환 몬스터를 찾지 못함");
 }

 // 소환 몬스터가 있는 조우는 상한만큼, 나머지는 몬스터 수만큼, 허수아비 1칸
 static Dictionary<string,int> ExpectedSlotCounts(List<EncounterDefinition> encounters,HashSet<int> summoners,int maxEnemies){
  var result=new Dictionary<string,int>();
  foreach(var e in encounters)result[e.id]=e.monsters.Any(summoners.Contains)?Math.Max(maxEnemies,e.monsters.Count):e.monsters.Count;
  result[SpawnLayoutData.RankingScareCrowId]=1;
  return result;
 }

 static int Fill(SpawnLayoutData data,List<EncounterDefinition> encounters,HashSet<int> summoners,int maxEnemies){
  int added=0;
  var ordered=new List<SpawnLayoutEntry>();
  foreach(var pair in ExpectedSlotCounts(encounters,summoners,maxEnemies)){
   SpawnLayoutEntry entry=data.Find(pair.Key);
   if(entry==null){
    entry=new SpawnLayoutEntry{id=pair.Key,slots=new()};
    added++;
   }
   for(int i=entry.slots.Count;i<pair.Value;i++){
    SpawnSlot d=SceneDefaults[Math.Min(i,SceneDefaults.Length-1)];
    entry.slots.Add(new SpawnSlot(d.x,d.y));
   }
   ordered.Add(entry);
  }
  // 조우 데이터 순서로 정렬, 모르는 항목은 뒤에 유지
  ordered.AddRange(data.layouts.Where(l=>!ordered.Contains(l)));
  data.layouts=ordered;
  return added;
 }

 static SpawnLayoutData Parse(string json)=>JsonSerializer.Deserialize<SpawnLayoutData>(json.TrimStart('\uFEFF'),Options);

 static List<EncounterDefinition> LoadEncounters(string root){
  string json=File.ReadAllText(Path.Combine(root,"Assets/Resources/JsonData/encounters.json"));
  var data=JsonSerializer.Deserialize<EncounterDefinitionRoot>(json.TrimStart('\uFEFF'),Options);
  Check(data.encounters.Count>0,"조우 데이터 없음");
  return data.encounters;
 }

 // 행동 목록 어디든 summon이 있는 몬스터 ID
 static HashSet<int> LoadSummoners(string root){
  string json=File.ReadAllText(Path.Combine(root,"Assets/Resources/JsonData/monsters.json"));
  using var doc=JsonDocument.Parse(json.TrimStart('\uFEFF'));
  var result=new HashSet<int>();
  foreach(var m in doc.RootElement.GetProperty("monsters").EnumerateArray())
   if(HasSummon(m))result.Add(m.GetProperty("id").GetInt32());
  return result;
 }

 static bool HasSummon(JsonElement e){
  if(e.ValueKind==JsonValueKind.Object){
   if(e.TryGetProperty("type",out var t)&&t.ValueKind==JsonValueKind.String&&t.GetString()==MonsterActionDefinition.Summon)return true;
   foreach(var p in e.EnumerateObject())if(HasSummon(p.Value))return true;
  }else if(e.ValueKind==JsonValueKind.Array){
   foreach(var c in e.EnumerateArray())if(HasSummon(c))return true;
  }
  return false;
 }

 static int LoadMaxEnemies(string root){
  string json=File.ReadAllText(Path.Combine(root,"Assets/Resources/JsonData/rules.json"));
  using var doc=JsonDocument.Parse(json.TrimStart('\uFEFF'));
  int value=doc.RootElement.GetProperty("combat").GetProperty("maxEnemies").GetInt32();
  Check(value>0&&value<=SceneDefaults.Length,$"maxEnemies {value}가 씬 스폰 위치 수 밖");
  return value;
 }

 static bool Near(float a,float b)=>Math.Abs(a-b)<0.001f;
}
