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

### 확장 요구사항: 곡 정보 표시 (2026-11-17 추가, PLAN.md "결과 화면 곡 정보 표시" 통합)
- 상태: 계획 확정 (2026-11-17 사용자 승인, 추천안) / 구현·Claude 검증 완료 (2026-11-17) / 사용자 확인 대기.
- 확정: 데이터는 `PlayResult`를 바꾸지 않고 `SongSelection`에서 읽음, 표시 위치는 오른쪽 위 2줄(4.1), 값이 없을 때 규칙은 4.1 표대로. 위 확정 요구사항과 기존 구현은 그대로 유지한다.
- 결과 화면에 플레이한 곡의 **곡명, 아티스트, 난이도**를 표시한다.
- 기존 표시(랭크, 점수, 최대 콤보, 정확도, 판정 개수, BEST/NEW BEST, RETRY, SONG SELECT, NO RESULT)와 계산·기록 규칙은 바꾸지 않는다.
- `result_ui_ux_plan.md`의 확정 디자인(랭크 패널, 정보 패널, 버튼 배치, 색·폰트)은 유지하고 재설계하지 않는다. 새 이미지·폰트·애니메이션 없음.
- 한글·영문 곡명이 모두 보여야 하고, 긴 곡명이 화면 밖으로 나가지 않아야 한다.
- 새 난이도는 추가하지 않는다.

## 2. 기존 코드 분석
- `ScoreComboState`에는 `Score`, `Combo`만 있고 최대 콤보가 없다. 콤보는 틱으로도 오른다.
- 판정 횟수는 `GameplayManager.OnJudged`로 받을 수 있다.
- `GameplayManager.IsFinished`는 판정 완료까지만 보고, 회색으로 표시 중인 Miss 롱노트(`missedLongNotes`)는 포함하지 않는다.
- 씬은 `IngameScene` 하나이고 Build Settings에도 이것만 있다.

### 곡 정보 표시 확장을 위한 분석 (2026-11-17, 로컬 소스 확인)
- **곡 데이터**: `SongData`(ScriptableObject, `Assets/Songs/*.asset`)의 `Title`, `Artist` (읽기 전용 속성). 곡마다 값:

  | 에셋 | Title | Artist | 난이도 채보 |
  |---|---|---|---|
  | Huimang | 희망 | GrandChace | Easy/Normal/Hard |
  | Kick | kick | claude_ai | Easy/Normal/Hard |
  | TestSong1~3 | Test Song 1~3 | Test | 없음 (`chart` 하나) |

- **난이도 구분**: `SongData.HasDifficulties` — 난이도 채보가 하나라도 있으면 true. false인 곡(Test Song)은 `GetChart`가 난이도와 상관없이 `chart`를 쓰고, 곡 선택 화면은 난이도 버튼을 숨기며, 기록 키는 `Single`이다(`PlayRecordStore`).
- **선택 상태**: `SongSelection.Selected`(SongData), `SongSelection.SelectedDifficulty`(Difficulty, 기본 Normal). static이며 Play 시작 시 null / Normal로 초기화된다.
  - 값을 쓰는 곳: 곡 선택 화면의 `StartSong`, `OpenSettings`(돌아왔을 때 선택 복원용), `SelectDifficulty`. 플레이·결과 씬에서는 바뀌지 않는다.
  - `GameplayManager`는 시작 시 `Selected`가 있으면 `GetChart(SelectedDifficulty)`, null이면 인스펙터 채보를 쓴다.
  - `ResultTransition.LoadResult`도 같은 `Selected`·`SelectedDifficulty`로 기록을 저장한다.
- **결과 씬 진입 경로**: `ResultScene`을 불러오는 코드는 `ResultTransition.LoadResult` 하나뿐이다(Scripts 검색). 따라서 결과 화면이 열리는 시점의 `SongSelection` 값은 방금 플레이한 곡·난이도와 같다.
- **흐름별 값**:

  | 상황 | `PlayResult.Last` | `SongSelection.Selected` |
  |---|---|---|
  | 곡 선택 → 플레이 완료 (일반·AUTO) | 이번 결과 | 플레이한 곡 |
  | RETRY → 다시 완료 | 새 결과 | 같은 곡 (RETRY는 값을 바꾸지 않음) |
  | SONG SELECT → 다른 곡 → 완료 | 새 결과 | 새로 고른 곡 (`StartSong`에서 저장) |
  | 인게임 씬 직접 실행 → 완료 | 이번 결과 | null (인스펙터 채보로 플레이) |
  | 결과 씬 직접 실행 | null (NO RESULT) | null |

- 결론: `PlayResult`를 바꾸지 않고 `ResultScreen`에서 `SongSelection`을 읽으면 된다. `ResultTransition`이 기록 저장에 같은 값을 쓰고 있어 기존 구조와도 맞다.
- 현재 `ResultScene` 위쪽 배치(`result_ui_ux_plan.md`): 왼쪽 위 `Title`("RESULT", 80, 50, 600×80)·`TitleAccent`(80, 132), 패널은 y 160부터 시작. **x 860~1840, y 30~150 영역이 비어 있다.**

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

| `Assets/Scripts/Result/ResultScreen.cs` | 수정 (2026-11-17, 곡 정보 — 미구현) | `SongSelection`에서 곡명·아티스트·난이도를 읽어 표시. 새 Text 필드 2개, 난이도 색 필드 |
| `Assets/Scenes/ResultScene.unity` | 수정 (2026-11-17, 곡 정보 — 미구현) | 오른쪽 위에 `SongTitleText`, `SongSubText` 추가, `ResultScreen`에 연결 |

- 곡 정보 확장에서 수정하지 않음: `PlayResult`, `ResultTransition`, `SongData`, `SongSelection`, `Difficulty`, 기록 저장, 다른 씬, 에셋(곡·채보·폰트·이미지)
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

### 4.1 곡 정보 표시 (2026-11-17 확장, 미구현)
#### 데이터 가져오기
- 결과가 있고(`PlayResult.Last != null`) 곡이 있을 때(`SongSelection.Selected != null`)만 표시한다. `PlayResult`·`ResultTransition`은 바꾸지 않는다(2절 분석).
- 난이도는 `song.HasDifficulties`일 때만 표시한다. Test Song처럼 난이도 구분이 없는 곡은 숨김 (곡 선택 화면이 난이도 버튼을 숨기는 것과 같은 규칙).

#### UI 배치 (1920×1080, 왼쪽 위 기준 px — `result_ui_ux_plan.md`와 같은 표기)
```
┌───────────────────────────────────────────────────────────────────────────────┐
│ RESULT                                                                  희망  │  ← SongTitleText (오른쪽 정렬)
│ ▬▬                                                      GrandChace  ·  HARD   │  ← SongSubText (아티스트 회색 · 난이도 색)
│ ┌ RankPanel ─────────┐ ┌ InfoPanel ───────────────────────────────────────┐   │  y 160
```
| 오브젝트 | 구분 | x, y, 폭×높이 | 폰트 / 크기 | 색 / 기타 |
|---|---|---|---|---|
| SongTitleText | 신규 Text | 860, 36, 980×60 | LegacyRuntime 44 Bold, 오른쪽 정렬 | 흰색. Best Fit 28~44, 가로 Wrap·세로 Truncate (긴 곡명은 줄어들다가 칸 안에서 자름) |
| SongSubText | 신규 Text | 860, 100, 980×40 | LegacyRuntime 28, 오른쪽 정렬, Rich Text | 아티스트 회색 #8A8FA1, 난이도는 난이도 색 Bold. 가로 Overflow 없음(Wrap)·세로 Truncate |

- 한글 곡명: LegacyRuntime(OS 대체 글꼴로 한글 표시, 곡 선택 화면에서 "희망" 표시 확인됨). Kanit은 한글이 없어 쓰지 않는다.
- 난이도 글자: `EASY` / `NORMAL` / `HARD` (결과 화면 라벨이 모두 대문자라 맞춤). 색은 곡 선택 화면 난이도 버튼 색과 같은 값: Easy #33A64C, Normal #3373D9, Hard #C62828.
- 별도 배지 이미지 없이 Text 하나에 Rich Text 색 태그로 표시한다 → 아티스트·난이도 중 하나가 없어도 위치 계산 없이 오른쪽 정렬이 유지된다.
- 패널(y 160~)·RESULT 제목(x 80~680)과 겹치지 않는다.

#### 값이 없을 때
| 경우 | SongTitleText | SongSubText |
|---|---|---|
| 정상 (난이도 곡) | 곡명 | `아티스트  ·  HARD` |
| 난이도 없는 곡 (Test Song) | 곡명 | `아티스트` |
| 아티스트가 비어 있음 | 곡명 | `HARD` (난이도만, 구분점 없음) / 난이도도 없으면 빈칸 |
| 곡명이 비어 있음 | 에셋 이름 (`song.name`) | 위 규칙 그대로 |
| 곡 없음 (인게임 직접 실행) | 숨김 | 숨김 |
| 결과 없음 (NO RESULT) | 숨김 | 숨김 |

#### 코드 (`ResultScreen.cs`, 표시만 추가)
```csharp
[SerializeField] private Text songTitleText; // 곡명 (오른쪽 위)
[SerializeField] private Text songSubText;   // 아티스트 · 난이도
[SerializeField] private Color[] difficultyColors = new Color[3]; // Easy/Normal/Hard 글자 색 (곡 선택 화면과 같은 값)

// Start()의 결과 표시 뒤에 호출. 결과가 없으면(NO RESULT) song 인자를 null로 넘겨 숨긴다.
private void ShowSongInfo(SongData song)
{
    bool show = song != null;
    if (songTitleText != null) songTitleText.gameObject.SetActive(show);
    if (songSubText != null) songSubText.gameObject.SetActive(show);
    if (!show) return;

    SetText(songTitleText, string.IsNullOrEmpty(song.Title) ? song.name : song.Title);

    string sub = song.Artist ?? "";
    if (song.HasDifficulties)
    {
        Difficulty difficulty = SongSelection.SelectedDifficulty;
        int i = (int)difficulty;
        string hex = ColorUtility.ToHtmlStringRGB(i < difficultyColors.Length ? difficultyColors[i] : Color.white);
        string label = "<color=#" + hex + "><b>" + difficulty.ToString().ToUpperInvariant() + "</b></color>";
        sub = sub.Length > 0 ? sub + "  ·  " + label : label;
    }
    SetText(songSubText, sub);
}
```
- `Start()`: 결과 없음 분기에서 `ShowSongInfo(null)`, 결과 있음 분기 끝에서 `ShowSongInfo(SongSelection.Selected)`.
- 아티스트·곡명에 `<`, `>`가 들어가면 Rich Text 태그로 해석될 수 있다. 현재 곡 5개에는 없다. 아티스트 부분은 그대로 두고, 문제가 생기면 그때 처리한다(현재 데이터 기준으로 불필요한 처리 추가 안 함).

#### 예상 문제 및 해결
| 문제 | 해결 |
|---|---|
| 긴 곡명이 RESULT 제목 쪽으로 넘침 | 칸을 x 860부터로 두고 Best Fit + Wrap + Truncate → 칸 밖으로 나가지 않음. 아주 긴 곡명은 잘림(말줄임표 없음) — 테스트에서 확인 |
| LegacyRuntime 한글 줄 높이로 위아래가 잘림 | 스크린샷으로 확인, 필요하면 칸 높이만 조정 |
| Hard 색(#C62828)이 어두운 배경에서 약해 보임 | 곡 선택 화면과 같은 색을 유지. 사용자 확인 항목으로 둠 |

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

### 곡 정보 표시 확장 (2026-11-17) — 별도 체크리스트
- [x] 계획 확정 (2026-11-17 사용자 승인, 추천안)
- [x] 1단계: `ResultScreen.cs`에 `ShowSongInfo` 추가 → 컴파일 확인 (에러 0건)
- [x] 2단계: `ResultScene`에 `SongTitleText`·`SongSubText` 추가, `ResultScreen` 필드·난이도 색 연결
- [x] 3단계: 6절 곡 정보 테스트 (SI-01~12)
- [x] 4단계: 7절에 결과 기록, 스크린샷 정리
- 완료: 계획 확정, 구현, Claude 검증 / 진행 중: 없음 / 남은 작업: 사용자 확인 (7절)

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

### 곡 정보 표시 테스트 (2026-11-17 확장)
C = Claude가 Play 모드에서 직접 확인, U = 사용자 확인

| # | 항목 | 방법 | 기대 결과 | 담당 | 결과 |
|---|---|---|---|---|---|
| SI-01 | 곡명 | 곡 선택 → AUTO 3 완료 (kick) | `kick` | C | 통과 (곡 선택 → kick Easy AUTO 3 실제 플레이: `kick`) |
| SI-02 | 아티스트 | 같은 실행 | `claude_ai` 회색 | C | 통과 (`claude_ai  ·  EASY`, 아티스트 회색) |
| SI-03 | Easy/Normal/Hard | kick 세 난이도 각각 완료 (또는 난이도별 `SelectedDifficulty`로 결과 씬 로드) | `EASY`·`NORMAL`·`HARD`, 난이도 색 | C (색 느낌 U) | C 통과 (EASY #33A64C / NORMAL #3373D9 / HARD #C62828, 결과 씬 로드 방식 + kick Easy 실제 플레이) / U 미검증 |
| SI-04 | 난이도 없는 곡 | Test Song 1 완료 | 곡명 + `Test`, 난이도 없음 | C | 통과 (`Test Song 1` / `Test`, 실제 플레이 포함) |
| SI-05 | 한글 | 희망 선택 후 결과 | `희망` / `GrandChace` 정상 표시 | C | 통과 (스크린샷에서 한글 정상, `GrandChace  ·  HARD`) |
| SI-06 | 긴 곡명 | 메모리에서만 만든 임시 SongData(긴 제목, 에셋 저장 안 함)를 `Selected`에 넣고 결과 씬 로드 | 칸 안에서 줄어듦·잘림, RESULT·패널과 안 겹침 | C | 통과 (영문 93자: 28pt로 줄고 첫 줄 68자까지 표시 / 한글 51자: 28pt, 42자까지 표시. 단어 단위로 잘리고 말줄임표 없음. RESULT·패널과 겹침 없음) |
| SI-07 | RETRY 후 | 결과 → RETRY → 다시 완료 | 같은 곡·난이도 | C | 통과 (Test Song 1 → RETRY → 완료: `Test Song 1`) |
| SI-08 | 다른 곡 | 결과 → SONG SELECT → 다른 곡 완료 | 새 곡 정보 | C | 통과 (SONG SELECT → Test Song 2 완료: `Test Song 2`) |
| SI-09 | AUTO TEST | AUTO로 완료 | 곡 정보 정상, 기록 저장 안 됨(기존 규칙) | C | 통과 (AUTO 결과 모두 곡 정보 표시, `Record.kick.Easy` 없음 유지, Test Song 기록 전후 같음, NEW BEST 숨김) |
| SI-10 | 데이터 없음 | 결과 씬 직접 실행(NO RESULT) / 인게임 직접 실행 후 완료 | 둘 다 곡 정보 숨김, 나머지 결과는 기존대로 | C | 통과 (NO RESULT: 곡 정보 숨김 / 인게임 직접 실행 후 완료: 숨김, 35,800 SS 표시) |
| SI-11 | 회귀 | AUTO 3 기준값 (Test Song 1 35,800/61, Test Song 2 9,800/14, kick Normal 263,200/364), BEST·NEW BEST, 버튼 | 기존과 같음 | C | 통과 (Test Song 1 35,800 / Test Song 2 9,800 / kick Easy 121,600·186 — 기준값과 같음. kick Normal 대신 kick Easy로 확인. RETRY·SONG SELECT 동작) |
| SI-12 | 겹침·잘림 | 1920×1080 스크린샷 (희망 Hard, Test Song 1, 긴 곡명, NO RESULT) | 겹침·잘림 없음 | C, 전체 느낌 U | C 통과 (4장 확인) / U 미검증 |

- 실제 키보드·마우스 입력과 화면 느낌은 U로 남긴다. 테스트로 바뀐 값(임시 SongData, 기록)은 끝나면 원래대로 되돌린다.

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
- 곡 정보 표시 확장 검증 결과 (2026-11-17, Claude가 Play 모드에서 직접 실행):
  - 컴파일 에러 0건, Play 모드 에러·경고 0건, `ResultScreen` 참조 누락 0개.
  - 씬: `SongTitleText`(860, 36, 980×60, LegacyRuntime 44 Bold, Best Fit 28~44), `SongSubText`(860, 100, 980×40, 28, 회색, Rich Text) 추가. 버튼보다 뒤에 그려짐. 기존 오브젝트 변경 없음.
  - SI-01~12 C 항목 모두 통과 (6절 표). 테스트용 임시 SongData는 메모리에서만 만들어 에셋에 저장하지 않음. 기록 값 전후 같음. 테스트 스크린샷 삭제.
  - 계획과 다른 점: SI-11 회귀의 kick은 Normal 대신 Easy(121,600/186)로 확인 (점수 계산 코드는 바뀌지 않았고 Test Song 1·2 기준값도 같음).
  - 긴 곡명은 단어 단위로 잘리고 말줄임표(…)가 붙지 않는다 (계획 4.1대로). 현재 곡 5개는 모두 한 줄에 들어감.
- 사용자 확인 필요: 곡 정보 위치·글자 크기·난이도 색(특히 HARD 빨강) 느낌, 실제 키 입력으로 플레이했을 때의 표시
- 변경 이력: 초안 작성 → 2026-11-15 미확정 사항 확정 및 상세 설계 반영 → 구현 및 검증 → 2026-11-15 씬 이름 변경(SampleScene → IngameScene), 에디터 Play 시작 씬을 SongSelectScene으로 고정 → 2026-11-17 곡 정보 표시 확장 계획 추가 (PLAN.md "결과 화면 곡 정보 표시" 통합)
