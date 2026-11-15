# 게임 결과 화면 구현 계획서

## 1. 작업 목적 및 요구사항
- 프로젝트: music_game (Unity 6.6, C#, URP, Mono, Input System)
- 목적: 곡 종료 후 해당 플레이의 결과를 표시한다.
- 상태: 구현 완료 / 사용자 확인 완료 (2026-11-15, PLAN.md 확인)
- 이 문서는 개발 진행에 따라 수정한다. 작업 범위 확대는 사용자 확인 후 반영한다.

### 요구사항
- 곡 종료 후 해당 플레이의 결과를 표시한다.
- 기존 기능을 보존하고 요청 범위에 필요한 변경만 수행한다.
- 확정 사항 (2026-11-15 사용자 승인):

| 항목 | 확정값 |
|---|---|
| 표시 항목 | 랭크, 점수, 최대 콤보, 정확도(%), Perfect/Great/Good/Miss 개수 (틱은 개수에 포함하지 않음) |
| 정확도 | (Perfect×100 + Great×70 + Good×30 + Miss×0) ÷ 판정 수. 틱 제외, 소수 첫째 자리까지 |
| 랭크 | SS 100%(전부 Perfect) / S 95% 이상 / A 90% / B 80% / C 70% / D 그 미만 |
| 화면 방식 | 별도 `ResultScene`. Build Settings에 추가 (프로젝트 설정 변경, 사용자 승인) |
| 전환 조건 | ① 모든 노트 판정 완료 ② Miss 롱노트 회색 표시까지 모든 노트가 화면에서 사라짐 ③ 음악 재생 종료. 셋 다 만족하면 1초 대기 후 0.5초 동안 검게 페이드 아웃하고 전환. 음악이 먼저 끝나도 ①②를 기다림 |
| 음악 | 곡 파일이 없으면 "재생 종료"로 본다. 곡이 지정되면 재생이 끝날 때까지 기다림 |
| 다시 하기 | 버튼을 누르면 `IngameScene`을 다시 불러와 처음부터 플레이. AUTO TEST 단계는 씬 저장값(0)으로 돌아감 |
| 곡 선택 버튼 (2026-11-15, `song_select_plan.md`) | RETRY 옆에 SONG SELECT 버튼. 누르면 `SongSelectScene`으로 이동 |
| 결과 데이터 | 전환 직전에 바뀌지 않는 `PlayResult` 객체로 복사해 넘김. 다음 플레이가 시작돼도 이전 결과는 바뀌지 않음 |

## 2. 기존 코드 분석
- `ScoreComboState`에는 `Score`, `Combo`만 있고 최대 콤보가 없다. 콤보는 틱으로도 오른다.
- 판정 횟수는 `GameplayManager.OnJudged`로 받을 수 있다.
- `GameplayManager.IsFinished`는 판정 완료까지만 보고, 회색으로 표시 중인 Miss 롱노트(`missedLongNotes`)는 포함하지 않는다.
- 씬은 `IngameScene` 하나이고 Build Settings에도 이것만 있다.

## 3. 작업 범위 및 수정 대상 파일
| 파일 | 구분 | 내용 |
|---|---|---|
| `Assets/Scripts/Gameplay/PlayResult.cs` | 신규 | 바뀌지 않는 결과 데이터(점수, 최대 콤보, 판정별 개수), 정확도·랭크 계산, 마지막 결과 `PlayResult.Last` |
| `Assets/Scripts/Gameplay/ResultTransition.cs` | 신규 | `OnJudged`로 판정 횟수 집계, 전환 조건 확인 → 1초 대기 → 0.5초 페이드 → 결과 저장 → `ResultScene` 불러오기 |
| `Assets/Scripts/Result/ResultScreen.cs` | 신규 | `PlayResult.Last` 표시, 다시 하기 버튼. 결과가 없으면 "결과 없음" 표시 |
| `Assets/Scripts/Gameplay/ScoreComboState.cs` | 수정 | 최대 콤보 `MaxCombo` 추가 |
| `Assets/Scripts/Gameplay/GameplayManager.cs` | 수정 | `IsFinished`에 화면에 남은 Miss 롱노트 포함, 음악 재생 종료 여부 `IsMusicFinished` 추가 |
| `Assets/Scenes/IngameScene.unity` | 수정 | 화면 전체를 덮는 검은 `FadeOverlay`(처음엔 투명), `ResultTransition` 오브젝트 |
| `Assets/Scenes/ResultScene.unity` | 신규 | 카메라, Canvas(RESULT 제목, 랭크, 점수, 최대 콤보, 정확도, 판정별 개수, 다시 하기 버튼), EventSystem |
| `ProjectSettings/EditorBuildSettings.asset` | 수정 | `ResultScene` 추가 |
| `Assets/Scripts/Result/ResultScreen.cs` | 수정 (2026-11-15, 곡 선택) | SONG SELECT 버튼 연결 |
| `Assets/Scenes/ResultScene.unity` | 수정 (2026-11-15, 곡 선택) | SONG SELECT 버튼 추가 |

- 영향: `IsFinished` 정의 변경으로 AUTO TEST 요약 로그가 마지막 회색 롱노트가 사라진 뒤에 찍힌다(집계 값은 동일).
- 제외 범위: 결과 씬 페이드 인, 기록 저장, 결과 연출 애니메이션. (곡 선택 이동은 2026-11-15 `song_select_plan.md`에서 추가)

## 4. 상세 구현 방법 및 필요한 코드 스니펫
- `ResultTransition`은 매 프레임 `gameplay.IsFinished && gameplay.IsMusicFinished`를 확인한다. 처음 만족한 시점부터 1초 뒤 페이드를 시작하고, 0.5초 동안 `FadeOverlay` 알파를 0→1로 올린 뒤 `PlayResult.Last`에 결과를 저장하고 `ResultScene`을 불러온다.
- `ResultScreen`은 `Start`에서 `PlayResult.Last`를 읽어 텍스트에 채운다. 다시 하기 버튼은 `IngameScene`을 불러온다.

```csharp
// 정확도: 판정 가중 평균 (틱 제외)
public float Accuracy => Total == 0 ? 0f : (Perfect * 100f + Great * 70f + Good * 30f) / Total;
// 랭크: 정확도 기준
public string Rank => Total > 0 && Perfect == Total ? "SS"
    : Accuracy >= 95f ? "S" : Accuracy >= 90f ? "A" : Accuracy >= 80f ? "B" : Accuracy >= 70f ? "C" : "D";
```

## 5. 구현 순서 및 진행 상태
- [x] 결과 데이터(`PlayResult`)·최대 콤보·`IsFinished`/`IsMusicFinished` → 컴파일 확인 (에러 0건)
- [x] `ResultTransition`, `ResultScreen` → 컴파일 확인 (에러 0건)
- [x] `IngameScene` 페이드·전환 구성, `ResultScene` 생성, Build Settings 추가
  - `IngameScene`: `Canvas/FadeOverlay`(전체 화면 검정, 알파 0, 클릭 막지 않음, 맨 위), `ResultTransition` 오브젝트
  - `ResultScene`: `Main Camera`(배경 짙은 남색), `Canvas`(RESULT, RankText 200pt, ScoreText, MaxComboText, AccuracyText, JudgementText 4줄, 주황 `RetryButton`), `EventSystem`+`InputSystemUIInputModule`, `ResultScreen`
  - `EditorBuildSettings.asset`: IngameScene(0), ResultScene(1)
- [x] AUTO TEST로 검증
- 계획과 다른 점: 결과가 없을 때 표시를 "결과 없음" 대신 "NO RESULT"로 함 (내장 폰트 LegacyRuntime에 한글 글리프가 없을 수 있음)

- 완료: 계획, 구현, Claude 자동 검증
- 진행 중: 없음
- 남은 작업: 사용자 확인 (페이드 화면, RETRY 실제 클릭, 결과 화면 배치)

## 6. 테스트 및 검증 방법
| 테스트 항목 | 기대 결과 | 상태 |
|---|---|---|
| 정상 곡 종료 | 전환 조건 만족 1초 후 페이드, 결과 씬 표시 | 통과 |
| 점수 확인 | AUTO 3단계: SS / 35800 / 최대 콤보 61 / 100.0% | 통과 |
| 실패 플레이 | AUTO 1단계: D / 5000 / 최대 콤보 2 / Perfect 5, Miss 20 / 20.0% | 통과 (최대 콤보 기대값을 1→2로 정정: 8.0초 동시 롱노트 2개 머리가 같은 프레임에 Perfect) |
| 섞인 판정 | AUTO 2단계: 결과 씬 값이 요약 로그와 같음 | 통과 |
| 전환 대기 | 마지막 회색 롱노트(12.5초)가 사라지기 전에는 전환되지 않음 | 통과 |
| 새 플레이 초기화 | 다시 하기 후 새 플레이가 끝나기 전까지 `PlayResult.Last` 불변 | 통과 |
| 연속 플레이 | 이전 곡 결과 혼입 없음 | 통과 |

- [x] Unity 컴파일 오류 확인
- [x] 실제 플레이에서 기능 검증 (AUTO TEST 1~3단계, Claude가 Play 모드에서 직접 실행)
- [x] 관련 기존 기능 회귀 테스트 (AUTO 요약 로그 점수 일치, 3단계 35800 유지)

## 7. 검증 결과 및 미해결 사항
- 검증 결과 (2026-11-15, AUTO TEST로 Play 모드 진행, 프레임당 0.04초):
  - 컴파일 에러 0건, Play 모드 중 에러·경고 0건.
  - 3단계: 12.54초 `IsFinished` → 13.54초 페이드 시작 → 13.98초(알파 0.96) 직후 `ResultScene` 전환. 결과 씬 `SS / SCORE 35800 / MAX COMBO 61 / ACCURACY 100.0% / PERFECT 25 GREAT 0 GOOD 0 MISS 0`.
  - 1단계: 판정은 11.58초에 모두 끝났지만 회색 롱노트 1개가 남아 있어 대기 → 12.54초 `IsFinished` → 같은 타이밍으로 전환. 결과 씬 `D / 5000 / MAX COMBO 2 / 20.0% / PERFECT 5 MISS 20`.
  - 2단계: 결과 `D / 19180 / MAX COMBO 22 / 54.4% / 8·5·7·5`가 요약 로그(`점수 19180, 일치`)와 같음. 정확도 계산 (800+350+210)/25 = 54.4 확인.
  - RETRY(`onClick` 호출) → `SampleScene` 재시작, 점수 0·콤보 0·AUTO 0. 새 플레이 진행 중에도 `PlayResult.Last`는 이전 결과(35800) 유지.
  - 연속 플레이: 같은 Play 세션에서 3단계 → RETRY → 1단계 완료 시 결과 씬에 1단계 값만 표시.
  - Play 종료 후 `SampleScene` 변경 없음.
- 미해결: 페이드가 화면에서 자연스러운지, RETRY 실제 마우스 클릭, 결과 화면 배치 (사용자 확인 필요)
- 곡 선택 연동 (2026-11-15, `song_select_plan.md`): SONG SELECT 버튼 추가·동작 확인. `PlayResult.Last`를 Play 시작 시 비우도록 수정(Enter Play Mode Options로 static 값이 남는 문제) → ResultScene 단독 실행 시 `NO RESULT` 확인.
- 변경 이력: 초안 작성 → 2026-11-15 미확정 사항 확정 및 상세 설계 반영 → 구현 및 검증 → 2026-11-15 씬 이름 변경(SampleScene → IngameScene), 에디터 Play 시작 씬을 SongSelectScene으로 고정
