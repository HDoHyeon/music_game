# 플레이 기록 저장 구현 계획서

## 1. 작업 목적 및 요구사항
- 프로젝트: music_game (Unity 6.6, C#, URP, Mono, Input System)
- 대상: PLAN.md 2-3. 사용자 편의성 및 기록 관리 / 1. 플레이 기록 저장
- 목적: 곡을 끝까지 플레이한 결과를 곡·난이도별로 저장하고, 게임을 다시 실행해도 유지한다.
- 상태: 구현 완료(2026-11-17) / 사용자 확인 완료(2026-11-17, "잘된다")
- 작성 경위: GPT가 만든 작업 프롬프트(사용자 제공, 2026-11-17)를 로컬 소스·씬·에셋과 대조해 작성.
- 이 문서는 개발 진행에 따라 수정한다. 작업 범위 확대는 사용자 확인 후 반영한다.

### 요구사항 (사용자 프롬프트 기준)
| 항목 | 내용 |
|---|---|
| 저장 항목 (곡·난이도별) | BEST SCORE, BEST COMBO, BEST ACCURACY, BEST RANK, LAST RESULT, PLAY COUNT |
| 갱신 규칙 | 최고 점수·콤보·정확도·랭크는 각각 따로, 기존보다 높을 때만 갱신(낮아지지 않음). LAST RESULT와 PLAY COUNT(+1)는 정상 완료마다 |
| 미플레이 | 기록이 없는 곡·난이도는 미플레이로 구분, 곡 선택 화면에 `NO RECORD` |
| 구분 | kick·희망 × Easy/Normal/Hard 6개가 서로 독립. 새 곡도 같은 구조. 제목 변경·목록 순서 변경으로 기록이 섞이지 않음 |
| 저장 시점 | 곡을 끝까지 플레이해 결과가 확정될 때 1번. RETRY·SONG SELECT는 기존 기록 유지. 일시정지 RESTART·SONG SELECT(중도 이탈)는 저장 안 함. 결과 화면이 여러 번 열려도 중복 저장 없음 |
| AUTO TEST | 결과 화면에는 정상 표시, 기록·PLAY COUNT에는 넣지 않음. 플레이 중 AUTO를 켜고 끈 경우도 모호하지 않게 |
| 곡 선택 표시 | 선택 곡·난이도의 BEST SCORE / BEST RANK / BEST COMBO, 곡·난이도를 바꾸면 바로 갱신 |
| 결과 화면 | 기존 표시 유지 + 최고 점수 갱신 시 `NEW BEST`, 이번 결과와 저장된 최고 점수를 구분해 표시 |
| 저장 | 재실행 후 유지, 데이터 누락·손상 시 멈추지 않음, 불필요한 파일 접근·중복 저장 없음, 기존 설정 저장값과 충돌 없음 |

### 제외 범위
- 기록 조회·목록 화면, 온라인 랭킹·서버·클라우드, 점수 공식·랭크 기준·판정·음원·채보 변경, Test Song 삭제·변경, 기록 삭제(초기화) 기능

## 2. 기존 코드 분석 (로컬 확인 2026-11-17, 커밋 934e853)
- **결과 데이터 `PlayResult`**: `Score`, `MaxCombo`, `Perfect/Great/Good/Miss`, `Total`(틱 제외 판정 수), `Accuracy`(Perfect 100·Great 70·Good 30·Miss 0 가중 평균 %), `Rank`(전부 Perfect면 SS, 그 외 정확도 S 95 / A 90 / B 80 / C 70 / D). 객체는 생성 후 바뀌지 않음. `PlayResult.Last`(static)로 결과 씬에 넘긴다.
- **곡 완료 → 결과 전환 `ResultTransition`**: `gameplay.IsFinished`(모든 노트 판정·화면 정리 끝) && `IsMusicFinished`일 때 1초 대기 + 0.5초 페이드 후 `LoadResult()`에서 `PlayResult.Last`를 만들고 ResultScene을 불러온다. `loading` 플래그로 한 번만 실행된다. 일시정지 중에는 진행하지 않는다.
  - → **정상 완료는 `LoadResult()` 한 곳뿐**이다. 일시정지 RESTART(`PauseController.Restart` → 씬 다시 불러오기)와 일시정지 SONG SELECT는 이 지점에 도달하지 않는다.
- **결과 화면 `ResultScreen`**: `Start`에서 `PlayResult.Last`를 표시(RANK, SCORE, MAX COMBO, ACCURACY, 판정 수). RETRY = IngameScene 다시 불러오기, SONG SELECT = 곡 선택. 결과 씬이 다시 열려도 같은 `PlayResult.Last`를 읽기만 한다.
  - 배치(1920×1080 가운데 기준): Title (0, 450), RankText (0, 250) 600×240, ScoreText (0, 70), MaxComboText (0, 0), AccuracyText (0, −70), JudgementText (0, −250) 800×240, RETRY (−220, −440), SONG SELECT (220, −440). 좌우(x ±500 밖)는 비어 있다.
- **AUTO TEST `AutoPlayer`**: `Level` 0(OFF, 키보드)~3. 씬마다 인스펙터 값(IngameScene 0)으로 시작하고, **플레이 중 AutoTestPanel 버튼으로 언제든 바꿀 수 있다**(`Level` setter가 남은 노트를 다시 계획). `GameplayManager.UseAutoPlay`(private) = `autoPlayer.Level > 0`.
- **곡·난이도 `SongData` / `SongSelection`**: SongData는 title·artist·chart·music·volume·easy/normal/hardChart. 고유 ID 칸은 없다. `SongSelection.Selected`(곡), `SelectedDifficulty`(난이도, 기본 Normal). IngameScene을 곡 선택 없이 바로 실행하면 `Selected`가 null.
  - Test Song 1~3은 난이도 채보가 없어 `chart` 하나로 플레이(`HasDifficulties` false, 난이도 버튼 숨김).
- **곡 선택 화면 배치**: 오른쪽 열 x 1000 — SongTitle −260, SongArtist −350, SongNotes −460, SongLength −530, DIFFICULTY −615, 난이도 버튼 −660, START −760, CANNOT PLAY (1390, −775), 배속 줄 −900. NOTES·LENGTH 줄 오른쪽(x 1400 부근)이 비어 있다.
- **기존 PlayerPrefs 키**: `KeyBinding.Lane0~3`, `TimingOffsetMs`, `NoteSpeed`, `MasterVolume`, `MusicVolume`, `SfxVolume`. 모두 접두어가 달라 `Record.`로 시작하는 키와 겹치지 않는다.

## 3. 작업 범위 및 수정 대상 파일
### 3.1 결정 사항 (제안)
**① 곡 식별: SongData에 고유 ID 칸 추가 — [중요 — 사용자 승인 필요] → 2026-11-17 승인**
- 제목(`title`)은 바꿀 수 있고 한글이라 키로 불안정, 목록 순서는 바뀔 수 있으며, 에셋 GUID는 실행 중(빌드)에 읽을 수 없다.
- → `SongData`에 `songId`(문자열) 칸을 추가하고 kick = `kick`, 희망 = `huimang`, Test Song = `test_song_1~3`으로 정한다. 제목·목록 순서·파일 위치를 바꿔도 기록이 유지된다.
- `songId`가 비어 있는 곡(앞으로 실수로 비운 경우)은 에셋 이름을 대신 쓴다(기록이 사라지지 않게).
- 영향: SongData에 직렬화 칸 1개 추가, 곡 에셋 5개에 값 입력. 기존 칸·값은 그대로.

**② 저장 방식: PlayerPrefs + 곡·난이도마다 JSON 문자열 1개**
| 방식 | 장점 | 단점 |
|---|---|---|
| PlayerPrefs (항목마다 키) | 기존 설정과 같은 방식 | 기록 1개에 키가 10개 넘게 늘어남 |
| JSON 파일 (persistentDataPath) | 구조가 한눈에 보임, 크기 제한 없음 | 파일 읽기·쓰기·손상 처리 코드가 새로 필요, 기존 설정과 다른 경로 |
| **PlayerPrefs + JSON 문자열 (추천)** | 기존 설정과 같은 저장소, 곡·난이도마다 키 1개(`Record.<songId>.<난이도>`), `JsonUtility`로 읽기·쓰기, 항목이 늘어도 키 추가 없음 | 문자열 크기가 커지면 비효율(현재 수백 바이트라 문제 없음) |
- 읽기는 곡 선택 화면에서 곡·난이도를 바꿀 때 해당 키 1개만, 쓰기는 정상 완료 때 1번 + `PlayerPrefs.Save()` 1번.
- 읽을 수 없는 문자열(손상)·없는 키는 "기록 없음"으로 처리하고 게임은 계속된다. 일부 항목이 빠진 JSON은 그 항목을 0(없음)으로 읽는다.

**③ 난이도 없는 곡(Test Song 1~3)**: 난이도 칸 대신 `Single`로 저장한다(`Record.test_song_1.Single`). 곡 선택 화면에도 같은 방식으로 기록을 보여 준다. (3.3 결정 1)

**④ AUTO TEST 판별**: 한 플레이 동안 **AUTO가 한 번이라도 켜졌으면**(단계 1~3) 그 플레이는 기록하지 않는다. 처음부터 끝까지 OFF(0)였던 플레이만 기록. 중간에 켰다 끈 플레이도 저장하지 않아 모호함이 없다.

**⑤ 저장 시점**: `ResultTransition.LoadResult()`(정상 완료의 유일한 지점, `loading`으로 1번만)에서 저장한다. 결과 화면은 저장하지 않고 표시만 하므로 RETRY·SONG SELECT·결과 씬 재진입으로 중복 저장되지 않는다. IngameScene을 곡 선택 없이 바로 실행한 경우(`Selected` null)는 곡을 알 수 없어 저장하지 않는다.

### 3.2 수정 대상 파일
| 파일 | 구분 | 내용 |
|---|---|---|
| `Assets/Scripts/Song/PlayRecord.cs` | 신규 | 곡·난이도 1개 기록 데이터(최고값 4개 + 마지막 결과 + 플레이 횟수), 갱신 규칙 |
| `Assets/Scripts/Song/PlayRecordStore.cs` | 신규 | `Record.<songId>.<난이도>` 키로 읽기·쓰기(JsonUtility), 손상 대응, 랭크 순서 비교 |
| `Assets/Scripts/Song/SongData.cs` | 수정 | `songId` 칸, `RecordId` 속성(비면 에셋 이름) |
| `Assets/Scripts/Gameplay/PlayResult.cs` | 수정 | `AutoPlayUsed`, `IsNewBest`, `BestScore`(저장 후 최고 점수) 읽기 전용 속성 추가, 생성자 인자 추가. 점수·정확도·랭크 계산은 그대로 |
| `Assets/Scripts/Gameplay/GameplayManager.cs` | 수정 | `AutoPlayUsed`(플레이 중 AUTO가 한 번이라도 켜졌는지) 읽기 전용 속성 |
| `Assets/Scripts/Gameplay/ResultTransition.cs` | 수정 | `LoadResult()`에서 AUTO 미사용·곡 있음이면 기록 저장, NEW BEST 여부·최고 점수를 PlayResult에 담음 |
| `Assets/Scripts/Result/ResultScreen.cs` | 수정 | `bestText`(최고 점수)·`newBestText`(NEW BEST) 표시 |
| `Assets/Scripts/Song/SongSelectScreen.cs` | 수정 | `recordText` — 선택 곡·난이도의 BEST SCORE / RANK / COMBO 또는 NO RECORD, `RefreshInfo`에서 갱신 |
| `Assets/Scenes/ResultScene.unity` | 수정 | BestText, NewBestText 추가 |
| `Assets/Scenes/SongSelectScene.unity` | 수정 | RecordText 추가 |
| `Assets/Songs/*.asset` (5개) | 수정 | `songId` 값만 입력 |

- 수정하지 않음: 점수·콤보(ScoreComboState), 판정, 랭크·정확도 공식, AutoPlayer·AutoTestPanel, PauseController, 음원·채보, 설정 관련 코드.
- PLAN.md: 2-3 #1 상태 칸만 바꾼다(미작성 → 미확인). 목록 추가 없음.

### 3.3 사용자 결정 (2026-11-17, 추천안 확정)
1. **Test Song 1~3 기록**: 다른 곡과 같이 `Single` 키로 저장하고 곡 선택 화면에도 표시한다.
2. **AUTO TEST 결과 화면 안내 문구**: 넣지 않는다. AUTO를 쓴 플레이는 저장하지 않고 NEW BEST도 표시하지 않는다(BEST 줄은 저장된 최고 점수만 표시).
3. **songId 추가 승인**: SongData에 `songId` 칸 추가, kick = `kick`, 희망 = `huimang`, Test Song = `test_song_1~3`.

## 4. 상세 구현 방법 및 필요한 코드 스니펫
### 4.1 곡 ID
```csharp
// SongData.cs
[SerializeField] private string songId; // 기록 저장용 고유 ID (예: kick). 제목·목록 순서와 무관. 한 번 정하면 바꾸지 않는다
public string RecordId => string.IsNullOrEmpty(songId) ? name : songId; // 비어 있으면 에셋 이름
```

### 4.2 기록 데이터와 저장소
```csharp
// PlayRecord.cs — JsonUtility로 저장하므로 public 필드
[System.Serializable]
public class PlayRecord
{
    public int playCount;                       // 0이면 미플레이
    public int bestScore, bestCombo;
    public float bestAccuracy;
    public string bestRank = "";
    public int lastScore, lastCombo, lastPerfect, lastGreat, lastGood, lastMiss;
    public float lastAccuracy;
    public string lastRank = "";

    public bool HasRecord => playCount > 0;

    // 정상 완료 1회 반영: 최고값은 각각 높을 때만, 마지막 결과·횟수는 항상. 점수가 올랐으면 true(NEW BEST)
    public bool Apply(PlayResult r)
    {
        bool newBest = !HasRecord || r.Score > bestScore;
        if (newBest) bestScore = r.Score;
        if (r.MaxCombo > bestCombo) bestCombo = r.MaxCombo;
        if (r.Accuracy > bestAccuracy) bestAccuracy = r.Accuracy;
        if (PlayRecordStore.RankOrder(r.Rank) > PlayRecordStore.RankOrder(bestRank)) bestRank = r.Rank;
        lastScore = r.Score; /* … 마지막 결과 복사 … */
        playCount++;
        return newBest;
    }
}

// PlayRecordStore.cs
public static class PlayRecordStore
{
    private const string Prefix = "Record.";
    public const string SingleKey = "Single"; // 난이도 채보가 없는 곡

    public static string Key(SongData song, Difficulty d) =>
        Prefix + song.RecordId + "." + (song.HasDifficulties ? d.ToString() : SingleKey);

    // 없으면 빈 기록(미플레이). 손상된 문자열이면 경고 후 빈 기록 — 게임은 계속
    public static PlayRecord Load(SongData song, Difficulty d)
    {
        string json = PlayerPrefs.GetString(Key(song, d), "");
        if (string.IsNullOrEmpty(json)) return new PlayRecord();
        try { return JsonUtility.FromJson<PlayRecord>(json) ?? new PlayRecord(); }
        catch (System.Exception e) { Debug.LogWarning("기록을 읽지 못해 새로 시작합니다: " + e.Message); return new PlayRecord(); }
    }

    public static void Save(SongData song, Difficulty d, PlayRecord record)
    {
        PlayerPrefs.SetString(Key(song, d), JsonUtility.ToJson(record));
        PlayerPrefs.Save();
    }

    // 랭크 높낮이: D < C < B < A < S < SS, 빈 값(기록 없음)은 가장 낮음
    public static int RankOrder(string rank) => System.Array.IndexOf(new[] { "D", "C", "B", "A", "S", "SS" }, rank);
}
```
- 손상된 값은 덮어쓰지 않고 읽을 때만 빈 기록으로 본다. 다음 정상 완료 때 새 기록으로 저장된다.

### 4.3 AUTO 사용 여부
```csharp
// GameplayManager: 이번 플레이 중 AUTO가 한 번이라도 켜졌는지 (켰다 꺼도 true 유지). 기록 저장 여부 판단용
public bool AutoPlayUsed { get; private set; }
// Start와 Update에서: if (UseAutoPlay) AutoPlayUsed = true;
```
- RESTART는 씬을 다시 불러오므로 새 플레이에서 다시 false로 시작한다.

### 4.4 저장 시점 (ResultTransition.LoadResult)
```csharp
// 결과 확정: 곡을 알고 AUTO를 쓰지 않은 플레이만 기록. 결과 씬 이동 전에 1번만 실행된다(loading)
SongData song = SongSelection.Selected;
bool saved = song != null && !gameplay.AutoPlayUsed;
bool newBest = false; int bestScore = 0; bool hadRecord = false;
var temp = new PlayResult(score, maxCombo, p, g, gd, m);
if (song != null)
{
    PlayRecord record = PlayRecordStore.Load(song, SongSelection.SelectedDifficulty);
    hadRecord = record.HasRecord;
    if (saved) { newBest = record.Apply(temp); PlayRecordStore.Save(song, SongSelection.SelectedDifficulty, record); }
    bestScore = record.HasRecord ? record.bestScore : 0;
}
PlayResult.Last = new PlayResult(score, maxCombo, p, g, gd, m, gameplay.AutoPlayUsed, newBest, bestScore, hadRecord || saved);
```
- 첫 기록도 `NEW BEST`로 표시한다(기존 최고 점수가 없었으므로 갱신).

### 4.5 결과 화면
| 오브젝트 | 위치 | 내용 |
|---|---|---|
| NewBestText | (560, 70) | 최고 점수를 갱신했으면 `NEW BEST` (주황, 굵게), 아니면 숨김 |
| BestText | (560, 0) | 저장된 최고 점수 `BEST  263,200`. 기록이 없으면(AUTO만 했거나 곡 선택 없이 실행) `BEST  -` |
- 기존 RANK·SCORE·MAX COMBO·ACCURACY·판정 수 표시는 그대로. 이번 결과는 기존 SCORE 줄, 저장된 최고는 오른쪽 BEST 줄로 구분.

### 4.6 곡 선택 화면
| 오브젝트 | 위치 | 내용 |
|---|---|---|
| RecordText | (1400, −460) 480×110, 3줄 | `BEST SCORE  263,200` / `BEST RANK  SS` / `BEST COMBO  364`, 기록이 없으면 `NO RECORD` |
- `RefreshInfo()`(곡 선택·난이도 변경 시 호출)에서 `PlayRecordStore.Load`로 해당 키 1개만 읽어 갱신. 미리듣기·START 조건은 건드리지 않는다.

## 5. 구현 순서 및 진행 상태
- [x] 프롬프트 검토, 로컬 소스·씬·에셋 분석
- [x] 사용자 결정: Test Song도 저장, AUTO 안내 문구 없음, songId 추가 승인 (2026-11-17)
- [x] 구현 승인 (2026-11-17)
- [x] 1단계: SongData `songId`·`RecordId`, 곡 에셋 5개 값(kick, huimang, test_song_1~3), PlayRecord·PlayRecordStore → 컴파일 오류 0
- [x] 2단계: GameplayManager `AutoPlayUsed`, PlayResult 속성(기존 생성자 유지 + 기록 인자 생성자), ResultTransition 저장 → 컴파일, AUTO 회귀
- [x] 3단계: 결과 화면 NewBestText (620, 70)·BestText (620, 0), 곡 선택 화면 RecordText (1400, −460) → 스크린샷
- [x] 4단계: 테스트 (6절)
- [x] 사용자 실제 플레이 확인 (2026-11-17)
- [x] 검증 결과 기록 (PLAN.md 2-3 #1은 계획서 저장 시 미작성 → 미확인)

## 6. 테스트 및 검증 방법
- C: Claude (Play Mode 함수 직접 호출, PlayerPrefs 확인, AUTO TEST, 스크린샷) / U: 사용자 (실제 키보드 플레이로 기록 저장·표시)
- 기록은 AUTO로는 저장되지 않으므로, C 검증은 (1) AUTO 끈 상태에서 판정을 직접 넣거나 (2) `PlayRecord.Apply`·`ResultTransition.LoadResult`를 정해진 PlayResult로 직접 호출해 확인한다. 실제 키보드로 한 곡을 끝까지 치는 확인은 U.
- 검증 전 `Record.`로 시작하는 저장값을 기록해 두고(현재 없음 예상), 검증 후 지워 원래대로 되돌린다.

| ID | 테스트 | 기대 결과 | 주체 | 상태 |
|---|---|---|---|---|
| RS-01 | 첫 플레이 저장 | 플레이 횟수 1, 최고값 = 이번 결과, NEW BEST 표시 | C/U | 통과 (Test Song 2를 AUTO OFF로 끝까지(입력 없음, 전부 Miss) → playCount 1, best 0/D/0, 결과 화면 NEW BEST·BEST 0 / 함수 직접: kick Normal 100,000·S → NEW BEST) / U 통과 (2026-11-17 사용자 확인) |
| RS-02 | 더 높은 점수 | 최고 점수 갱신, NEW BEST, 횟수 +1 | C | 통과 (150,000 → 최고 점수 갱신·NEW BEST, 콤보 50·정확도 97.0·랭크 S는 유지, 횟수 2) |
| RS-03 | 더 낮은 점수 | 최고값 유지, LAST만 갱신, NEW BEST 없음, 횟수 +1 | C | 통과 (50,000·D → 최고값 유지, LAST만 50,000·D, NEW BEST 없음, 횟수 3) |
| RS-04 | 최고 콤보만 갱신 | 콤보만 바뀌고 점수·정확도·랭크 유지 | C | 통과 (콤보 80 → 콤보만 50 → 80) |
| RS-05 | 최고 정확도만 갱신 | 정확도만 바뀜 | C | 통과 (정확도 99.4 → 정확도만 97.0 → 99.4) |
| RS-06 | 최고 랭크만 갱신 | 랭크만 바뀜 (예: 점수는 낮지만 랭크는 높은 결과) | C | 통과 (랭크는 정확도로 정해져 실제 결과로는 랭크만 따로 오르지 않음. 저장값을 일부러 정확도 99·랭크 A로 맞추지 않은 상태에서 97.9%·S 결과 → 랭크만 S, 점수·콤보·정확도 유지 / 정상 흐름: 100%·SS 결과 → 정확도와 랭크가 함께 갱신) |
| RS-07 | 곡별 분리 | kick 기록이 희망에 영향 없음 | C | 통과 (kick 기록 후 희망 Normal 기록 없음) |
| RS-08 | 난이도별 분리 | kick Easy 기록이 Normal·Hard에 영향 없음 | C | 통과 (kick Easy 기록 후 kick Normal 6회·150,000 그대로, kick Hard 기록 없음) |
| RS-09 | 재실행 | Play 다시 시작 후 곡 선택 화면에 같은 기록 | C/U | 통과 (Play 종료 후 다시 시작 → 곡 선택 Test Song 2 `BEST SCORE 0 / BEST RANK D / BEST COMBO 0`) / U 통과 (2026-11-17 사용자 확인) |
| RS-10 | RESTART·중도 이탈 | 일시정지 RESTART·SONG SELECT 후 기록·횟수 변화 없음 | C | 통과 (일시정지 RESTART → 일시정지 SONG SELECT 후 Test Song 2 횟수 2 그대로) |
| RS-11 | AUTO TEST | 처음부터 AUTO, 중간에 켜기, 켰다 끄기 모두 저장 안 함·횟수 변화 없음, 결과 화면 정상 표시 | C | 통과 (AUTO 3 처음부터 SS 35,800 / AUTO 0 → 4초에 3: C 29,800 / AUTO 3 → 4초에 0: D 6,000 — 세 경우 모두 결과 화면 정상 표시, 저장 없음·BEST -, NEW BEST 없음) |
| RS-12 | RETRY 후 갱신 | RETRY로 다시 끝까지 치면 다시 저장(횟수 +1) | C/U | 통과 (RETRY 후 다시 완료 → 횟수 2, 같은 점수라 NEW BEST 없음) / U 통과 (2026-11-17 사용자 확인) |
| RS-13 | 중복 방지 | 결과 씬을 다시 열어도 횟수 그대로, LoadResult 1번만 | C | 통과 (결과 씬을 다시 불러와도 횟수 1 그대로) |
| RS-14 | 누락·손상 | 깨진 JSON·일부 항목만 있는 JSON·키 없음 → 오류 없이 NO RECORD/0, 다음 완료 때 정상 저장 | C | 통과 (깨진 JSON → 경고 1건 후 NO RECORD, 일부 항목만 있는 JSON → BEST SCORE 5,000·없는 랭크는 "-"·콤보 0, 키 없음 → NO RECORD. 게임 계속) |
| RS-15 | 곡 선택 표시 | 곡·난이도 바꾸면 기록 즉시 갱신, 미플레이는 NO RECORD, Test Song도 Single 기록 표시 | C/U | C 통과 (곡·난이도 바꿀 때 해당 키로 갱신, 미플레이 NO RECORD, Test Song 2 Single 기록 표시) / U 통과 (2026-11-17 사용자 확인) |
| RS-16 | 회귀 | AUTO 3 점수 그대로(Test Song 1 35,800/61, Test Song 2 9,800/14, kick Normal 263,200/364, 희망 Hard 1,174,000/1,510 등), 난이도·미리듣기·배속·키·오프셋·소리 설정 정상 | C | 통과 (AUTO 3: Test Song 1 35,800/61, Test Song 2 9,800/14, kick Normal 263,200/364, 결과 화면 기존 표시 그대로. AUTO 실행 후 새 기록 키 생기지 않음) |
| RS-17 | 화면 배치 | 결과 화면·곡 선택 화면 겹침·잘림 없음 (스크린샷) | C/U | C 통과 (결과 화면·곡 선택 화면 스크린샷 겹침·잘림 없음) / U 통과 (2026-11-17 사용자 확인) |

## 7. 검증 결과 및 미해결 사항
- 구현: 계획 4절 그대로. 계획과 다른 점 — 기록에 랭크가 빠져 있으면(손상·일부 누락) 곡 선택 화면에 빈칸 대신 "-"로 표시하도록 1줄 추가(RS-14에서 발견).
- 검증 방식: (1) Test Song 2를 AUTO OFF로 끝까지 진행해 실제 저장 흐름(ResultTransition → 결과 화면 → RETRY/SONG SELECT) 확인, (2) PlayRecord.Apply·PlayRecordStore를 정해진 결과로 직접 호출해 갱신 규칙·분리 확인, (3) AUTO 3 회귀, (4) 스크린샷.
- 저장값 정리: 검증 전 `Record.` 키 없음 → 검증 후 생긴 키를 모두 지워 원래대로 되돌림.
- 결과: 6절 표대로 C 항목 모두 통과. 실제 키보드로 곡을 끝까지 쳐서 기록이 쌓이는지, 게임을 완전히 껐다 켰을 때 유지되는지는 사용자 확인 필요.
- 미해결: 없음 (3.3 결정 완료).

### 변경 이력
| 날짜 | 내용 |
|---|---|
| 2026-11-17 | GPT 작업 프롬프트(사용자 제공)를 바탕으로 로컬 소스·씬·에셋 분석 후 최초 작성 |
| 2026-11-17 | 사용자 결정 반영(추천안 확정) |
| 2026-11-17 | 구현 및 검증 (6·7절) |
| 2026-11-17 | 사용자 실제 플레이 확인 완료 — U 항목 통과 처리 |
