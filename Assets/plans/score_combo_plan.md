# 점수 및 콤보 시스템 구현 계획서

## 1. 작업 목적 및 요구사항
- 프로젝트: music_game (Unity 6.6, C#, URP, Mono, Input System)
- 목적: 판정 결과를 점수와 콤보로 반영한다.
- 상태: 구현 완료 / 사용자 확인 완료 (2026-11-15, PLAN.md 확인)
- 이 문서는 개발 진행에 따라 수정한다. 작업 범위 확대는 사용자 확인 후 반영한다.

### 요구사항
- 판정 결과를 점수와 콤보로 반영한다.
- 기존 기능을 보존하고 요청 범위에 필요한 변경만 수행한다.
- 확정 사항 (2026-11-15 사용자 승인):

| 항목 | 확정값 |
|---|---|
| 판정별 점수 | Perfect 1000 / Great 700 / Good 300 / Miss 0 |
| 콤보 | Perfect·Great·Good이면 +1, Miss면 0으로 초기화 |
| 롱노트 틱 점수 (2026-11-15 추가, `long_note_plan.md`) | 틱 1회마다 롱노트 시작 판정 등급 점수의 30% (Perfect 300 / Great 210 / Good 90), 콤보 +1 |
| 최대 콤보 (2026-11-15, `result_screen_plan.md`) | 플레이 중 가장 높았던 콤보(틱 포함)를 `MaxCombo`로 기록 |
| 초기화 시점 | 플레이 시작 시 (`Start`) |
| 화면 표시 | Canvas(Screen Space Overlay) + 기본 uGUI `Text` 2개. 점수 오른쪽 위, 콤보 화면 중앙 위쪽, 콤보 0이면 숨김 |
| 결과 화면 전달 | `Score`, `Combo` 읽기 전용 속성만 공개. 실제 전달은 `result_screen_plan.md` |

## 2. 기존 코드 분석
- `GameplayManager`(`Assets/Scripts/Gameplay/GameplayManager.cs`)가 판정 확정 시 `event Action<Judgement, int> OnJudged`(판정, 레인)를 노트당 1회 호출한다. `Note.IsJudged` 플래그로 중복 판정을 막는다.
- `Judgement` enum: Perfect, Great, Good, Miss (`Judgement.cs`).
- UI: `com.unity.ugui` 2.6.0 설치. TMP Essential Resources는 미임포트 → 에셋 추가 없이 쓸 수 있는 기본 `UnityEngine.UI.Text`(내장 폰트 `LegacyRuntime.ttf`) 사용.
- 씬: `IngameScene`에 Canvas 없음.

## 3. 작업 범위 및 수정 대상 파일
| 파일 | 구분 | 역할 |
|---|---|---|
| `Assets/Scripts/Gameplay/ScoreComboState.cs` | 신규 | `OnJudged` 구독, 점수·콤보 갱신 및 Text 표시 |
| `Assets/Scenes/IngameScene.unity` | 수정 | `Canvas`, `ScoreText`, `ComboText`, `ScoreComboState` 추가 |
| `Assets/Scripts/Gameplay/ScoreComboState.cs` | 수정 (2026-11-15, 롱노트 틱) | `GameplayManager.OnTick` 구독, `HandleTick` 추가 |
| `Assets/Scripts/Gameplay/ScoreComboState.cs` | 수정 (2026-11-15, 결과 화면) | 최대 콤보 `MaxCombo` 추가 |

- 기존 스크립트는 수정하지 않는다. (롱노트 틱 이벤트 `OnTick`은 `long_note_plan.md` 작업에서 `GameplayManager`에 추가됨)
- 제외 범위: 최대 콤보, 결과 화면 전달, 판정 텍스트·연출, 패키지 및 프로젝트 설정 변경.

## 4. 상세 구현 방법 및 필요한 코드 스니펫
- `ScoreComboState`(MonoBehaviour)는 `GameplayManager`, `scoreText`, `comboText`를 인스펙터로 참조한다.
- `OnEnable`에서 `OnJudged`·`OnTick` 구독, `OnDisable`에서 해제한다.
- `Start`에서 점수·콤보를 0으로 초기화하고 표시를 갱신한다.
- 판정 수신 시 점수 가산, 콤보 증가 또는 초기화 후 표시를 갱신한다. 콤보가 0이면 `comboText`를 숨긴다.

```csharp
// 판정 등급별 점수
private static int GetPoints(Judgement judgement)
{
    switch (judgement)
    {
        case Judgement.Perfect: return 1000;
        case Judgement.Great: return 700;
        case Judgement.Good: return 300;
        default: return 0;
    }
}

private void HandleJudged(Judgement judgement, int lane)
{
    Score += GetPoints(judgement);
    Combo = judgement == Judgement.Miss ? 0 : Combo + 1;
    RefreshTexts();
}

// 롱노트 틱: 시작 판정 등급 점수의 30%, 콤보 +1 (OnJudged와 분리된 OnTick으로 수신)
private void HandleTick(Judgement headJudgement, int lane)
{
    Score += GetPoints(headJudgement) * 30 / 100;
    Combo++;
    RefreshTexts();
}
```

## 5. 구현 순서 및 진행 상태
- [x] 미확정 사항 확정 및 계획 승인
- [x] `ScoreComboState.cs` 작성 → 컴파일 확인 (에러 0건)
- [x] 씬에 Canvas·Text·`ScoreComboState` 구성 (`Canvas` 1920x1080 기준 ScaleWithScreenSize, `ScoreText` 오른쪽 위 64pt, `ComboText` 중앙 위쪽 80pt, 씬 저장)
- [x] Play 모드 검증 (점수·콤보 로직) — 실제 키 입력 판정은 사용자 확인 필요
- [x] 롱노트 틱 점수 추가 (2026-11-15, `long_note_plan.md` 요청에 따름)
- [x] 최대 콤보 `MaxCombo` 추가 (2026-11-15, `result_screen_plan.md`): AUTO 3단계 61, 1단계 2, 2단계 22 확인

- 완료: 계획, 구현, 로직 검증
- 진행 중: 없음
- 남은 작업: 없음

## 6. 테스트 및 검증 방법
| 테스트 항목 | 기대 결과 | 상태 |
|---|---|---|
| 연속 성공 판정 | 점수 누적 및 콤보 증가 | 통과 (이벤트 주입) |
| 콤보 중단 판정 | 콤보 초기화 | 통과 (실제 Miss) |
| 동일 노트 중복 처리 | 점수 중복 증가 없음 | 통과 (AUTO 3단계 점수 35800 = 계산값, 중복 가산 없음) |
| 새 곡 시작 | 점수와 콤보 초기화 | 통과 (Play 시작 시 0) |
| 롱노트 틱 | 시작 등급 점수의 30% 가산, 콤보 +1 | 통과 (틱 간격 0.15초 기준 Great 틱 9회 → +1890, 콤보 +9. `long_note_plan.md` 7장) |

- [x] Unity 컴파일 오류 확인
- [x] 실제 플레이에서 기능 검증 (키 입력: 사용자 확인, 자동: AUTO TEST 1~3단계 요약 로그 점수 일치)
- [x] 관련 기존 기능 회귀 테스트 (노트 낙하·Miss 정상)

## 7. 검증 결과 및 미해결 사항
- 검증 결과 (2026-11-15):
  - 컴파일 에러 0건.
  - Unity 창 포커스가 없어 Play 모드를 일시정지 후 `EditorApplication.Step()`으로 프레임을 넘기며 확인.
  - 시작 직후: score 0, combo 0, `ComboText` 숨김.
  - `OnJudged`에 Perfect, Perfect, Great, Good을 직접 호출(구독자 1개 확인): score 3000, combo 4, `ScoreText` "3000", `ComboText` "4 COMBO" 표시.
  - 이후 2.0초 노트 실제 Miss: score 3000 유지, combo 0, `ComboText` 숨김.
  - 콘솔 경고 1건("PlayerLoop internal function has been called recursively"): 검증용으로 `EditorApplication.Step()`을 외부에서 반복 호출해 생긴 것으로 판단. 게임 코드와 무관(추정).
- 미해결: 없음 (사용자 직접 플레이로 확인, 2026-11-15)
- 변경 이력: 초안 작성 → 2026-11-15 미확정 사항 확정 및 상세 설계 반영 → 구현 및 로직 검증 → 롱노트 틱 점수 추가 → 2026-11-15 씬 이름 변경(SampleScene → IngameScene), 에디터 Play 시작 씬을 SongSelectScene으로 고정
