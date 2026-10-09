using System;
using System.Collections.Generic;
namespace UnityEngine {
 public class MonoBehaviour {}
 public class SerializeField : Attribute {}
 public class TooltipAttribute : Attribute { public TooltipAttribute(string text) {} }
 public struct Color { public Color(float r,float g,float b) {} public static Color white => new(); }
 public static class Debug { public static void LogError(object message) {} }
 public static class Application { public static bool CanStreamedLevelBeLoaded(string name) => true; }
}
namespace UnityEngine.SceneManagement {
 public static class SceneManager { public static string Loaded; public static void LoadScene(string name) { Loaded = name; } }
}
namespace UnityEngine.UI {
 public struct ColorBlock { public UnityEngine.Color normalColor,selectedColor,highlightedColor; }
 public class Button { public bool interactable; public ColorBlock colors; public ButtonEvent onClick = new(); }
 public class ButtonEvent { public void AddListener(Action action) {} public void RemoveListener(Action action) {} }
}
public class BossModeDeckSlot { public void SetSelected(bool selected) {} }
public class DictionarySavedDeckPanel {
 public event Action DecksChanged;
 public Character Character;
 public List<CharacterDeckSave> Decks = new();
 public bool TryGetDisplayedDeck(int index, out Character character, out CharacterDeckSave deck) {
  character=Character; deck=index>=0 && index<Decks.Count ? Decks[index] : null; return deck!=null;
 }
 public void Refresh() => DecksChanged?.Invoke();
}
public class PlayerData { public int LastDamage; public static void Reset() {} }
public static class TrainingRunState { public static void ResetRun() {} }
public static class TrainingBattleManager { public static BuildingDeck buildingDeck; }
public static class SelectedButtonControl { public static List<Character> selectedCharacterList; }
public class BuildingDeck { public List<int> Cards; public void InitializeFromCardIds(List<int> ids) { Cards = new(ids); } }
public static class CardManager { public static object GetCardAsCard(int id) => id>0 ? new object() : null; }
public class Library { public List<CharacterDeckSave> decks = new(); }
public class PlayerProfileSave {
 public long bossModeLastDamage, bossModeBestDamage;
 public string bossModeLastPlayedAtUtc;
 public Dictionary<int,Library> Libraries = new();
 public Library FindLibrary(int id) => Libraries.TryGetValue(id,out var value) ? value : null;
}
public static class ProfileSaveManager {
 public static PlayerProfileSave CurrentProfile = new(); public static int Saves; public static bool Fail;
 public static void Save(PlayerProfileSave profile) { if(Fail) throw new Exception(); Saves++; }
}
public class GiantFlowerSpiderBossMonster {}
public class ProphetBossMonster {}
public class IceAndFireBossMonster {}
public class VoidLordBossMonster {}
