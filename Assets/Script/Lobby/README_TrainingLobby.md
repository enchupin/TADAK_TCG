# TrainingLobby 씬 - 캐릭터 선택 시스템 구현 가이드

## 📋 개요

TrainingLobby 씬에서 6개의 직업 중 최대 3개까지 선택할 수 있는 시스템입니다.
선택된 캐릭터 정보는 다른 씬으로 전달됩니다.

## 🎯 주요 기능

1. **6개의 직업 선택 버튼** (전사, 마법사, 궁수, 암살자, 성직자, 기사)
2. **스크롤뷰** - Unity의 ScrollRect을 사용하여 모든 버튼을 스크롤하여 확인 가능
3. **최대 3개 선택 제한**
4. **선택/해제 토글** - 버튼 클릭 시 선택 상태 토글
5. **씬 간 데이터 전달** - DontDestroyOnLoad를 통한 데이터 유지

## 📂 생성된 파일

```
Assets/Script/Lobby/
├── CharacterClassButton.cs          # 개별 버튼 컴포넌트
├── CharacterSelectionManager.cs     # 선택 관리자 (최대 3개 제한)
├── SelectedCharactersData.cs        # 씬 간 데이터 전달용 싱글톤
└── CharacterDataReceiver.cs         # 다른 씬에서 데이터를 받는 예제
```

**참고:** 이 시스템은 `Assets/Script/Character/Character.cs`에 정의된 기존 Character enum을 사용합니다.

## 🔧 Unity 에디터 설정 가이드

### 1단계: TrainingLobby 씬 UI 구조 생성

#### 1-1. Canvas 생성
1. `Hierarchy` → 우클릭 → `UI` → `Canvas` 생성
2. Canvas 설정:
   - Render Mode: `Screen Space - Overlay`
   - UI Scale Mode: `Scale With Screen Size`
   - Reference Resolution: `1920 x 1080`

#### 1-2. ScrollView 생성
1. `Canvas` 우클릭 → `UI` → `Scroll View` 생성
2. ScrollView 설정:
   - Rect Transform: Anchor를 중앙으로 설정
   - Width: 800, Height: 600
   - Scroll Rect 컴포넌트:
     - Horizontal: ☐ (체크 해제)
     - Vertical: ☑ (체크)
     - Movement Type: `Elastic`
     - Scrollbar Visibility: `Auto Hide And Expand Viewport`

#### 1-3. Content 설정
1. `ScrollView` → `Viewport` → `Content` 선택
2. Content에 `Vertical Layout Group` 추가:
   - Spacing: 20
   - Child Alignment: `Upper Center`
   - Child Controls Size - Height: ☑
   - Child Force Expand - Height: ☐
3. Content에 `Content Size Fitter` 추가:
   - Vertical Fit: `Preferred Size`

### 2단계: 캐릭터 버튼 생성

#### 2-1. 버튼 생성
1. `Content` 우클릭 → `UI` → `Button` 생성
2. 버튼 이름: `CharacterButton_Warrior`
3. 버튼 설정:
   - Width: 700, Height: 100
   - Image 컴포넌트 색상: 흰색 (1, 1, 1, 1)

#### 2-2. 버튼 텍스트 설정
1. 버튼의 자식 Text 오브젝트 선택
2. Text 설정:
   - Text: "전사"
   - Font Size: 32
   - Alignment: Center, Middle
   - Color: 검정색 (0, 0, 0, 1)

#### 2-3. 버튼에 스크립트 추가
1. 버튼 오브젝트 선택
2. Inspector에서 `Add Component` 클릭
3. `CharacterClassButton` 스크립트 추가
4. 스크립트 설정:
   - **Character**: `Warrior` (드롭다운에서 선택)
   - **Button Image**: 버튼의 Image 컴포넌트 드래그
   - **Normal Color**: 흰색 (1, 1, 1, 1)
   - **Selected Color**: 초록색 (0, 1, 0, 1)

#### 2-4. 나머지 5개 버튼 복제 및 설정
1. 위에서 만든 버튼을 5번 복제 (Ctrl+D)
2. 각 버튼 이름 및 설정 변경:

| 버튼 이름 | Character 설정 | 텍스트 |
|---------|---------------|--------|
| CharacterButton_Warrior | Warrior | 전사 |
| CharacterButton_Mage | Mage | 마법사 |
| CharacterButton_Archer | Archer | 궁수 |
| CharacterButton_Assassin | Assassin | 암살자 |
| CharacterButton_Priest | Priest | 성직자 |
| CharacterButton_Knight | Knight | 기사 |

### 3단계: CharacterSelectionManager 설정

#### 3-1. 빈 오브젝트 생성
1. `Hierarchy` 우클릭 → `Create Empty`
2. 이름: `CharacterSelectionManager`

#### 3-2. 스크립트 추가
1. `CharacterSelectionManager` 오브젝트 선택
2. `Add Component` → `CharacterSelectionManager` 스크립트 추가
3. 스크립트 설정:
   - **Max Selection Count**: `3`
   - **Scroll Rect**: `ScrollView` 오브젝트 드래그
   - **Character Button Container**: `Content` 오브젝트 드래그

#### 3-3. UI 요소 추가 (선택사항)

**선택 개수 표시 텍스트:**
1. `Canvas` 우클릭 → `UI` → `Text`
2. 이름: `SelectionCountText`
3. 위치: 화면 상단 중앙
4. Text: "선택된 캐릭터: 0/3"
5. Font Size: 24
6. Alignment: Center
7. CharacterSelectionManager의 `Selection Count Text`에 드래그

**확인 버튼:**
1. `Canvas` 우클릭 → `UI` → `Button`
2. 이름: `ConfirmButton`
3. 위치: 화면 하단 중앙
4. 버튼 자식 Text: "확인"
5. CharacterSelectionManager의 `Confirm Button`에 드래그

### 4단계: 완료 후 다음 씬으로 이동 설정

`CharacterSelectionManager.cs`의 `OnConfirmButtonClicked()` 메서드에서 다음 씬으로 이동하는 코드를 추가하세요:

```csharp
private void OnConfirmButtonClicked()
{
    if (selectedCharacters.Count == 0)
    {
        Debug.LogWarning("[CharacterSelectionManager] 선택된 캐릭터가 없습니다.");
        return;
    }
    
    // 선택된 캐릭터 데이터 저장
    SelectedCharactersData.Instance.SetSelectedCharacters(selectedCharacters);
    
    Debug.Log($"[CharacterSelectionManager] {selectedCharacters.Count}개의 캐릭터가 선택되어 저장되었습니다.");
    
    // 다음 씬으로 이동 (씬 이름을 실제 씬 이름으로 변경하세요)
    UnityEngine.SceneManagement.SceneManager.LoadScene("NextSceneName");
}
```

## 🎮 사용 방법

### TrainingLobby 씬에서:

1. 플레이 모드 실행
2. 캐릭터 버튼 클릭하여 선택 (최대 3개)
3. 선택된 버튼은 초록색으로 변경됨
4. 다시 클릭하면 선택 해제
5. "확인" 버튼 클릭하여 다음 씬으로 이동

### 다른 씬에서 선택된 캐릭터 사용:

```csharp
using TADAK.Lobby;

public class YourScript : MonoBehaviour
{
    private void Start()
    {
        // 선택된 캐릭터 가져오기
        var selectedCharacters = SelectedCharactersData.Instance.GetSelectedCharacters();
        
        foreach (var character in selectedCharacters)
        {
            Debug.Log($"선택된 캐릭터: {character}");
            
            // 캐릭터별 처리
            switch (character)
            {
                case Character.Warrior:
                    // 전사 캐릭터 처리
                    break;
                case Character.Mage:
                    // 마법사 캐릭터 처리
                    break;
                case Character.Archer:
                    // 궁수 캐릭터 처리
                    break;
                case Character.Assassin:
                    // 암살자 캐릭터 처리
                    break;
                case Character.Priest:
                    // 성직자 캐릭터 처리
                    break;
                case Character.Knight:
                    // 기사 캐릭터 처리
                    break;
            }
        }
        
        // 특정 캐릭터 선택 여부 확인
        if (SelectedCharactersData.Instance.IsCharacterSelected(Character.Warrior))
        {
            Debug.Log("전사가 선택되었습니다!");
        }
        
        // 선택된 캐릭터 수
        int count = SelectedCharactersData.Instance.GetSelectionCount();
        Debug.Log($"총 {count}개의 캐릭터가 선택되었습니다.");
    }
}
```

**CharacterDataReceiver 예제 사용:**

다른 씬에 `CharacterDataReceiver` 컴포넌트를 추가하면 자동으로 선택된 캐릭터를 로드하고 표시합니다.

## 📝 커스터마이징

### 최대 선택 개수 변경
`CharacterSelectionManager`의 Inspector에서 `Max Selection Count` 값 수정

### 버튼 색상 변경
각 `CharacterClassButton`의 Inspector에서:
- **Normal Color**: 기본 상태 색상
- **Selected Color**: 선택된 상태 색상

### 스크롤바 숨기기/표시
`ScrollView`의 `Scroll Rect` 컴포넌트에서:
- Horizontal Scrollbar Visibility: `Auto Hide`
- Vertical Scrollbar Visibility: `Auto Hide`

### 버튼 레이아웃 변경
`Content`의 `Vertical Layout Group`에서:
- Spacing: 버튼 간격 조절
- Padding: 상하좌우 여백 조절

## 💡 주요 특징

### ✅ 간단한 구조
- LeanTween 같은 외부 라이브러리 불필요
- 호버 효과 없이 심플한 디자인
- Unity 기본 UI 컴포넌트만 사용

### ✅ 유연한 확장성
- 기존 Character enum 사용으로 다른 시스템과 통합 용이
- 최대 선택 개수 쉽게 조절 가능
- 씬 전환 시에도 데이터 유지

### ✅ 사용자 친화적
- 명확한 선택 상태 표시 (색상 변경)
- 선택 개수 실시간 표시
- ScrollRect으로 모든 버튼 확인 가능

## 🐛 문제 해결

### 버튼이 선택되지 않음
- CharacterSelectionManager가 씬에 있는지 확인
- CharacterClassButton의 Character 값이 설정되어 있는지 확인
- EventSystem이 씬에 있는지 확인

### 스크롤이 작동하지 않음
- ScrollView의 Content에 Content Size Fitter가 있는지 확인
- Vertical Layout Group이 제대로 설정되어 있는지 확인
- Content의 Height가 Viewport보다 큰지 확인

### 다른 씬에서 데이터가 null
- SelectedCharactersData는 DontDestroyOnLoad이므로 자동으로 유지됨
- TrainingLobby 씬에서 확인 버튼을 클릭했는지 확인
- GetSelectedCharacters() 호출 전에 SetSelectedCharacters()가 호출되었는지 확인

### Character enum을 찾을 수 없음
- `Assets/Script/Character/Character.cs` 파일이 존재하는지 확인
- 네임스페이스가 일치하는지 확인

## 🎨 다음 단계 권장사항

1. **버튼에 캐릭터 이미지 추가**
   - Resources 폴더에 캐릭터 스프라이트 저장
   - CharacterClassButton에 Image 컴포넌트 추가

2. **선택 제한 알림 UI**
   - 최대 3개 초과 시 팝업 또는 토스트 메시지 표시

3. **사운드 효과 추가**
   - 버튼 클릭 시 효과음
   - 선택/해제 시 다른 효과음

4. **애니메이션 강화**
   - 선택 시 간단한 스케일 애니메이션
   - 화면 전환 페이드 효과

5. **저장 기능**
   - PlayerPrefs로 마지막 선택 저장/복원
   - JSON으로 선택 이력 관리

6. **캐릭터 스탯 시스템 연동**
   - CharacterData ScriptableObject 생성
   - 직업별 스탯, 스킬, 설명 표시

---

**작성일:** 2026-01-21  
**버전:** 2.0  
**변경사항:** LeanTween 제거, 호버 효과 제거, 기존 Character enum 사용  
**작성자:** Antigravity AI
