# 롱노트 시스템 구현 계획서

## 1. 작업 목적 및 요구사항
- 프로젝트: music_game (Unity 6.6, C#, URP, Mono, Input System)
- 목적: 시작부터 종료까지 입력을 유지하는 노트를 처리한다.
- 상태: 롱노트 Miss 표시 추가 완료(2026-11-15) / 사용자 화면 확인 대기
- 이 문서는 개발 진행에 따라 수정한다. 작업 범위 확대는 사용자 확인 후 반영한다.

### 요구사항
- 시작부터 종료까지 입력을 유지하는 노트를 처리한다.
- 기존 기능을 보존하고 요청 범위에 필요한 변경만 수행한다.
- 확정 사항 (2026-11-15 사용자 승인):

| 항목 | 확정값 |
|---|---|
| 채보 형식 | `시각,레인,종료시각` — 3번째 칸이 있고 시작보다 늦으면 롱노트, 없으면 일반 노트 |
| 시작 판정 | 일반 노트와 같은 범위 (Perfect ±50 / Great ±100 / Good ±150ms) |
| 시작 놓침 | 시작 Miss + 끝 Miss, 2회 기록 |
| 유지 | 키를 계속 누르고 있어야 함 |
| 조기 해제 | 종료 150ms 전보다 일찍 떼면 끝 판정 Miss. 그 안에서 떼면 성공 |
| 종료 처리 | 키를 뗄 필요 없음. 종료 시각까지 누르고 있으면 자동으로 끝 판정 Perfect |
| 점수·콤보 | 시작 등급 + 끝 Perfect/Miss는 `OnJudged`로 보고(일반 판정과 동일 점수) |
| 틱(유지) 판정 (2026-11-15 추가) | 시작 후 0.15초마다(종료 시각 미만, 2026-11-15 0.25초에서 변경) 유지 중이면 틱 1회. 점수는 시작 판정 등급 점수의 30%(Perfect 300 / Great 210 / Good 90), 콤보 +1 |
| Miss 표시 (2026-11-15 추가) | 시작 놓침·조기 해제로 Miss를 받으면 바로 지우지 않고 회색 반투명(알파 0.4)으로 표시. 머리는 판정선에 고정, 몸통만 줄어들다 종료 시각에 삭제. 다시 눌러도 반응 없음. 일반 노트 Miss는 기존대로 즉시 삭제 |
| 실패 시 틱 | 조기 해제·시작 놓침 시 남은 틱은 버림. 기록은 기존대로(조기 해제: 끝 Miss 1회, 시작 놓침: 시작 Miss + 끝 Miss) |
| 표시 | 머리~꼬리 몸통 막대. 누르는 동안 머리는 판정선에 고정되고 몸통이 줄어듦 |

- 제외 범위: 점수 비율 조정, 롱노트 전용 색·연출. (유지 중 틱 판정은 2026-11-15 사용자 요청으로 범위에 추가)

## 2. 기존 코드 분석
- `NoteData`: `Time`, `Lane`만 있음. `ChartLoader`는 `시각,레인` 2칸만 허용.
- `Note`: 위치 계산과 `IsJudged` 플래그만 있음. 상태 개념 없음.
- `GameplayManager`: 키 누름(`wasPressedThisFrame`) 시 즉시 최종 판정 후 노트 제거. Good 범위 경과 시 Miss. 판정마다 `OnJudged` 1회.
- `ScoreComboState`: `OnJudged`만 구독하므로 판정 2회를 보내면 수정 없이 반영됨.
- 계획서 예시의 `LongNoteState` 별도 클래스 대신 기존 `Note`에 상태를 둔다(클래스 추가 최소화).

## 3. 작업 범위 및 수정 대상 파일
| 파일 | 구분 | 내용 |
|---|---|---|
| `Assets/Scripts/Gameplay/NoteData.cs` | 수정 | `EndTime`, `IsLong` 추가 |
| `Assets/Scripts/Gameplay/ChartLoader.cs` | 수정 | 3번째 칸(종료 시각) 선택 파싱, 종료 ≤ 시작이면 경고 후 건너뜀 |
| `Assets/Scripts/Gameplay/Note.cs` | 수정 | `HoldState`(Waiting/Holding/Completed/Failed), 몸통 길이 계산 |
| `Assets/Scripts/Gameplay/GameplayManager.cs` | 수정 | 시작 판정 후 유지 상태 전환, 유지·조기 해제·종료 판정 |
| `Assets/Prefabs/Note.prefab` | 수정 | 자식 `Body` 스프라이트 추가 (일반 노트는 숨김) |
| `Assets/Charts/test_chart.txt` | 수정 | 8초 이후 롱노트 추가 (동시 롱노트 포함) |
| `Assets/Scripts/Gameplay/Note.cs` | 수정 (틱) | 시작 판정 등급 `HeadJudgement`, 다음 틱 시각 `NextTickTime` 저장 |
| `Assets/Scripts/Gameplay/GameplayManager.cs` | 수정 (틱) | 유지 중 틱 판정, `OnTick(시작 등급, 레인)` 이벤트 추가 |
| `Assets/Scripts/Gameplay/Note.cs` | 수정 (Miss 표시) | `ShowMissed()`로 머리·몸통을 회색 반투명으로, `Failed` 상태에서도 머리를 판정선에 고정 |
| `Assets/Scripts/Gameplay/GameplayManager.cs` | 수정 (Miss 표시) | Miss 롱노트를 표시 목록으로 옮기고, 매 프레임 위치 갱신, 종료 시각에 삭제 |
| `Assets/Scripts/Gameplay/ScoreComboState.cs` | 수정 (틱) | `OnTick` 구독: 시작 등급 점수의 30% 가산, 콤보 +1 |

## 4. 상세 구현 방법 및 필요한 코드 스니펫
- 키 누름: 레인의 `Waiting` 노트 중 가장 가까운 것을 판정. 롱노트면 시작 판정을 보고하고 `Holding`으로 전환(노트 유지), 일반 노트면 기존처럼 최종 판정.
- 매 프레임 `Holding` 노트: `songTime >= EndTime`이면 끝 Perfect로 완료. 키가 안 눌려 있고 `EndTime - songTime > 0.150`이면 끝 Miss로 실패.
- Miss 처리: Good 범위를 지난 `Waiting` 노트만 대상. 롱노트면 시작 Miss + 끝 Miss.
- Miss 표시: 롱노트가 Miss로 최종 판정되면 레인 목록에서 빼서 입력·틱·Miss 처리 대상에서 제외하고, 별도 목록(`missedLongNotes`)에서 위치만 갱신한다. 종료 시각이 지나면 삭제한다. 머리를 놓친 경우 Miss 확정 시점(노트 시각 + 150ms)에 머리가 판정선보다 약 0.75유닛 아래에 있어 고정 시 살짝 올라가 보인다.
- 틱: 시작 판정 성공 시 `HeadJudgement` 저장, `NextTickTime = 시작 + 0.15`. 유지 중 매 프레임 조기 해제 확인 → `NextTickTime < EndTime && songTime >= NextTickTime`인 동안 틱 보고 후 0.15초 증가. 0.15는 이진 소수로 정확히 표현되지 않아 누적 오차로 종료 시각과 겹치는 틱이 생길 수 있으므로 비교에 여유값(`TickEpsilon = 1e-6`)을 둔다 → 종료 확인 순으로 처리. 틱은 `OnJudged`가 아닌 별도 `OnTick`으로 보고해 판정 횟수(결과·정확도용)와 섞이지 않게 한다.
- 표시: 머리 Y = 판정선 + (시작 - 곡 시간) × 속도, `Holding` 중에는 판정선에 고정. 꼬리 Y = 판정선 + (종료 - 곡 시간) × 속도. 몸통은 머리~꼬리 길이로 스케일.

```csharp
public enum HoldState { Waiting, Holding, Completed, Failed }

// 유지 중인 롱노트: 종료 시각 도달 시 완료, 너무 일찍 떼면 실패
if (songTime >= note.Data.EndTime) { note.State = HoldState.Completed; Judge(note, Judgement.Perfect); }
else if (!pressed && note.Data.EndTime - songTime > JudgementWindow.GoodWindow) { note.State = HoldState.Failed; Judge(note, Judgement.Miss); }
```

## 5. 구현 순서 및 진행 상태
- [x] 롱노트 Miss 표시 (2026-11-15 요청): `Note.missedColor`·`ShowMissed()`, `Failed`에서 머리 고정, `GameplayManager.missedLongNotes`에서 위치 갱신·종료 시각 삭제
- [x] 틱 간격 0.25초 → 0.15초 변경 (2026-11-15 요청), 종료 시각 겹침 방지 여유값 추가
- [x] 틱(유지) 판정 추가 (2026-11-15 요청): `Note.HeadJudgement`·`NextTickTime`, `GameplayManager.OnTick`·`ReportTick`, `ScoreComboState.HandleTick`
- [x] 노트 데이터 구조 확인 및 계획 승인
- [x] 롱노트 표시 및 상태 구현 (`Note.HoldState`, 프리팹 `Body` 자식, 폭 0.6)
- [x] 입력 유지와 종료 판정 연결 (`GameplayManager.HandleHolds`, 판정 보고는 `Report`로 분리)
- [x] 점수 시스템 연동 (판정 2회 보고로 연동, 점수 코드 수정 없음)
- [x] 검증용 채보에 롱노트 5개·일반 노트 1개 추가 (8.0~12.5초)

- 완료: 계획, 구현, 자동 검증 가능한 범위 검증
- 진행 중: 없음
- 남은 작업: 없음

## 6. 테스트 및 검증 방법
| 테스트 항목 | 기대 결과 | 상태 |
|---|---|---|
| 정상 유지 | 종료 시각까지 누르면 끝 Perfect | 통과 (사용자 플레이, AUTO 3단계 롱노트 5개 끝 Perfect) |
| 조기 해제 | 종료 150ms 전보다 일찍 떼면 끝 Miss 1회 | 통과 (상태 강제 전환) |
| 시작 입력 누락 | 시작 Miss + 끝 Miss 2회 | 통과 |
| 동시 노트 | 각 노트 상태 독립 유지 | 통과 (8.0초 동시 롱노트 2개 각각 처리) |
| 몸통 표시 | 머리~종료 시각 위치까지 표시 | 통과 |
| 일반 노트 회귀 | 기존과 동일하게 낙하·Miss | 통과 |
| 틱 판정 | 유지 중 0.15초마다 틱, 시작 등급 점수의 30% + 콤보 1 | 통과 (HandleHolds 직접 호출) |
| 틱 후 끝 판정 | 마지막 틱 뒤 종료 시각에 끝 Perfect, 이후 중복 없음 | 통과 (HandleHolds 직접 호출) |
| 시작 놓침 표시 | 회색 반투명으로 남고 머리는 판정선에 고정, 종료 시각에 삭제 | 통과 (AUTO 0단계) |
| 조기 해제 표시 | 회색 반투명으로 남고 머리는 판정선에 고정, 종료 시각에 삭제 | 통과 (AUTO 1단계) |
| Miss 표시 중 입력 | 판정 없음 | 통과 (Miss 롱노트가 레인 목록에서 빠져 입력 대상 아님, `IsJudged` true) |
| 조기 해제 시 틱 | 남은 틱 버리고 끝 Miss 1회 | 통과 (HandleHolds 직접 호출) |

- [x] Unity 컴파일 오류 확인
- [x] 실제 플레이에서 기능 검증 (키 입력: 사용자 확인, 자동: AUTO TEST 3단계 시작·틱 36회·끝 모두 Perfect, 2단계 조기 해제 포함)
- [x] 관련 기존 기능 회귀 테스트

## 7. 검증 결과 및 미해결 사항
- 검증 결과 (2026-11-15):
  - 컴파일 에러 0건. 콘솔 경고는 검증용 `EditorApplication.Step()` 반복 호출로 생긴 "PlayerLoop ... called recursively" 1건뿐(게임 코드와 무관한 것으로 추정).
  - 몸통 표시: 7.9초에 8.0~9.0초 롱노트 머리 y=-2.50, 몸통 -2.50~2.50(길이 5 = 1초 × 속도 5). 9.5~11.0초 롱노트 몸통 5.00~12.50. 계산값과 일치.
  - 유지 중 머리 고정: `Holding`으로 바꾼 노트의 머리가 판정선(y=-3.00)에 고정됨.
  - 시작 누락: 롱노트 4개 모두 시작 Miss + 끝 Miss가 각각 1회씩 기록됨. 8.0초 동시 롱노트 2개도 각각 처리.
  - 조기 해제: 8.0~9.5초 노트를 `Holding`으로 강제 전환(머리 성공 대신) → 키가 안 눌린 상태에서 다음 프레임에 끝 Miss 1회만 기록, 시작 Miss는 중복 기록되지 않음.
  - 키 입력: `InputSystem.QueueStateEvent` + `InputSystem.Update()`로 키 상태 주입은 되지만(`isPressed` true 확인), Game 뷰 포커스가 없어 게임 프레임 안에서는 입력이 반영되지 않음(`editorInputBehaviorInPlayMode = PointersAndKeyboardsRespectGameViewFocus`). 따라서 머리 판정 성공·유지 완료(끝 Perfect)·실제 키 해제는 미검증.
- 미해결: 없음 (사용자 직접 플레이로 확인, 2026-11-15)
- 틱 판정 검증 결과 (2026-11-15):
  - 컴파일 에러 0건, Play 모드 중 에러·경고 0건.
  - 키 입력 주입은 `EditorApplication.Step()` 프레임에 반영되지 않음. 입력 라우팅(`editorInputBehaviorInPlayMode`)과 백그라운드 동작(`backgroundBehavior`)을 런타임에서 임시로 바꿔도 동일 → 검증 후 원래 값으로 복원(Input System 설정 에셋이 프로젝트에 없어 파일 변경 없음).
  - 대신 노트를 `Holding`으로 강제 전환하고 `HandleHolds(곡 시간)`를 리플렉션으로 직접 호출해 로직 확인:
    - 9.0~10.0초 노트(시작 Perfect 가정), 9.3초 키 안 눌림 → 끝 Miss 1회, 틱 없음, 점수·콤보 변화 없음. 통과
    - 9.5~11.0초 노트(시작 Great 가정), 10.9초(종료 0.1초 전, 허용 범위) → 틱 9.75/10.0/10.25/10.5/10.75초 5회, 점수 +1050(210×5), 콤보 +5. 통과
    - 11.0초 → 끝 Perfect, 점수 +1000, 콤보 +1. 11.2초 재호출 시 추가 판정 없음. 통과
  - 틱 간격 0.15초로 변경 후 재검증 (2026-11-15, 같은 방법):
    - 8.0~9.5초 노트(1.5초, 0.15×10이 종료와 겹침, 시작 Perfect 가정): 9.4초 호출 → 틱 8.15~9.35초 9회 +2700·콤보 +9, 9.5초 호출 → 9.5초 틱 없이 끝 Perfect +1000. 통과
    - 9.5~11.0초 노트(시작 Great 가정): 10.9초 호출 → 틱 9.65~10.85초 9회 +1890, 11.0초 → 끝 Perfect +1000, 재호출 시 추가 없음. 통과
    - 9.0~10.0초 노트 9.3초 조기 해제 → 끝 Miss 1회, 틱 없음. 통과
  - 실제 키 누름 흐름: 사용자 직접 플레이로 확인(2026-11-15). AUTO TEST 3단계에서 틱 36회·끝 Perfect 5회로 재확인
- 변경 이력: 초안 작성 → 2026-11-15 미확정 사항 확정 및 상세 설계 반영 → 구현 및 검증
- Miss 표시 검증 결과 (2026-11-15, AUTO TEST로 Play 모드 진행, 프레임당 0.04초):
  - 컴파일 에러 0건, Play 모드 중 에러·경고 0건.
  - 조기 해제(AUTO 1단계): 8.0초 롱노트 2개가 머리 Perfect 후 8.05초에 떼어짐 → 8.14초에 `Failed`, 레인 목록에서 빠짐, 머리 y=-3.00 고정, 색 (0.5, 알파 0.4). 8.54초에 몸통 -3.00~-0.70으로 줄어듦. 9.0초 노트는 9.06초에 삭제됨, 9.5초 노트는 9.54초에 삭제됨.
  - 시작 놓침(AUTO 0단계, 입력 없음): 8.14초에는 아직 대기(머리 -3.70, 판정선 아래), 8.22초에 `Failed`·회색 반투명·머리 -3.00 고정(예상대로 0.7유닛 위로 올라감). 이후 같은 방식으로 줄어들다 종료 시각에 삭제.
  - 회귀: AUTO 3단계 `Perfect 25 / Tick 36 / 점수 35800 (일치)`, 1단계 `Perfect 5 / Miss 20 / 점수 5000 (일치)` — 변경 전과 동일.
  - 참고(기존 동작): 머리를 친 프레임에는 위치 갱신이 판정보다 먼저라 머리가 판정선 아래(최대 1프레임 이동량)에 보이고, 다음 프레임부터 판정선에 고정된다.
  - 미검증: 실제 화면에서 회색 반투명이 잘 보이는지 (사용자 확인)
