using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
// 로컬라이징 코어 검증: 조사 · 복수형 · 문구 해석 · 언어 목록 · 폴백
class Program {
 static int count;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
 static void Equal(string actual,string expected,string message)=>Check(actual==expected,$"{message} (기대 [{expected}], 실제 [{actual}])");
 static void Main(string[] args){
  Josa();
  Plural();
  Format();
  References();
  Catalog();
  LanguageList(args[0]);
  SyncCore();
  Csv();
  SourceTables(args[0]);
  Console.WriteLine($"로컬라이징 검증 {count}개 통과");
 }

 static Dictionary<string,object> A(params object[] pairs){
  var map=new Dictionary<string,object>();
  for(int i=0;i<pairs.Length;i+=2)map[(string)pairs[i]]=pairs[i+1];
  return map;
 }

 static void Josa(){
  Equal(KoreanJosa.Attach("부식","를"),"부식을","받침 있음 을");
  Equal(KoreanJosa.Attach("보호","을"),"보호를","받침 없음 를");
  Equal(KoreanJosa.Attach("결속","이(가)"),"결속이","이(가) 형태");
  Equal(KoreanJosa.Attach("재생","(으)로"),"재생으로","으로");
  Equal(KoreanJosa.Attach("물","으로"),"물로","ㄹ 받침은 로");
  Equal(KoreanJosa.Attach("카드","으로"),"카드로","받침 없음 로");
  Equal(KoreanJosa.Attach("힘","와"),"힘과","과");
  Equal(KoreanJosa.Attach("방어","은"),"방어는","는");
  Equal(KoreanJosa.Attach("<color=#FF2400>부식</color>","를"),"<color=#FF2400>부식</color>을","리치 텍스트 태그 무시");
  Equal(KoreanJosa.Attach("강화: 포션","을"),"강화: 포션을","콜론 포함 이름");
  Equal(KoreanJosa.Attach("3","를"),"3을","숫자 3 삼");
  Equal(KoreanJosa.Attach("2","을"),"2를","숫자 2 이");
  Equal(KoreanJosa.Attach("10","를"),"10을","숫자 10 십");
  Equal(KoreanJosa.Attach("1","으로"),"1로","숫자 1 일 ㄹ 받침");
  Equal(KoreanJosa.Attach("6","로"),"6으로","숫자 6 육");
  Equal(KoreanJosa.Attach("변이 꽃 I","를"),"변이 꽃 I을","로마 숫자 I 일");
  Equal(KoreanJosa.Attach("변이 꽃 II","을"),"변이 꽃 II를","로마 숫자 II 이");
  Equal(KoreanJosa.Attach("변이 꽃 III","를"),"변이 꽃 III을","로마 숫자 III 삼");
  Equal(KoreanJosa.Attach("변이 꽃 IV","을"),"변이 꽃 IV를","로마 숫자 IV 사");
  Equal(KoreanJosa.Attach("Joker","을"),"Joker을","판정 불가 단어는 원래 조사");
  Equal(KoreanJosa.Attach("Joker","을(를)"),"Joker을(를)","판정 불가 단어는 병기 유지");
  Equal(KoreanJosa.Attach("(부식)","를"),"(부식)을","닫는 괄호 무시");
  Equal(KoreanJosa.Select("",""),"","빈 조사");
  Check(KoreanJosa.MatchJosaAt("을(를) 얻습니다",0,out var josa)==4&&josa=="을(를)","병기 형태를 먼저 인식");
  Check(KoreanJosa.MatchJosaAt("으로 바꿉니다",0,out josa)==2&&josa=="으로","으로 인식");
  Check(KoreanJosa.MatchJosaAt(" 얻습니다",0,out josa)==0,"조사 없음");
 }

 static void Plural(){
  string[] expectRu={"many","one","few","few","few","many","many","many","many","many","many","many","many","many","many","many","many","many","many","many","many","one","few"};
  for(int n=0;n<expectRu.Length;n++)Equal(PluralRules.GetCategory("ru",n),expectRu[n],$"러시아어 {n}");
  Equal(PluralRules.GetCategory("ru",111),"many","러시아어 111");
  Equal(PluralRules.GetCategory("ru",101),"one","러시아어 101");
  Equal(PluralRules.GetCategory("ru",112),"many","러시아어 112");
  Equal(PluralRules.GetCategory("ru",122),"few","러시아어 122");
  Equal(PluralRules.GetCategory("en",1),"one","영어 1");
  Equal(PluralRules.GetCategory("en",0),"other","영어 0");
  Equal(PluralRules.GetCategory("en",2),"other","영어 2");
  Equal(PluralRules.GetCategory("de",1),"one","독일어 1");
  Equal(PluralRules.GetCategory("fr",0),"one","프랑스어 0은 단수");
  Equal(PluralRules.GetCategory("fr",1),"one","프랑스어 1");
  Equal(PluralRules.GetCategory("fr",2),"other","프랑스어 2");
  foreach(var code in new[]{"ko","ja","zh-Hans","zh-Hant"})Equal(PluralRules.GetCategory(code,1),"other",$"{code} 구분 없음");
  Equal(PluralRules.GetCategory("pl",22),"few","폴란드어 22");
  Equal(PluralRules.GetCategory("pl",25),"many","폴란드어 25");
  Equal(PluralRules.GetCategory("xx",1),"one","미등록 언어는 영어 규칙");
  Equal(PluralRules.GetPrimaryLanguage("zh-Hans"),"zh","기본 언어 추출");
  Equal(PluralRules.GetCategory("en",-1),"one","음수는 절댓값");
 }

 static void Format(){
  string draw="Draw {n} {n, plural, one {card} other {cards}}.";
  Equal(LocalizedTextFormatter.Format(draw,"en",A("n",1)),"Draw 1 card.","영어 단수");
  Equal(LocalizedTextFormatter.Format(draw,"en",A("n",3)),"Draw 3 cards.","영어 복수");
  string ru="Вы добираете {n} {n, plural, one {карту} few {карты} many {карт} other {карт}}.";
  Equal(LocalizedTextFormatter.Format(ru,"ru",A("n",1)),"Вы добираете 1 карту.","러시아어 1");
  Equal(LocalizedTextFormatter.Format(ru,"ru",A("n",2)),"Вы добираете 2 карты.","러시아어 2");
  Equal(LocalizedTextFormatter.Format(ru,"ru",A("n",5)),"Вы добираете 5 карт.","러시아어 5");
  Equal(LocalizedTextFormatter.Format(ru,"ru",A("n",21)),"Вы добираете 21 карту.","러시아어 21");
  // # 치환과 =N 우선
  string hits="Deals {damage} damage{hits, plural, =1 {} other { # times}}.";
  Equal(LocalizedTextFormatter.Format(hits,"en",A("damage",6,"hits",1)),"Deals 6 damage.","=1 분기");
  Equal(LocalizedTextFormatter.Format(hits,"en",A("damage",6,"hits",3)),"Deals 6 damage 3 times.","# 치환");
  // 중첩 복수형의 #은 안쪽 값
  string nested="{a, plural, other {# / {b, plural, one {# card} other {# cards}}}}";
  Equal(LocalizedTextFormatter.Format(nested,"en",A("a",2,"b",1)),"2 / 1 card","중첩 복수형");
  // 선택
  string select="{target, select, all {all enemies} other {an enemy}}";
  Equal(LocalizedTextFormatter.Format(select,"en",A("target","all")),"all enemies","선택 분기");
  Equal(LocalizedTextFormatter.Format(select,"en",A("target","one")),"an enemy","선택 other");
  // 인자가 없는 자리표시는 원문 유지 (카드 {attack} · 버프 {stack장} · {동력}을)
  Equal(LocalizedTextFormatter.Format("피해를 {attack} 줍니다.","ko"),"피해를 {attack} 줍니다.","인자 없는 자리표시 유지");
  Equal(LocalizedTextFormatter.Format("{stack장}을 뽑습니다.","ko",A("other",1)),"{stack장}을 뽑습니다.","단위 붙은 자리표시 유지");
  Equal(LocalizedTextFormatter.Format("{동력}을 얻습니다.","ko"),"{동력}을 얻습니다.","동적 이름 자리표시 유지");
  Equal(LocalizedTextFormatter.Format("{n, plural, one {card} other {cards}}","en"),"{n, plural, one {card} other {cards}}","인자 없는 복수형 유지");
  // 한국어 인자 뒤 조사
  Equal(LocalizedTextFormatter.Format("{name}를 1마리 소환합니다.","ko",A("name","변이 꽃 I")),"변이 꽃 I을 1마리 소환합니다.","인자 뒤 조사 보정");
  Equal(LocalizedTextFormatter.Format("{name}을(를) 소환합니다.","ko",A("name","허수아비")),"허수아비를 소환합니다.","병기 조사 보정");
  Equal(LocalizedTextFormatter.Format("{name}을 얻습니다.","en",A("name","부식")),"부식을 얻습니다.","한국어가 아니면 조사 보정 안 함");
  // 숫자 · 문자열 형식
  Equal(LocalizedTextFormatter.Format("{v}","en",A("v",2.5f)),"2.5","실수는 고정 형식");
  Equal(LocalizedTextFormatter.Format("{n, plural, one {#} other {#}}","en",A("n","2")),"2","문자열 숫자");
  Equal(LocalizedTextFormatter.Format("{n, plural, one {x}}","en",A("n",2)),"{n, plural, one {x}}","분기가 없으면 원문");
  // 프랑스어 작은따옴표는 그대로
  Equal(LocalizedTextFormatter.Format("Gagnez {n} d'Armure.","fr",A("n",5)),"Gagnez 5 d'Armure.","작은따옴표 유지");
  Equal(LocalizedTextFormatter.Format("","en"),"","빈 문구");
  Equal(LocalizedTextFormatter.Format(null,"en"),"","null 문구");
  Equal(LocalizedTextFormatter.Format("no tokens","en"),"no tokens","토큰 없음");
  Equal(LocalizedTextFormatter.Format("{broken","en"),"{broken","닫히지 않은 중괄호");
  // 인자 이름 추출
  var names=LocalizedTextFormatter.ExtractArgumentNames(hits);
  Check(names.SetEquals(new[]{"damage","hits"}),"인자 이름 추출");
  Check(LocalizedTextFormatter.ExtractArgumentNames(nested).SetEquals(new[]{"a","b"}),"중첩 인자 이름 추출");
 }

 static bool Resolve(LocalizedReference reference,out string text){
  var names=new Dictionary<int,string>{{4001,"부식"},{3012,"결속"},{3031,"보호"}};
  if(reference.Kind==LocalizedReference.Buff&&names.TryGetValue(reference.Id,out var name)){
   text=reference.HasDisplayOverride?reference.DisplayOverride:name;
   text=$"<color=#26619C>{text}</color>";
   return true;
  }
  text=null;
  return false;
 }

 static void References(){
  Equal(LocalizedTextFormatter.Format("[buff:4001]를 3 부여합니다.","ko",null,Resolve),"<color=#26619C>부식</color>을 3 부여합니다.","참조 해석 + 조사");
  Equal(LocalizedTextFormatter.Format("[buff:3031]을 얻습니다.","ko",null,Resolve),"<color=#26619C>보호</color>를 얻습니다.","받침 없는 참조");
  Equal(LocalizedTextFormatter.Format("Накладывает 3 [buff:4001|коррозии].","ru",null,Resolve),"Накладывает 3 <color=#26619C>коррозии</color>.","표시 문구 덮어쓰기");
  Equal(LocalizedTextFormatter.Format("[buff:9999]","en",null,Resolve),"[buff:9999]","해석 못 한 참조는 원문");
  Equal(LocalizedTextFormatter.Format("[buff:9999|word]","en",null,Resolve),"word","해석 못 해도 표시 문구는 출력");
  Equal(LocalizedTextFormatter.Format("[buff:4001]","en"),"[buff:4001]","해석기 없으면 원문");
  Equal(LocalizedTextFormatter.Format("[not a token] [Buff:1] [buff:x]","en",null,Resolve),"[not a token] [Buff:1] [buff:x]","토큰 형식이 아니면 그대로");
  Equal(LocalizedTextFormatter.Format("{n, plural, one {[buff:3012]} other {[buff:3012] ×#}}","en",A("n",2),Resolve),"<color=#26619C>결속</color> ×2","복수형 안의 참조");
  var refs=LocalizedTextFormatter.ExtractReferences("[buff:4001]를 주고 [keyword:2] [buff:3012|x] [card:101010]");
  Check(refs.Count==4,"참조 추출 개수");
  Check(refs[0].Kind=="buff"&&refs[0].Id==4001&&!refs[0].HasDisplayOverride,"참조 추출 버프");
  Check(refs[1].Kind=="keyword"&&refs[1].Id==2,"참조 추출 키워드");
  Check(refs[2].DisplayOverride=="x","참조 추출 표시 문구");
  Check(refs[3].Kind=="card"&&refs[3].Id==101010,"참조 추출 카드");
 }

 static LocalizationLanguageList SampleList()=>new LocalizationLanguageList{
  source="ko",defaultLanguage="en",
  languages=new List<LocalizationLanguageInfo>{
   new(){code="ko",systemLanguages=new(){"Korean"}},
   new(){code="en",systemLanguages=new(){"English"}},
   new(){code="zh-Hant",systemLanguages=new(){"ChineseTraditional"},fallback=new(){"zh-Hans"}},
   new(){code="zh-Hans",systemLanguages=new(){"ChineseSimplified"},fallback=new(){"en"}},
   new(){code="fr",systemLanguages=new(){"French"},fallback=new(){"en"},enabled=false},
   new(){code="loop",fallback=new(){"loop","en"}},
  }};

 static void Catalog(){
  var catalog=new LocalizationCatalog(SampleList());
  catalog.AddEntries("ko",new Dictionary<string,string>{{"a","가"},{"b","나"},{"c","다"}});
  catalog.AddEntries("en",new Dictionary<string,string>{{"a","A"},{"b",""}});
  catalog.AddEntries("zh-hans",new Dictionary<string,string>{{"a","甲"}});
  Check(catalog.TryGetText("en","a",out var t)&&t=="A","직접 조회");
  Check(catalog.TryGetText("en","b",out t,out var found)&&t=="나"&&found=="ko","빈 문자열은 폴백");
  Check(catalog.TryGetText("zh-Hant","a",out t,out found)&&t=="甲"&&found=="zh-Hans","번체 → 간체 폴백");
  Check(catalog.TryGetText("zh-Hant","c",out t)&&t=="다","폴백 끝은 원문 언어");
  Check(!catalog.TryGetText("en","zzz",out t),"없는 키");
  Check(string.Join(",",catalog.GetFallbackChain("zh-Hant"))=="zh-Hant,zh-Hans,en,ko","폴백 순서");
  Check(string.Join(",",catalog.GetFallbackChain("loop"))=="loop,en,ko","순환 폴백 제거");
  Check(catalog.NormalizeCode("ZH-hant")=="zh-Hant","코드 정규화");
  Check(catalog.NormalizeCode("es")==null,"미등록 코드");
  Check(catalog.ResolveSystemLanguage("ChineseTraditional")=="zh-Hant","시스템 언어 매핑");
  Check(catalog.ResolveSystemLanguage("French")=="en","비활성 언어는 기본 언어로");
  Check(catalog.ResolveSystemLanguage("Spanish")=="en","목록에 없는 시스템 언어는 기본 언어");
  Check(catalog.DefaultLanguage=="en","기본 언어");
  Check(catalog.TryGetOwnText("zh-Hans","a",out t)&&!catalog.TryGetOwnText("zh-Hant","a",out t),"자기 표만 조회");
  catalog.AddEntries("ko",new Dictionary<string,string>{{"a","갑"}});
  Check(catalog.TryGetText("ko","a",out t)&&t=="갑","같은 키는 덮어씀");
  Check(catalog.HasEntries("en")&&!catalog.HasEntries("fr"),"표 유무");
  catalog.ClearEntries("en");
  Check(!catalog.HasEntries("en"),"표 비우기");
  var noDefault=new LocalizationCatalog(new LocalizationLanguageList{source="ko",defaultLanguage="fr",languages=SampleList().languages});
  Check(noDefault.DefaultLanguage=="ko","기본 언어가 비활성이면 원문 언어");
  var empty=new LocalizationCatalog(null);
  Check(empty.Languages.Count==0&&string.Join(",",empty.GetFallbackChain("en"))=="en,ko","빈 목록");
 }

 // 실제 languages.json 형식 · 내용 검증
 static void LanguageList(string root){
  string path=Path.Combine(root,"Assets/Resources/Localization/languages.json");
  var options=new JsonSerializerOptions{IncludeFields=true,PropertyNameCaseInsensitive=false};
  var list=JsonSerializer.Deserialize<LocalizationLanguageList>(File.ReadAllText(path),options);
  Check(list!=null&&list.languages.Count>=6,"languages.json 읽기");
  var catalog=new LocalizationCatalog(list);
  Check(catalog.SourceLanguage=="ko","원문 언어 ko");
  string[] required={"ko","en","ja","zh-Hans","zh-Hant","ru"};
  foreach(var code in required){
   var language=catalog.FindLanguage(code);
   Check(language!=null&&language.enabled,$"{code} 사용 가능");
   Check(!string.IsNullOrWhiteSpace(language.nativeName),$"{code} 표시 이름");
   Check(catalog.GetFallbackChain(code).Last()=="ko",$"{code} 폴백 끝은 ko");
  }
  Check(list.languages.Select(l=>l.code).Distinct(StringComparer.OrdinalIgnoreCase).Count()==list.languages.Count,"언어 코드 중복 없음");
  foreach(var language in list.languages)
   foreach(var fallback in language.fallback)
    Check(catalog.FindLanguage(fallback)!=null,$"{language.code} 폴백 {fallback} 등록됨");
  // 기존 저장 값 호환 (PlayerPrefs Localization.Language)
  foreach(var saved in new[]{"ko","en","ja","zh-Hant","zh-Hans","ru"})Check(catalog.NormalizeCode(saved)==saved,$"기존 저장 값 {saved}");
  Check(catalog.ResolveSystemLanguage("Korean")=="ko"&&catalog.ResolveSystemLanguage("Japanese")=="ja","시스템 언어 ko · ja");
 }

 static void SyncCore(){
  var names=new Dictionary<string,int>{{"부식",4001},{"강화: 포션",1002}};
  var report=new LocalizationSyncReport();
  Equal(LocalizationSourceSync.ConvertSourceText("(부식)을 3 부여합니다.",names,report,"t"),"[buff:4001]을 3 부여합니다.","괄호 버프 → ID");
  Equal(LocalizationSourceSync.ConvertSourceText("(강화: 포션)을 얻습니다. (턴당 1회)",names,report,"t"),"[buff:1002]을 얻습니다. (턴당 1회)","버프가 아닌 괄호 유지");
  Check(report.KeptParentheses.Count==1&&report.KeptParentheses[0].Contains("턴당 1회"),"유지한 괄호 보고");
  Equal(LocalizationSourceSync.ConvertSourceText("{동력}을 2 얻습니다. 피해를 {attack} 줍니다.",names,report,"t"),"{runeBuff}을 2 얻습니다. 피해를 {attack} 줍니다.","동적 이름 키 변환, 수치 자리표시 유지");
  Equal(LocalizationSourceSync.ConvertSourceText(null,names,report,"t"),"","null 원문");
  Equal(LocalizationSourceSync.GetTableName("card.1.name"),"cards","카드 표");
  Equal(LocalizationSourceSync.GetTableName("buff.1.desc"),"buffs","버프 표");
  Equal(LocalizationSourceSync.GetTableName("monster.101.name"),"monsters","몬스터 표");
  Equal(LocalizationSourceSync.GetTableName("intent.attack.name"),"intents","의도 표");
  Equal(LocalizationSourceSync.GetTableName("ui.endTurn"),"ui","UI 표");
  string hash=LocalizationSourceSync.ComputeHash("피해를 {attack} 줍니다.");
  Check(hash.Length==12&&hash==LocalizationSourceSync.ComputeHash("피해를 {attack} 줍니다.")&&hash!=LocalizationSourceSync.ComputeHash("피해를 {attack} 입힙니다."),"원문 해시");
  Check(LocalizationSourceSync.ResolveStatus("a","",hash)==TranslationStatus.Missing,"번역 없음");
  Check(LocalizationSourceSync.ResolveStatus("a","A",null)==TranslationStatus.Unverified,"해시 기록 없음");
  Check(LocalizationSourceSync.ResolveStatus("a","A",LocalizationSourceSync.ComputeHash("a"))==TranslationStatus.UpToDate,"최신");
  Check(LocalizationSourceSync.ResolveStatus("b","A",LocalizationSourceSync.ComputeHash("a"))==TranslationStatus.Stale,"원문 변경");
  string json=LocalizationSourceSync.SerializeTable(new SortedDictionary<string,string>(StringComparer.Ordinal){{"a","\"인용\" d'Armure\n줄"},{"b","카드"}});
  Check(!json.Contains("\r")&&json.EndsWith("}\n")&&json.Contains("    \"a\": "),"표 직렬화 형식 (LF · 4칸)");
  var parsed=JsonSerializer.Deserialize<Dictionary<string,string>>(json);
  Check(parsed["a"]=="\"인용\" d'Armure\n줄"&&parsed["b"]=="카드","표 직렬화 왕복");
  Check(json.Contains("카드"),"한글을 이스케이프하지 않음");
 }

 static void Csv(){
  string[] values={"plain","콤마, 포함","\"따옴표\"","줄\n바꿈","",""};
  string line=string.Join(",",values.Select(LocalizationSourceSync.EscapeCsv));
  var records=LocalizationSourceSync.ParseCsv("﻿key,value\r\n"+line+"\r\nlast,1");
  Check(records.Count==3,"CSV 레코드 수");
  Check(records[0][0]=="key","BOM 제거");
  Check(records[1].SequenceEqual(values),"CSV 왕복");
  Check(records[2].SequenceEqual(new[]{"last","1"}),"마지막 줄바꿈 없음");
  Check(LocalizationSourceSync.ParseCsv("").Count==0,"빈 CSV");
 }

 // ko 생성 표가 JsonData와 같아야 함 (원문 수정 후 동기화를 빼먹으면 실패)
 static void SourceTables(string root){
  var report=new LocalizationSyncReport();
  var tables=LocalizationSourceSync.BuildSourceTables(root,report);
  Check(report.Warnings.Count==0,"원문 경고 없음: "+string.Join(" / ",report.Warnings));
  foreach(var table in tables){
   string path=Path.Combine(root,LocalizationSourceSync.LocalizationFolder,"ko",table.Key+".json");
   var onDisk=LocalizationSourceSync.ReadTable(path);
   var diff=table.Value.Where(e=>!onDisk.TryGetValue(e.Key,out var v)||v!=e.Value).Select(e=>e.Key).Concat(onDisk.Keys.Where(k=>!table.Value.ContainsKey(k))).ToList();
   Check(diff.Count==0,$"ko/{table.Key}.json이 JsonData와 다릅니다. Tools/Localization/한국어 원문 동기화 필요: {string.Join(", ",diff.Take(5))}");
  }
  Check(tables["cards"].Keys.Count(k=>k.EndsWith(".name"))==343,"카드 343장");
  Check(tables["buffs"].Keys.Count(k=>k.EndsWith(".name"))==115,"버프 115개");
  Check(tables["characters"].Keys.Count(k=>k.EndsWith(".identity"))==10,"아이덴티티 10개");
  // 모든 언어 표의 참조 버프는 존재하는 ID
  var buffIds=new HashSet<int>(tables["buffs"].Keys.Select(k=>int.Parse(k.Split('.')[1])));
  var keywordIds=new HashSet<int>(tables["keywords"].Keys.Select(k=>int.Parse(k.Split('.')[1])));
  foreach(var dir in Directory.GetDirectories(Path.Combine(root,LocalizationSourceSync.LocalizationFolder))){
   string code=Path.GetFileName(dir);
   if(code=="Cards")continue;
   foreach(var entry in LocalizationSourceSync.ReadLanguage(root,code))
    foreach(var reference in LocalizedTextFormatter.ExtractReferences(entry.Value)){
     if(reference.Kind==LocalizedReference.Buff)Check(buffIds.Contains(reference.Id),$"{code} {entry.Key}: 없는 버프 {reference.Id}");
     if(reference.Kind==LocalizedReference.Keyword)Check(keywordIds.Contains(reference.Id),$"{code} {entry.Key}: 없는 키워드 {reference.Id}");
    }
  }
 }
}
