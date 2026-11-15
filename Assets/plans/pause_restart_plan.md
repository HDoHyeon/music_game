# 일시정지 및 재시작 구현 계획서

## 1. 작업 목적 및 요구사항
- 프로젝트: music_game (Unity 6.6, C#, URP, Mono, Input System)
- 대상: PLAN.md 2-2. 플레이 안정성 및 데이터 구조 / 2. 일시정지 및 재시작
- 목적: 플레이 중 음악과 게임 진행을 함께 멈추고, 이어서 하기·처음부터 다시 하기·곡 선택으로 돌아가기를 할 수 있게 한다.
- 상태: 구현 완료(2026-11-15) / 사용자 확인 대기 (실제 Esc, 정지 중 레인 키, 메뉴 클릭 차단, 화면 배치, 음악 청취)
- 이 문서는 개발 진행에 따라 수정한다. 작업 범위 확대는 사용자 확인 후 반영한다.
- 작성 경위: GPT 초안을 실제 로컬 소스와 대조해 검토하고, 사용자 결정(2026-11-15)을 반영해 다시 작성.

### 요구사항
- 기존 판정, 점수, 콤보, 롱노트, 배속 및 결과 전환 규칙은 유지한다.
- 확정 사항 (2026-11-15 사용자 승인):

| 항목 | 확정값 |
|---|---|
| 일시정지 | 플레이 중 Esc |
| 일시정지 메뉴 | RESUME / RESTART / SONG SELECT 3개 버튼 (마우스 클릭) |
| 재개 | RESUME 버튼 또는 메뉴에서 Esc → 3-2-1 카운트다운(3초) 후 정지한 곡 시간부터 이어서 진행 |
| 카운트다운 중 | 게임은 계속 정지 상태. 이 동안 레인 키를 다시 잡을 수 있음. Esc를 누르면 카운트다운을 취소하고 메뉴로 돌아감 |
| 롱노트 | 정지 중 키를 떼도 Miss·틱 없음. 카운트다운이 끝나는 순간부터 실제 키 유지 여부로 기존 판정 적용 (유지 중이 아니면 기존 규칙대로 끝 Miss) |
| RESTART | 같은 곡을 처음부터 (씬 다시 불러오기) |
| SONG SELECT | 곡 선택 화면으로 돌아감. 고른 곡은 선택된 상태 유지 |
| 중단한 플레이 | 결과로 저장하지 않음 (`PlayResult.Last`는 다음 정상 완료까지 유지) |
| 결과 전환 중 | 결과 전환(1초 대기·페이드)이 시작되면 일시정지 불가 |
| 일시정지 중 배속 변경 | 없음 (만들지 않는 기능) |
| AUTO TEST | RESTART 시 씬 기본 단계(0)로 돌아감 (`AutoPlayer.Level`은 씬 저장값) |

## 2. 기존 코드 분석
| 파일 | 현재 동작 및 영향 |
|---|---|
| `GameplayManager.cs` | `Start`에서 `SongSelection.Selected`의 채보·음악 적용 후 재생. `SongTime`은 음악이 있으면 `audioSource.time`, 없으면 `Time.timeAsDouble - startTime`. `Update`: `SpawnNotes → MoveNotes → HandleInput → HandleHolds → HandleMisses → prevSongTime 갱신`. 정지 상태 없음. `IsMusicFinished => !UseAudioClock \|\| !audioSource.isPlaying` |
| `ResultTransition.cs` | `IsFinished && IsMusicFinished`이면 `finishedElapsed`를 0부터 늘려 1초 대기, 0.5초 페이드 후 `PlayResult.Last` 저장·ResultScene 로드. `loading`으로 중복 로드 방지 |
| `AutoPlayer.cs` | 노트별 누를·뗄 시각 계획. `WasPressed`는 `prevTime < PressTime <= songTime` 구간 사용. 완료 로그는 `IsFinished`가 된 뒤 한 번만 |
| `AutoTestPanel.cs` | AUTO 단계 버튼. 클릭 시 `AutoPlayer.Level` 변경 |
| `ResultScreen.cs` | RETRY는 `SceneManager.LoadScene(playSceneName)`, SONG SELECT는 `songSelectSceneName`("SongSelectScene") 로드 |
| `KeyBindings.cs` | Esc는 레인 키로 쓸 수 없음 → 일시정지 키와 겹치지 않음 |
| `IngameScene.unity` | GameplayManager, Music(AudioSource), Canvas(ScoreText, AutoTestPanel, FadeOverlay), EventSystem, AutoPlayer, ScoreComboState |

### 주의할 점
1. `AudioSource.Pause()` 후 `isPlaying`이 false가 되어 지금의 `IsMusicFinished`가 "음악 끝남"으로 잘못 판단한다 → 정지 중에는 false.
2. 음악 없는 곡은 `Time.timeAsDouble` 기준이라 `Update`만 멈추면 재개 시 정지한 시간만큼 곡 시간이 건너뛴다 → 정지한 시간만큼 `startTime`을 뒤로 민다.
3. 정지 중 `prevSongTime`을 갱신하지 않아야 AUTO 입력이 빠지거나 겹치지 않는다.
4. `Time.timeScale = 0`은 쓰지 않는다 (오디오·입력이 함께 멈추지 않고, AUTO 검증에서 `Time.timeScale`을 쓰고 있음).
5. 음악이 끝나면 `audioSource.time`이 0으로 돌아가 음악보다 긴 채보의 끝부분이 처리되지 않는 잠재 문제가 있다. 이번 작업 범위가 아니며 악화시키지 않는다.

## 3. 작업 범위 및 수정 대상 파일
| 파일 | 구분 | 내용 |
|---|---|---|
| `Assets/Scripts/Gameplay/GameplayManager.cs` | 수정 | `IsPaused`, `TryPause`/`TryResume`, 정지 중 곡 시간 고정, `Update` 차단, `IsMusicFinished` 보정 |
| `Assets/Scripts/Gameplay/ResultTransition.cs` | 수정 | 정지 중 종료 판정 중단, 전환 시작 여부 `IsTransitioning` 공개 |
| `Assets/Scripts/Gameplay/PauseController.cs` | 신규 | Esc 입력, 메뉴 표시, 카운트다운, RESUME/RESTART/SONG SELECT, 중복 씬 로드 방지 |
| `Assets/Scenes/IngameScene.unity` | 수정 | `PauseController` 오브젝트, `PausePanel`(반투명 전체 배경 + PAUSED + 버튼 3개, 기본 비활성), `CountdownText`(기본 비활성) |

- 수정하지 않음: `AutoPlayer`(정지 중에는 노트 처리가 멈춰 완료 로그 조건이 바뀌지 않음), `AutoTestPanel`(메뉴가 열려 있는 동안 전체 배경이 뒤쪽 클릭을 막음), `ScoreComboState`, `Note`, `ChartLoader`, `Judgement`, `ResultScreen`, `KeyBindings`, `NoteSpeed`, `SongSelection`, `PlayResult`.
- 제외 범위: 일시정지 중 배속 변경, 메뉴 키보드 조작(↑↓/Enter), 설정 화면, 오프셋 보정, 창 포커스를 잃으면 자동 정지, 최종 UI 디자인, 기록 저장, 패키지·프로젝트 설정 변경.

## 4. 상세 구현 방법 및 필요한 코드 스니펫
아래 코드는 설계 예시이며 구현 시 기존 코드에 맞춰 조정한다.

### 4.1 GameplayManager: 정지와 곡 시간
```csharp
public bool IsPaused { get; private set; }
private double pausedSongTime;  // 정지한 순간의 곡 시간
private double pauseStartedAt;  // 정지한 순간의 Time.timeAsDouble (음악 없는 곡 보정용)
private bool resumeAudio;       // 정지할 때 음악이 재생 중이었는지

// 정지 중에는 정지한 순간의 곡 시간을 그대로 돌려준다.
public double SongTime => IsPaused ? pausedSongTime
    : (UseAudioClock ? audioSource.time : Time.timeAsDouble - startTime);

// 정지 중에는 음악이 멈춰 있어도 "끝남"으로 보지 않는다.
public bool IsMusicFinished => !IsPaused && (!UseAudioClock || !audioSource.isPlaying);

public bool TryPause()
{
    if (IsPaused) return false;
    pausedSongTime = SongTime;
    pauseStartedAt = Time.timeAsDouble;
    resumeAudio = UseAudioClock && audioSource.isPlaying;
    IsPaused = true;
    if (resumeAudio) audioSource.Pause();
    return true;
}

public bool TryResume()
{
    if (!IsPaused) return false;
    if (!UseAudioClock) startTime += Time.timeAsDouble - pauseStartedAt; // 정지한 시간만큼 기준 시각을 민다
    if (resumeAudio) audioSource.UnPause(); // 이미 끝난 음악은 다시 틀지 않음
    resumeAudio = false;
    IsPaused = false;
    return true;
}
```
- `Update` 첫 줄에 `if (IsPaused) return;` → 노트 생성·이동·입력·롱노트·Miss 처리가 모두 멈춘다. `prevSongTime`도 갱신되지 않아 재개 첫 프레임에 정지 직전부터 이어서 AUTO 입력 구간을 처리한다.
- 정지 전후 `audioSource.time`이 이어지는지는 Unity에서 검증한다. 시간이 뒤로 가거나 건너뛰면 원인을 보고하고 보정은 승인 후 결정한다.

### 4.2 PauseController: 입력, 메뉴, 카운트다운
- 상태: 플레이 중 → (Esc) 메뉴 → (RESUME 또는 Esc) 카운트다운 3·2·1 → 재개. 카운트다운 중 Esc → 메뉴로 돌아감.
- 카운트다운 동안 `GameplayManager`는 계속 정지 상태이고 메뉴는 숨기며 숫자만 표시한다. 시간은 `Time.deltaTime` 기준(기존 `ResultTransition`과 동일).
- RESTART: `SceneManager.LoadScene(SceneManager.GetActiveScene().name)`. SONG SELECT: `SceneManager.LoadScene(songSelectSceneName)`(기본값 "SongSelectScene"). 로드 요청 후에는 다른 요청을 무시한다(`loading` 플래그).
- 결과 전환이 시작되었으면(`ResultTransition.IsTransitioning`) Esc를 무시한다.
- Esc는 `Keyboard.current.escapeKey.wasPressedThisFrame`으로 읽는다.

```csharp
// PauseController 핵심 흐름 (예시)
private void Update()
{
    if (loading || transition.IsTransitioning) return;
    if (countdownLeft > 0f)
    {
        countdownLeft -= Time.deltaTime;
        if (countdownLeft <= 0f) { countdownText.SetActive(false); gameplay.TryResume(); }
        else countdownLabel.text = Mathf.CeilToInt(countdownLeft).ToString(); // 3 → 2 → 1
    }
    Keyboard keyboard = Keyboard.current;
    if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
    if (!gameplay.IsPaused) Pause();       // 플레이 중 → 메뉴
    else if (countdownLeft > 0f) ShowMenu(); // 카운트다운 중 → 메뉴로 돌아감
    else Resume();                          // 메뉴 → 카운트다운
}
```

### 4.3 롱노트와 판정 보존
- 정지 중에는 `HandleHolds`/`HandleMisses`가 호출되지 않으므로 키를 떼도 Miss·틱이 생기지 않는다. 노트 상태(`Holding`, `HeadJudgement`, `NextTickTime`)와 회색 Miss 롱노트 목록은 그대로 둔다.
- 카운트다운이 끝나 재개되는 첫 프레임부터 기존 `IsHeld`로 키 상태를 확인한다. 카운트다운 중 키를 다시 잡아 두면 이어서 유지 판정을 받는다.

### 4.4 결과 전환 보호
```csharp
// ResultTransition: 결과 전환(대기·페이드)이 시작되었는지
public bool IsTransitioning => loading || finishedElapsed >= 0f;

// Update 첫 줄: 정지 중에는 종료 판정과 대기 시간 누적을 하지 않는다
if (loading || gameplay == null || gameplay.IsPaused) return;
```

### 4.5 최소 UI
- 기존 Canvas에 `PausePanel` 추가: 화면 전체 반투명 검은 배경(뒤쪽 클릭 차단), 가운데 `PAUSED` 글자와 `RESUME` / `RESTART` / `SONG SELECT` 버튼. 기본 비활성으로 저장.
- `CountdownText`: 화면 가운데 큰 숫자. 기본 비활성으로 저장.
- 그리는 순서: `FadeOverlay`보다 앞(아래)에 둔다. 글꼴은 기존 UI와 같은 기본 글꼴, 글자는 영문.

## 5. 구현 순서 및 진행 상태
- [x] GPT 초안 검토, 로컬 소스 대조, 사용자 결정 반영
- [x] `GameplayManager` 정지·재개(`IsPaused`, `TryPause`, `TryResume`, `SongTime`·`IsMusicFinished` 보정, `Update` 차단), `ResultTransition` 보호(`IsTransitioning`, 정지 중 종료 판정 중단) → 컴파일 확인 (에러 0건)
- [x] `PauseController` 작성 (Esc 처리, 메뉴, 3초 카운트다운, RESUME/RESTART/SONG SELECT, `loading`으로 중복 로드 방지, 재개 시 UI 선택 해제) → 컴파일 확인 (에러 0건)
- [x] `IngameScene` 구성: Canvas에 `PausePanel`(검은 70% 전체 배경, `PausedText`, `ResumeButton` / `RestartButton` / `SongSelectButton`, 기본 비활성), `CountdownText`(가운데 큰 숫자, 기본 비활성), 둘 다 `FadeOverlay`보다 아래 순서. `PauseController` 오브젝트의 참조 7개 모두 연결 확인
- [x] 검증
- 관련 문서 수정: `note_speed_plan.md`의 "일시정지 화면에서 배속 변경 추가 예정" 3곳을 "일시정지 중 배속 변경 없음(2026-11-15 사용자 결정)"으로 수정 (사용자 승인)

- 완료: 계획, 구현, Claude 검증
- 진행 중: 없음
- 남은 작업: 사용자 확인 (실제 Esc, 정지 중 레인 키, 메뉴가 뒤쪽 클릭을 막는지, 화면 배치, 음악 청취)

## 6. 테스트 및 검증 방법
확인 주체: C = Claude(Play 모드에서 함수·버튼 `onClick` 호출, AUTO TEST), U = 사용자.
제약: 주입한 키는 `wasPressedThisFrame`에 반영되지 않아 Esc 자체는 Claude가 누를 수 없다 → `PauseController`의 메뉴 함수를 직접 호출한다. 에디터 일시정지 중에는 오디오가 멈추므로 음악 있는 곡(kick)은 실시간으로 진행해 검증한다.

| ID | 테스트 | 기대 결과 | 주체 | 상태 |
|---|---|---|---|---|
| PR-01 | 음악 재생 중(kick) 정지, 5초 대기 | 음악·곡 시간·노트 위치·점수·콤보 변화 없음 | C | 통과 (kick 22.955초에서 정지 → 5초 동안 `isPlaying` false, `time` 22.955 고정, 점수 변화 없음) |
| PR-02 | 음악 없는 곡에서 5초 정지 후 재개 | 정지한 시간이 곡 시간에 포함되지 않음 | C | 통과 (Test Song 1 5.020초에서 정지, 5초 동안 곡 시간·점수·노트 Y 고정) |
| PR-03 | 재개(카운트다운 후) | 3·2·1 표시 후 같은 곡 시간부터 진행, 노트 점프 없음 | C | 통과 (카운트다운 3→2→1 동안 계속 정지, 재개 후 5.020 → 5.180으로 이어짐(재개 후 0.16초 진행분), kick도 정지 위치에서 이어 재생) |
| PR-04 | 카운트다운 중 Esc | 카운트다운 취소, 메뉴 다시 표시, 계속 정지 | C(함수 호출) / U(Esc) | 통과 (함수 호출: 카운트다운 중 Pause → 메뉴 다시 표시, 1.2초 뒤에도 정지 유지). 실제 Esc는 미검증 (Esc 주입 불가, 사용자 확인 필요) |
| PR-05 | 정지 중 레인 키 입력 | 판정·점수·틱·Miss 없음 | U (C는 코드로만 확인 가능) | 미검증 (주입한 키는 `wasPressedThisFrame`에 반영되지 않음. 정지 중 `Update`가 처리 전에 반환하는 것은 코드로 확인. 사용자 확인 필요) |
| PR-06 | 롱노트 유지 중 정지, 키 뗌 | 정지 중 Miss·틱 없음 | C(`isPressed` 주입) | 통과 (8.42초 롱노트 2개 유지 중 정지 → 2초 동안 점수 17200 그대로, 틱 없음) |
| PR-07 | 카운트다운 중 키를 다시 잡고 재개 | 틱·끝 판정 정상 | C(`isPressed` 주입) / U | 통과 (정지 중 AUTO 0으로 바꾸고 D만 누른 상태로 재개 → 레인 1 롱노트 틱 8.45~8.90초 이어짐, 끝 Perfect). 실제 키는 사용자 확인 |
| PR-08 | 키를 잡지 않고 재개, 종료까지 150ms 초과 남음 | 기존 규칙대로 끝 Miss | C(`isPressed` 주입) | 통과 (같은 상황에서 K를 누르지 않은 레인 4 롱노트는 재개 직후 끝 Miss(9.5초 종료, 150ms 초과 남음)) |
| PR-09 | 회색 Miss 롱노트 표시 중 정지·재개 | 회색 표시 유지, 재개 후 종료 시각에 삭제 | C | 미검증 (레인 4 Miss 롱노트가 회색(Failed)으로 표시되다 종료 시각 뒤 삭제되는 것은 확인. 회색 표시 중에 정지·재개하는 경우는 실행하지 않음) |
| PR-10 | 점수가 쌓인 뒤 RESTART | 같은 곡 처음부터, 점수·콤보·판정·틱 0, AUTO 단계 0 | C | 통과 (RESTART → 씬 다시 불러옴, 곡 시간 0부터, 점수 0, AUTO 단계 0, `PlayResult.Last` 35800 유지) |
| PR-11 | SONG SELECT | 곡 선택 화면, 같은 곡이 선택된 상태, 결과 저장 안 됨 | C | 통과 (SONG SELECT → 곡 선택 화면, Test Song 1 선택 상태, `PlayResult.Last` 유지) |
| PR-12 | 정지·재개 반복, RESTART·SONG SELECT 연속 클릭 | 상태 일관, 씬 중복 로드 없음 | C | 통과 (Restart 2번 연속 호출 → 씬 1번만 다시 불러옴, 에러 없음. 정지 아닐 때 Resume·Restart 호출은 무시됨. SONG SELECT 연속 호출은 실행하지 않음(같은 `loading` 플래그 사용)) |
| PR-13 | 키 설정·배속 바꾼 뒤 RESTART | 설정 유지 | C | 미검증 (이번 검증에서 설정 변경 후 RESTART는 하지 않음. 씬 다시 불러오기는 기존 RETRY와 같은 방식이고 설정은 PlayerPrefs·static 값이라 영향 없음을 코드로만 확인) |
| PR-14 | 마지막 노트·음악 종료 직전 정지 | 정지 중 결과 전환 없음, 재개 후 정상 종료 | C | 통과 (12.34초 마지막 롱노트 유지 중 정지 → 4초 동안 IngameScene 유지, `IsTransitioning` false, 재개 후 정상 종료) |
| PR-15 | 결과 대기·페이드 중 Esc(메뉴 함수 호출) | 메뉴 안 열림, 결과 정상 전환 | C | 통과 (결과 전환 시작 후 Pause 호출 → 정지 안 됨, 결과 정상 전환) |
| PR-16 | AUTO 3단계 중 정지·재개 반복 | 판정·틱 누락·중복 없음, 기준값과 같음 | C | 통과 (Test Song 1: 정지 4회·취소 1회 포함 SS / 35800 / 콤보 61 / Perfect 25 / Tick 36 "일치". Test Song 2: 정지 1회 포함 9800 "일치") |
| PR-17 | 메뉴 열린 동안 AUTO TEST 버튼 위치 클릭 | 배경이 클릭을 막음 | U | 미검증 (실제 마우스 클릭 필요. 배경 Image의 `raycastTarget` 켜짐 확인. 사용자 확인 필요) |
| PR-18 | 화면 배치 (1920×1080, 창 크기 변경) | 메뉴·숫자 정상 표시 | U | 미검증 (스크린샷으로 메뉴·카운트다운 표시는 확인. 창 크기 변경은 사용자 확인 필요) |
| PR-19 | 컴파일·Play 모드·씬 참조 | 새 에러·경고 0건, Missing Script·참조 누락 없음 | C | 통과 (컴파일 에러 0건, Play 중 에러·경고 0건, 참조 누락 없음, 검증 후 씬 변경 남지 않음) |
| PR-20 | 실제 Esc로 정지·재개, 음악 청취 | 소리가 끊김 없이 이어짐 | U | 미검증 (사용자 확인 필요) |

### 회귀 기준 (AUTO 3단계, 정지 없는 플레이와 정지·재개 플레이 모두)
| 곡 | 점수 | 최대 콤보 | 판정 |
|---|---:|---:|---|
| Test Song 1 | 35,800 | 61 | Perfect 25, Tick 36 |
| Test Song 2 | 9,800 | 14 | Perfect 8 |
| kick | 263,200 | 364 | Perfect 220 |

- 점수뿐 아니라 판정 수·틱 수를 요약 로그와 비교한다.
- AUTO 1·2단계도 정지·재개 후 동작 확인 (2단계는 랜덤이라 점수 고정값 없음, 요약 로그 "일치" 확인).
- AUTO TEST만으로 실제 키 입력·화면 표시·오디오 싱크가 정상이라고 판단하지 않는다 (CLAUDE.md 6.3).

- [x] Unity 컴파일 오류 확인
- [ ] 실제 플레이에서 기능 검증 (실제 Esc·레인 키·마우스 클릭·음악 청취는 사용자 확인)
- [x] 관련 기존 기능 회귀 테스트 (Test Song 1·2 AUTO 3단계 기준값 일치. kick은 아래 미검증 참조)

## 7. 검증 결과 및 미해결 사항
- 검증 결과 (2026-11-15): 음악 없는 곡은 Play 모드를 `EditorApplication.Step()`으로 진행(`Time.timeScale = 2`), kick은 에디터 일시정지를 풀고 실시간 진행. 메뉴는 `PauseController`의 `Pause`/`Resume`/`Restart`/`GoToSongSelect`를 직접 호출. 상세는 6장 표.
  - kick 정지·재개: 정지 중 음악·곡 시간 고정, 정지 위치에서 이어 재생 확인 (PR-01, PR-03).
  - kick 정지 포함 끝까지 플레이: 259200 / Miss 4 (기준 263200과 다름). Miss 4개는 모두 정지 **전** 노트(21.857~22.714초, 정지는 22.955초)라 정지 처리와 무관. 확인 명령을 연달아 보내는 동안 Unity 메인 스레드가 멈추고 음악만 진행해 AUTO 입력이 판정 범위를 지난 것으로 추정 (검증 방법의 한계).
  - 다시 확인하려 했으나 두 번째 실시간 실행에서는 에디터가 포커스 없이 프레임을 진행하지 않아(음악만 진행, 점수 0) 확인하지 못함.
  - 스크린샷: 메뉴(배경 어둡게, PAUSED, 버튼 3개)와 카운트다운 숫자 표시 확인.
- 미해결:
  - kick 정지 포함 회귀(263200): 미검증 (위 사유). Unity 창에 포커스를 둔 상태에서 사용자 확인 또는 재검증 필요
  - 사용자 확인 필요: 실제 Esc로 정지·재개·카운트다운 취소, 정지 중 레인 키 무시, 메뉴가 AUTO TEST 버튼 클릭을 막는지, 화면 배치, 정지·재개 시 음악이 끊김 없이 이어지는지
- 관련 문서: `note_speed_plan.md`의 일시정지 중 배속 변경 문구 3곳 수정 완료 (사용자 승인)
- 변경 이력: GPT 초안(사용자 제공) → 2026-11-15 로컬 소스 대조 검토, 사용자 결정 반영(배속 변경 제외, 메뉴 3버튼, 재개 카운트다운), 불필요한 장치(실행 순서 지정·프레임 가드·AutoPlayer/AutoTestPanel 수정) 제거, 검증 항목 주체 구분 → 2026-11-15 구현 및 검증
