using System;
using System.Collections.Generic;
using System.Linq;
// v1.0 훈련 맵 생성 검증 (R-50 ~ R-56, 8절 계층별 조우 풀): 무작위 맵을 여러 번 만들어 규칙 확인
class Program {
 static int count;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
 static void Main(string[] args){
  UnityEngine.Resources.Root=args[0];
  MonsterDatabase.Reload();
  var map=GameRules.Training.map;
  var tierPools=MonsterDatabase.AllEncounters.ToDictionary(e=>MonsterSpawner.BuildEncounterSignature(e.monsters)+"|"+e.kind,e=>e);
  for(int run=0;run<200;run++){
   TrainingRunState.StartNewRun("Battle");
   var nodes=TrainingRunState.GetAllNodes();
   var byId=nodes.ToDictionary(n=>n.nodeId);
   var floors=nodes.GroupBy(n=>n.FloorNumber).OrderBy(g=>g.Key).ToList();
   Check(nodes.Count==35&&floors.Count==15,"R-50 15층 35개 노드");
   Check(floors.Select(f=>f.Count()).SequenceEqual(map.nodesPerFloor),"R-50 층별 노드 수");
   foreach(var n in nodes){
    if(n.FloorNumber==1)Check(n.nodeType==TrainingNodeType.Start,"1층 시작");
    else if(n.FloorNumber==8||n.FloorNumber==15)Check(n.nodeType==TrainingNodeType.Event,"8 · 15층 탈출구");
    else Check(n.nodeType==TrainingNodeType.Rest||n.nodeType==TrainingNodeType.Monster||n.nodeType==TrainingNodeType.Named,"R-51 휴식 · 일반 · 네임드");
    Check(n.nextNodeIds.Count<=map.maxOutDegree,"R-53 다음 층 연결 최대 2");
    if(n.FloorNumber<=3)Check(n.nodeType!=TrainingNodeType.Rest&&n.nodeType!=TrainingNodeType.Named,"2 – 3층은 휴식 · 네임드 없음");
    if(n.nodeType==TrainingNodeType.Rest)Check(n.nextNodeIds.All(id=>byId[id].nodeType!=TrainingNodeType.Rest),"휴식 연속 금지");
    if(n.FloorNumber<15)Check(n.nextNodeIds.Count>=1&&n.nextNodeIds.All(id=>byId[id].FloorNumber==n.FloorNumber+1),"R-54 나가는 간선");
    if(n.FloorNumber>1)Check(nodes.Any(o=>o.nextNodeIds.Contains(n.nodeId)),"R-54 들어오는 간선");
    if(n.nodeType==TrainingNodeType.Monster||n.nodeType==TrainingNodeType.Named){
     string kind=n.nodeType==TrainingNodeType.Named?"named":"normal";
     Check(n.HasPlannedEncounter&&tierPools.TryGetValue(MonsterSpawner.BuildEncounterSignature(n.plannedEncounter)+"|"+kind,out var e)&&e.tier==MonsterSpawner.ResolveEncounterTier(n.FloorNumber),"8절 층이 속한 계층 풀의 조우");
    }
   }
   // R-52 같은 층 사이 간선 교차 없음
   foreach(var f in floors){
    var edges=f.SelectMany(n=>n.nextNodeIds.Select(t=>(from:n.laneIndex,to:byId[t].laneIndex))).ToList();
    Check(!edges.Any(a=>edges.Any(b=>a.from<b.from&&a.to>b.to)),"R-52 간선 교차 없음");
   }
   // R-55 · R-56 1 → 15층 모든 경로
   var start=nodes.Single(n=>n.FloorNumber==1);
   foreach(var path in Paths(start,byId)){
    Check(path.Count(n=>n.nodeType==TrainingNodeType.Rest)>=map.minRestPerPath,"R-55 경로마다 휴식 2 이상");
    Check(path.Count(n=>n.nodeType==TrainingNodeType.Named)>=map.minNamedPerPath,"R-55 경로마다 네임드 1 이상");
    var signatures=path.Where(n=>n.HasPlannedEncounter).Select(n=>MonsterSpawner.BuildEncounterSignature(n.plannedEncounter)).ToList();
    Check(signatures.Distinct().Count()==signatures.Count,"R-56 한 경로에서 같은 조우 중복 없음");
   }
  }
  Console.WriteLine($"훈련 맵 검증 {count}개 통과");
 }
 static IEnumerable<List<TrainingMapNodeData>> Paths(TrainingMapNodeData node,Dictionary<int,TrainingMapNodeData> byId){
  if(node.nextNodeIds.Count==0){yield return new List<TrainingMapNodeData>{node};yield break;}
  foreach(int next in node.nextNodeIds)foreach(var rest in Paths(byId[next],byId)){rest.Insert(0,node);yield return rest;}
 }
}
