# 노트 배속 설정 구현 계획서

## 1. 작업 목적 및 요구사항
- 프로젝트: music_game (Unity 6.6, C#, URP, Mono, Input System)
- 목적: 노트의 화면 이동 속도를 변경한다.
- 상태: 구현 완료(2026-11-15) / 사용자 확인 완료 (PLAN.md 확인)
- 이 문서는 개발 진행에 따라 수정한다. 작업 범위 확대는 사용자 확인 후 반영한다.

### 요구사항
- 노트의 화면 이동 속도를 변경한다.
- 기존 기능을 보존하고 요청 범위에 필요한 변경만 수행한다.
- 확정 사항 (2026-11-15 사용자 승인):

| 항목 | 확정값 |
|---|---|
| 범위·단위 | x1.0 ~ x5.0, 0.5 단위 (9단계) |
| 기본값 | x3.0 = 현재 노트 속도(초당 5칸). 현재 속도를 평균값으로 본다 |
| 속도 계산 | 실제 속도 = 5 × (배속 ÷ 3). x1.0 ≈ 초당 1.67칸(판정선까지 5.4초), x5.0 ≈ 초당 8.33칸(1.08초) |
| 설정 위치 | 곡 선택 화면: `SPEED x3.0` 표시 + -/+ 버튼, 키보드 ←→. 키 설정 패널이 열려 있으면 무시. **2026-11-17 변경: ←→는 난이도 조절로 바뀌어 배속은 −/+ 버튼으로만 바꿈 (`song_data_plan.md` 8절)** |
| 플레이 중 변경 | 불가. 일시정지 중 배속 변경도 없음 (2026-11-15 사용자 결정, `pause_restart_plan.md`) |
| 저장 | PlayerPrefs `NoteSpeed`. 저장값이 범위 밖이거나 0.5 단위가 아니면 가장 가까운 유효값으로 맞춤 |
| 판정·음악 | 배속은 화면 위치 계산에만 적용. 판정 시각과 오디오 재생 속도는 변경하지 않음 |

## 2. 기존 코드 분석
- 노트 속도는 `GameplayManager.fallSpeed` 하나로 정해진다 (IngameScene 저장값 5, 초당 5칸).
- 노트 생성 Y `spawnY` = 6(화면 위 바깥, 카메라 orthographic size 5), 판정선 `judgeLineY` = -3 → 현재 생성 후 1.8초 뒤 판정선 도달.
- `GameplayManager.SpawnNotes`: `leadTime = (spawnY - judgeLineY) / fallSpeed` 이내로 다가온 노트를 생성한다.
- `GameplayManager.MoveNotes` / `SpawnNotes`가 `Note.UpdatePosition(songTime, judgeLineY, fallSpeed)`를 호출하고, 위치는 매 프레임 곡 시간으로 계산한다(`judgeLineY + (노트 시각 - 곡 시간) × fallSpeed`, 롱노트 꼬리도 동일). 따라서 배속은 판정 시각·음악에 영향을 주지 않는다.
- 곡 선택 화면 키보드 처리는 `SongSelectScreen.Update`(↑↓/Enter, 키 설정 패널이 열려 있으면 무시).
- ←→는 `KeyBindings.IsAllowed`에서 레인 키로 사용 불가 → 배속 조절 키로 써도 겹치지 않는다.
- Esc 일시정지 기능은 없다.
- Enter Play Mode Options가 켜져 있어 static 값이 Play 세션 사이에 남는다 → 새 static 값도 Play 시작 시 다시 불러온다.

## 3. 작업 범위 및 수정 대상 파일
| 파일 | 구분 | 내용 |
|---|---|---|
| `Assets/Scripts/Gameplay/NoteSpeed.cs` | 신규 | 배속 값(static `Value`), `Min`/`Max`/`Step`/`Default`, `Increase`/`Decrease`, PlayerPrefs 저장·불러오기, Play 시작 시 다시 불러오기 |
| `Assets/Scripts/Gameplay/GameplayManager.cs` | 수정 | `fallSpeed`를 "x3.0일 때 속도"로 의미 변경, `FallSpeed` 속성으로 노트 생성 시점·위치 계산에 배속 반영 |
| `Assets/Scripts/Song/SongSelectScreen.cs` | 수정 | SPEED 표시, -/+ 버튼 연결, ←→ 처리 |
| `Assets/Scenes/SongSelectScene.unity` | 수정 | `SpeedText`, `SpeedDownButton`, `SpeedUpButton` 추가 (곡 정보 아래, START 근처) |

- 인스펙터 `fallSpeed` 값(5)과 필드 이름은 유지한다 → IngameScene 저장값 변경 없음.
- 제외 범위: Esc 일시정지(별도 기능 `pause_restart_plan.md`), 일시정지 중 배속 변경(만들지 않음, 2026-11-15 사용자 결정), 플레이 중 배속 변경, 플레이 화면의 배속 표시, 다른 기능 신규 구현, 패키지 및 프로젝트 설정 변경.

## 4. 상세 구현 방법 및 필요한 코드 스니펫
```csharp
// GameplayManager: 기준 배속(x3.0)일 때 fallSpeed, 그 외에는 배속 비율만큼 빠르거나 느리게
private float FallSpeed => fallSpeed * NoteSpeed.Value / NoteSpeed.Default;
```
- `SpawnNotes`, `MoveNotes`에서 `fallSpeed`를 쓰던 곳을 `FallSpeed`로 바꾼다. 배속이 낮을수록 `leadTime`이 길어져 더 일찍 생성되고, 생성 위치는 화면 밖(y=6)으로 유지된다.

```csharp
// NoteSpeed: 범위 안에서 0.5 단위로 올리거나 내리고 저장한다.
public static void Increase() => Set(Value + Step);
public static void Decrease() => Set(Value - Step);
private static void Set(float value)
{
    Value = Mathf.Clamp(Mathf.Round(value / Step) * Step, Min, Max); // 0.5 단위로 맞추고 범위 제한
    PlayerPrefs.SetFloat(PrefsKey, Value);
    PlayerPrefs.Save();
}
```
- 표시 형식: `SPEED x3.0` (소수 첫째 자리).

## 5. 구현 순서 및 진행 상태
- [x] 기존 노트 이동 및 시간 구조 확인
- [x] `NoteSpeed` 작성 (배속 값 관리, 저장·불러오기) → 컴파일 확인 (에러 0건). Play 시작 시 PlayerPrefs에서 다시 불러오도록 static 초기화 포함
- [x] `GameplayManager` 노트 위치 계산 연결 → 컴파일 확인 (에러 0건). `FallSpeed` 속성 추가, `SpawnNotes`의 `leadTime`과 `UpdatePosition` 호출 3곳에 적용
- [x] `SongSelectScreen` 수정(`speedText`/`speedDownButton`/`speedUpButton`, `SpeedDown`/`SpeedUp`, ←→), `SongSelectScene`에 SPEED UI 구성: START 아래 `SpeedDownButton`(-) · `SpeedText` · `SpeedUpButton`(+) (y=-900, `KeySettingPanelRoot`보다 뒤에 그려져 패널이 위를 덮음)
- [x] 검증 (검증 후 PlayerPrefs `NoteSpeed`를 검증 전 상태(저장값 없음)로 되돌림)

- 완료: 계획, 구현, Claude 검증
- 진행 중: 없음
- 남은 작업: 없음 (←→ 실제 키 입력, 키 설정 패널이 열린 동안 ←→ 무시, 체감 속도, 화면 배치는 사용자 확인 완료 (PLAN.md 확인 처리 기준))

## 6. 테스트 및 검증 방법
| 테스트 항목 | 기대 결과 | 상태 |
|---|---|---|
| 기본값 | 저장값이 없으면 x3.0, 노트 속도가 기존과 동일(초당 5칸) | 통과 (SPEED x3.0, 측정 속도 5.000) |
| 배속 변경 | -/+로 0.5씩 변경, x1.0 ~ x5.0 밖으로 나가지 않음, 표시 갱신 | 통과 (버튼 `onClick` 호출) |
| 저장 | PlayerPrefs 저장, Play 재시작 후에도 유지 | 통과 |
| 잘못된 저장값 | 가장 가까운 유효값으로 보정 | 통과 (7.3→5, 2.2→2, 0.2→1, 3.74→3.5) |
| 배속 증가·감소 | 같은 시간 차에서 판정선까지 거리: x1.0 = 기존의 1/3, x5.0 = 5/3 | 통과 (측정 속도 x1.0 1.667 / x3.0 5.000 / x5.0 8.333) |
| 판정 시각 | x1.0 / x3.0 / x5.0 모두 AUTO 3단계 35800점, 최대 콤보 61 | 통과 (세 배속 모두 SS, 35800, 61) |
| 음악 재생 | 배속 변경 전후 동일 (오디오 코드 변경 없음) | 미검증 (검증 당시 테스트 곡에 음악이 없어 실행 확인 불가. 오디오 코드는 변경하지 않음) |
| 키 설정 패널 | 패널이 열린 동안 ←→ 무시 | 미검증 (키 주입이 `wasPressedThisFrame`에 반영되지 않음. 기존 `IsOpen` 조기 반환 뒤에 ←→ 처리가 있음을 코드로만 확인. 사용자 확인 필요) |

- [x] Unity 컴파일 오류 확인
- [x] 실제 플레이에서 기능 검증 (←→ 실제 키 입력, 체감 속도는 사용자 확인 완료 (PLAN.md 확인 처리 기준))
- [x] 관련 기존 기능 회귀 테스트 (AUTO 3단계 35800, 곡 선택 → 플레이 → 결과 → SONG SELECT 흐름)
- 검증 후 PlayerPrefs `NoteSpeed`는 검증 전 상태로 되돌린다.

## 7. 검증 결과 및 미해결 사항
- 검증 결과 (2026-11-15, Play 모드에서 버튼은 `onClick` 호출, 노트 속도는 `(노트 Y - 판정선 Y) ÷ (노트 시각 - 곡 시간)`으로 측정):
  - 컴파일 에러 0건, Play 모드 중 에러·경고 0건.
  - 저장값 없음 → SPEED x3.0. - 5번: x2.5 → x2.0 → x1.5 → x1.0 → x1.0. + 10번: x1.5 → … → x5.0 → x5.0 → x5.0, PlayerPrefs 5. Play 재시작 후 x5.0 유지.
  - Test Song 1, AUTO 3단계: x1.0 속도 1.667(가장 먼 노트 4.94초 앞, 생성 시간 5.4초 이내), x3.0 속도 5.000(1.38초 앞), x5.0 속도 8.333(1.06초 앞). 세 배속 모두 SS / 35800 / 최대 콤보 61, 요약 로그 "예상 35800, 일치".
  - 검증 전 PlayerPrefs에 `NoteSpeed`가 없었으므로 검증 후 삭제해 원상태로 되돌림. 씬 변경 남지 않음.
  - 화면 배치: 캔버스 기준 해상도 1920×1080(너비 기준 맞춤)에서 SPEED 줄은 y=-900~-990으로 화면 안. 다만 Game 뷰 비율이 16:9보다 가로로 넓으면 START 버튼과 함께 화면 아래로 잘릴 수 있음(기존 배치와 같은 조건). 실제 화면 배치는 사용자 확인 필요.
- 미해결:
  - 일시정지 화면에서의 배속 변경: 만들지 않기로 결정 (2026-11-15 사용자 결정, `pause_restart_plan.md`)
  - Claude가 확인하지 못한 항목(←→ 실제 키 입력, 키 설정 패널이 열린 동안 ←→ 무시, 체감 속도, 화면 배치)은 사용자 확인 완료 (PLAN.md 확인 처리 기준). 6장 표의 미검증은 Claude 검증 기준으로 그대로 둠
- 변경 이력: 초안 작성 → 2026-11-15 소스 분석, 미확정 사항 확정 및 상세 설계 반영 → 구현 및 검증 → 2026-11-17 ←→ 키를 난이도 조절로 변경, 배속은 −/+ 버튼만 (사용자 요청, `song_data_plan.md` 8절)
