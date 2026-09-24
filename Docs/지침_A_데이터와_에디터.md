# 지침 A · 데이터와 에디터

**먼저 읽을 것:** `Docs/작업_계약.md` → `Docs/Unity_구조.md` §2 §7 → `Docs/좋은_피자_위대한_리듬_기획서.md` §8

한 줄 요약: **에셋과 에디터를 만든다.** 채보가 어떤 모양인지 정하고, 사람이 그걸 고칠 창을 만들고, 데모 채보 하나를 실제 에셋으로 뽑는다.

---

## 1. 내가 만드는 파일

```
Assets/GPGR/Charting/            ← 이미 들어와 있다. 내 소유지만 새로 만들 필요 없다
    GPGR.Charting.asmdef
    Judgement.cs
    NoteRow.cs
    PhaseDefinition.cs
    SongChart.cs
    MotionPathMath.cs
    NoteRowMath.cs
    ChartLayout.cs
Assets/GPGR/Charting/Tests/
    GPGR.Charting.Tests.asmdef       (Editor 전용, NUnit)
    ChartLayoutTests.cs
    JudgementWindowsTests.cs
Assets/GPGR/EditorTools/
    GPGR.EditorTools.asmdef          (Editor 전용)
    ChartEditorWindow.cs
    NotePresetLibrary.cs
    DemoChartFactory.cs
Assets/GPGR/Charts/
    DemoChart.asset
```

이 밖의 파일은 만들지 않는다. 특히 `.unity` 씬과 `.prefab`은 C만 만진다.

---

## 2. 먼저 할 일 — 들어와 있는 코드 읽기

`Assets/GPGR/Charting/`은 이미 만들어져 있다. 데이터 타입, `MotionPathMath`, `NoteRowMath`, `ChartLayout.Build`, `ChartLayout.Validate`가 다 들어 있다. **새로 만들지 말고 읽고 시작한다.**

그다음 할 일은 두 개다.

1. `ChartLayout`에 테스트를 붙여 실제로 맞는지 못박는다 (3절)
2. `GPGR/Chart Editor` 창과 데모 생성 메뉴를 만든다 (5~7절)

들어와 있는 코드에서 버그를 찾으면 고쳐도 된다. **단, 공개 시그니처는 바꾸지 않는다.** B와 C가 그걸 보고 코드를 쓰고 있다. 시그니처를 바꿔야 할 것 같으면 사람에게 묻는다.

---

## 3. `ChartLayout` — 이 프로젝트에서 가장 조심할 코드

`Docs/Unity_구조.md` §2의 규칙이 전부다. 요약하면:

1. 스폰 박은 행의 **첫 경로 키 시각**이다. 접근 박을 행에 저장하지 않는다.
2. 페이즈의 가장 이른 스폰 박은 `min(행마다 첫 경로 키 시각)`이다. **히트가 가장 이른 행이 아니다.** 페퍼로니처럼 접근 박이 짧은 행이 섞이면 둘이 달라진다.
3. 페이즈가 비워지는 박은 `max(행마다 지워지는 박)`과 `holdBeats` 중 큰 쪽이다.
4. 다음 페이즈 원점 = 이전 페이즈가 비워지는 박 − 다음 페이즈의 가장 이른 스폰 박.
5. 이 값들은 **판정 결과와 무관한 순수 함수**다. 미스로 노트가 일찍 사라져도 다시 계산하지 않는다. 그래야 곡과 안 어긋난다.

### 테스트로 못박을 것

`Assets/GPGR/Charting/Tests/ChartLayoutTests.cs`에 최소 이만큼 쓴다.

- 페이즈가 하나면 `EarliestSpawnBeat == 0`이다.
- 페이즈 두 개를 이으면 `2번 EarliestSpawnBeat == 1번 ClearedBeat`다.
- **접근 박이 섞인 경우:** 한 페이즈에 접근 2박 행과 접근 1박 행이 있고 후자의 히트가 더 늦을 때, 앞 페이즈 노트와 겹치지 않는다. 즉 그 페이즈의 모든 행에 대해 `origin + SpawnBeat(row) >= 이전 ClearedBeat`.
- 노트가 없고 `holdBeats = 8`인 페이즈의 길이가 8박이다.
- `Single`의 지워지는 박이 `마지막 히트 + BeatsFromMs(badMs)`다.
- `Long`의 지워지는 박이 `머리 히트 + tailBeats`다.

`JudgementWindowsTests.cs`:

- 오차 0 → Perfect, 30 → Perfect, 50 → Perfect, 51 → Good, 100 → Good, 101 → Bad, 160 → Bad, 161 → Miss.
  (윈도우가 겹치므로 위에서부터 먼저 맞는 줄을 쓴다는 규칙 확인)

---

## 4. `Validate` — 에디터가 보여줄 문제 목록

`ChartLayout.Validate(chart)`는 사람이 읽을 문자열 목록을 돌려준다. 최소 이만큼 잡는다.

- `phases`가 비어 있다.
- 행의 `path`가 2개보다 적다.
- `path`의 `beat`가 오름차순이 아니다.
- `hitBeats`가 비어 있거나 오름차순이 아니다.
- `kind == Long`인데 `hitBeats.Count != 1`이다.
- `kind == Long`인데 `tailBeats <= 0`이다.
- 어떤 `hitBeats` 값이 `path`의 첫 키와 마지막 키 사이에 없다.
- 히트 박에서 `MotionPathMath.AngleAt`으로 구한 각도가 `defaultHitAngleDeg`와 많이 다르다 → **경고**. 화면에서 하단에 안 맞물린다는 뜻이다.
- 노트가 없는 페이즈인데 `holdBeats <= 0`이다.

경고와 오류를 구분해서 표시한다. 들어와 있는 구현이 이미 이만큼 잡는다. 빠진 게 있으면 보탠다.

---

## 5. `GPGR/Chart Editor` 창

`EditorWindow`. UI Toolkit이든 IMGUI든 자유. 있어야 하는 것:

- 편집 대상 `SongChart`를 고르는 필드
- BPM·오프셋·기본 접근 박·기본 스폰/히트 각·오디오 클립·판정 창·재생 방식·자동 Good 비율·시드
- 페이즈 목록. 추가·삭제·순서 바꾸기
- 페이즈마다: 이름, 배경, 피자, 원판 틴트(체크박스 + 색), `holdBeats`
- 페이즈 안의 노트 행 목록. 추가·삭제, 그리고 **프리셋으로 채우기**
- 행마다: 종류, 히트 박 목록, 경로 키 목록(시각 + 각도), 꼬리 박, 아이콘, 색
- `ChartLayout.Build` 결과를 읽기 전용으로 같이 보여준다. 페이즈마다 **절대 원점 / 첫 스폰 / 비워지는 박**. 사람이 페이즈 기준 박만 입력하는데 곡 전체에서 언제인지 알 수 없으면 채보를 짤 수 없다.
- `Validate` 결과를 창 아래에 항상 띄운다.
- 변경은 `Undo.RecordObject` + `EditorUtility.SetDirty`로 되돌릴 수 있게 한다.

미리보기 재생은 **만들지 않는다.** 원판은 한 벌이고 그건 B와 C가 만든다. 미리보기가 필요하면 C의 트레일러 씬을 플레이한다.

---

## 6. 프리셋

`NotePresetLibrary`는 노트 행 템플릿 목록이다. 에디터 전용이고, 런타임 데이터로 저장하지 않는다.

프리셋 하나는 `(이름, 만드는 함수)` 꼴이다. 함수는 `(SongChart chart, double hitBeat)`를 받아 `NoteRow` 하나를 돌려준다. 접근 박은 `chart.defaultApproachBeats`에서 가져오되, 페퍼로니처럼 다른 값을 쓰는 프리셋은 자기 값을 쓴다.

| 프리셋 | 만드는 행 |
| --- | --- |
| 반죽 | Long, 히트 1개, 기본 호, 꼬리 2박 |
| 토마토 소스 | Single, 히트 1개, 기본 호 |
| 치즈 뿌리기 | Single, 히트 2개, 기본 호 + 튕김 키 2개 |
| 페퍼로니 | Single, 히트 1개, 접근 1박 |
| 오븐 | Long, 히트 1개, 기본 호, 꼬리 4박 |

기본 호는 키 두 개다: `(히트 박 − 접근 박, 스폰 각)`, `(히트 박, 히트 각)`.

치즈는 거기에 두 개를 더한다: `(히트 박 + 1, 210°)`, `(히트 박 + 2, 270°)`. 두 번째 히트 박은 복귀 키의 시각, 즉 `히트 박 + 2`다.

**재료를 추가하는 일은 프리셋 하나를 등록하는 일이어야 한다.** 프리셋을 추가하려고 `NoteRow`에 필드를 늘리거나 `switch`를 고쳐야 하면 설계가 잘못된 것이다.

---

## 7. `GPGR/Create Demo Chart Asset`

`MenuItem("GPGR/Create Demo Chart Asset")`. 등록된 프리셋 순서로 페이즈 6개를 만들어 `Assets/GPGR/Charts/DemoChart.asset`으로 저장한다.

숫자는 `Docs/작업_계약.md` §7의 데모 채보 표를 그대로 쓴다. 이미 파일이 있으면 덮어쓸지 묻는다.

만든 뒤 `Validate`를 돌려 결과를 콘솔에 찍는다. 오류가 있으면 에셋을 저장하지 않는다.

아이콘은 비워 두고 색만 넣는다. C가 플레이스홀더를 만들고 나면 아이콘을 채운다.

---

## 8. CLI로 확인하는 법

`Docs/작업_계약.md` §3가 전체 규칙이다. 내가 자주 쓸 것만 적는다.

```bash
unity command set_autotick --enable true                     # 처음 한 번
unity command recompile ; unity command recompile_status     # 컴파일 확인

unity command list_tests --mode editor
unity command run_tests --mode editor --filter ChartLayoutTests
unity command run_tests --mode editor --filter ChartLayoutTests.접근_박이_섞여도_겹치지_않는다
```

에디터 창을 실제로 열어 확인할 수 있다.

```bash
unity command menu --path "GPGR/Chart Editor"
unity command capture_editor_element --window "GPGR Chart Editor" --selector ".unity-scroll-view" --output Temp/editor.png
unity command menu --path "GPGR/Create Demo Chart Asset"
unity command find_assets --filter "t:SongChart"
```

데모 에셋이 제대로 만들어졌는지는 `read_text_file`로 `.asset`(YAML)을 직접 읽어 확인하거나, `AgentScripts/A_DumpChart.cs`에 덤프 스크립트를 써서 `run_script`로 돌린다. 레이아웃 결과(절대 박)를 표로 찍어 보면 채보가 의도대로 펼쳐졌는지 바로 보인다.

테스트가 실패하면 결과가 뭉개져 나올 수 있으니 `--filter`를 한 테스트로 좁혀 다시 돌린다.

`editor_play`와 씬·프리팹 명령은 쓰지 않는다. C 것이다.

---

## 9. 완료 기준

- [ ] `ChartLayout` 테스트가 전부 통과한다. 특히 접근 박이 섞인 경우
- [ ] `GPGR/Chart Editor`로 페이즈와 행을 추가·삭제·편집할 수 있고, 절대 박이 같이 보인다
- [ ] `Validate`가 잘못된 채보를 잡는다 (일부러 키 순서를 뒤집어 확인)
- [ ] `GPGR/Create Demo Chart Asset`이 계약 §7 표와 같은 에셋을 만든다
- [ ] 코드 어디에도 재료 이름으로 분기하는 곳이 없다 (프리셋 등록 표는 예외)

---

## 10. 하지 않는 것

- 씬, 프리팹, 머티리얼, 스프라이트
- 런타임 컴포넌트 (`MonoBehaviour`). `GPGR.Charting`은 `ScriptableObject`와 순수 클래스만이다
- 판정 실행, 입력, 자동 재생 → B
- 미리보기 재생, HUD → C
- `Docs/*.md` 수정. 모순을 찾으면 사람에게 보고한다
