# 자동 플레이(AUTO TEST) 구현 계획서

## 1. 작업 목적 및 요구사항
- 프로젝트: music_game (Unity 6.6, C#, URP, Mono, Input System)
- 목적: 직접 치지 않아도 게임이 채보대로 자동 입력하게 한다. 사용자는 화면 버튼으로, Claude는 MCP로 단계를 골라 검증한다.
- 상태: 구현 완료 / PLAN.md 확인 (2026-11-15, 사용자 요청으로 상태 변경)
- 이 문서는 개발 진행에 따라 수정한다. 작업 범위 확대는 사용자 확인 후 반영한다.

### 요구사항
- 확정 사항 (2026-11-15 사용자 승인):

| 단계 | 이름 | 동작 |
|---|---|---|
| 0 | OFF | 자동 입력 없음, 키보드로 직접 플레이 |
| 1 | 실패 점검 | 노트마다 실패 방식을 돌아가며 적용. 일반 노트는 ① 안 치기 ② 판정 범위보다 250ms 먼저 누르기(무시된 뒤 Miss)를 번갈아, 롱노트는 머리는 치고 50ms 뒤 바로 떼기(끝 Miss). 결과는 거의 Miss |
| 2 | 여러 판정 | 노트마다 입력 타이밍을 ±200ms 안에서 랜덤으로 어긋냄. Perfect/Great/Good/Miss가 대략 1/4씩 섞임. 머리를 친 롱노트는 30% 확률로 중간에 일찍 뗌. 매번 결과가 달라짐 |
| 3 | 완벽한 플레이 | 노트 시각에 정확히 누르고, 롱노트는 종료 시각까지 누름. 모두 Perfect |

- UI:
  - 화면 왼쪽 위에 "AUTO TEST" 글자와 `[0] [1] [2] [3]` 버튼을 두고, 선택된 버튼을 강조한다.
  - 플레이 도중에 바꿔도 바로 적용된다. 아직 안 친 노트부터 새 단계로 계산한다.
- 요약 로그: 모든 노트 처리가 끝나면 Console에 한 줄 남긴다.
  - 예: `[AutoPlay] 단계 2 완료: Perfect 7 / Great 5 / Good 4 / Miss 9 / Tick 31 / 점수 12340 (예상 12340, 일치)`
  - "예상 점수"는 판정 횟수로 따로 계산한 값이다. 화면 점수와 같은지 비교한다.
- 판정·점수·콤보·틱은 수동 플레이와 똑같은 코드로 처리한다.

## 2. 기존 코드 분석
- 입력은 `GameplayManager` 두 군데에서만 읽는다.
  - `HandleInput`: 그 프레임에 눌렸는지(`wasPressedThisFrame`)
  - `HandleHolds`: 누르고 있는지(`isPressed`)
- 이 두 곳만 "키보드 또는 AUTO"에서 읽도록 바꾸면 판정 로직은 그대로 둘 수 있다.
- 씬의 `Canvas`에는 점수와 콤보 글자만 있고, 버튼 클릭에 필요한 EventSystem은 없다. EventSystem과 Input System용 UI 입력 모듈(`InputSystemUIInputModule`)을 추가한다. 패키지(`com.unity.inputsystem` 1.20.1, `com.unity.ugui` 2.6.0)는 이미 설치돼 있어 설치나 설정 변경은 필요 없다.
- Unity 창 포커스가 없을 때 Claude는 일시정지 후 `EditorApplication.Step()`으로 프레임을 한 칸씩 넘겨 진행한다. AUTO 입력은 키보드를 거치지 않으므로 이 방식에서도 동작한다.

## 3. 작업 범위 및 수정 대상 파일
| 파일 | 구분 | 내용 |
|---|---|---|
| `Assets/Scripts/Gameplay/AutoPlayer.cs` | 신규 | 단계(0~3), 노트별 누를 시각·뗄 시각 계산, 레인 입력 판단, 판정 횟수 집계와 요약 로그 |
| `Assets/Scripts/Gameplay/AutoTestPanel.cs` | 신규 | `[0]~[3]` 버튼으로 단계 변경, 선택한 버튼 강조 |
| `Assets/Scripts/Gameplay/GameplayManager.cs` | 수정 | `AutoPlayer` 참조(선택), 입력을 "이번 프레임에 눌림 / 누르고 있음" 함수로 분리, 노트 생성 시 AutoPlayer에 알림, 모든 노트 처리 끝남 여부(`IsFinished`) |
| `Assets/Scenes/IngameScene.unity` | 수정 | `AutoPlayer` 오브젝트, 왼쪽 위 AUTO TEST 패널(글자 + 버튼 4개), EventSystem 추가. 단계 0으로 저장 |

- 범위 밖: 단축키로 바꾸기, 판정 텍스트·연출, 랜덤 시드 고정, 프로젝트·패키지 설정 변경.

## 4. 상세 구현 방법 및 필요한 코드 스니펫
- 노트가 생성될 때 `AutoPlayer`가 현재 단계 기준으로 그 노트를 언제 누르고 뗄지 정한다. 단계를 바꾸면 아직 안 친 노트를 다시 계산한다.
- 이번 프레임에 눌림: 그 레인 노트의 누를 시각이 `직전 프레임 곡 시간 < 누를 시각 ≤ 현재 곡 시간` 사이에 있으면 눌린 것으로 본다.
- 누르고 있음: 그 레인에 유지 중인 롱노트가 있고 아직 뗄 시각 전이면 누르고 있는 것으로 본다.
- 랜덤은 매번 다른 결과가 나오도록 `System.Random`을 시드 없이 쓴다.

```csharp
// 레인 입력: AUTO 단계가 1~3이면 AutoPlayer 계획대로, 0이면 키보드
private bool WasPressed(int lane, double prevTime, double songTime) =>
    autoPlayer != null && autoPlayer.Level > 0
        ? autoPlayer.WasPressed(lane, prevTime, songTime)
        : Keyboard.current != null && Keyboard.current[laneInputKeys[lane]].wasPressedThisFrame;
```

- 주의: 3단계의 "무조건 Perfect"는 프레임 간격이 50ms 이하(초당 20프레임 이상)일 때 보장된다. 노트 시각 직후 첫 프레임에 누르기 때문이다.

## 5. 구현 순서 및 진행 상태
- [x] `AutoPlayer` + `GameplayManager` 입력 분리 → 컴파일 확인 (에러 0건)
  - `GameplayManager`: `autoPlayer` 참조, `WasPressed`/`IsHeld` 헬퍼, `prevSongTime`, `IsFinished`, 노트 생성 시 `RegisterNote`·최종 판정 시 `UnregisterNote`
  - `AutoPlayer`: 단계별 계획(`MakePlan`), 단계 변경 시 대기 노트 재계획(`Level` setter, 인스펙터 변경은 `OnValidate`), 판정·틱 집계와 예상 점수, 끝나면 요약 로그 1회
- [x] `AutoTestPanel`, 씬 UI, EventSystem 구성 → 컴파일 확인 (에러 0건)
  - `Canvas/AutoTestPanel`: 왼쪽 위(30, -30), "AUTO TEST" 36pt, 버튼 `Button0~3`(75×70, 85 간격), 선택 색 주황(1, 0.6, 0.1)·기본 색 회색
  - `EventSystem` + `InputSystemUIInputModule` 추가, `Canvas`에 `GraphicRaycaster` 추가(버튼 클릭 수신에 필요)
  - `AutoPlayer` 오브젝트 단계 0으로 저장
- [x] 단계별 자동 플레이 검증

- 완료: 계획, 구현, Claude 자동 검증
- 진행 중: 없음
- 남은 작업: 없음 (아래 7장 미해결 항목은 사용자 직접 확인 권장)

## 6. 테스트 및 검증 방법
| 테스트 항목 | 기대 결과 | 상태 |
|---|---|---|
| 3단계 | 일반 노트 15개 Perfect, 롱노트 5개 시작·틱·끝 모두 Perfect, Miss 0, 콤보 안 끊김 | 통과 |
| 2단계 | 여러 판정이 섞여 나오고, 요약 로그의 점수가 예상 점수와 일치 | 통과 (2회 실행, 결과 서로 다름) |
| 1단계 | 일반 노트는 모두 Miss, 롱노트는 시작 Perfect + 끝 Miss, 점수 일치 | 통과 |
| 0단계 | 자동 입력 없음 (키보드 경로) | 통과 (자동 입력 없이 Miss). 실제 키보드 입력은 사용자 확인 |
| 플레이 중 단계 변경 | 아직 안 친 노트부터 새 단계로 적용 | 통과 |
| UI | 버튼을 누르면 단계가 바뀌고 강조 표시가 바뀜 | 통과 (onClick 호출). 실제 마우스 클릭·배치는 사용자 확인 |

- [x] Unity 컴파일 오류 확인
- [x] 실제 플레이에서 기능 검증 (AUTO 단계 1~3, Claude가 Play 모드에서 직접 실행)
- [x] 관련 기존 기능 회귀 테스트 (판정·점수·콤보·틱이 기존 코드로 처리됨, 3단계 점수 = 계산값)

## 7. 검증 결과 및 미해결 사항
- 검증 결과 (2026-11-15, Play 모드를 `EditorApplication.Step()`으로 진행, `Time.timeScale = 2`로 프레임당 0.04초):
  - 컴파일 에러 0건. Play 모드 중 에러·경고 0건 (MCP 연결 로그와 Step 반복 호출 경고 제외).
  - 3단계: `Perfect 25 / Great 0 / Good 0 / Miss 0 / Tick 36 / 점수 35800 (예상 35800, 일치)`, 최종 콤보 61. 계산값(판정 25×1000 + 틱 36×300 = 35800)과 일치.
  - 1단계: `Perfect 5 / Miss 20 / Tick 0 / 점수 5000 (일치)`. 일반 노트 15개 모두 Miss, 롱노트 5개 시작 Perfect + 끝 Miss.
  - 2단계 1회차: `Perfect 7 / Great 4 / Good 3 / Miss 11 / Tick 19 / 점수 15230 (일치)`. 2회차: `Perfect 8 / Great 4 / Good 7 / Miss 6 / Tick 18 / 점수 14760 (일치)`. 일찍 뗀 롱노트(끝 Miss) 포함.
  - 단계 변경(0 → 2.42초에 3 → 6.22초에 1): 2.0초 노트 Miss(0단계, 자동 입력 없음), 2.5~6.0초 노트 Perfect(3단계), 6.5·7.0초 노트 Miss와 이후 롱노트 시작 Perfect + 끝 Miss(1단계). 점수 15000 일치.
  - UI: 버튼 2→3→1→0 `onClick` 호출 시 단계와 강조 색이 그대로 바뀜. AutoPlayer에서 단계를 바꿔도 다음 프레임에 강조 표시가 맞춰짐. Play 종료 후 씬의 단계는 0, 씬 변경 없음.
- 참고: 요약 로그의 "단계 N"은 끝날 때의 단계다. 플레이 중 단계를 바꾸면 집계에는 여러 단계 결과가 섞인다.
- 미해결: UI 버튼 실제 마우스 클릭과 화면 배치, 입력 코드 분리 후 0단계 키보드 플레이는 Claude가 직접 확인하지 못함 (2026-11-15 PLAN.md는 사용자 요청으로 확인 처리)
- 변경 이력: 2026-11-15 초안 작성 (3단계 구분·AUTO TEST UI 반영) → 구현 및 검증 → 2026-11-15 씬 이름 변경(SampleScene → IngameScene), 에디터 Play 시작 씬을 SongSelectScene으로 고정
