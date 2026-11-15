# 핵심 플레이(노트 낙하·입력·판정) 구현 계획서

## 1. 작업 목적 및 요구사항
- 프로젝트: music_game (Unity 6.6, C#, URP 2D, Mono, Input System)
- 목적: 리듬게임의 최소 플레이 루프를 만든다. 레인 위로 노트가 판정선까지 내려오고, 키 입력 시각과 노트 시각의 차이로 판정한다.
- 상태: 구현 완료 / 사용자 확인 완료 (2026-11-15, PLAN.md 확인)
- 이 계획은 `score_combo_plan.md`, `long_note_plan.md`, `note_speed_plan.md`, `key_setting_plan.md`, `result_screen_plan.md`의 전제 조건이다.

### 요구사항
1. 고정된 수의 레인과 레인별 입력 키가 있다.
2. 채보(노트 시각 + 레인 목록)를 읽어 노트를 생성한다.
3. 노트는 곡 시간 기준으로 위치가 계산되어 판정선을 향해 내려온다.
4. 키를 누르면 해당 레인에서 가장 가까운 미판정 노트를 시간 차로 판정한다.
5. 판정 범위를 지나친 노트는 Miss로 처리한다.
6. 판정 결과를 이벤트로 한 번만 알린다(이후 점수·콤보 시스템이 구독).

### 범위 밖 (이번 작업에서 하지 않음)
- 점수·콤보, 롱노트, 배속 변경 UI, 키 변경 UI, 곡 선택, 결과 화면 (각 계획서에서 진행)
- 판정 텍스트·이펙트 등 연출 (필요하면 별도 확인)
- 패키지 및 프로젝트 설정 변경

### 미확정 사항 (사용자 확인 필요, 괄호 안은 제안값)
| 항목 | 제안값 |
|---|---|
| 레인 수 / 기본 키 | 4레인, `D` `F` `J` `K` (Input System `Key`) (2026-11-15부터 `KeyBindings`에서 읽음, 변경 가능 — `key_setting_plan.md`) |
| 판정 범위 | Perfect ±50ms, Great ±100ms, Good ±150ms, 그 밖 Miss |
| 채보 형식 | `TextAsset`(.txt), 한 줄에 `시각(초),레인번호` |
| 곡 오디오 | 프로젝트에 오디오 파일이 없음. AudioClip이 지정되면 `AudioSource` 시간 사용, 없으면 시작 후 경과 시간 사용 |
| 노트 외형 | 기존 `Block`처럼 기본 Square 스프라이트를 쓴 프리팹 |
| 기존 `Block` 오브젝트 | 그대로 둠 (노트 프리팹으로 바꿀지 확인 필요) |
| 노트 낙하 속도 | 고정 5 유닛/초 (배속은 `note_speed_plan.md`에서) |

## 2. 기존 코드 분석 (2026-11-15 확인)
- 게임 스크립트 없음. `Assets/` 내 C# 파일은 템플릿의 `Assets/Welcome/Welcome2DScript.cs`뿐.
- 씬: `Assets/Scenes/IngameScene.unity` — `Main Camera`(Orthographic), `Global Light 2D`, `Block`(기본 Square 스프라이트, 원점).
- 패키지: URP 17.6.0, 2D 패키지 일체, Input System 1.20.1. Active Input Handling은 Input System 전용(`activeInputHandler: 1`)이므로 입력은 `Keyboard.current` 사용.
- 오디오·채보·프리팹 에셋 없음.

## 3. 작업 범위 및 수정 대상 파일
| 파일 | 구분 | 역할 |
|---|---|---|
| `Assets/Scripts/Gameplay/NoteData.cs` | 신규 | 노트 하나의 데이터(시각, 레인) |
| `Assets/Scripts/Gameplay/ChartLoader.cs` | 신규 | TextAsset 채보를 `List<NoteData>`로 변환 |
| `Assets/Scripts/Gameplay/Note.cs` | 신규 | 노트 오브젝트. 곡 시간으로 Y 위치 계산 |
| `Assets/Scripts/Gameplay/Judgement.cs` | 신규 | 판정 등급 enum 및 시간 차 → 등급 변환 |
| `Assets/Scripts/Gameplay/GameplayManager.cs` | 신규 | 곡 시간, 노트 생성, 키 입력, 판정, 판정 이벤트 |
| `Assets/Charts/test_chart.txt` | 신규 | 검증용 채보 |
| `Assets/Scripts/Gameplay/GameplayManager.cs` | 수정 (2026-11-15, 키 설정) | 레인 키를 `laneInputKeys` 대신 `KeyBindings.Get(lane)`으로 조회 |
| `Assets/Prefabs/Note.prefab` | 신규 | Square 스프라이트 + `Note` |
| `Assets/Scenes/IngameScene.unity` | 수정 | `GameplayManager` 오브젝트, 판정선 표시 추가 |

## 4. 상세 구현 방법
- **곡 시간**: `GameplayManager.SongTime`. AudioClip이 있으면 `audioSource.time`, 없으면 시작 시점부터의 `Time.timeAsDouble` 경과값.
- **노트 위치**: `y = judgeLineY + (noteTime - songTime) * fallSpeed`. 레인 X는 `laneXs[lane]`.
- **생성**: 노트 시각이 `songTime + 화면 도달 시간` 이내가 되면 미리 생성. 채보는 시각순 정렬.
- **판정**: 키 입력 시 해당 레인의 미판정 노트 중 `|noteTime - songTime|`이 가장 작은 것을 선택. Good 범위 밖이면 무시(빈 입력). 범위 안이면 등급 확정 후 노트 제거.
- **Miss**: `songTime - noteTime > goodWindow`인 미판정 노트는 Miss 처리 후 제거.
- **중복 방지**: 노트마다 `IsJudged` 플래그. 한 번 판정된 노트는 다시 판정하지 않음.
- **이벤트**: `public event System.Action<Judgement, int> OnJudged;` (등급, 레인).

### 코드 스니펫 (설계 예시)
```csharp
// 시간 차(초)를 판정 등급으로 변환한다. 범위 밖이면 false.
public static bool TryGetJudgement(double diff, out Judgement result)
{
    double abs = System.Math.Abs(diff);
    if (abs <= 0.050) { result = Judgement.Perfect; return true; }
    if (abs <= 0.100) { result = Judgement.Great; return true; }
    if (abs <= 0.150) { result = Judgement.Good; return true; }
    result = Judgement.Miss;
    return false;
}
```

## 5. 구현 순서 및 진행 상태
- [x] 미확정 사항 확정 및 계획 승인 (2026-11-15, 1장 제안값 그대로 승인)
- [x] `NoteData`, `Judgement`, `ChartLoader` 작성 → 컴파일 확인 (에러 0건)
- [x] `Note`, `GameplayManager` 작성 → 컴파일 확인 (에러 0건)
- [x] 노트 프리팹·검증용 채보·씬 구성 (`JudgeLine`, `GameplayManager` 추가 후 저장. 사용자 확인에 따라 에디터에서 삭제된 `Block`은 삭제 상태로 저장)
- [x] 입력을 Input System으로 변경 (2026-11-15 사용자 결정): `Input.GetKeyDown(KeyCode)` → `Keyboard.current[Key].wasPressedThisFrame`, 필드 `laneKeys`(KeyCode[]) → `laneInputKeys`(Key[])
- [x] Play 모드 검증 — 낙하·Miss는 Claude 확인, 키 입력 판정은 사용자 직접 플레이로 확인(PLAN.md 확인), 이후 AUTO TEST 3단계로 재확인

## 6. 테스트 및 검증 방법
| 테스트 항목 | 기대 결과 | 상태 |
|---|---|---|
| 채보 로드 | 줄 수만큼 노트 데이터 생성, 잘못된 줄은 경고 후 건너뜀 | 통과 (20줄 → 판정 25회 = 일반 15 + 롱노트 5×2, AUTO 3단계). 잘못된 줄 경고는 미검증 |
| 노트 낙하 | 노트 시각에 판정선 위치에 도달 | 통과 (2.0초 노트가 2.0초에 y=-3.00) |
| 정타 입력 | Perfect 판정, 노트 제거, 이벤트 1회 | 통과 (사용자 플레이, AUTO 3단계 Perfect 25회) |
| 범위 밖 입력 | 판정 없음, 노트 유지 | 통과 (AUTO 1단계 250ms 먼저 누르기 → 판정 없이 이후 Miss) |
| 입력 없음 | 범위 경과 후 Miss 1회 | 통과 |
| 다른 레인 키 | 해당 레인 노트에 영향 없음 | 통과 (사용자 플레이 확인 기준, 개별 테스트는 하지 않음) |

- [x] Unity 콘솔 컴파일 오류 0건
- [x] Play 모드에서 판정 로그(`Debug.Log`) 확인

## 7. 검증 결과 및 미해결 사항
- 검증 결과 (2026-11-15):
  - 컴파일: 에러 0건
  - Play 모드: 매 프레임 `InvalidOperationException` 발생. `UnityEngine.Input`을 읽었는데 Player Settings의 Active Input Handling이 Input System 패키지 전용으로 설정되어 있음.
  - 원인: `ProjectSettings.asset`의 `activeInputHandler: 1`(Input System Package 전용), `com.unity.inputsystem` 1.20.1 설치됨. CLAUDE.md의 "Legacy Input Manager" 기재와 실제 설정이 다름. (당시 CLAUDE.md 기준, 이후 2026-11-15 Input System 기준으로 CLAUDE.md 수정됨)
  - 영향: 예외로 `Update()`가 `HandleInput`에서 중단되어 Miss 처리가 실행되지 않음. 노트 낙하·판정은 미검증.
- 재검증 (2026-11-15, Input System 입력으로 변경 후):
  - 컴파일: 에러 0건. Play 모드 중 예외·경고 0건.
  - Unity 창이 포커스를 잃어 Play 모드 프레임이 진행되지 않음 → 일시정지 후 `EditorApplication.Step()`으로 프레임을 넘기며 확인.
  - 채보 로드 / 노트 생성: 통과. 노트 시각 - 1.8초에 y=6 부근에서 생성됨.
  - 노트 낙하: 통과. 2.0초 노트가 곡 시간 0.6초에 y=4.00, 2.0초에 y=-3.00(판정선).
  - 입력 없음 → Miss: 통과. 2.0초·2.5초 노트가 각각 Good 범위 경과 후 Miss 1회씩 기록됨.
  - 키 입력 판정: **미검증**. `InputSystem.QueueStateEvent`로 J 키 입력을 넣었으나 판정되지 않음. 에디터가 포커스를 잃은 상태라 입력이 무시된 것으로 추정(원인 미확정). Unity 창을 포커스한 상태에서 직접 플레이 테스트 필요.
- 미해결: 없음 (키 입력 판정은 사용자 직접 플레이로 확인, 2026-11-15). 남은 미검증: 채보의 잘못된 줄 경고 처리
- 변경 이력: 2026-11-15 초안 작성 → 승인 → 구현 → 입력 설정 문제로 검증 중단 → Input System으로 입력 변경 → 낙하·Miss 검증 → 2026-11-15 씬 이름 변경(SampleScene → IngameScene), 에디터 Play 시작 씬을 SongSelectScene으로 고정
