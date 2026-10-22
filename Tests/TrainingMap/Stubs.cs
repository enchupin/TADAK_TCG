using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
namespace UnityEngine {
 public static class Mathf {
  public static int Max(int a,int b)=>Math.Max(a,b); public static int Min(int a,int b)=>Math.Min(a,b); public static float Max(float a,float b)=>Math.Max(a,b);
  public static int Clamp(int v,int a,int b)=>Math.Clamp(v,a,b);
 }
 public static class Random { public static System.Random Rng=new(11); public static int Range(int a,int b)=>b<=a?a:Rng.Next(a,b); public static float value=>(float)Rng.NextDouble(); }
 public static class Debug { public static void Log(object o){} public static void LogWarning(object o)=>Console.WriteLine(o); public static void LogError(object o)=>throw new Exception(o?.ToString()); }
 public class TextAsset { public string text; }
 public static class Resources {
  public static string Root;
  public static T Load<T>(string path) where T:TextAsset,new(){string file=Path.Combine(Root,"Assets/Resources",path+".json");return File.Exists(file)?new T{text=File.ReadAllText(file)}:null;}
 }
}
// 실제 rules.json을 읽음
public static class GameRulesJsonParser {
 public static GameRulesData LoadOrDefault()=>JsonConvert.DeserializeObject<GameRulesData>(File.ReadAllText(Path.Combine(UnityEngine.Resources.Root,"Assets/Resources/JsonData/rules.json")).TrimStart('﻿'),new JsonSerializerSettings{ObjectCreationHandling=ObjectCreationHandling.Replace});
}
public enum Character { Monster=0 }
// MonsterSpawner의 조우 후보 조회만 사용 (encounters.json, 층 → 계층 변환은 실제 코드와 동일)
public static class MonsterSpawner {
 public static int ResolveEncounterTier(int floorNumber){
  int tier=1;
  foreach(MapSegmentData segment in GameRules.Training.map.segments){
   if(floorNumber>=segment.fromFloor&&floorNumber<=segment.toFloor)return segment.tier;
   if(floorNumber>segment.toFloor)tier=Math.Max(tier,segment.tier);
  }
  return tier;
 }
 static List<EncounterDefinition> Table(TrainingNodeType type,int floor)=>type switch{
  TrainingNodeType.Named=>MonsterDatabase.GetEncounters(MonsterDefinition.KindNamed,ResolveEncounterTier(floor)),
  TrainingNodeType.Boss=>MonsterDatabase.GetEncounters(MonsterDefinition.KindBoss,0),
  TrainingNodeType.Monster=>MonsterDatabase.GetEncounters(MonsterDefinition.KindNormal,ResolveEncounterTier(floor)),
  _=>new List<EncounterDefinition>()};
 public static List<int> CreateEncounterPlan(TrainingNodeType type,int floor){var t=Table(type,floor);return t.Count>0?new List<int>(t[0].monsters):new List<int>();}
 public static List<List<int>> GetEncounterCandidates(TrainingNodeType type,int floor)=>Table(type,floor).Select(e=>new List<int>(e.monsters)).ToList();
 public static string BuildEncounterSignature(IReadOnlyList<int> encounter)=>string.Join(",",encounter.OrderBy(x=>x));
}
