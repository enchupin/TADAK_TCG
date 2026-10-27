using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
// 몬스터 이미지 여백 측정 데이터 검증: 측정 규칙 · 이미지와 데이터 일치 · 몬스터별 이미지 경로
// --write 인자를 주면 측정 데이터를 다시 생성 (에디터 메뉴 Tools/Monster/이미지 여백 측정과 같은 결과)
class Program {
 static int count;
 static readonly List<string> warnings=new();
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
 // 아직 이미지가 없는 몬스터 (경고만 출력)
 static readonly HashSet<string> PendingImages=new(){"Priestess"};

 static int Main(string[] args){
  string root=args[0];
  bool write=args.Contains("--write");
  MeasureRule();
  PathRule();
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
  Check(File.Exists(jsonPath),$"측정 데이터가 없습니다: {jsonPath} (Tools/Monster/이미지 여백 측정 실행)");
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
  var classNames=doc.RootElement.GetProperty("monsters").EnumerateArray().Select(m=>m.GetProperty("className").GetString()).Distinct().OrderBy(n=>n,StringComparer.Ordinal);
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
  // MutantFlower는 상태별 캔버스를 1536×1536으로 통일 (2026-10-27 결정)
  foreach(string suffix in MonsterImagePaths.StateSuffixes){
   var e=measured.Find(MonsterImagePaths.BuildResourcePath("MutantFlower",suffix));
   Check(e!=null&&e.width==1536&&e.height==1536,$"MutantFlower_{suffix} 캔버스가 1536×1536이 아님");
  }
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
