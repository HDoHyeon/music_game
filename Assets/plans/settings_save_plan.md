# 설정 화면 통합 구현 계획서

## 1. 작업 목적 및 요구사항
- 프로젝트: music_game (Unity 6.6, C#, URP, Mono, Input System)
- 대상: PLAN.md 2-3. 사용자 편의성 및 기록 관리 / 3. 설정 화면 통합
- 목적: 곡 선택 화면에 흩어져 있던 키 설정과 오프셋(자동 싱크) 설정을 별도의 설정 화면으로 모은다.
- 상태: 구현 완료(2026-11-16) / 사용자 확인 대기 (실제 키·마우스 조작, Esc, 화면 배치)
- 작성 경위: GPT 초안(GitHub main 7120eb4 기준)을 로컬 소스·씬(7120eb4, 미커밋 변경 없음)과 대조해 검토하고, 사용자 결정(2026-11-16)과 화면 이동 그림을 반영해 다시 작성.
- 이 문서는 개발 진행에 따라 수정한다. 작업 범위 확대는 사용자 확인 후 반영한다.

### 요구사항 (사용자 확정 2026-11-16)
| 항목 | 확정 내용 |
|---|---|
| 진입 | 곡 선택 화면의 SETTINGS 버튼으로 별도 SettingsScene에 들어간다. 인게임·일시정지 메뉴에는 설정 진입을 추가하지 않는다 |
| SETTINGS 버튼 위치 | 지금 KEY SETTING 버튼 자리 (곡 선택 화면 오른쪽 위) |
| 설정 화면 구성 | KEY SETTING 버튼(기존 키 설정 패널), OFFSET SETTING 버튼(새 오프셋 패널), BACK 버튼 |
| 오프셋 설정 | 키 설정처럼 **별도 패널**. OFFSET SETTING을 누르면 패널이 열리고 그 안에 현재 오프셋 값과 AUTO SYNC 버튼, CLOSE 버튼이 있다 |
| AUTO SYNC | 기존 OffsetCalibrationScene을 그대로 쓴다. APPLY / CANCEL / Esc로 **설정 화면**에 돌아온다. RETRY는 기존대로 다시 측정 |
| BACK | 곡 선택 화면으로 돌아가고, 설정에 들어가기 직전에 고르고 있던 곡이 선택된 상태 |
| 곡 선택 기억 | SETTINGS를 누를 때 `SongSelection.Selected`를 지금 고른 곡으로 바꾼다 (새 기억 값을 만들지 않는 단순한 방식) |
| 미리듣기 | 설정에 들어가면 멈추고, 돌아오면 선택된 곡을 처음부터 한 번 재생 (기존 미리듣기 규칙 그대로) |
| 노트 배속 | 곡 선택 화면에 그대로 둔다 |
| 저장 | 키 변경·RESET은 지금처럼 즉시 저장, 오프셋은 AUTO SYNC의 APPLY에서만 저장. 별도 SAVE 버튼이나 통합 저장 파일은 만들지 않는다 |

### 화면 이동 (사용자 그림 기준)
```
곡 선택 화면 --START--> 인게임 화면 (기존)
곡 선택 화면 --SETTINGS--> 설정 화면 --BACK--> 곡 선택 화면
설정 화면 --KEY SETTING--> 키 설정 패널 --CLOSE--> 설정 화면
설정 화면 --OFFSET SETTING--> 오프셋 패널 --AUTO SYNC--> 자동 싱크 보정 화면 --APPLY / CANCEL / Esc--> 설정 화면
                               오프셋 패널 --CLOSE--> 설정 화면
```

### 제외 범위
- 볼륨 설정(PLAN.md 2-3 #2 별도), 수동 오프셋 ±버튼, 새 보정 방식, 인게임 설정, 전체 UI 리디자인, 신규 폰트·패키지·에셋
- 설정 화면 전체에서 Esc로 뒤로 가기 (요청 없음)

## 2. 기존 코드 분석 (로컬 확인 2026-11-16)
- **SongSelectScreen.cs**
  - `Start()`: 목록 버튼 생성 → `SongSelection.Selected`의 인덱스(없으면 0)로 `Select()`. `selectedIndex` 초기값 −1이라 첫 `Select`에서 미리듣기가 시작된다.
  - 설정 관련 참조: `keySettingPanel`, `offsetText`, `autoSyncButton`, `calibrationSceneName`. `Update()`는 키 패널이 열려 있으면 ↑↓/Enter/←→를 막는다. `OpenAutoSync()`는 미리듣기를 멈추고 보정 씬으로 간다. `UpdateOffsetText()`가 OFFSET 글자를 갱신한다.
  - `StartSong()`에서만 `SongSelection.Selected`를 저장한다.
- **SongSelection.Selected를 읽는 곳은 두 군데뿐** (전체 검색): `GameplayManager.Start`(플레이할 곡), `SongSelectScreen.Start`(처음 선택할 곡). START가 항상 새로 저장하므로, SETTINGS에서 바꿔도 인게임·결과 RETRY·일시정지 복귀에 영향이 없다.
- **KeySettingPanel.cs**: `panelRoot`, `openButton`(주석: "곡 선택 화면의 KEY SETTING 버튼"), `closeButton`, `resetButton`, `laneButtons[4]`, `messageText`. `IsOpen`, `Open/Close/BeginRebind/ApplyKey/ResetKeys`. 패널이 열린 동안 키 입력을 받는다.
- **KeyBindings / OffsetSetting**: PlayerPrefs `KeyBinding.Lane0~3`, `TimingOffsetMs`. 변경 즉시 저장. Play 시작 시 캐시 초기화(SubsystemRegistration).
- **OffsetCalibration.cs**: `songSelectSceneName = "SongSelectScene"` 직렬화 필드, `Apply()`(저장 후)·`Cancel()`(Esc 포함)이 `LoadSongSelect()`로 곡 선택 화면에 간다. `Retry()`는 다시 측정.
- **SongSelectScene 배치** (Canvas 1920×1080):
  - KeySettingButton: 오른쪽 위 (−40, −40), 360×90
  - OffsetText (120, −900), AutoSyncButton (490, −900), 배속 줄 (1000~1480, −900)
  - KeySettingPanelRoot(처음엔 꺼짐) 안에 Box 900×820
  - 루트: Main Camera(AudioListener), Canvas, EventSystem, SongSelectScreen, KeySettingPanel, SongPreviewAudio
- **Build Settings**: SongSelectScene 0, IngameScene 1, ResultScene 2, OffsetCalibrationScene 3.

## 3. 작업 범위 및 수정 대상 파일
| 파일 | 구분 | 내용 |
|---|---|---|
| `Assets/Scenes/SettingsScene.unity` | 신규 | 카메라·Canvas·EventSystem, SETTINGS 제목, BACK, KEY SETTING, OFFSET SETTING, 키 설정 패널(곡 선택 화면에서 이동), 오프셋 패널(신규) |
| `Assets/Scripts/Song/SettingsScreen.cs` | 신규 | BACK, 오프셋 패널 열기·닫기, 오프셋 값 표시, AUTO SYNC 이동, 패널이 열려 있을 때 다른 버튼 차단 |
| `Assets/Scripts/Song/SongSelectScreen.cs` | 수정 | SETTINGS 연결(`OpenSettings`), 설정 관련 참조·코드 제거 |
| `Assets/Scenes/SongSelectScene.unity` | 수정 | KEY SETTING 버튼·키 설정 패널을 설정 화면으로 이동, OffsetText·AutoSyncButton 삭제, SETTINGS 버튼 추가 |
| `Assets/Scripts/Song/OffsetCalibration.cs` | 수정 | 돌아갈 씬을 설정 화면으로 (필드·함수 이름과 주석), 측정·저장 로직은 그대로 |
| `Assets/Scenes/OffsetCalibrationScene.unity` | 수정 | 돌아갈 씬 값을 `SettingsScene`으로 저장 |
| `Assets/Scripts/Song/KeySettingPanel.cs` | 주석만 | `openButton` 주석 "곡 선택 화면의" → "설정 화면의". 동작 변경 없음 |
| `ProjectSettings/EditorBuildSettings.asset` | 수정 | SettingsScene을 마지막(4번)에 등록. 기존 순서 유지 |

- 수정하지 않음: SongSelection.cs, KeyBindings, OffsetSetting, NoteSpeed, GameplayManager, PauseController, IngameScene, ResultScene, Note.prefab, 음원·채보.
- **중요 — 사용자 승인 필요**: Build Settings에 SettingsScene 등록, 자동 싱크 복귀 목적지를 곡 선택 → 설정 화면으로 변경, 곡 선택 화면에서 KEY SETTING·OFFSET·AUTO SYNC 제거.
- 관련 계획서(key_setting_plan.md, offset_setting_plan.md, song_select_plan.md)의 "곡 선택 화면에 있음" 설명은 구현 후 변경 내용을 보고하고 필요한 부분만 고친다. PLAN.md는 목록 상태 칸만 바꾼다.

## 4. 상세 구현 방법 및 필요한 코드 스니펫
### 4.1 SettingsScene 구성 (1920×1080 기준, 좌표는 스크린샷 보며 조정)
| 오브젝트 | 내용 |
|---|---|
| Main Camera | 곡 선택 화면과 같은 설정. 소리를 내지 않으므로 AudioListener는 넣되 AudioSource는 없음 |
| Canvas / EventSystem | Screen Space Overlay, Scale With Screen Size 1920×1080, InputSystemUIInputModule (곡 선택 화면과 동일) |
| Title | 위 가운데 "SETTINGS" |
| BackButton | 왼쪽 위 (40, −40), 260×90, "BACK" |
| KeySettingButton | 가운데 (0, 70), 480×110, "KEY SETTING" — 곡 선택 화면의 버튼을 옮겨 위치·크기만 변경 |
| OffsetSettingButton | 가운데 (0, −80), 480×110, "OFFSET SETTING" |
| KeySettingPanelRoot | 곡 선택 화면에서 그대로 옮김 (처음엔 꺼짐) |
| OffsetPanelRoot (신규, 처음엔 꺼짐) | 키 패널과 같은 모양의 Box(900×600): 제목 "OFFSET", 현재 값 "OFFSET +15 ms", AUTO SYNC 버튼, CLOSE 버튼 |
| KeySettingPanel / SettingsScreen | 항상 켜져 있는 오브젝트에 컨트롤러 배치 |

- 키 설정 패널은 **복제하지 않고 옮긴다** (SongSelectScene과 SettingsScene을 함께 열고 오브젝트를 새 Canvas 아래로 이동). 배치와 버튼 연결이 그대로 유지되고, 이동 후 KeySettingPanel의 참조가 모두 연결돼 있는지 확인한다.
- 패널 바탕(Box 뒤 전체 화면)이 뒤쪽 버튼 클릭을 막는지 확인하고, 막지 않으면 패널이 열려 있는 동안 SettingsScreen이 BACK·다른 패널 버튼을 무시한다(아래 4.2).

### 4.2 SettingsScreen.cs (신규)
```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 설정 화면: 키 설정 패널(기존 KeySettingPanel)과 오프셋 패널을 열고, BACK으로 곡 선택 화면에 돌아간다.
// 오프셋 패널에는 현재 오프셋 값과 AUTO SYNC(자동 싱크 보정 화면 이동) 버튼이 있다. 오프셋 저장은 보정 화면의 APPLY에서만 한다.
public class SettingsScreen : MonoBehaviour
{
    [SerializeField] private KeySettingPanel keySettingPanel;
    [SerializeField] private Button backButton;
    [SerializeField] private Button offsetOpenButton;    // OFFSET SETTING
    [SerializeField] private GameObject offsetPanelRoot; // 오프셋 패널 (처음엔 숨김)
    [SerializeField] private Button offsetCloseButton;
    [SerializeField] private Text offsetText;            // 현재 오프셋 (예: OFFSET +15 ms)
    [SerializeField] private Button autoSyncButton;
    [SerializeField] private string songSelectSceneName = "SongSelectScene";
    [SerializeField] private string calibrationSceneName = "OffsetCalibrationScene";
    private bool loading;

    private bool IsOffsetOpen => offsetPanelRoot != null && offsetPanelRoot.activeSelf;
    private bool IsKeyOpen => keySettingPanel != null && keySettingPanel.IsOpen;

    private void Start()
    {
        if (offsetPanelRoot != null) offsetPanelRoot.SetActive(false);
        if (offsetText != null) offsetText.text = OffsetSetting.Label; // 보정 화면에서 APPLY 후 돌아오면 새 값
        if (backButton != null) backButton.onClick.AddListener(Back);
        if (offsetOpenButton != null) offsetOpenButton.onClick.AddListener(OpenOffset);
        if (offsetCloseButton != null) offsetCloseButton.onClick.AddListener(CloseOffset);
        if (autoSyncButton != null) autoSyncButton.onClick.AddListener(OpenAutoSync);
    }

    // 곡 선택 화면으로 돌아간다. 패널이 열려 있으면 무시 (먼저 CLOSE)
    public void Back()
    {
        if (loading || IsKeyOpen || IsOffsetOpen) return;
        Load(songSelectSceneName);
    }

    // 오프셋 패널을 연다. 키 설정 패널이 열려 있으면 무시
    public void OpenOffset()
    {
        if (loading || IsKeyOpen || offsetPanelRoot == null) return;
        offsetPanelRoot.SetActive(true);
    }

    public void CloseOffset()
    {
        if (offsetPanelRoot != null) offsetPanelRoot.SetActive(false);
    }

    // 자동 싱크 보정 화면으로 이동한다 (오프셋 패널 안의 버튼)
    public void OpenAutoSync()
    {
        if (loading || !IsOffsetOpen) return;
        Load(calibrationSceneName);
    }

    private void Load(string sceneName)
    {
        loading = true;
        SceneManager.LoadScene(sceneName);
    }
}
```
- 오프셋 패널이 열려 있는 동안 KEY SETTING 버튼도 막아야 한다. KeySettingPanel.cs는 동작을 바꾸지 않기로 했으므로, 오프셋 패널 바탕이 뒤쪽 버튼 클릭을 막도록(전체 화면 Image, Raycast Target 켬) 만든다. 키 패널도 같은 방식인지 구현 전에 확인한다.

### 4.3 곡 선택 화면 SETTINGS 연결 (SongSelectScreen.cs)
```csharp
[SerializeField] private Button settingsButton;                     // 설정 화면으로 이동 (오른쪽 위)
[SerializeField] private string settingsSceneName = "SettingsScene";
// Start: if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);

// 설정 화면으로 이동한다. 돌아왔을 때 지금 고른 곡이 선택되도록 SongSelection.Selected에 넣어 둔다.
// (Selected를 읽는 곳은 인게임 시작과 이 화면의 처음 선택뿐이고, START가 항상 새로 저장하므로 다른 흐름에 영향 없음)
public void OpenSettings()
{
    if (loading) return;
    loading = true;
    StopPreview();
    SongData[] songs = Songs;
    if (selectedIndex >= 0 && selectedIndex < songs.Length) SongSelection.Selected = songs[selectedIndex];
    SceneManager.LoadScene(settingsSceneName);
}
```
- 제거: `keySettingPanel`, `offsetText`, `autoSyncButton`, `calibrationSceneName` 필드, `Start`의 AUTO SYNC 연결·`UpdateOffsetText()` 호출, `Update`의 키 패널 검사, `OpenAutoSync()`, `UpdateOffsetText()`. 씬의 해당 UI와 참조도 함께 정리한다.
- 곡 선택·START·배속·미리듣기는 그대로. 돌아오면 새 SongSelectScreen이 `selectedIndex = −1`에서 `Select`하므로 미리듣기가 처음부터 한 번 재생된다.
- 빈 목록(`selectedIndex = −1`)이면 `Selected`를 건드리지 않는다.

### 4.4 자동 싱크 복귀 변경 (OffsetCalibration.cs)
```csharp
[SerializeField] private string settingsSceneName = "SettingsScene"; // APPLY / CANCEL / Esc 후 돌아갈 씬 (기존 songSelectSceneName)

// 저장 여부와 상관없이 설정 화면으로 돌아간다.
private void LoadSettings()
{
    loading = true;
    audioSource.Stop();
    SceneManager.LoadScene(settingsSceneName);
}
// Apply: 기존대로 OffsetSetting.Set(resultMs) 후 LoadSettings()
// Cancel: 기존대로 loading 검사 후 LoadSettings()
```
- 필드 이름이 바뀌면 씬에 저장된 기존 값("SongSelectScene")은 쓰이지 않는다. 씬 Inspector에서 새 필드 값이 `SettingsScene`인지 확인하고 저장한다.
- 클래스 맨 위 주석("CANCEL/Esc로 저장 없이 곡 선택 화면으로 돌아간다")도 설정 화면으로 고친다.
- 측정·중앙값·범위·키·배속·메트로놈은 수정하지 않는다.

### 4.5 Build Settings
- SettingsScene을 4번으로 추가 (0 SongSelectScene, 1 IngameScene, 2 ResultScene, 3 OffsetCalibrationScene 유지). 에디터 Play 시작 씬(SongSelectScene)도 그대로.

### 4.6 저장 및 기존 기능 보호
- PlayerPrefs 키 이름·저장 시점·기본값을 바꾸지 않는다. BACK은 저장된 값을 되돌리지 않는다.
- 인게임은 지금처럼 KeyBindings·OffsetSetting을 읽는다. 설정 화면에서는 음악을 재생하지 않으므로 미리듣기와 메트로놈이 겹치지 않는다.

## 5. 구현 순서 및 진행 상태
- [x] GPT 초안 검토, 로컬 소스·씬 대조
- [x] 사용자 결정 반영 (오프셋 별도 패널, 단순한 곡 선택 기억, SETTINGS 버튼 위치) (2026-11-16)
- [x] 사용자 구현 승인 (Build Settings 등록, 보정 복귀 경로, 곡 선택 화면 UI 제거 포함, 2026-11-16)
- [x] 1단계: SettingsScreen.cs, SongSelectScreen.cs, OffsetCalibration.cs, KeySettingPanel 주석(3줄) → 컴파일 오류 0
- [x] 2단계: SettingsScene 생성, 키 설정 버튼·패널·컨트롤러 이동, 오프셋 패널(키 패널 복제 후 레인 버튼 삭제)·OFFSET SETTING·BACK 구성, 참조 연결
- [x] 3단계: SongSelectScene 정리(OffsetText·AutoSyncButton 삭제, SETTINGS 버튼), OffsetCalibrationScene 복귀 값 SettingsScene, Build Settings 4번 SettingsScene
- [x] 4단계: 기능·회귀 테스트 (6절)
- [ ] 사용자 실제 입력·화면 확인
- [x] 검증 결과 기록, 관련 계획서(key_setting_plan.md, offset_setting_plan.md, song_select_plan.md) 갱신
- PLAN.md: 계획서 저장 시(2026-11-16) 2-3 #3을 미작성 → 미확인으로 바꿈(상태 정의: 미확인 = 계획서 작성 후). 키 설정·오프셋·곡 선택 상태는 임의로 바꾸지 않는다.

## 6. 테스트 및 검증 방법
- C: Claude (컴파일, Play Mode에서 함수 직접 호출, 씬·참조 확인, AUTO TEST, 스크린샷) / U: 사용자 (실제 키·마우스, 화면)
- 저장 테스트 전 기존 PlayerPrefs 값(키·오프셋)을 기록하고, 검증 후 원래 값으로 되돌린다.

| ID | 테스트 | 기대 결과 | 주체 | 상태 |
|---|---|---|---|---|
| ST-01 | 컴파일·씬 참조 | 오류 0, Missing Script·참조 누락 없음, 씬마다 AudioListener 1개 | C | 통과 (컴파일 오류 0, SettingsScreen·KeySettingPanel 참조 전부 연결, SongSelectScreen 빈 참조 없음, Play 중 Error/Warning 0) |
| ST-02 | 곡 선택 SETTINGS | 설정 화면 진입, 미리듣기 정지 | C/U | C 통과 (OpenSettings 직후 미리듣기 clip 없음·정지, loading, Selected=Kick, SettingsScene 진입) / U 미검증 |
| ST-03 | 설정 BACK | 들어가기 전 고른 곡 선택, 음악 있으면 처음부터 한 번 재생 | C/U | C 통과 (BACK → kick 선택, 미리듣기 재생 진행 확인) / U 미검증 |
| ST-04 | 마지막 플레이 A, 고른 곡 B → 설정 왕복 | B 선택 상태로 복귀. START하면 B 플레이 | C | 통과 (마지막 플레이 Test Song 2 → kick 고르고 설정 왕복 → kick 선택, Selected=Kick) |
| ST-05 | 빈 목록·음악 없는 곡 | 설정 왕복 정상, 무음, START 조건 그대로 | C | 통과 (빈 목록에서 설정 진입 시 Selected 그대로, 왕복 후 오류 없음) |
| ST-06 | KEY SETTING 열기·닫기 | 현재 4키 표시, CLOSE 후 설정 화면 | C/U | C 통과 (KEY SETTING → 패널 열림, D/F/J/K 표시, CLOSE 후 설정 화면, 스크린샷) / U 미검증 |
| ST-07 | 키 변경 | 즉시 저장, 인게임·보정 화면에 적용 | C/U | C 통과 (레인 1 → A, PlayerPrefs KeyBinding.Lane0 = 15(A) 저장, Play 다시 시작 후 A,F,J,K) / U 미검증 (실제 키) |
| ST-08 | 중복 키·금지 키·Esc | 교환 / 거부 / 대기 취소 (기존과 같음) | C/U | C 통과 (레인 2 → A: 교환 F,A,J,K / Enter: 거부 / Esc: 대기 취소) / U 미검증 |
| ST-09 | RESET | D/F/J/K로 저장·표시 | C/U | C 통과 (RESET → D,F,J,K) / U 미검증 |
| ST-10 | OFFSET SETTING 열기·닫기 | 패널에 현재 오프셋 표시, CLOSE 후 설정 화면 | C/U | C 통과 (OFFSET SETTING → 패널에 `OFFSET -15 ms`, 스크린샷) / U 미검증 |
| ST-11 | 패널이 열린 동안 다른 버튼 | 키 패널 열림 중 BACK·OFFSET SETTING 무시, 오프셋 패널 열림 중 BACK·KEY SETTING 무시 | C/U | C 통과 (키 패널 열림 중 Back·OpenOffset 무시, 오프셋 패널 열림 중 Back 무시, KEY SETTING 버튼 위치 레이캐스트 최상단 = 패널 Box, 패널 닫힘 상태 OpenAutoSync 무시) / U 미검증 |
| ST-12 | 오프셋 패널 AUTO SYNC | 기존 보정 화면, 메트로놈만 재생 | C/U | C 통과 (오프셋 패널 AUTO SYNC → OffsetCalibrationScene) / U 미검증 |
| ST-13 | APPLY | 결과 저장, 설정 화면 복귀, 오프셋 패널을 열면 새 값 | C/U | C 통과 (+30ms 16박 → RESULT +30 → APPLY → 설정 화면, 패널 `OFFSET +30 ms`) / U 미검증 |
| ST-14 | CANCEL / Esc | 기존 오프셋 유지, 설정 화면 복귀 | C/U | C 통과 (CANCEL → 설정 화면, −15 ms 유지). 실제 Esc 키는 U 미검증 |
| ST-15 | RETRY / 유효 입력 부족 | 다시 측정, 부족하면 APPLY 불가 (기존과 같음) | C | 통과 (1박만 → APPLY 불가, RETRY → 유효 0·결과 숨김) |
| ST-16 | 설정 → 보정 여러 번 → BACK | 처음 고른 곡 선택 유지 | C | 통과 (설정 → 보정 3번(CANCEL·APPLY·CANCEL) → BACK → kick 선택 유지) |
| ST-17 | Play 다시 시작 | 저장된 키·오프셋 유지 | C | 통과 (Play 종료 후 오프셋 30·키 A 유지, 다시 시작 후 KeyBindings A,F,J,K) |
| ST-18 | 곡 선택 회귀 | 클릭·↑↓·Enter·배속·미리듣기 정상, KEY SETTING·OFFSET·AUTO SYNC는 곡 선택 화면에 없음 | C/U | C 통과 (곡 선택 화면에 KeySettingButton·OffsetText·AutoSyncButton 없음, SettingsButton 있음, 곡 선택·START·미리듣기 정상) / U 미검증 (실제 클릭·↑↓·Enter) |
| ST-19 | AUTO 3 회귀 | Test Song 1 35800/61, Test Song 2 9800/14, kick 263200/364 | C | 통과 (Test Song 1 35,800/61, Test Song 2 9,800/14, kick 263,200/364 — kick은 음악 시간 직접 진행) |
| ST-20 | 결과·일시정지 복귀 | 마지막 플레이 곡 선택, RETRY 정상 (기존과 같음) | C | 통과 (Test Song 2 플레이 후 일시정지 메뉴 Song Select → Test Song 2 선택). 결과 화면 RETRY는 SongSelection을 거치는 코드 변경이 없어 실행 안 함 |
| ST-21 | 화면 배치 | 1920×1080에서 잘림·겹침 없음 (스크린샷) | C/U | C 통과 (곡 선택·설정·키 패널·오프셋 패널 스크린샷에서 잘림·겹침 없음) / U 미검증 |

## 7. 검증 결과 및 미해결 사항
- 구현: 4절 그대로. 화면 좌표도 제안값 그대로 사용(스크린샷 확인).
- 검증 방식: Play Mode에서 함수 직접 호출·버튼 onClick 호출, 스크린샷, AUTO TEST. 실제 키보드·마우스 입력은 넣을 수 없어 U 항목은 사용자 확인 필요.
- 결과: 6절 표대로 C 항목 모두 통과.
- 검증 중 있었던 일:
  - Build Settings 변경이 메모리에만 반영되고 파일에 바로 저장되지 않아 `AssetDatabase.SaveAssets()`로 저장했다(ProjectSettings/EditorBuildSettings.asset에 SettingsScene 4번 확인).
  - 첫 검증 스크립트가 키 설정 저장값을 문자열로 읽어(실제는 정수) 원래 값을 "없음"으로 기록했고, 복원 때 KeyBinding.Lane0~3 저장 키를 지웠다. 검증 전 실제 적용 키가 기본값 D/F/J/K였으므로 결과적으로 같은 상태(저장 키 없음 = D/F/J/K)다. 오프셋은 −15ms로 복원했다.
- 관련 계획서 갱신(2026-11-16, 사용자 확인): key_setting_plan.md·offset_setting_plan.md·song_select_plan.md의 설정 위치·화면 이동 설명을 설정 화면 기준으로 고치고 변경 이력 추가. 당시 분석·검증 기록은 그대로 둠.

### 변경 이력
| 날짜 | 내용 |
|---|---|
| 2026-11-16 | GPT 초안 작성 (GitHub main 7120eb4 기준) |
| 2026-11-16 | 로컬 대조, 사용자 결정 반영: 오프셋을 별도 패널로(그림의 "오프셋 설정" 단계), 곡 선택 기억을 SETTINGS 때 Selected 갱신 방식으로 단순화(SongSelection.cs 수정 없음), SETTINGS 버튼을 KEY SETTING 자리에, 키 설정 패널은 복제 대신 이동, KeySettingPanel 주석 수정 명시 |
| 2026-11-16 | 구현 및 검증 (6절·7절) |
