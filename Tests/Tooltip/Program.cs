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
  Check(BuffTooltipFormatter.FormatEntry("힘","공격 피해가 {stack} 증가합니다.",3,false,c)==$"{B("힘")}: 공격 피해가 {V(3)} 증가합니다.","힘 예시");
  Check(BuffTooltipFormatter.FormatEntry("힘","공격 피해가 {stack} 증가합니다.",-2,false,c).Contains(V(-2)),"음수 중첩 그대로");
  Check(BuffTooltipFormatter.FormatEntry("부식","받는 피해가 25% 증가합니다.",2,true,c)==$"{H("부식")}: 받는 피해가 25% 증가합니다.","해로운 효과 이름 스칼렛, 고정 문구 수치는 그대로");
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
  Check(BuffTooltipFormatter.FormatEntry("족보","",0,false,c)==B("족보"),"설명 없으면 이름만");
  Check(BuffTooltipFormatter.FormatEntry("","설명",0,false,c)=="설명","이름 없으면 설명만");
  // 조사 보정
  Check(BuffTooltipFormatter.AdjustJosa("보호","을")=="를"&&BuffTooltipFormatter.AdjustJosa("공격","를")=="을","을/를");
  Check(BuffTooltipFormatter.AdjustJosa("보호","이")=="가"&&BuffTooltipFormatter.AdjustJosa("동력","가")=="이","이/가");
  Check(BuffTooltipFormatter.AdjustJosa("보호","은")=="는"&&BuffTooltipFormatter.AdjustJosa("보호","과")=="와","은/는 · 과/와");
  Check(BuffTooltipFormatter.AdjustJosa("동력","로")=="으로"&&BuffTooltipFormatter.AdjustJosa("물결","으로")=="로"&&BuffTooltipFormatter.AdjustJosa("보호","으로")=="로","으로/로 (ㄹ 받침 포함)");
 }

 // buffs.json 전체: 남는 중괄호 없음, 괄호 참조는 버프 이름이거나 알려진 문구
 static void BuffData(string root){
  using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"Assets/Resources/Localization/buffs.json")));
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
}
