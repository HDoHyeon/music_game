# 오프셋(싱크 보정) 설정 구현 계획서

## 1. 작업 목적 및 요구사항
- 프로젝트: music_game (Unity 6.6, C#, URP, Mono, Input System)
- 대상: PLAN.md 2-2. 플레이 안정성 및 데이터 구조 / 1. 오프셋(싱크 보정) 설정
- 목적: 음악과 노트의 시간 차이(오디오 출력 지연, 사용자 환경 차이)를 사용자 설정으로 보정한다.
- 상태: 구현 완료(2026-11-15) / 사용자 확인 대기 (음악을 들으며 싱크 조절, 실제 키 입력, 화면 배치)
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
| 설정 위치 | 곡 선택 화면에 `OFFSET 0 ms` 형식 표시(양수는 `+15`처럼 부호 표시)와 -/+ 버튼 (키보드 단축키 없음. ←→는 배속이 사용 중) |
| 적용 시점 | 플레이 시작 시 값을 읽어 그 플레이 동안 고정. 플레이 중·일시정지 중 변경 없음 |
| 저장 | PlayerPrefs `TimingOffsetMs`, 전체 곡 공통. 저장값이 범위 밖이거나 5ms 단위가 아니면 가장 가까운 값으로 맞춤 |

- 제외 범위: 자동 싱크 측정, 곡별 오프셋, 표시·입력 분리 오프셋, 일시정지 중 변경, 설정 화면 통합, 음악 종료 후 시간 처리 보완(아래 2장 주의할 점 3).

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

## 3. 작업 범위 및 수정 대상 파일
| 파일 | 구분 | 내용 |
|---|---|---|
| `Assets/Scripts/Gameplay/OffsetSetting.cs` | 신규 | 오프셋 값(ms), 범위·단위, 올리기·내리기, 저장·불러오기, 표시 문자열 |
| `Assets/Scripts/Gameplay/GameplayManager.cs` | 수정 | `Start`에서 오프셋을 읽어 고정(음악 있는 곡만), `SongTime`에서 보정 시간 빼기 |
| `Assets/Scripts/Song/SongSelectScreen.cs` | 수정 | `offsetText`, `offsetDownButton`, `offsetUpButton` 연결과 표시 갱신 |
| `Assets/Scenes/SongSelectScene.unity` | 수정 | `OffsetDownButton`(-) / `OffsetText` / `OffsetUpButton`(+) 추가 |

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

## 5. 구현 순서 및 진행 상태
- [x] GPT 초안 검토, 로컬 소스·채보 대조, 계획 작성, 정책·배치 확정 (2026-11-15)
- [x] `OffsetSetting` 작성 → 컴파일 확인 (에러 0건). Play 시작 시 PlayerPrefs에서 다시 불러오도록 static 초기화 포함
- [x] `GameplayManager` 보정 시간 적용 (`offsetSeconds`를 `Start`에서 음악 있는 곡만 고정, `SongTime`에서 빼기) → 컴파일 확인 (에러 0건)
- [x] `SongSelectScreen` 수정(`offsetText`/`offsetDownButton`/`offsetUpButton`, `OffsetDown`/`OffsetUp`), `SongSelectScene`에 SPEED 줄을 복제해 `OffsetDownButton` / `OffsetText` / `OffsetUpButton` 추가(y=-900, x=120 / 230 / 600, `KeySettingPanelRoot`보다 뒤에 그려져 패널이 위를 덮음) → 참조 연결 확인
- [x] 검증 (검증 후 PlayerPrefs `TimingOffsetMs`를 검증 전 상태(저장값 없음)로 되돌림. `NoteSpeed` 저장값은 건드리지 않음)

- 완료: 계획, 구현, Claude 검증
- 진행 중: 없음
- 남은 작업: 사용자 확인 (음악을 들으며 싱크 조절, 실제 키 입력, 화면 배치)

## 6. 테스트 및 검증 방법
확인 주체: C = Claude(Play 모드에서 함수·버튼 `onClick` 호출, AUTO TEST), U = 사용자.
제약: 에디터 일시정지 중에는 오디오가 멈춰 kick은 실시간으로 진행해야 하며, 에디터에 포커스가 없으면 프레임이 진행되지 않거나 확인 명령 중 메인 스레드가 멈출 수 있다(pause_restart_plan 7장). 그래서 보정 공식은 실시간 플레이 대신 `SongTime`과 `audioSource.time`의 차이를 직접 읽어 확인한다.

| ID | 테스트 | 기대 결과 | 주체 | 상태 |
|---|---|---|---|---|
| OF-01 | 저장값 없음 | 0ms, 표시 `OFFSET 0 ms` | C | 통과 (`OFFSET 0 ms`) |
| OF-02 | -/+ 버튼과 경계 | 5ms씩 변경, ±200ms에서 멈춤, 표시 갱신 | C | 통과 (버튼 `onClick` 호출: - 42번 → -200에서 멈춤, + 85번 → +200에서 멈춤, - 1번 → +195) |
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

### 회귀 기준 (AUTO 3단계)
| 곡 | 점수 | 최대 콤보 | 판정 |
|---|---:|---:|---|
| Test Song 1 | 35,800 | 61 | Perfect 25, Tick 36 |
| Test Song 2 | 9,800 | 14 | Perfect 8 |
| kick | 263,200 | 364 | Perfect 220 |

- kick은 일시정지 포함 회귀가 미검증 상태이므로 0ms 기준을 먼저 다시 확인한다. 실시간 검증이 되지 않으면 그 사유를 기록한다.
- AUTO TEST만으로 실제 싱크·키 입력이 정상이라고 판단하지 않는다 (CLAUDE.md 6.3).

- [x] Unity 컴파일 오류 확인
- [ ] 실제 플레이에서 기능 검증 (음악 청취·실제 키 입력은 사용자 확인)
- [x] 관련 기존 기능 회귀 테스트 (Test Song 1 35800, kick ±200ms 263200, 일시정지 포함)

## 7. 검증 결과 및 미해결 사항
- 검증 결과 (2026-11-15): 버튼은 `onClick` 호출, 보정 공식은 `SongTime`과 `audioSource.time`을 직접 읽어 비교, kick은 에디터 일시정지를 풀고 실시간 진행. 상세는 6장 표.
  - kick -200ms + 일시정지 첫 시도: 259200 / Miss 4. Miss는 모두 정지 명령 직전 약 1초 안의 노트(30.857~31.286초, 정지 31.688초). 처음 보내는 확인 명령은 Unity 안에서 컴파일되는 동안 메인 스레드를 멈추고 그동안 음악만 진행해, 프레임이 판정 범위를 건너뛴 것(검증 방법의 영향).
  - 같은 정지·재개 명령을 노트가 없는 인트로(1초)에서 미리 한 번 실행한 뒤 24.24초에 정지·재개 → 263200 / Miss 0. 위 원인이 맞음을 확인. (pause_restart_plan의 kick 미검증 항목도 이 결과로 확인됨)
- 미해결:
  - 음악 종료 후 `audioSource.time`이 0으로 돌아가는 잠재 문제는 이번 범위 밖 (2장 주의할 점 3)
  - 사용자 확인 필요: 음악을 들으며 싱크 조절(부호 방향 포함), 실제 키 입력, 화면 배치
- 변경 이력: GPT 초안(사용자 제공) → 2026-11-15 로컬 소스·채보 대조 검토, 음악 종료 후 시간 처리·시작 전 판정 위험 항목 제거(채보로 해당 없음 확인), 곡 선택 화면 배치 제안 추가, 형식을 기존 계획서에 맞춤 → 2026-11-15 제안값·배치 사용자 확정 → 구현 및 검증
