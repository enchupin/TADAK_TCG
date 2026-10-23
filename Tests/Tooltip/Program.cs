using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
// 툴팁 고정 위치 계산 · 리치 텍스트 강조 · 버프 문구 포맷 검증
class Program {
 static int count;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
 static void Near(float actual,float expected,string message)=>Check(Math.Abs(actual-expected)<0.001f,$"{message} (기대 {expected}, 실제 {actual})");
 static void Main(string[] args){
  Layout();
  RichText();
  BuffFormat();
  BuffData(args[0]);
  LocalizedBuffTooltips(args[0]);
  IntentFormat();
  Console.WriteLine($"툴팁 검증 {count}개 통과");
 }

 static void Layout(){
  // 화면 -400 ~ 400, -300 ~ 300 (캔버스 중앙 피벗)
  var screen=new TooltipRect(-400,-300,400,300);
  var icon=new TooltipRect(-100,100,-60,140);

  // 버프: 아이콘 하단, 좌측 맞춤
  var (x,y)=TooltipLayout.ResolveTopLeft(TooltipPlacement.Bottom,icon,200,80,4,screen,8);
  Near(x,-100,"하단 배치 좌측 맞춤");
  Near(y,96,"하단 배치 간격");

  // 의도: 영역 우측, 상단 맞춤
  (x,y)=TooltipLayout.ResolveTopLeft(TooltipPlacement.Right,icon,200,80,6,screen,8);
  Near(x,-54,"우측 배치 간격");
  Near(y,140,"우측 배치 상단 맞춤");

  // 화면 오른쪽을 넘으면 방향은 유지하고 안으로 밀어 넣음
  var rightEdge=new TooltipRect(300,0,350,40);
  (x,y)=TooltipLayout.ResolveTopLeft(TooltipPlacement.Right,rightEdge,200,80,6,screen,8);
  Near(x,192,"우측 경계 클램프");
  Near(y,40,"우측 경계에서 세로 위치 유지");

  // 화면 아래를 넘으면 위로 밀어 넣음
  var bottomEdge=new TooltipRect(-100,-280,-60,-240);
  (x,y)=TooltipLayout.ResolveTopLeft(TooltipPlacement.Bottom,bottomEdge,200,80,4,screen,8);
  Near(y,-212,"하단 경계 클램프");

  // 화면 위를 넘으면 아래로
  var topEdge=new TooltipRect(-100,250,-60,320);
  (x,y)=TooltipLayout.ResolveTopLeft(TooltipPlacement.Right,topEdge,200,80,6,screen,8);
  Near(y,292,"상단 경계 클램프");

  // 화면 왼쪽을 넘으면 오른쪽으로
  var leftEdge=new TooltipRect(-420,0,-380,40);
  (x,y)=TooltipLayout.ResolveTopLeft(TooltipPlacement.Bottom,leftEdge,200,80,4,screen,8);
  Near(x,-392,"좌측 경계 클램프");

  // 툴팁이 화면보다 크면 좌측 · 상단 우선
  (x,y)=TooltipLayout.ResolveTopLeft(TooltipPlacement.Bottom,icon,1000,900,4,screen,8);
  Near(x,-392,"큰 툴팁 좌측 우선");
  Near(y,292,"큰 툴팁 상단 우선");

  // 뒤집힌 좌표도 정규화
  var flipped=new TooltipRect(-60,140,-100,100);
  (x,y)=TooltipLayout.ResolveTopLeft(TooltipPlacement.Bottom,flipped,200,80,4,screen,8);
  Near(x,-100,"사각형 정규화 x");
  Near(y,96,"사각형 정규화 y");
 }

 static void RichText(){
  var colors=TooltipColors.Default;
  Check(colors.BeneficialHex=="26619C"&&colors.HarmfulHex=="FF2400"&&colors.IntentHex=="808000"&&colors.ValueHex=="FBCEB1","기본 색상 라피스 · 스칼렛 · 올리브 · 아프리콧");
  Check(colors.InactiveHex=="808080"&&TooltipRichText.Inactive("1",colors)=="<color=#808080>1</color>","미달성 · 수령 완료 회색");
  Check(TooltipRichText.EffectName("힘",false,colors)=="<color=#26619C>힘</color>","이로운 효과 이름 라피스");
  Check(TooltipRichText.EffectName("부식",true,colors)=="<color=#FF2400>부식</color>","해로운 효과 이름 스칼렛");
  Check(TooltipRichText.IntentName("공격",colors)=="<color=#808000>공격</color>","의도 이름 올리브");
  Check(TooltipRichText.Value(3,colors)=="<color=#FBCEB1>3</color>","수치 아프리콧");
  Check(TooltipRichText.Value(-2,colors)=="<color=#FBCEB1>-2</color>","음수 수치");
  Check(TooltipRichText.Colorize("",colors.ValueHex)==""&&TooltipRichText.Colorize(null,colors.ValueHex)=="","빈 문자열은 태그 없음");
  Check(TooltipRichText.Colorize("힘",null)=="힘","색 없으면 원문");
 }

 static string V(int n)=>$"<color=#FBCEB1>{n}</color>";
 static string B(string n)=>$"<color=#26619C>{n}</color>";
 static string H(string n)=>$"<color=#FF2400>{n}</color>";

 static void BuffFormat(){
  var c=TooltipColors.Default;
  BuffTooltipFormatter.ReferenceLookup refs=(string name,out bool harmful)=>{harmful=name=="부식"||name=="발화";return name is "부식" or "발화" or "힘" or "관통";};
  BuffTooltipFormatter.DynamicNameLookup guard=(string key,out string name,out bool harmful)=>{name=key=="동력"?"우피: 보호":null;harmful=false;return name!=null;};
  BuffTooltipFormatter.DynamicNameLookup rune=(string key,out string name,out bool harmful)=>{name=key=="동력"?"동력":null;harmful=false;return name!=null;};
  BuffTooltipFormatter.DynamicNameLookup attack=(string key,out string name,out bool harmful)=>{name=key=="동력"?"우피: 공격":null;harmful=false;return name!=null;};

  // 요구 예시: 힘: 공격 피해가 3 증가합니다.
  Check(BuffTooltipFormatter.FormatEntry("힘","공격 피해가 {stack} 증가합니다.",3,false,c)==$"{B("힘")}\n공격 피해가 {V(3)} 증가합니다.","힘 예시 (이름 한 줄)");
  Check(BuffTooltipFormatter.FormatEntry("힘","공격 피해가 {stack} 증가합니다.",-2,false,c).Contains(V(-2)),"음수 중첩 그대로");
  Check(BuffTooltipFormatter.FormatEntry("부식","받는 피해가 25% 증가합니다.",2,true,c)==$"{H("부식")}\n받는 피해가 25% 증가합니다.","해로운 효과 이름 스칼렛, 고정 문구 수치는 그대로");
  Check(BuffTooltipFormatter.FormatDescription("카드를 {stack장} 뽑습니다.",2,c)==$"카드를 {V(2)}장 뽑습니다.","{stack장} 단위");
  Check(BuffTooltipFormatter.FormatDescription("공격을 {stack회} 회피합니다.",1,c)==$"공격을 {V(1)}회 회피합니다.","{stack회} 단위");
  Check(BuffTooltipFormatter.FormatDescription("적에게 (부식)을 {stack} 부여합니다.",3,c,refs)==$"적에게 {H("부식")}을 {V(3)} 부여합니다.","참조 버프 괄호 제거 · 극성 색");
  Check(BuffTooltipFormatter.FormatDescription("(힘)을 1 얻습니다.",0,c,refs)==$"{B("힘")}을 1 얻습니다.","이로운 참조 라피스");
  Check(BuffTooltipFormatter.FormatDescription("에너지를 2 얻습니다. (턴당 1회)",0,c,refs)=="에너지를 2 얻습니다. (턴당 1회)","버프가 아닌 괄호 문구 유지");
  Check(BuffTooltipFormatter.FormatDescription("(턴당 1회)",0,c,null)=="(턴당 1회)","조회 없으면 괄호 유지");
  // {동력}: 우피 모드에 따라 이름과 조사가 바뀜
  Check(BuffTooltipFormatter.FormatDescription("매 턴 시작 시 {동력}을 {stack} 얻습니다.",2,c,refs,rune)==$"매 턴 시작 시 {B("동력")}을 {V(2)} 얻습니다.","동력 상태");
  Check(BuffTooltipFormatter.FormatDescription("매 턴 시작 시 {동력}을 {stack} 얻습니다.",2,c,refs,guard)==$"매 턴 시작 시 {B("우피: 보호")}를 {V(2)} 얻습니다.","우피: 보호 상태 조사 보정");
  Check(BuffTooltipFormatter.FormatDescription("{동력}을 얻을 때 {stack} 더 얻습니다.",1,c,refs,attack)==$"{B("우피: 공격")}을 얻을 때 {V(1)} 더 얻습니다.","우피: 공격 상태");
  Check(BuffTooltipFormatter.FormatDescription("{동력}을 얻습니다.",1,c)=="동력을 얻습니다.","조회 없으면 중괄호만 제거");
  // 치환 결과가 다시 해석되지 않음
  Check(BuffTooltipFormatter.FormatDescription("(부식)과 (발화)",0,c,refs)==$"{H("부식")}과 {H("발화")}","여러 참조");
  // 로컬라이징 표의 ID 참조: 현재 언어 이름 · 극성 색 · 조사 보정, 표시 문구 덮어쓰기
  BuffTooltipFormatter.BuffIdLookup ids=(int id,out string name,out bool harmful)=>{
   name=id==4001?"부식":id==3031?"보호":id==4011?"Хрупкость":null;harmful=id==4001||id==4011;return name!=null;};
  Check(BuffTooltipFormatter.FormatDescription("적에게 [buff:4001]를 {stack} 부여합니다.",3,c,refs,null,ids)==$"적에게 {H("부식")}을 {V(3)} 부여합니다.","ID 참조 · 조사 보정");
  Check(BuffTooltipFormatter.FormatDescription("[buff:3031]을 얻습니다.",0,c,refs,null,ids)==$"{B("보호")}를 얻습니다.","ID 참조 받침 없음");
  Check(BuffTooltipFormatter.FormatDescription("Получает 2 [buff:4011|Хрупкости].",0,c,refs,null,ids)==$"Получает 2 {H("Хрупкости")}.","표시 문구 덮어쓰기 · 극성 색");
  Check(BuffTooltipFormatter.FormatDescription("[buff:9999]을 얻습니다.",0,c,refs,null,ids)=="[buff:9999]을 얻습니다.","조회 실패 시 원문");
  Check(BuffTooltipFormatter.FormatDescription("[buff:9999|word] x",0,c,refs,null,ids)=="word x","조회 실패 시 표시 문구");
  Check(BuffTooltipFormatter.FormatDescription("{runeBuff}을 {stack} 얻습니다.",2,c,refs,(string key,out string name,out bool harmful)=>{name=key=="runeBuff"?"우피: 보호":null;harmful=false;return name!=null;})==$"{B("우피: 보호")}를 {V(2)} 얻습니다.","runeBuff 동적 이름");
  Check(BuffTooltipFormatter.FormatEntry("족보","",0,false,c)==B("족보"),"설명 없으면 이름만");
  Check(BuffTooltipFormatter.FormatEntry("","설명",0,false,c)=="설명","이름 없으면 설명만");
  // 족보 한 줄: 이름: 조건 → 보상, 달성 조건은 기본 색 · 미달성은 회색, 구분자는 기본 색
  static string G(string t)=>$"<color=#808080>{t}</color>";
  Check(BuffTooltipFormatter.FormatProgressEntry("더블",new List<(string,bool)>{("?",false),("?",false)},new[]{"카드 1장"},false,c)
   ==$"{B("더블")}: {G("?")} · {G("?")} → 카드 1장","더블 기본");
  Check(BuffTooltipFormatter.FormatProgressEntry("더블",new List<(string,bool)>{("1",true),("1",false)},new[]{"카드 1장"},false,c)
   ==$"{B("더블")}: 1 · {G("1")} → 카드 1장","더블 진행 중");
  Check(BuffTooltipFormatter.FormatProgressEntry("풀하우스",new List<(string,bool)>{("3↑",false)},new[]{"1회 더 발동"},false,c)
   ==$"{B("풀하우스")}: {G("3↑")} → 1회 더 발동","풀하우스 기본");
  // 이미 받은 보상: 조건과 보상 모두 회색 (이름 · 구분자는 그대로)
  Check(BuffTooltipFormatter.FormatProgressEntry("풀하우스",new List<(string,bool)>{("3↑",true)},new[]{"1회 더 발동"},true,c)
   ==$"{B("풀하우스")}: {G("3↑")} → {G("1회 더 발동")}","풀하우스 완료");
  Check(BuffTooltipFormatter.FormatProgressEntry("스트레이트",new List<(string,bool)>{("1",true),("2",false),("3",true)},new[]{"에너지 2","카드 1장"},false,c)
   ==$"{B("스트레이트")}: 1 · {G("2")} · 3 → 에너지 2 · 카드 1장","스트레이트 진행 중");
  Check(BuffTooltipFormatter.FormatProgressEntry("스트레이트",new List<(string,bool)>{("1",true),("2",true),("3",true)},new[]{"에너지 2","카드 1장"},true,c)
   ==$"{B("스트레이트")}: {G("1")} · {G("2")} · {G("3")} → {G("에너지 2")} · {G("카드 1장")}","스트레이트 완료");
  Check(BuffTooltipFormatter.FormatProgressEntry("로열",new List<(string,bool)>{("이슬라",true),("잭",false),("크림",false)},new[]{"에너지 1"},false,c)
   ==$"{B("로열")}: 이슬라 · {G("잭")} · {G("크림")} → 에너지 1","로열 진행 중");
  // 조사 보정
  Check(BuffTooltipFormatter.AdjustJosa("보호","을")=="를"&&BuffTooltipFormatter.AdjustJosa("공격","를")=="을","을/를");
  Check(BuffTooltipFormatter.AdjustJosa("보호","이")=="가"&&BuffTooltipFormatter.AdjustJosa("동력","가")=="이","이/가");
  Check(BuffTooltipFormatter.AdjustJosa("보호","은")=="는"&&BuffTooltipFormatter.AdjustJosa("보호","과")=="와","은/는 · 과/와");
  Check(BuffTooltipFormatter.AdjustJosa("동력","로")=="으로"&&BuffTooltipFormatter.AdjustJosa("물결","으로")=="로"&&BuffTooltipFormatter.AdjustJosa("보호","으로")=="로","으로/로 (ㄹ 받침 포함)");
 }

 // buffs.json 전체: 남는 중괄호 없음, 괄호 참조는 버프 이름이거나 알려진 문구
 static void BuffData(string root){
  using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"Assets/Resources/JsonData/buffs.json")));
  var buffs=doc.RootElement.GetProperty("buffs").EnumerateArray().Select(b=>(
   id:b.GetProperty("buffId").GetInt32(),
   name:b.GetProperty("name").GetString(),
   description:b.GetProperty("description").GetString(),
   harmful:b.TryGetProperty("polarity",out var p)&&p.GetString()=="harmful")).ToList();
  var byName=buffs.GroupBy(b=>b.name).ToDictionary(g=>g.Key,g=>g.First());
  var c=TooltipColors.Default;
  var unresolved=new HashSet<string>();
  BuffTooltipFormatter.ReferenceLookup refs=(string name,out bool harmful)=>{
   harmful=false;
   if(!byName.TryGetValue(name,out var b)){unresolved.Add(name);return false;}
   harmful=b.harmful;return true;};
  BuffTooltipFormatter.DynamicNameLookup dynamic=(string key,out string name,out bool harmful)=>{name=key=="동력"?"동력":null;harmful=false;return name!=null;};
  foreach(var b in buffs){
   string text=BuffTooltipFormatter.FormatEntry(b.name,b.description,7,b.harmful,c,refs,dynamic);
   Check(!text.Contains('{')&&!text.Contains('}'),$"{b.id} 중괄호 남음: {text}");
   if(b.description.Contains("{stack"))Check(text.Contains(V(7)),$"{b.id} 중첩 수치 표시");
   Check(text.StartsWith(b.harmful?$"<color=#FF2400>":$"<color=#26619C>"),$"{b.id} 이름 극성 색");
  }
  // 버프가 아닌 괄호 문구만 남아야 함 (새 참조가 이름과 어긋나면 여기서 확인)
  var allowed=new HashSet<string>{"턴당 1회","강공"};
  var unexpected=unresolved.Where(n=>!allowed.Contains(n)).ToList();
  Check(unexpected.Count==0,"buffs.json에 없는 참조: "+string.Join(", ",unexpected));
 }

 // 모든 언어 버프 툴팁: 복수형 선해석 → 툴팁 포맷 후 남는 토큰 없음 (실행 중 BuffTooltipTextBuilder와 같은 순서)
 static void LocalizedBuffTooltips(string root){
  var meta=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"Assets/Resources/JsonData/buffs.json"))).RootElement.GetProperty("buffs").EnumerateArray()
   .ToDictionary(b=>b.GetProperty("buffId").GetInt32(),b=>b.TryGetProperty("polarity",out var p)&&p.GetString()=="harmful");
  var c=TooltipColors.Default;
  foreach(var dir in Directory.GetDirectories(Path.Combine(root,"Assets/Resources/Localization"))){
   string code=Path.GetFileName(dir);
   string path=Path.Combine(dir,"buffs.json");
   if(!File.Exists(path))continue;
   var table=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(path));
   BuffTooltipFormatter.BuffIdLookup ids=(int id,out string name,out bool harmful)=>{
    harmful=meta.TryGetValue(id,out var h)&&h;
    return table.TryGetValue($"buff.{id}.name",out name);};
   BuffTooltipFormatter.DynamicNameLookup dynamic=(string key,out string name,out bool harmful)=>{name=key=="runeBuff"||key=="동력"?"RUNE":null;harmful=false;return name!=null;};
   foreach(int stack in new[]{1,2,5,21}){
    foreach(var id in meta.Keys){
     if(!table.TryGetValue($"buff.{id}.desc",out var desc))continue;
     string pre=LocalizedTextFormatter.Format(desc,code,new Dictionary<string,object>{{"stack",stack}},null,false);
     string text=BuffTooltipFormatter.FormatEntry(table.GetValueOrDefault($"buff.{id}.name",""),pre,stack,meta[id],c,null,dynamic,ids);
     Check(!text.Contains('{')&&!text.Contains('}')&&!text.Contains("[buff:"),$"{code} {id} 툴팁에 토큰이 남음: {text}");
    }
   }
  }
 }

 static string O(string n)=>$"<color=#808000>{n}</color>";

 static void IntentFormat(){
  var c=TooltipColors.Default;
  BuffTooltipFormatter.ReferenceLookup refs=(string name,out bool harmful)=>{harmful=name=="부식";return name is "부식" or "힘";};
  // 행동 종류마다 한 줄, 이름 올리브 · 숫자 아프리콧
  var text=IntentTooltipFormatter.FormatLines(new List<(string,string)>{("공격","피해를 6씩 2회 입힙니다."),("방어","보호막을 5 얻습니다.")},c,refs);
  Check(text==$"{O("공격")}\n피해를 {V(6)}씩 {V(2)}회 입힙니다.\n\n{O("방어")}\n보호막을 {V(5)} 얻습니다.","의도 줄 형식 (이름 한 줄, 종류 사이 빈 줄)");
  // 버프 표기는 괄호 제거 · 극성 색, 숫자 색 태그 안의 16진수는 다시 해석하지 않음
  Check(IntentTooltipFormatter.FormatSentence("(부식)을 2 부여합니다.",c,refs)==$"{H("부식")}을 {V(2)} 부여합니다.","해로운 효과 부여");
  Check(IntentTooltipFormatter.FormatSentence("(힘)을 3 얻습니다.",c,refs)==$"{B("힘")}을 {V(3)} 얻습니다.","이로운 효과 획득");
  Check(IntentTooltipFormatter.FormatSentence("(4001)을 2 부여합니다.",c,refs)==$"(4001)을 {V(2)} 부여합니다.","버프가 아닌 괄호 유지");
  Check(IntentTooltipFormatter.FormatSentence("받는 피해 25%",c)==$"받는 피해 {V(25)}%","퍼센트 수치");
  // 로컬라이징 표의 ID 참조: ID 숫자는 수치로 보지 않고 현재 언어 이름 · 극성 색 · 조사 보정
  BuffTooltipFormatter.BuffIdLookup ids=(int id,out string name,out bool harmful)=>{name=id==4001?"부식":id==3012?"결속":null;harmful=id==4001;return name!=null;};
  Check(IntentTooltipFormatter.FormatSentence("[buff:4001]를 2 부여합니다.",c,refs,ids)==$"{H("부식")}을 {V(2)} 부여합니다.","ID 참조 해로운 효과");
  Check(IntentTooltipFormatter.FormatSentence("모든 아군이 [buff:3012]을 3 얻습니다.",c,refs,ids)==$"모든 아군이 {B("결속")}을 {V(3)} 얻습니다.","ID 참조 이로운 효과");
  Check(IntentTooltipFormatter.FormatSentence("Applies 2 [buff:4001|Corrosion].",c,refs,ids)==$"Applies {V(2)} {H("Corrosion")}.","ID 참조 표시 문구");
  Check(IntentTooltipFormatter.FormatSentence("[buff:9999]을 2 얻습니다.",c,refs,ids)==$"[buff:9999]을 {V(2)} 얻습니다.","조회 실패 시 토큰 유지");
  // 특수 패턴: 여러 의도 이름을 이어서 표시
  Check(IntentTooltipFormatter.FormatLines(new List<(string,string)>{("강화 · 약화","이로운 효과를 훔칩니다.")},c)==$"{O("강화 · 약화")}\n이로운 효과를 훔칩니다.","여러 의도 이름");
  Check(IntentTooltipFormatter.FormatLines(new List<(string,string)>{("","설명만")},c)=="설명만","이름 없으면 문장만");
  Check(IntentTooltipFormatter.FormatLines(new List<(string,string)>{("기절","")},c)==O("기절"),"문장 없으면 이름만");
  Check(IntentTooltipFormatter.FormatLines(new List<(string,string)>(),c)==""&&IntentTooltipFormatter.FormatLines(null,c)=="","빈 의도");
 }
}
