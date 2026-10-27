using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
// 몬스터 이미지 여백 측정 데이터 검증: 측정 규칙 · 이미지와 데이터 일치 · 몬스터별 이미지 경로
// 배치 설정 검증: 배치 계산 · monsterDisplay.json 서식 · 몬스터별 항목
// --write 인자를 주면 측정 데이터를 다시 생성
class Program {
 static int count;
 static readonly List<string> warnings=new();
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
 // 아직 이미지가 없는 몬스터 (경고만 출력)
 static readonly HashSet<string> PendingImages=new(){"ScareCrow"};
 // monsters.json 밖에서 생성되는 몬스터 (랭킹 모드 허수아비)
 static readonly string[] ExtraMonsterClasses={"ScareCrowMonster"};

 static int Main(string[] args){
  string root=args[0];
  bool write=args.Contains("--write");
  MeasureRule();
  PathRule();
  LayoutRule();
  MonsterSpriteBoundsData measured=MeasureAll(root);
  string jsonPath=Path.Combine(root,"Assets/Resources",MonsterSpriteBoundsData.ResourcePath+".json");
  if(write){
   File.WriteAllText(jsonPath,measured.ToJson(),new UTF8Encoding(false));
   Console.WriteLine($"측정 데이터 생성: 이미지 {measured.images.Count}장 → {jsonPath}");
   return 0;
  }
  Stored(jsonPath,measured);
  MonsterImages(root,measured);
  CanvasRules(measured);
  Display(root,measured);
  foreach(string warning in warnings)Console.WriteLine("경고: "+warning);
  Console.WriteLine($"몬스터 이미지 검증 {count}개 통과");
  return 0;
 }

 static void MeasureRule(){
  // 4×3 이미지, 좌하단 원점 (1,1)~(2,1)만 불투명
  byte[,] alpha=new byte[4,3];
  alpha[1,1]=255;alpha[2,1]=200;alpha[3,2]=10;
  var e=MonsterSpriteBoundsEntry.Measure("t",4,3,(x,y)=>alpha[x,y],16);
  Check(e.x==1&&e.y==1&&e.w==2&&e.h==1,$"측정 범위 오류 {e.x},{e.y},{e.w},{e.h}");
  Check(e.BottomPadding==1,"하단 여백 오류");
  var empty=MonsterSpriteBoundsEntry.Measure("t",2,2,(x,y)=>0,16);
  Check(empty.IsEmpty&&empty.BottomPadding==0,"빈 이미지 처리 오류");
 }

 static void LayoutRule(){
  var defaults=MonsterSpriteLayout.Compute(new MonsterSpriteBoundsEntry{path="d",width=200,height=100,w=200,h=100},null,new MonsterDisplayData(),null);
  Check(Near(defaults.scale,1)&&Near(defaults.width,200),"기본 밀도(UI 1단위 = 1px) 오류");
  // 기본 배율 = 1 / pixelsPerUnit = 0.3
  var settings=new MonsterDisplayData{pixelsPerUnit=10f/3f};
  var idle=new MonsterSpriteBoundsEntry{path="i",width=500,height=600,x=79,y=26,w=344,h=550};
  var l=MonsterSpriteLayout.Compute(idle,idle,settings,null);
  Check(Near(l.scale,0.3f)&&Near(l.width,150)&&Near(l.height,180),$"기본 배율 오류 {l.scale}");
  // 발(하단 여백 위) = 지면 높이 10 → 캔버스 하단 = 10 - 26×0.3
  Check(Near(l.bottom,10-7.8f),$"하단 정렬 오류 {l.bottom}");
  Check(Near(l.idleContentTop,10+550*0.3f),$"그림 상단 오류 {l.idleContentTop}");
  Check(Near(l.contentXRatio,79/500f)&&Near(l.contentYRatio,26/600f)&&Near(l.contentWidthRatio,344/500f)&&Near(l.contentHeightRatio,550/600f),"그림 영역 비율 오류");
  // 다른 캔버스의 공격 이미지도 같은 배율, 의도 기준은 Idle 그림 상단
  var attack=new MonsterSpriteBoundsEntry{path="a",width=900,height=600,x=10,y=40,w=880,h=500};
  var a=MonsterSpriteLayout.Compute(attack,idle,settings,new MonsterDisplayEntry{scale=2,offsetX=5,offsetY=30});
  Check(Near(a.scale,0.6f)&&Near(a.width,540),$"상태별 고정 배율 오류 {a.scale}");
  Check(Near(a.bottom,10+30-40*0.6f)&&Near(a.centerX,5),$"띄움 · 좌우 보정 오류 {a.bottom}");
  Check(Near(a.idleContentTop,40+550*0.6f),$"의도 기준 오류 {a.idleContentTop}");
  // 상태별 보정: 공격 이미지만 배율 · 위치 추가, 의도 기준은 Idle 보정 사용
  var entry=new MonsterDisplayEntry{scale=2,offsetY=30};
  entry.attack.scale=0.5f;entry.attack.offsetX=7;entry.attack.offsetY=-4;entry.idle.offsetY=6;
  var sa=MonsterSpriteLayout.Compute(attack,idle,settings,entry,"Attack");
  Check(Near(sa.scale,0.3f)&&Near(sa.centerX,7)&&Near(sa.bottom,10+30-4-40*0.3f),$"상태별 보정 오류 {sa.scale} {sa.bottom}");
  Check(Near(sa.idleContentTop,10+30+6+550*0.6f),$"상태별 의도 기준 오류 {sa.idleContentTop}");
  var si=MonsterSpriteLayout.Compute(idle,idle,settings,entry,"Idle");
  Check(Near(si.scale,0.6f)&&Near(si.bottom,10+30+6-26*0.6f),$"Idle 보정 오류 {si.bottom}");
  var empty=MonsterSpriteLayout.Compute(new MonsterSpriteBoundsEntry{path="e",width=100,height=100},null,settings,null);
  Check(Near(empty.contentWidthRatio,1)&&Near(empty.bottom,10),"빈 이미지 배치 오류");
 }
 static bool Near(float a,float b)=>Math.Abs(a-b)<0.001f;

 static void PathRule(){
  Check(MonsterImagePaths.NormalizeImageName("CactusMonster")=="Cactus","이름 정규화 오류");
  Check(MonsterImagePaths.BuildResourcePath("Cactus","Idle")=="Image/Monster/Cactus_Idle","일반 경로 오류");
  Check(MonsterImagePaths.BuildResourcePath("ProphetBoss","Hit")=="Image/BossMonster/Prophet/Prophet_Hit","보스 경로 오류");
  Check(MonsterImagePaths.BuildResourcePath("","Idle")=="","빈 이름 처리 오류");
 }

 static MonsterSpriteBoundsData MeasureAll(string root){
  string resources=Path.Combine(root,"Assets/Resources");
  var paths=new List<string>();
  foreach(string folder in new[]{MonsterImagePaths.ImageResourceFolder,MonsterImagePaths.BossImageResourceFolder}){
   string full=Path.Combine(resources,folder);
   if(!Directory.Exists(full))continue;
   foreach(string file in Directory.GetFiles(full,"*.png",SearchOption.AllDirectories)){
    string rel=Path.GetRelativePath(resources,file).Replace('\\','/');
    paths.Add(rel.Substring(0,rel.Length-4));
   }
  }
  paths.Sort(StringComparer.Ordinal);
  var data=new MonsterSpriteBoundsData();
  foreach(string path in paths){
   var(width,height,rgba)=Png.Decode(Path.Combine(resources,path+".png"));
   // PNG는 위쪽 행부터 저장되므로 좌하단 원점으로 변환
   data.images.Add(MonsterSpriteBoundsEntry.Measure(path,width,height,(x,y)=>rgba[((height-1-y)*width+x)*4+3],data.alphaThreshold));
  }
  return data;
 }

 static void Stored(string jsonPath,MonsterSpriteBoundsData measured){
  Check(File.Exists(jsonPath),$"측정 데이터가 없습니다: {jsonPath} (Run.ps1 -Write 실행)");
  string text=File.ReadAllText(jsonPath);
  var stored=JsonSerializer.Deserialize<StoredData>(text,new JsonSerializerOptions{IncludeFields=true});
  Check(stored.version==MonsterSpriteBoundsData.CurrentVersion,"측정 데이터 버전 불일치");
  Check(stored.alphaThreshold==measured.alphaThreshold,"알파 기준값 불일치");
  var storedPaths=stored.images.Select(i=>i.path).ToList();
  var measuredPaths=measured.images.Select(i=>i.path).ToList();
  foreach(string missing in measuredPaths.Except(storedPaths))Check(false,$"측정 데이터에 없는 이미지: {missing} (측정 다시 실행)");
  foreach(string extra in storedPaths.Except(measuredPaths))Check(false,$"없는 이미지의 측정 데이터: {extra} (측정 다시 실행)");
  foreach(var m in measured.images){
   var s=stored.images.First(i=>i.path==m.path);
   Check(s.width==m.width&&s.height==m.height&&s.x==m.x&&s.y==m.y&&s.w==m.w&&s.h==m.h,
    $"이미지와 측정 데이터 불일치: {m.path} (저장 {s.width}x{s.height} [{s.x},{s.y},{s.w},{s.h}] / 실제 {m.width}x{m.height} [{m.x},{m.y},{m.w},{m.h}])");
  }
  Check(text.Replace("\r\n","\n")==measured.ToJson(),"측정 데이터 서식이 생성 도구 출력과 다름");
 }

 // 전투에 나오는 모든 몬스터 클래스가 상태별 이미지와 측정값을 가짐
 static void MonsterImages(string root,MonsterSpriteBoundsData measured){
  string json=File.ReadAllText(Path.Combine(root,"Assets/Resources/JsonData/monsters.json"));
  using var doc=JsonDocument.Parse(json.TrimStart('\uFEFF'));
  var classNames=doc.RootElement.GetProperty("monsters").EnumerateArray().Select(m=>m.GetProperty("className").GetString()).Concat(ExtraMonsterClasses).Distinct().OrderBy(n=>n,StringComparer.Ordinal);
  foreach(string className in classNames){
   string imageName=MonsterImagePaths.NormalizeImageName(className);
   foreach(string suffix in MonsterImagePaths.StateSuffixes){
    string path=MonsterImagePaths.BuildResourcePath(imageName,suffix);
    var entry=measured.Find(path);
    if(entry==null&&PendingImages.Contains(imageName)){warnings.Add($"{className} 이미지 없음: {path}");continue;}
    Check(entry!=null,$"{className} 이미지 없음: {path}");
    Check(!entry.IsEmpty,$"{path} 이미지가 완전히 투명함");
   }
  }
 }

 static void CanvasRules(MonsterSpriteBoundsData measured){
  // 5단계 가공 후 이미지는 그림 영역 + 가장자리 여유 4px로 잘려 있음 (새 이미지도 같은 규칙으로 넣어야 함)
  const int maxMargin=6;
  foreach(var e in measured.images){
   if(e.IsEmpty)continue;
   int right=e.width-e.x-e.w,top=e.height-e.y-e.h;
   Check(e.x<=maxMargin&&e.y<=maxMargin&&right<=maxMargin&&top<=maxMargin,
    $"{e.path} 투명 여백이 큼 (좌 {e.x} 하 {e.y} 우 {right} 상 {top}), .claude-work/imagebake 규칙으로 가공 필요");
   Check(e.width<=2048&&e.height<=2048,$"{e.path} 크기가 임포트 최대 크기 2048을 넘음");
  }
 }

 static void Display(string root,MonsterSpriteBoundsData measured){
  string path=Path.Combine(root,"Assets/Resources",MonsterDisplayData.ResourcePath+".json");
  Check(File.Exists(path),$"배치 설정이 없습니다: {path}");
  string text=File.ReadAllText(path);
  var data=JsonSerializer.Deserialize<MonsterDisplayData>(text,new JsonSerializerOptions{IncludeFields=true});
  Check(data.version==MonsterDisplayData.CurrentVersion,"배치 설정 버전 불일치");
  Check(data.pixelsPerUnit>0,"UI 1단위당 픽셀 수 오류");
  Check(text.Replace("\r\n","\n")==data.ToJson(),"배치 설정 서식이 저장 도구 출력과 다름");
  var names=data.monsters.Select(m=>m.image).ToList();
  Check(names.Distinct().Count()==names.Count,"배치 설정에 중복 몬스터");
  string json=File.ReadAllText(Path.Combine(root,"Assets/Resources/JsonData/monsters.json"));
  using var doc=JsonDocument.Parse(json.TrimStart('\uFEFF'));
  var imaged=doc.RootElement.GetProperty("monsters").EnumerateArray().Select(m=>MonsterImagePaths.NormalizeImageName(m.GetProperty("className").GetString()))
   .Concat(ExtraMonsterClasses.Select(MonsterImagePaths.NormalizeImageName)).Distinct()
   .Where(n=>measured.Find(MonsterImagePaths.BuildResourcePath(n,MonsterImagePaths.IdleImageSuffix))!=null).ToList();
  foreach(string name in imaged)Check(names.Contains(name),$"배치 설정에 없는 몬스터: {name}");
  foreach(string name in names)Check(imaged.Contains(name),$"이미지 없는 몬스터의 배치 설정: {name}");
  foreach(var m in data.monsters)Check(m.scale>0,$"{m.image} 배율이 0 이하");
 }

 // JSON 역직렬화로만 채워지는 필드
#pragma warning disable CS0649
 class StoredData{public int version;public int alphaThreshold;public List<StoredEntry> images;}
 class StoredEntry{public string path;public int width;public int height;public int x;public int y;public int w;public int h;}
#pragma warning restore CS0649
}

// 8비트 RGBA · 비인터레이스 PNG만 해석 (몬스터 이미지 형식)
static class Png {
 public static (int width,int height,byte[] rgba) Decode(string file){
  byte[] data=File.ReadAllBytes(file);
  int pos=8,width=0,height=0;
  var idat=new MemoryStream();
  while(pos<data.Length){
   int length=ReadInt(data,pos);string type=Encoding.ASCII.GetString(data,pos+4,4);
   if(type=="IHDR"){
    width=ReadInt(data,pos+8);height=ReadInt(data,pos+12);
    byte bitDepth=data[pos+16],colorType=data[pos+17],interlace=data[pos+20];
    if(bitDepth!=8||colorType!=6||interlace!=0)throw new Exception($"지원하지 않는 PNG 형식 (8비트 RGBA만): {file} depth={bitDepth} color={colorType} interlace={interlace}");
   }
   else if(type=="IDAT")idat.Write(data,pos+8,length);
   else if(type=="IEND")break;
   pos+=12+length;
  }
  idat.Position=0;
  var raw=new MemoryStream();
  using(var z=new ZLibStream(idat,CompressionMode.Decompress))z.CopyTo(raw);
  byte[] src=raw.ToArray();
  int stride=width*4;
  byte[] rgba=new byte[stride*height];
  for(int y=0;y<height;y++){
   int filter=src[y*(stride+1)];int s=y*(stride+1)+1;int d=y*stride;
   for(int i=0;i<stride;i++){
    int a=i>=4?rgba[d+i-4]:0,b=y>0?rgba[d-stride+i]:0,c=i>=4&&y>0?rgba[d-stride+i-4]:0;
    int value=src[s+i];
    value+=filter switch{0=>0,1=>a,2=>b,3=>(a+b)/2,4=>Paeth(a,b,c),_=>throw new Exception($"잘못된 PNG 필터: {file}")};
    rgba[d+i]=(byte)value;
   }
  }
  return(width,height,rgba);
 }
 static int Paeth(int a,int b,int c){int p=a+b-c,pa=Math.Abs(p-a),pb=Math.Abs(p-b),pc=Math.Abs(p-c);return pa<=pb&&pa<=pc?a:pb<=pc?b:c;}
 static int ReadInt(byte[] d,int p)=>(d[p]<<24)|(d[p+1]<<16)|(d[p+2]<<8)|d[p+3];
}
