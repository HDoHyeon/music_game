# 오프셋(싱크 보정) 설정 구현 계획서

## 1. 작업 목적 및 요구사항
- 프로젝트: music_game (Unity 6.6, C#, URP, Mono, Input System)
- 대상: PLAN.md 2-2. 플레이 안정성 및 데이터 구조 / 1. 오프셋(싱크 보정) 설정
- 목적: 음악과 노트의 시간 차이(오디오 출력 지연, 사용자 환경 차이)를 사용자 설정으로 보정한다.
- 상태: 1단계·2단계(탭 보정) 구현 완료, 사용자 확인 문제 3건 수정 완료(2026-11-15) / 사용자 확인 대기 (소리, 실제 레인 키로 측정, 화면)
- 단계: 1단계 오프셋 적용·직접 조절(구현·검증 완료) → 2단계 직접 조절(-/+)을 탭 보정으로 대체(2026-11-15 사용자 결정)
- 이 문서는 개발 진행에 따라 수정한다. 작업 범위 확대는 사용자 확인 후 반영한다.
- 작성 경위: GPT 초안(사용자 제공)을 로컬 소스·채보와 대조해 검토하고, 범위를 넘는 설계(음악 종료 후 시간 처리)를 빼고 다시 작성.

### 요구사항
- 기존 판정 범위, 점수, 콤보, 롱노트, 배속, 일시정지, 결과 화면 규칙을 유지한다.
- 확정 사항 (2026-11-15 사용자 승인):

| 항목 | 확정값 |
|---|---|
| 기본값 | 0ms (기존 동작과 같음) |
| 범위·단위 | -200ms ~ +200ms, 5ms 단위 |
| 부호 | +: 노트가 늦게 판정선에 도착(판정도 늦어짐) / -: 일찍 도착. 소리가 노트보다 늦게 들리면 +로 올린다 |
| 적용 대상 | 음악이 있는 곡만 (음악 없는 곡은 맞출 음악이 없어 기존 시간 그대로) |
| 보정 범위 | 노트 표시·판정·롱노트(시작·틱·끝)·Miss·AUTO TEST에 같은 보정 시간 적용 (표시만 따로, 판정만 따로 보정하지 않음) |
| 설정 위치 | ~~곡 선택 화면에~~ → 2026-11-16부터 **설정 화면의 OFFSET SETTING 패널**에 `OFFSET 0 ms` 형식 표시(양수는 `+15`처럼 부호 표시)와 `AUTO SYNC` 버튼 → 탭 보정으로 측정해 결과를 적용 (2단계, 아래 표). 1단계의 -/+ 직접 조절 버튼은 제거 |
| 적용 시점 | 플레이 시작 시 값을 읽어 그 플레이 동안 고정. 플레이 중·일시정지 중 변경 없음 |
| 저장 | PlayerPrefs `TimingOffsetMs`, 전체 곡 공통. 저장값이 범위 밖이거나 5ms 단위가 아니면 가장 가까운 값으로 맞춤 |

- 탭 보정(자동 싱크) 확정 사항 (2026-11-15 사용자 승인, 예시 이미지 `Assets/ex_image/ex_offset_setting.svg`):

| 항목 | 확정값 |
|---|---|
| 화면 | 별도 씬 `OffsetCalibrationScene`. ~~곡 선택 화면의~~ 설정 화면 오프셋 패널의 `AUTO SYNC` 버튼으로 이동, 끝나거나 취소하면 ~~곡 선택 화면~~ **설정 화면**으로 돌아감 (2026-11-16 변경) |
| 구성 | (2026-11-15 수정) 인게임과 같은 4레인: 인게임 `Note` 프리팹, 레인 x = -1.5 / -0.5 / 0.5 / 1.5, 생성 y=6, 판정선 y=-3, 판정선·Global Light 2D는 IngameScene에서 복사. 진행 횟수 `N / 16`. 예시 이미지는 구성 참고용으로만 사용 (처음 구현의 레인 1개·`SPACE` 표시는 삭제) |
| 소리 | 120 BPM 메트로놈 클릭(0.5초 간격, 2026-11-15 재확인). `AudioClip.Create`로 코드에서 생성 (새 음원 파일 없음). 노트는 클릭 순간에 판정선에 도착. 카메라에 AudioListener 필요 |
| 노트 | 박자마다 노트 1개, 레인 1→2→3→4 순서로 반복 (준비 박자 포함). 속도는 곡 선택의 배속(SPEED) 적용 (x3.0 = 초당 5칸, 인게임과 같음) |
| 진행 | 준비 4박(세지 않음) → 16박 동안 SPACE 입력 |
| 측정 | 누를 때마다 가장 가까운 박자와의 차이(ms) 기록. ±250ms보다 멀면 실수로 보고 제외. 시간은 메트로놈의 재생 위치 기준, 측정 중에는 저장된 오프셋을 적용하지 않음(0 기준) |
| 결과 | 차이들의 중앙값을 5ms 단위로 맞춰 `RESULT +35 ms`처럼 표시. 유효 입력 8번 미만이면 `NOT ENOUGH TAPS`. ±200ms를 넘으면 ±200으로 자르고 범위 초과 표시 |
| 부호 | 늦게 치면 + (기존 오프셋 부호와 같음: +면 노트·판정이 늦어짐) |
| 버튼 | APPLY(저장하고 곡 선택으로) / RETRY(다시 측정) / CANCEL(기존 값 유지하고 곡 선택으로). 측정 중 Esc = CANCEL |
| 노트 사라짐 | (2026-11-15 추가) 측정에 들어간 입력(노트 레인의 키, 박자 ±250ms 안, 그 박자 첫 입력)이면 그 박자의 노트를 인게임처럼 바로 숨긴다. 다른 레인 키·크게 빗나간 입력·준비 박자 입력은 노트를 그대로 둔다 |
| 입력 키 | (2026-11-15 수정) 인게임과 같은 레인 키 (`KeyBindings`, 기본 D/F/J/K). 노트가 있는 레인의 키를 눌렀을 때만 측정에 넣고 다른 레인 키는 무시 |

- 제외 범위: 곡별 오프셋, 표시·입력 분리 오프셋, 일시정지 중 변경, 설정 화면 통합, 음악 종료 후 시간 처리 보완(아래 2장 주의할 점 3).

## 2. 기존 코드 분석
| 파일 | 현재 동작 및 영향 |
|---|---|
| `GameplayManager.cs` | `SongTime`: 정지 중이면 `pausedSongTime`, 아니면 음악이 있으면 `audioSource.time`, 없으면 `Time.timeAsDouble - startTime`. `Update`에서 같은 `songTime`을 `SpawnNotes`/`MoveNotes`/`HandleInput`/`HandleHolds`/`HandleMisses`에 넘기고 `prevSongTime` 갱신. `TryPause`는 `SongTime`을 저장, `TryResume`은 음악 없는 곡의 `startTime`만 보정 |
| `Note.cs` | `UpdatePosition(songTime, …)`에서 `노트 시각 - songTime`으로 위치 계산 → 넘겨받는 곡 시간만 바꾸면 표시도 같이 보정됨 |
| `AutoPlayer.cs` | 채보 원본 시각으로 누를·뗄 시각을 정하고 `prevTime < PressTime <= songTime`으로 입력 판단 → 넘겨받는 곡 시간 기준이라 수정 불필요 |
| `NoteSpeed.cs` | static 값, PlayerPrefs 저장, `SubsystemRegistration`에서 다시 불러오기 → 같은 구조로 오프셋 설정을 만든다 |
| `SongSelectScreen.cs` | SPEED 표시와 -/+ 버튼(`speedText`, `speedDownButton`, `speedUpButton`), ←→ 키 |
| `SongSelectScene.unity` | 곡 정보는 오른쪽(x=1000), START y=-760, SPEED 줄 y=-900(x=1000~1580). 왼쪽 곡 목록은 x=120~820, 4곡이 y=-220~-550 사이 |

### 채보 확인 (2026-11-15)
| 곡 | 음악 | 첫 노트 | 마지막 노트 끝 | 음악 길이 |
|---|---|---|---|---|
| kick | 있음 | 6.857초 | 88.714초 | 90.64초 |
| Test Song 1~3 | 없음 | 1.5~2.0초 | 5.0~12.5초 | - |

### 주의할 점
1. 노트 위치만 보정하면 보이는 타이밍과 판정 타이밍이 달라진다 → `SongTime` 한 곳에서 보정해 모든 처리에 같은 값을 넘긴다.
2. 음악 시작 전 판정 문제: -200ms에서도 kick 첫 노트는 음악 6.657초에 판정 → 음악 시작 전에 판정해야 하는 노트 없음. +200ms면 곡 시작 직후 `SongTime`이 -0.2초가 되는데, 음수 시간은 기존 계산에 문제가 없다(노트 생성 조건은 `노트 시각 - 곡 시간 <= 생성 시간`).
3. 음악이 끝나면 `audioSource.time`이 0으로 돌아가 음악보다 긴 채보의 끝부분이 처리되지 않는 잠재 문제가 있다(pause_restart_plan 2장에 기록). +200ms에서도 kick 마지막 노트는 음악 88.914초에 끝나 음악 끝(90.64초)까지 1.7초 이상 남으므로 이번 작업에서는 다루지 않는다. 음악 끝 근처까지 노트가 있는 곡을 추가할 때 별도로 다룬다.
4. AUTO TEST도 보정된 곡 시간으로 입력하므로, AUTO 점수가 같다는 것은 "보정해도 처리가 깨지지 않음"만 보여 준다. 실제 싱크가 맞는지는 음악을 들어 봐야 알 수 있다.

### 탭 보정 관련 (2단계 분석)
- 오프셋 저장·적용은 1단계 `OffsetSetting` / `GameplayManager`를 그대로 쓴다. 탭 보정은 값을 측정해 `OffsetSetting`에 저장하는 화면만 추가한다.
- 곡 선택 화면에는 1단계의 `OffsetDownButton` / `OffsetText` / `OffsetUpButton`(y=-900, x=120 / 230 / 600)과 `SongSelectScreen.OffsetDown` / `OffsetUp`이 있다 → 버튼 2개와 두 함수를 지우고 AUTO SYNC 버튼으로 바꾼다. `OffsetSetting.Increase` / `Decrease`는 곡 선택 화면에서만 쓰여 함께 정리한다.
- 씬을 불러오려면 Build Settings에 등록되어 있어야 한다 (지금 SongSelectScene 0 / IngameScene 1 / ResultScene 2).
- Esc는 레인 키로 쓸 수 없고(`KeyBindings`), SPACE는 레인 키로 쓸 수 있지만 보정 씬에는 레인 입력이 없어 겹치지 않는다.

## 3. 작업 범위 및 수정 대상 파일
1단계 (구현 완료):

| 파일 | 구분 | 내용 |
|---|---|---|
| `Assets/Scripts/Gameplay/OffsetSetting.cs` | 신규 | 오프셋 값(ms), 범위·단위, 올리기·내리기, 저장·불러오기, 표시 문자열 |
| `Assets/Scripts/Gameplay/GameplayManager.cs` | 수정 | `Start`에서 오프셋을 읽어 고정(음악 있는 곡만), `SongTime`에서 보정 시간 빼기 |
| `Assets/Scripts/Song/SongSelectScreen.cs` | 수정 | `offsetText`, `offsetDownButton`, `offsetUpButton` 연결과 표시 갱신 |
| `Assets/Scenes/SongSelectScene.unity` | 수정 | `OffsetDownButton`(-) / `OffsetText` / `OffsetUpButton`(+) 추가 |


2단계 탭 보정 (구현 승인 대기):

| 파일 | 구분 | 내용 |
|---|---|---|
| `Assets/Scripts/Song/OffsetCalibration.cs` | 신규 → 수정(2026-11-15) | 메트로놈 생성·재생, 인게임 `Note` 프리팹으로 4레인 노트 표시, 레인 키 입력 기록(`RegisterTap(레인, 시각)`), 중앙값 계산, APPLY / RETRY / CANCEL, Esc |
| `Assets/Scenes/OffsetCalibrationScene.unity` | 신규 → 수정(2026-11-15) | 카메라(AudioListener 추가, orthographic size 5), 판정선·Global Light 2D(IngameScene에서 복사), Canvas(제목·현재 오프셋·`N / 16`·안내 문구·결과·버튼 3개, UI 레인·노트·SPACE 삭제), EventSystem, AudioSource, `OffsetCalibration` |
| `Assets/Scripts/Gameplay/OffsetSetting.cs` | 수정 | `Increase` / `Decrease` 대신 값을 지정해 저장하는 `Set(int)` 공개 (±200ms 제한·5ms 단위 유지) |
| `Assets/Scripts/Song/SongSelectScreen.cs` | 수정 | `offsetDownButton` / `offsetUpButton`, `OffsetDown` / `OffsetUp` 제거, `autoSyncButton` 연결(보정 씬 불러오기) |
| `Assets/Scenes/SongSelectScene.unity` | 수정 | `OffsetDownButton` / `OffsetUpButton` 삭제, `OffsetText` + `AutoSyncButton`(`AUTO SYNC`) 배치 (같은 y=-900 줄) |
| `ProjectSettings/EditorBuildSettings.asset` | 수정 (프로젝트 설정, 이번 계획 승인으로 함께 승인) | `OffsetCalibrationScene`을 Build Settings 3번에 추가 |

- 수정하지 않음: `Note`, `NoteData`, `ChartLoader`, `Judgement`, `ScoreComboState`, `AutoPlayer`, `NoteSpeed`, `PauseController`, `ResultTransition`, `ResultScreen`, `PlayResult`.
- 배치 (2026-11-15 확정): SPEED 줄 아래는 1080 화면 밖으로 나가므로, 왼쪽 곡 목록 아래 같은 높이(y=-900)에 SPEED 줄과 같은 모양으로 둔다: `-`(x=120~220) / `OFFSET 0 ms`(x=230~590) / `+`(x=600~700).

## 4. 상세 구현 방법 및 필요한 코드 스니펫
아래 코드는 설계 예시이며 구현 시 기존 코드에 맞춰 조정한다.

### 4.1 OffsetSetting
`NoteSpeed`와 같은 구조로 만든다.
```csharp
// 오프셋(싱크 보정) 설정. -200 ~ +200ms, 5ms 단위, 기본 0. PlayerPrefs에 저장된다.
public static class OffsetSetting
{
    public const int Min = -200;
    public const int Max = 200;
    public const int Step = 5;
    public const int Default = 0;
    private const string PrefsKey = "TimingOffsetMs";

    private static int value;
    private static bool loaded;

    public static int Value { get { EnsureLoaded(); return value; } }
    public static string Label => "OFFSET " + Value.ToString("+0;-0;0") + " ms"; // 예: OFFSET +15 ms

    public static void Increase() => Set(Value + Step);
    public static void Decrease() => Set(Value - Step);

    // 5ms 단위로 맞추고 범위를 넘지 않게 자른다.
    private static int Snap(int v) => Mathf.Clamp(Mathf.RoundToInt(v / (float)Step) * Step, Min, Max);
    // Set / EnsureLoaded / ResetOnPlay(SubsystemRegistration)는 NoteSpeed와 같은 방식
}
```

### 4.2 GameplayManager
```csharp
private double offsetSeconds; // 이번 플레이에 적용하는 보정 시간(초). 음악 없는 곡은 0

// Start: 채보·음악을 정한 뒤, prevSongTime 초기화 전에
offsetSeconds = UseAudioClock ? OffsetSetting.Value / 1000.0 : 0.0;

// 보정된 곡 시간. +100ms면 채보 10.000초 노트가 음악 10.100초에 판정선에 도착한다.
public double SongTime => IsPaused ? pausedSongTime
    : (UseAudioClock ? audioSource.time - offsetSeconds : Time.timeAsDouble - startTime);
```
- `Update`, `TryPause`(보정된 곡 시간 저장), `TryResume`, `prevSongTime`은 그대로 → 모두 같은 보정 시간축을 쓴다.
- 오디오 재생 위치와 채보 원본 시각은 바꾸지 않는다.

### 4.3 곡 선택 화면
- `SongSelectScreen.Start`에서 버튼에 `OffsetDown`/`OffsetUp`을 연결하고 `OFFSET` 표시를 갱신한다 (SPEED와 같은 방식).
- 키 설정 패널, START, 곡 선택, 배속 조작은 그대로 둔다.

### 4.4 탭 보정 (2단계)
```csharp
// OffsetCalibration 핵심 (예시)
private const double BeatInterval = 0.5;   // 120 BPM
private const int LeadInBeats = 4;         // 준비 박자 (세지 않음)
private const int CountedBeats = 16;
private const double MaxError = 0.25;      // ±250ms 밖은 실수로 제외
private readonly List<double> errors = new List<double>();

// 박자 i(0부터)의 시각: 클립 시작부터 LeadIn을 포함해 i * BeatInterval (+ 클립 앞 여유)
// SPACE를 누른 순간의 메트로놈 재생 위치로 가장 가까운 '세는 박자'와의 차이를 기록한다.
public void RegisterTap(double audioTime)
{
    int beat = (int)System.Math.Round((audioTime - firstBeatTime) / BeatInterval);
    if (beat < LeadInBeats || beat >= LeadInBeats + CountedBeats) return; // 준비 박자·범위 밖
    double error = audioTime - (firstBeatTime + beat * BeatInterval);
    if (System.Math.Abs(error) > MaxError) return;                          // 크게 빗나감
    errors.Add(error);
}

// 결과: 중앙값(ms)을 5ms 단위로. 유효 8번 미만이면 결과 없음.
```
- 메트로놈 클립: `AudioClip.Create`로 준비 4박 + 16박 + 끝 여유 길이의 모노 클립을 만들고, 박자마다 짧은 클릭(감쇠하는 사인파)을 써 넣는다. 첫 박자 앞에 약간의 여유를 둔다.
- 노트 표시: Canvas 안 UI 이미지. 박자마다 노트 1개가 `메트로놈 재생 위치` 기준으로 판정선에 도착하도록 위치를 계산한다 (오프셋 0 기준).
- SPACE: `Keyboard.current.spaceKey.wasPressedThisFrame`이면 `RegisterTap(audioSource.time)`. 한 박자에 여러 번 누르면 처음 1번만 센다.
- 16박이 지나면 결과를 보여 주고 버튼을 활성화한다. APPLY는 `OffsetSetting.Set(결과)` 후 곡 선택 씬, CANCEL은 저장 없이 곡 선택 씬, RETRY는 기록을 지우고 처음부터.
- 정확도 한계: 누른 시각은 프레임 단위로 잡혀 1회당 최대 약 16ms(60fps 기준) 오차가 있다. 16회 중앙값으로 줄이지만 정밀 측정 방식은 아니다.
- (2026-11-15 수정) 노트 표시는 `Note` 프리팹 20개를 미리 만들고 `Note.UpdatePosition(시각, 판정선 Y, 낙하 속도)`로 위치를 계산한다. 낙하 속도 = 5 × 배속 ÷ 3 (`GameplayManager.FallSpeed`와 같은 식). 생성 높이에 닿기 전과 판정선을 0.1초 지난 뒤에는 숨긴다.
- 레인 위치·생성 높이·판정선·기본 속도는 `GameplayManager` 인스펙터 값과 같은 값을 `OffsetCalibration`에도 둔다 (인게임 값을 바꾸면 함께 바꿔야 함).
- 박자 k의 레인 = k % 4. 레인 키 입력은 `KeyBindings.Get(레인).wasPressedThisFrame`.

## 5. 구현 순서 및 진행 상태
- [x] GPT 초안 검토, 로컬 소스·채보 대조, 계획 작성, 정책·배치 확정 (2026-11-15)
- [x] `OffsetSetting` 작성 → 컴파일 확인 (에러 0건). Play 시작 시 PlayerPrefs에서 다시 불러오도록 static 초기화 포함
- [x] `GameplayManager` 보정 시간 적용 (`offsetSeconds`를 `Start`에서 음악 있는 곡만 고정, `SongTime`에서 빼기) → 컴파일 확인 (에러 0건)
- [x] `SongSelectScreen` 수정(`offsetText`/`offsetDownButton`/`offsetUpButton`, `OffsetDown`/`OffsetUp`), `SongSelectScene`에 SPEED 줄을 복제해 `OffsetDownButton` / `OffsetText` / `OffsetUpButton` 추가(y=-900, x=120 / 230 / 600, `KeySettingPanelRoot`보다 뒤에 그려져 패널이 위를 덮음) → 참조 연결 확인
- [x] 검증 (검증 후 PlayerPrefs `TimingOffsetMs`를 검증 전 상태(저장값 없음)로 되돌림. `NoteSpeed` 저장값은 건드리지 않음)

- 2단계 탭 보정:
- [x] 예시 이미지·기존 코드 확인, 정책 확정, 계획 작성 (2026-11-15)
- [x] `OffsetSetting.Set` 공개, `Increase`/`Decrease` 제거 → 컴파일 확인 (에러 0건)
- [x] `OffsetCalibration` 작성 (메트로놈 클립 생성: 첫 박자 1.5초, 준비 박자 880Hz·세는 박자 1320Hz 클릭, 노트 20개, `RegisterTap`, 중앙값, APPLY/RETRY/CANCEL, Esc) → 컴파일 확인 (에러 0건)
- [x] `OffsetCalibrationScene` 생성: 카메라(곡 선택 씬과 같은 배경색), Canvas(1920×1080 기준), `Lane`(280×600) 안 `NoteTemplate`·`JudgeLine`, `SpaceKey`, `CounterText`(판정선 오른쪽), `CurrentOffsetText`, `GuideText`, `ResultPanel`(결과 글자 + 버튼 3개, 기본 비활성), EventSystem(Input System UI 모듈), `OffsetCalibration` 오브젝트(AudioSource 포함). 참조 10개 연결. Build Settings 3번 등록 (SongSelect 0 / Ingame 1 / Result 2 / OffsetCalibration 3)
- [x] `SongSelectScreen`: `offsetDownButton`/`offsetUpButton`·`OffsetDown`/`OffsetUp` 제거, `autoSyncButton`·`OpenAutoSync`(키 설정 패널이 열려 있으면 무시) 추가. `SongSelectScene`: -/+ 버튼 삭제, `OffsetText`를 x=120으로, `AutoSyncButton`(x=490, 260×90, 글자 32) 추가 → 참조 확인
- [x] 검증 (검증 후 PlayerPrefs `TimingOffsetMs`를 검증 전 상태(저장값 없음)로 되돌림)
- 사용자 확인에서 발견한 문제 (2026-11-15): ① 소리가 안 남 → 원인: 보정 씬 카메라에 AudioListener 없음(빈 씬에서 만들며 누락, 검증에서는 재생 위치만 보고 놓침) ② 입력을 인게임과 같게(D/F/J/K, 4레인) ③ 노트 간격이 짧음 → BPM은 120 유지, 인게임 속도·4레인 순환으로 화면상 간격을 넓힘
- [x] 수정: `OffsetCalibration` 4레인·인게임 `Note` 프리팹·레인 키(`KeyBindings`)·`RegisterTap(레인, 시각)`, 안내 문구에 실제 키 이름, 첫 박자 1.5→2.0초(노트가 생성 높이에서 내려올 시간 확보) → 컴파일 확인 (에러 0건)
- [x] 수정: 보정 씬 카메라에 AudioListener 추가, 카메라 크기·위치·배경색을 IngameScene과 같게, IngameScene의 `JudgeLine`·`Global Light 2D` 복사, UI `Lane`·`SpaceKey` 삭제, 카운터를 판정선 높이 오른쪽으로, `notePrefab`·`guideText` 연결 → 참조 확인 (IngameScene은 변경 없음)
- [x] 수정 후 검증
- 수정 중 발견·수정: 예전 UI 버전의 `judgeLineY`(60px) 값이 씬에 남아 새 코드의 판정선이 y=60이 되어 노트가 전혀 표시되지 않음 → 씬 값을 fallSpeed 5 / spawnY 6 / judgeLineY -3 / laneXs -1.5~1.5로 설정
- 처음 구현 검증 중 수정: 카운터 글자가 판정선 끝과 겹쳐 x=330→420으로 이동, AUTO SYNC 글자가 버튼을 넘쳐 52→32로 줄임, 노트가 레인 위 끝에서 반쯤 삐져나와 보여 노트 전체가 레인 안에 있을 때만 표시하도록 수정

- 완료: 1단계·2단계 계획·구현·Claude 검증
- 진행 중: 없음
- 남은 작업: 사용자 확인 (실제로 쳐서 측정, 메트로놈·음악 청취, 화면 배치)

## 6. 테스트 및 검증 방법
확인 주체: C = Claude(Play 모드에서 함수·버튼 `onClick` 호출, AUTO TEST), U = 사용자.
제약: 에디터 일시정지 중에는 오디오가 멈춰 kick은 실시간으로 진행해야 하며, 에디터에 포커스가 없으면 프레임이 진행되지 않거나 확인 명령 중 메인 스레드가 멈출 수 있다(pause_restart_plan 7장). 그래서 보정 공식은 실시간 플레이 대신 `SongTime`과 `audioSource.time`의 차이를 직접 읽어 확인한다.

| ID | 테스트 | 기대 결과 | 주체 | 상태 |
|---|---|---|---|---|
| OF-01 | 저장값 없음 | 0ms, 표시 `OFFSET 0 ms` | C | 통과 (`OFFSET 0 ms`) |
| OF-02 | -/+ 버튼과 경계 (1단계) | 5ms씩 변경, ±200ms에서 멈춤, 표시 갱신 | C | 통과 (버튼 `onClick` 호출: - 42번 → -200에서 멈춤, + 85번 → +200에서 멈춤, - 1번 → +195). 2단계에서 -/+ 버튼은 제거됨 |
| OF-03 | 저장·잘못된 저장값 | Play 재시작 후 유지, 범위 밖·5ms 단위 아님은 가장 가까운 값으로 | C | 통과 (PlayerPrefs 200 저장, Play 재시작 후 +195 유지. 저장값 7→5, 13→15, 250→200, -333→-200, 42→40) |
| OF-04 | +100ms, kick | `SongTime = audioSource.time - 0.1` | C | 통과 (+100ms: `audio.time - SongTime` = 0.1000. +200ms 실시간 플레이 중에도 0.2000) |
| OF-05 | -100ms, kick | `SongTime = audioSource.time + 0.1` | C | 통과 (-100ms: 차이 -0.1000. -200ms 실시간 플레이 중 -0.2000) |
| OF-06 | 0ms | 기존과 같음 | C | 통과 (0ms: 차이 0.0000) |
| OF-07 | 음악 없는 곡에 오프셋 설정 | 보정 0, AUTO 3단계 기준값 그대로 | C | 통과 (+100ms 설정에서 Test Song 1 `offsetSeconds` 0, AUTO 3단계 SS / 35800 / 콤보 61 / Tick 36 "일치") |
| OF-08 | 오프셋 적용 중 일시정지·재개 | 곡 시간 고정 후 이어짐 | C | 통과 (-200ms kick 실시간 24.24초에서 정지 → 4초 동안 곡 시간·음악 고정, 재개 후 차이 -0.2000 유지, 끝까지 263200) |
| OF-09 | RESTART / RETRY / SONG SELECT | 오프셋 설정 유지 | C | 통과 (SONG SELECT 후 표시 유지, RETRY·RESTART 후 값 100 유지) |
| OF-10 | 배속 변경과 함께 사용 | 속도만 바뀌고 보정 시간 그대로 | C | 미검증 (배속을 바꿔 가며 노트 위치를 재지 않음. 배속은 낙하 속도에만, 오프셋은 곡 시간에만 들어가 서로 독립임을 코드로만 확인) |
| OF-11 | kick AUTO 3단계 (0 / +200 / -200ms) | 판정·틱·점수 같음 (처리가 깨지지 않는지만 확인. 싱크 확인 아님) | C | 통과 (+200ms·-200ms 모두 실시간 AUTO 3단계 SS / 263200 / 콤보 364 / Perfect 220 / Tick 144 "일치". 0ms 실시간 전체 플레이는 이번에 실행하지 않음(오프셋 적용 코드는 0이면 기존과 같은 식)) |
| OF-12 | 실제 음악을 들으며 조절 | 부호 방향과 체감 싱크 | U | 미검증 (음악 청취 필요, 사용자 확인) |
| OF-13 | 실제 키 입력 | 보이는 위치와 판정이 맞음 | U | 미검증 (실제 키 입력 필요, 사용자 확인) |
| OF-14 | 화면 배치 | OFFSET 줄이 곡 목록·SPEED와 겹치지 않음 | U (C는 스크린샷) | 미검증 (스크린샷으로 곡 목록 아래 왼쪽에 겹침 없이 표시되는 것은 확인. 실제 화면 비율은 사용자 확인) |
| OF-15 | 컴파일·Play 모드·씬 참조 | 새 에러·경고 0건, 참조 누락 없음 | C | 통과 (컴파일 에러 0건, Play 중 에러·경고 0건, 참조 누락 없음, 검증 후 씬 변경 남지 않음) |

### 탭 보정 테스트 (2단계)
제약: SPACE는 Claude가 누를 수 없다(주입한 키는 `wasPressedThisFrame`에 반영되지 않음) → `RegisterTap(시각)`을 직접 호출해 "박자보다 정해진 시간만큼 늦게/일찍 누른" 입력을 넣는다.

| ID | 테스트 | 기대 결과 | 주체 | 상태 |
|---|---|---|---|---|
| TC-01 | 곡 선택 화면 AUTO SYNC | 보정 씬으로 이동, 현재 OFFSET 표시 | C | 통과 (AutoSyncButton → OffsetCalibrationScene, `CURRENT OFFSET 0 ms`, 카운터 READY) |
| TC-02 | 메트로놈·노트 (2026-11-15 수정 후 다시) | 클릭 0.5초 간격, 노트가 레인 1→2→3→4 순서로 클릭 시각에 판정선 도착, 배속 반영, `N / 16` 증가, AudioListener 있음 | C(시각 계산·컴포넌트) / U(청취·화면) | 미검증 (수정 후: AudioListener 있음 확인. 노트 위치 계산 함수를 정해진 시각으로 호출해 확인: t=3.0초에 Note2~5가 x=0.5/1.5/-1.5/-0.5(레인 3→4→1→2 순서), y가 기대값과 모두 일치, x3.0 속도 5.00·x1.0 속도 1.67. 실제 소리와 노트가 맞는지는 에디터 포커스가 없어 실시간 확인 불가, 사용자 확인 필요) |
| TC-03 | 16박 모두 +40ms 늦게 입력 | RESULT +40 ms | C | 통과 (수정 후 다시: RESULT +40 ms, VALID TAPS 16 / 16) |
| TC-04 | 16박 모두 -25ms 일찍 입력 | RESULT -25 ms | C | 통과 (RESULT -25 ms) |
| TC-05 | 대부분 +30ms, 2~3번은 크게 빗나감(±250ms 밖·근처) | 결과 +30 ms 근처 (중앙값, 빗나간 입력 제외) | C | 통과 (+300ms 1개 제외, +200·-240 포함 15개 → 중앙값 +30 ms) |
| TC-06 | 준비 4박 중 입력, 한 박자에 2번 입력 | 세지 않음 / 1번만 셈 | C | 통과 (준비 박자 입력 2개 무시, 박자 5의 두 번째 입력 무시 → 유효 15개, RESULT +20 ms) |
| TC-07 | 유효 입력 7번 | NOT ENOUGH TAPS, APPLY 비활성 | C | 통과 (NOT ENOUGH TAPS, VALID TAPS 7 / 16 (NEED 8), APPLY 비활성) |
| TC-08 | 16박 모두 +220ms 늦게 입력 (±250 안이라 제외되지 않지만 ±200 초과) | +200으로 자르고 범위 초과 표시 | C | 통과 (RESULT +200 ms + OUT OF RANGE (LIMIT -200 ~ +200 ms)) |
| TC-09 | APPLY | `OffsetSetting` 저장, 곡 선택 화면에 새 값 표시, 다음 플레이에 적용 | C | 통과 (APPLY → 곡 선택 화면 `OFFSET +40 ms`, PlayerPrefs 40, kick 시작 시 `offsetSeconds` 0.040) |
| TC-10 | CANCEL / Esc(함수 호출) | 기존 값 유지, 곡 선택 화면 | C / U(Esc) | 통과 (함수 호출: -50 ms 측정 후 CANCEL → 곡 선택 화면 +40 ms 유지). 실제 Esc는 미검증 (Esc 주입 불가, 사용자 확인) |
| TC-11 | RETRY | 기록 초기화 후 처음부터 다시 | C | 통과 (RETRY → 결과 숨김, 유효 입력 0, 메트로놈 처음부터) |
| TC-12 | 실제로 쳐서 측정 | 체감과 맞는 결과, 적용 후 kick 싱크 개선 | U | 미검증 (실제 입력 필요, 사용자 확인) |
| TC-13 | 화면 배치 | 예시 이미지와 비슷한 구성, 곡 선택 화면 OFFSET 줄 겹침 없음 | U (C는 스크린샷) | 미검증 (수정 후 스크린샷: 인게임과 같은 배경·판정선·노트, 레인 4개에 노트가 순서대로 표시, 카운터는 판정선 오른쪽. 실제 화면은 사용자 확인) |
| TC-14 | 컴파일·Play 모드·씬 참조·Build Settings | 새 에러·경고 0건, 참조 누락 없음, 씬 4개 등록 | C | 통과 (컴파일 에러 0건, Play 중 에러·경고 0건, 참조 누락 없음, Build Settings 씬 4개, 저장 후 글꼴 참조 정상) |
| TC-16 | 다른 레인 키 입력 (2026-11-15 추가) | 노트 레인과 다른 키는 무시 | C | 통과 (16박 +30ms 중 6박을 다른 레인 키로 입력 → 유효 11개, RESULT +30 ms) |
| TC-17 | 노트 사라짐 (2026-11-15 추가) | 측정에 들어간 입력이면 그 박자 노트가 숨겨지고 다시 나타나지 않음, 무시된 입력이면 노트 유지 | C | 통과 (함수 직접 호출: 박자6을 레인 키로 +20ms에 입력 → Note6 즉시 숨김, 다시 계산해도 안 나타남. 다른 레인 키·범위 밖·같은 박자 두 번째·준비 박자 입력은 노트 유지. 박자7 -10ms 입력 → Note7 숨김. 실제 키로 치는 화면은 사용자 확인) |
| TC-15 | 회귀 | kick·Test Song AUTO 3단계 기준값, 곡 선택 기존 조작 | C | 통과 (수정 후 다시: Test Song 1 AUTO 3단계 SS / 35800 / 콤보 61) |

### 회귀 기준 (AUTO 3단계)
| 곡 | 점수 | 최대 콤보 | 판정 |
|---|---:|---:|---|
| Test Song 1 | 35,800 | 61 | Perfect 25, Tick 36 |
| Test Song 2 | 9,800 | 14 | Perfect 8 |
| kick | 263,200 | 364 | Perfect 220 |

- kick은 일시정지 포함 회귀가 미검증 상태이므로 0ms 기준을 먼저 다시 확인한다. 실시간 검증이 되지 않으면 그 사유를 기록한다.
- AUTO TEST만으로 실제 싱크·키 입력이 정상이라고 판단하지 않는다 (CLAUDE.md 6.3).

- [x] Unity 컴파일 오류 확인 (1단계)
- [ ] 실제 플레이에서 기능 검증 (음악 청취·실제 키 입력은 사용자 확인)
- [x] 관련 기존 기능 회귀 테스트 (1단계: Test Song 1 35800, kick ±200ms 263200, 일시정지 포함)
- [x] 2단계 탭 보정 컴파일·기능·회귀 검증 (실제 입력·청취는 사용자 확인)

## 7. 검증 결과 및 미해결 사항
- 검증 결과 (2026-11-15): 버튼은 `onClick` 호출, 보정 공식은 `SongTime`과 `audioSource.time`을 직접 읽어 비교, kick은 에디터 일시정지를 풀고 실시간 진행. 상세는 6장 표.
  - kick -200ms + 일시정지 첫 시도: 259200 / Miss 4. Miss는 모두 정지 명령 직전 약 1초 안의 노트(30.857~31.286초, 정지 31.688초). 처음 보내는 확인 명령은 Unity 안에서 컴파일되는 동안 메인 스레드를 멈추고 그동안 음악만 진행해, 프레임이 판정 범위를 건너뛴 것(검증 방법의 영향).
  - 같은 정지·재개 명령을 노트가 없는 인트로(1초)에서 미리 한 번 실행한 뒤 24.24초에 정지·재개 → 263200 / Miss 0. 위 원인이 맞음을 확인. (pause_restart_plan의 kick 미검증 항목도 이 결과로 확인됨)
- 미해결:
  - 음악 종료 후 `audioSource.time`이 0으로 돌아가는 잠재 문제는 이번 범위 밖 (2장 주의할 점 3)
  - 사용자 확인 필요: 음악을 들으며 싱크 조절(부호 방향 포함), 실제 키 입력, 화면 배치
- 2단계 탭 보정 검증 결과 (2026-11-15): 결과 계산은 `RegisterTap(시각)`을 직접 호출해 정해진 차이로 입력(에디터 일시정지 상태), 버튼은 함수 호출. 상세는 6장 탭 보정 표. 결과 계산 검증 뒤에 화면 수정(카운터 위치, 버튼 글자 크기, 노트 표시 조건)을 했고, 수정 후 컴파일·Play 모드 에러 0건과 스크린샷을 다시 확인함.
  - 미해결: 실제로 SPACE를 쳐서 측정, 메트로놈 소리와 노트가 맞는지, 실제 Esc, 화면 배치 (사용자 확인 필요)
- 사용자 확인 문제 수정 후 검증 (2026-11-15): TC-01·03~11·14~16을 다시 실행해 모두 같은 결과(레인 키 입력은 `RegisterTap(레인, 시각)` 직접 호출). AudioListener 추가 확인. 노트 위치·레인 순서·배속 반영 확인(TC-02). 컴파일·Play 모드 에러 0건, IngameScene 변경 없음, 검증 후 `TimingOffsetMs`·`NoteSpeed` 저장값 원상태.
  - 미해결: 실제로 소리가 들리는지, 실제 레인 키로 측정, 화면에서 노트·소리 싱크 (사용자 확인 필요)
- 노트 사라짐 추가 (2026-11-15, 사용자 요청): `RegisterTap`에서 측정에 들어간 입력이면 그 박자 노트를 숨기고, `UpdateNotes`에서 이미 친 박자는 다시 표시하지 않음. 컴파일·Play 모드 에러 0건, TC-17 통과. 결과 계산은 바뀌지 않음.
  - 정확도 한계: 누른 시각은 프레임 단위(60fps 기준 1회당 최대 약 16ms 오차), 16회 중앙값으로 줄임
- 변경 이력: GPT 초안(사용자 제공) → 2026-11-15 로컬 소스·채보 대조 검토, 음악 종료 후 시간 처리·시작 전 판정 위험 항목 제거(채보로 해당 없음 확인), 곡 선택 화면 배치 제안 추가, 형식을 기존 계획서에 맞춤 → 2026-11-15 제안값·배치 사용자 확정 → 구현 및 검증 → 2026-11-15 -/+ 직접 조절을 탭 보정(자동 싱크)으로 대체하기로 결정(사용자), 2단계 계획 추가 → 2단계 구현 및 검증 → 2026-11-15 사용자 확인 문제 3건(소리, 인게임과 같은 4레인·레인 키, 노트 간격) 수정 계획 반영 → 수정 및 검증 → 2026-11-15 맞게 친 노트가 사라지는 동작 추가(사용자 요청)
- 2026-11-16 설정 화면 통합(settings_save_plan.md): OFFSET 표시·AUTO SYNC 버튼을 곡 선택 화면에서 설정 화면의 오프셋 패널로 이동, `SongSelectScreen`의 `offsetText`·`autoSyncButton`·`OpenAutoSync`·`UpdateOffsetText` 제거. `OffsetCalibration`의 복귀 씬 필드를 `songSelectSceneName` → `settingsSceneName`("SettingsScene"), `LoadSongSelect` → `LoadSettings`로 변경(측정·저장 로직 변경 없음). 4.3절과 6·7절 검증 기록은 당시(곡 선택 화면) 기준으로 그대로 둠. 이동 후 AUTO SYNC·APPLY·CANCEL·RETRY는 settings_save_plan.md ST-12~16에서 다시 확인(C 통과).
