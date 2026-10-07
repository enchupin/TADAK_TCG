# Steam 기본 연동

Steamworks.NET 2025.164.1을 Unity Package Manager의 고정 버전으로 사용합니다.
씬과 `.asset` 설정 파일을 수정할 필요가 없습니다.

## 설정

`Assets/Resources/SteamSettings.json`에서 설정합니다.

- `appId`: 이 게임의 App ID `5312490`을 반영했습니다.
- `enabled`: Steam 기능 사용 여부. Steam 없이 빌드하려면 false로 설정합니다.
- `initializeInEditor`: 기본값 false. 일반 에디터 플레이에서는 Steam API를 호출하지 않습니다.
- `restartThroughSteam`: 배포 실행 파일을 직접 실행하면 Steam을 통해 재실행할지 지정합니다.

App ID가 미설정인 Steam 빌드는 빌드 검사에서 중단됩니다.
샘플 게임 App ID 480을 실제 게임 ID 대신 사용하지 않습니다.

## 에디터에서 실제 계정 테스트

1. Steamworks에서 개발자에게 앱 접근 권한과 테스트 라이선스를 부여합니다.
2. 실제 App ID를 입력하고 `initializeInEditor`를 true로 설정합니다.
3. `Tools > Steam > 에디터 테스트 App ID 파일 준비`를 실행합니다.
4. Steam에 로그인하고 Unity를 다시 시작한 다음 플레이합니다.
5. `Tools > Steam > 연동 상태 출력`에서 Ready, SteamID, 닉네임을 확인합니다.

테스트가 끝나면 `initializeInEditor`를 false로 되돌립니다.
프로젝트 루트의 `steam_appid.txt`는 로컬 테스트 파일이며 Git 및 실제 배포에서 제외합니다.
파일 경로는 Unity 프로젝트 루트 또는 개발용 실행 파일과 같은 위치입니다.
Steamworks.NET이 루트에 기본 샘플 파일을 만들더라도 메뉴 실행 시 실제 ID로 덮어쓰며,
초기화 후에도 설정 ID와 Steam이 반환한 ID가 일치하는지 검사합니다.

## 게임 코드에서 사용

```csharp
if (SteamClient.TryGetUser(out ulong steamId, out string nickname))
{
    // 현재 Steam 계정 정보 사용
}
```

`SteamClient.IsInitialized`는 로컬 Steam API 사용 가능 상태이고,
`SteamClient.IsOnline`은 Steam 서버 로그인 상태입니다. 오프라인에서는 둘이 다를 수 있습니다.
`StatusChanged` 구독 시 현재 상태도 즉시 읽고, 화면 종료 시 구독을 해제합니다.
초기화 실패 사유는 `LastError`에 보관합니다. 실패 시 게임을 강제 종료하지 않으므로
향후 Steam 전용 화면은 상태를 확인하고 안내해야 합니다.

매 프레임 콜백을 처리하고 종료·오브젝트 파괴·에디터 어셈블리 리로드 시 API를 정리합니다.
초기화 실패 후에는 Steam 환경을 수정하고 게임 또는 에디터 플레이를 다시 시작합니다.
Steam 관련 코드는 `Assets/Script/Steam`, 에디터 도구는 그 아래 `Editor`에 있습니다.
Auto-Cloud용 계정별 저장은 구현되어 있습니다. 업적과 온라인 랭킹은 별도 작업입니다.
클라이언트 연동에는 비밀 Web API 키가 필요하지 않습니다.

## 출시 전 수동 검증

- 일반 에디터 플레이에서 Steam 없이 Disabled 상태로 정상 실행
- 올바른 App ID와 앱 라이선스로 Steam 실행 후 Ready 및 사용자 정보 확인
- App ID 불일치, Steam 미실행, 라이선스 없음에서 Failed와 오류 메시지 확인
- 씬 전환 후에도 SteamClient가 하나이고 콜백 처리가 유지되는지 확인
- Steam 오프라인 모드에서 온라인 기능과 기본 게임 진행을 구분
- Steam으로 실행한 배포 빌드 및 직접 실행 시 Steam 재실행 확인
- 반복 플레이 종료와 재시작에서 중복 초기화 또는 종료 오류가 없는지 확인
- 실제 업로드 폴더에 steam_appid.txt가 없는지 확인

공식 문서: https://partner.steamgames.com/doc/sdk/api
래퍼 설치 문서: https://steamworks.github.io/installation/

## SteamPipe Depot

Depot ID `5312491`은 런타임 API 초기화 값이 아니라 배포 파일 묶음의 ID입니다.
`SteamBuild/app_build_5312490.vdf`와 `depot_build_5312491.vdf`에 반영했습니다.
Unity Windows 빌드 결과를 `Builds/Windows`에 출력한 뒤 Steamworks SDK의 SteamCMD에서 해당 app build VDF를 실행합니다.
현재 `Preview=1`이므로 파일 목록과 로그만 생성합니다. 확인 후 실제 업로드할 때 `Preview=0`으로 변경합니다.
로그와 캐시는 `Temp/SteamPipe`에 생성되며 브랜치를 자동 게시하는 설정은 없습니다.
Steamworks에서 Depot을 앱 및 테스트 패키지에 연결하고 실행 파일 설정을 등록해야 합니다.
이 작업에서 Steam 업로드나 브랜치 게시를 실행하지 않았습니다.

## Auto-Cloud 저장

저장 위치는 `Application.persistentDataPath/Saves/<64BitSteamID>/profile.json`입니다.
현재 Windows 설정에서는 `%USERPROFILE%/AppData/LocalLow/DefaultCompany/TADAK_TCG/Saves/<64BitSteamID>/profile.json`입니다.
`Tools > Steam > 현재 프로필 경로 출력` 메뉴로 플레이 중 실제 경로를 확인할 수 있습니다.

- 프로필 버전 2에 덱 라이브러리, 선택 덱, Steam 소유자 ID, 랭킹 최고 피해량을 저장합니다.
- 저장은 임시 파일 기록 및 디스크 flush 후 교체하며, 직전 파일은 `profile.json.bak`으로 유지합니다.
- 본문이 손상되면 유효한 임시 파일 또는 백업에서 복구하며 손상 원본은 `.corrupt.<ID>`로 보존합니다.
- 복구할 수 없으면 오류를 발생시키고 기존 데이터를 덮어쓰지 않습니다. 미래 버전과 다른 계정의 파일도 덮어쓰지 않습니다.
- 저장 덱은 정확히 7장이어야 합니다. 카드 수가 다른 파일은 정상 저장으로 처리하지 않습니다.
- Steam 초기화에 실패하면 로컬 개발 프로필로 우회하지 않습니다. Steam 오프라인 모드라도 API가 계정 ID를 제공하면 저장할 수 있습니다.
- 에디터 기본 모드 또는 Steam 비활성 모드는 `Saves/Local/profile.json`을 사용합니다.
- 기존 루트 `profile.json`은 로컬 프로필이 없을 때만 원본을 보존한 채 이전합니다. 기존 PlayerPrefs 최고 기록도 로컬 프로필로 이전합니다.
- 계정 정보가 없던 개발 저장은 Steam 계정에 자동 귀속시키지 않습니다. Steam 계정은 별도 프로필로 시작합니다.
- 언어, 음량 등 장치 설정은 기존 PlayerPrefs에 남습니다.

### Steamworks에 등록할 Windows 설정

앱 `5312490`의 Steam Cloud 설정에서 저장 용량/파일 개수 할당량을 지정하고 다음 두 경로를 등록합니다.
초기 권장 할당량은 계정당 10 MiB, 10개 파일이며 덱 수에 따른 실측 크기를 보고 조정합니다.

| 항목 | 본문 | 백업 |
| --- | --- | --- |
| Root | WinAppDataLocalLow | WinAppDataLocalLow |
| Subdirectory | DefaultCompany/TADAK_TCG/Saves/{64BitSteamID} | DefaultCompany/TADAK_TCG/Saves/{64BitSteamID} |
| Pattern | profile.json | profile.json.bak |
| OS | Windows | Windows |
| Recursive | 해제 | 해제 |

`*` 패턴은 사용하지 않습니다. 임시/손상 파일과 Local 프로필은 동기화 대상이 아닙니다.
Company Name 또는 Product Name을 바꾸면 이 경로와 기존 데이터 이전도 함께 수정해야 합니다.
macOS/Linux 네이티브 배포를 추가할 때에는 실제 persistentDataPath를 확인하여 Root Overrides를 설정하고 원래 OS를 All OSes로 전환해야 합니다.
현재 문서의 등록값은 Windows 빌드용입니다.

Steamworks 설정 저장 및 Publish 후, Steam에서 실행한 빌드를 종료하여 업로드를 확인하고 다른 PC의 같은 계정으로 내려받아 검증합니다.
에디터 플레이만으로 Auto-Cloud 동기화 검증을 대신할 수 없습니다.
실제 Steamworks 설정 변경 및 두 PC 간 동기화 검증은 아직 수행하지 않았습니다.
동기화 비활성/오프라인에서도 로컬 저장은 동작하며 충돌 선택은 Steam 클라이언트에 맡깁니다.
현재 로컬 최고 기록은 온라인 랭킹의 신뢰할 수 있는 점수 증명으로 사용하면 안 됩니다. JSON 저장은 변조 방지 기능을 제공하지 않습니다.

공식 문서: [Steam Cloud](https://partner.steamgames.com/doc/features/cloud), [SteamPipe](https://partner.steamgames.com/doc/sdk/uploading)

### 저장 회귀 검증

프로젝트 루트에서 `pwsh -File Tests/SaveSystem/Run.ps1`로 파일 교체, 백업/임시 복구, 계정 격리, 버전 보호, 기존 데이터 이전을 검사합니다.
이 테스트는 Unity/Steam 호출을 대역으로 바꾸고 실제 저장 코드와 파일 I/O를 실행합니다. Unity 플레이와 실제 Steam 계정 검증은 별도로 필요합니다.
