# 키 설정 및 변경 구현 계획서

## 1. 작업 목적 및 요구사항
- 프로젝트: music_game (Unity 6.6, C#, URP, Mono, Input System)
- 목적: 노트 입력에 사용하는 키를 변경하고 플레이에 적용한다.
- 상태: 구현 완료(2026-11-15) / 사용자 확인 대기 (실제 키 입력으로 변경, ↑↓/Enter 차단, 바꾼 키로 플레이, 화면 배치)
- 이 문서는 개발 진행에 따라 수정한다. 작업 범위 확대는 사용자 확인 후 반영한다.

### 요구사항
- 노트 입력에 사용하는 키를 변경하고 플레이에 적용한다.
- 기존 기능을 보존하고 요청 범위에 필요한 변경만 수행한다.
- 확정 사항 (2026-11-15 사용자 승인):

| 항목 | 확정값 |
|---|---|
| 레인·기본 키 | 4레인, D / F / J / K |
| 설정 위치 | 곡 선택 화면의 KEY SETTING 버튼 → 설정 패널 |
| 변경 방법 | 패널에서 레인 버튼(`LANE 1  D` 형식)을 누르고 새 키를 누르면 적용 |
| 중복 키 | 서로 맞바꿈 (예: 레인 1(D)에 F → F를 쓰던 레인 2가 D로) |
| 입력 규칙 | 키 대기 중 Esc는 취소. Enter·숫자패드 Enter·방향키·Esc는 레인 키로 사용 불가("CANNOT USE" 표시) |
| 저장 | PlayerPrefs에 저장해 재실행 후에도 유지. RESET 버튼으로 D/F/J/K 복원 |
| 적용 | 플레이 씬의 키보드 입력(AUTO 0단계)이 저장된 키를 사용 |
| 패널이 열린 동안 | 곡 선택 화면의 ↑↓/Enter 동작 안 함. CLOSE로 닫음 |

## 2. 기존 코드 분석
- 레인 키는 `GameplayManager.laneInputKeys`(인스펙터 값 D/F/J/K)에만 있고 변경·저장 기능이 없다.
- 키보드 입력은 `GameplayManager.WasPressed` / `IsHeld` 두 곳에서만 읽는다(AUTO TEST 작업 때 분리).
- 곡 선택 화면은 `SongSelectScreen.Update`에서 ↑↓/Enter를 처리한다.
- Enter Play Mode Options가 켜져 있어 static 값이 Play 세션 사이에 남는다 → 새 static 값도 Play 시작 시 초기화한다.

## 3. 작업 범위 및 수정 대상 파일
| 파일 | 구분 | 내용 |
|---|---|---|
| `Assets/Scripts/Input/KeyBindings.cs` | 신규 | 레인별 키 조회(`Get`), 변경·맞바꿈(`TrySet`), 사용 가능 여부(`IsAllowed`), `Reset`, PlayerPrefs 저장·불러오기 |
| `Assets/Scripts/Song/KeySettingPanel.cs` | 신규 | 패널 열기·닫기, 레인 버튼 표시, 키 입력 대기(Esc 취소), 금지 키 안내, RESET |
| `Assets/Scripts/Gameplay/GameplayManager.cs` | 수정 | `laneInputKeys` 대신 `KeyBindings.Get(lane)` 사용 (인스펙터 키 항목 제거) |
| `Assets/Scripts/Song/SongSelectScreen.cs` | 수정 | 키 설정 패널이 열려 있으면 ↑↓/Enter 무시 |
| `Assets/Scenes/SongSelectScene.unity` | 수정 | KEY SETTING 버튼, 설정 패널(반투명 배경, 레인 버튼 4개, 안내 문구, RESET, CLOSE) |

- 제외 범위: 레인 수 변경, 게임패드, 별도 키 설정 화면, 플레이 중 키 변경.

## 4. 상세 구현 방법 및 필요한 코드 스니펫
```csharp
// 레인 키 변경: 다른 레인이 이미 쓰는 키면 서로 맞바꾼다. 금지 키면 false.
public static bool TrySet(int lane, Key key)
{
    if (!IsAllowed(key) || lane < 0 || lane >= LaneCount) return false;
    int other = System.Array.IndexOf(keys, key);
    if (other >= 0) keys[other] = keys[lane]; // 맞바꿈
    keys[lane] = key;
    Save();
    return true;
}
```
- 키 입력 대기: 매 프레임 `Keyboard.current.allKeys` 중 이번 프레임에 눌린 키를 찾아 적용한다.
- PlayerPrefs 키 이름: `KeyBinding.Lane0` ~ `KeyBinding.Lane3`.

## 5. 구현 순서 및 진행 상태
- [x] `KeyBindings` 작성, `GameplayManager` 연결 → 컴파일 확인 (에러 0건). Play 시작 시 PlayerPrefs에서 다시 불러오도록 static 초기화 포함, 저장값이 겹치거나 금지 키면 기본값 사용
- [x] `KeySettingPanel` 작성, `SongSelectScreen` 수정 → 컴파일 확인 (에러 0건). 레인 버튼 클릭 시 UI 선택을 해제해 Space/Enter가 UI 제출로 처리되지 않게 함
- [x] `SongSelectScene`에 키 설정 UI 구성: 오른쪽 위 `KeySettingButton`, `KeySettingPanelRoot`(반투명 배경이 뒤쪽 클릭 차단, 가운데 상자에 제목·`Lane0~3`·메시지·RESET·CLOSE), `KeySettingPanel` 오브젝트, `SongSelectScreen.keySettingPanel` 연결
- [x] 검증 (검증 후 PlayerPrefs를 검증 전 상태(저장값 없음)로 되돌림)

- 완료: 계획, 구현, Claude 검증
- 진행 중: 없음
- 남은 작업: 사용자 확인 (실제 키 입력으로 변경, 패널이 열린 동안 ↑↓/Enter 차단, 바꾼 키로 플레이, 화면 배치)

## 6. 테스트 및 검증 방법
| 테스트 항목 | 기대 결과 | 상태 |
|---|---|---|
| 키 변경·맞바꿈 | 레인 1에 K → K/F/J/D, 버튼 글자 갱신 | 통과 (`ApplyKey` 호출) |
| 금지 키 | Enter·방향키 거부 + CANNOT USE, Esc는 취소 | 통과 (거부 후 계속 대기, Esc로 CANCELED) |
| 저장 | PlayerPrefs에 저장, Play 재시작 후에도 유지 | 통과 |
| RESET | D/F/J/K로 복원 | 통과 |
| 패널 열린 동안 | 곡 선택 ↑↓/Enter 무시 | 미검증 (키 주입이 `wasPressedThisFrame`에 반영되지 않아 확인 불가, 코드로만 확인. 사용자 확인 필요) |
| 플레이 적용 | 플레이 씬 레인 키가 저장된 값 사용, AUTO 3단계 결과 동일 | 통과 (K/F/J/D 설정 후 K→레인1, F→레인2, D→레인4 누르고 있음 판정. AUTO 3단계 35800) |
| 이전 키 입력 | 변경된 레인에 반응하지 않음 | 통과 (D를 눌러도 레인1은 반응 없음, 누르고 있음 판정 기준). 실제 키 누름 판정은 사용자 확인 |

- [x] Unity 컴파일 오류 확인
- [ ] 실제 플레이에서 기능 검증 (실제 키 입력은 사용자 확인)
- [x] 관련 기존 기능 회귀 테스트 (AUTO 3단계 35800)

## 7. 검증 결과 및 미해결 사항
- 검증 결과 (2026-11-15, Play 모드에서 버튼은 `onClick` 호출):
  - 컴파일 에러 0건, Play 모드 중 에러·경고 0건.
  - 키 입력 주입: `InputSystem.QueueStateEvent` + `InputSystem.Update()`로 넣은 키는 `isPressed`에는 반영되지만 `wasPressedThisFrame`에는 반영되지 않음 → 패널의 키 감지(`Update`)와 곡 선택 ↑↓/Enter 차단은 직접 확인하지 못함. 키 적용은 `ApplyKey`를 직접 호출해 확인.
  - KEY SETTING → 패널 열림, `LANE 1 D` ~ `LANE 4 K`. LANE 1 클릭 → 대기(주황) + "PRESS A KEY (ESC: CANCEL)". K 적용 → K/F/J/D(맞바꿈).
  - LANE 2 대기 중 Enter·↑ → "CANNOT USE" 후 계속 대기, Esc → "CANCELED", 키 변경 없음.
  - PlayerPrefs `KeyBinding.Lane0~3` = K/F/J/D, Play 재시작 후에도 K/F/J/D.
  - 플레이 씬: K 누름 → 레인1만, F → 레인2만, D → 레인4만 누르고 있음 판정.
  - RESET → D/F/J/K, "RESET TO D / F / J / K".
  - 검증 전 PlayerPrefs에 저장값이 없었으므로 검증 후 저장 키를 삭제해 원상태로 되돌림.
- 미해결: 실제 키 입력으로 레인 키 변경, 패널이 열린 동안 곡 선택 ↑↓/Enter 차단, 바꾼 키로 노트 판정 (사용자 확인 필요)
- 변경 이력: 초안 작성 → 2026-11-15 미확정 사항 확정 및 상세 설계 반영 → 구현 및 검증
