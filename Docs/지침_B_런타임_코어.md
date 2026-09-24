# 지침 B · 런타임 코어

**먼저 읽을 것:** `Docs/작업_계약.md` → `Docs/Unity_구조.md` §3 §4 §5 §6 → `Docs/좋은_피자_위대한_리듬_기획서.md` §6

한 줄 요약: **채보를 곡 박자로 돌린다.** 클락, 페이즈 진행, 노트 스폰, 각도 계산, 판정, 정확도, 자동 재생까지. 화면에 그리는 일은 하지 않는다.

---

## 1. 내가 만드는 파일

```
Assets/GPGR/Runtime/
    GPGR.Runtime.asmdef          ← 이미 있다. GPGR.Charting 과 Unity.InputSystem 을 참조한다
    ChartClock.cs
    TimelineDirector.cs
    NoteWheel.cs
    NoteInstance.cs
    INotePattern.cs
    NotePattern.cs
    SingleNotePattern.cs
    LongNotePattern.cs
    MotionPath.cs
    AccuracyGauge.cs
    HitSource.cs
    PlayerHitSource.cs
    AutoPlayHitSource.cs
Assets/GPGR/Runtime/Tests/
    GPGR.Runtime.Tests.asmdef        (Editor 전용, NUnit)
    MotionPathTests.cs
    HitRoutingTests.cs
    AutoPlayTests.cs
```

`.unity`와 `.prefab`은 만들지 않는다. 컴포넌트를 씬에 올리는 일은 C가 한다.

---

## 2. 공개 면은 고정이다

`Docs/작업_계약.md` §5의 이름과 시그니처를 그대로 쓴다. C가 그것만 보고 뷰를 짜고 있다. 바꿔야 할 것 같으면 코드를 고치지 말고 사람에게 묻는다.

내부 구현은 자유다.

`Assets/GPGR/Charting/`은 이미 들어와 있다. 데이터 타입과 `ChartLayout`, `NoteRowMath`, `MotionPathMath`를 참조해서 쓴다. **내가 고치지 않는다.** 버그를 찾으면 A에게 넘긴다.

---

## 3. `ChartClock`

- `SongSeconds = 오디오 시각 + chart.offsetSeconds`
- 클립이 있으면 `AudioSource`의 재생 위치를 쓴다. `AudioSettings.dspTime` 기반으로 잡으면 프레임 흔들림이 줄어서 더 좋다. `AudioSource.time`은 프레임 단위로 튄다.
- 클립이 **비어 있어도 돌아야 한다.** 그때는 `Time.timeAsDouble` 기반으로 센다. 곡이 오기 전에 채보를 확인해야 하기 때문이다.
- `Beat = SongSeconds / chart.SecondsPerBeat`
- `Update`가 아니라 프레임마다 한 번 계산해 캐시하고, 같은 프레임 안에서 누가 여러 번 읽어도 같은 값이 나오게 한다.

---

## 4. `MotionPath`

각도 보간은 이미 `GPGR.Charting.MotionPathMath.AngleAt`에 있다. A의 에디터 검증이 같은 계산을 써야 해서 거기에 뒀다. `MotionPath.AngleAt`은 그걸 **그대로 부르기만 한다.** 계산을 다시 쓰면 두 구현이 갈라진다.

내가 새로 쓰는 건 `Position(angleDeg, center, radius)` 하나다. `center + radius * (cos, sin)`이고, 각도는 도(degree)로 받아 안에서 라디안으로 바꾼다.

규칙을 다시 적어 두면:

```
localBeat <= keys[0].beat        → keys[0].angleDeg
localBeat >= keys[last].beat     → keys[last].angleDeg
사이                              → 두 키 사이 선형 보간
```

**짧은 쪽으로 꺾지 않는다.** `Mathf.LerpAngle`이나 `Mathf.DeltaAngle`을 쓰면 안 된다. 270° → 210°는 −60°로 가야 하고, 이게 치즈 튕김이다.

각도를 매 프레임 직전 값에 속도를 더해 누적하지 않는다. 항상 현재 박에서 키를 다시 읽는다. 그래야 되감기와 에디터 확인이 맞는다.

테스트는 `MotionPathMath`를 직접 겨냥해 쓴다. 들어와 있는 구현이라 검증이 더 필요하다.

### 테스트

- 키 `(0,180) (2,270)`에서 `AngleAt(-1) == 180`, `AngleAt(0) == 180`, `AngleAt(1) == 225`, `AngleAt(2) == 270`, `AngleAt(5) == 270`
- 키 `(0,180) (2,270) (3,210) (4,270)`에서 `AngleAt(2.5) == 240`, `AngleAt(3.5) == 240`
  (같은 각도가 두 번 나온다. 되돌아간다는 뜻이고 정상이다)
- `Position(270°, (0,0), 3)` ≈ `(0, -3)`

---

## 5. 패턴과 노트

핵심 규칙 하나: **패턴 객체는 노트를 저장하지 않는다.** 맞은 횟수, 마지막 판정, 현재 각도는 `NoteInstance`에 있다. 그래서 `SingleNotePattern` 하나를 여러 노트가 같이 쓸 수 있다. 상태를 패턴 필드에 두면 노트 두 개가 서로를 망친다.

모든 메서드가 `NoteInstance note`를 첫 인자로 받는 이유가 그것이다.

### `SingleNotePattern`

- `hitBeats`를 순서대로 소비한다. `HitCount`번째 히트가 현재 목표다.
- `CanAcceptInput`: 목표 히트 박과의 오차가 Bad 창 안일 때 true.
- `Press`: 오차를 `JudgementWindows.Classify`에 넣어 판정을 낸다. `HitCount`를 올린다.
- 목표 히트 박 + Bad 창을 지났는데 안 맞았으면 **Miss이고 노트는 바로 사라진다.** 남은 히트와 그 뒤 경로는 가지 않는다. 치즈가 첫 타를 놓치면 튕기지 않는다는 규칙이 이것이다.
- `ClearBeat`: 남은 히트가 있으면 마지막 히트 + Bad 창, 이미 Miss로 죽었으면 죽은 박.

### `LongNotePattern`

- 히트는 머리 하나뿐이다. `Release`는 받되 **판정을 바꾸지 않는다.** 손을 떼는 시각은 보지 않는다.
- 머리가 판정된 뒤 `tailBeats`만큼 더 산다. `ClearBeat = 머리 히트 박 + tailBeats`.
- 머리를 놓쳤을 때는 `Single`과 같이 Miss로 바로 사라진다. 자동 재생에서는 안 나온다.
- 꼬리를 그리는 일은 C가 한다. B는 노트를 살려 두기만 한다.

---

## 6. `NoteWheel`

- `Active`에 살아 있는 노트를 담는다. 매 프레임 `Advance`를 불러 `AngleDeg`를 갱신하고, `ClearBeat`를 지난 노트를 빼면서 `Cleared` 이벤트를 쏜다.
- `Press(songBeat)` 라우팅 규칙: `CanAcceptInput`이 true인 노트 중 **오차가 가장 작은 하나에만** 넘긴다. 오차가 같으면 더 이른 히트 박을 가진 쪽이다. 나머지 노트에는 아무 일도 없다.
- **빈 입력은 감점하지 않는다.** 받을 노트가 없으면 조용히 무시한다. 판정 팝업도 띄우지 않는다.
- 판정이 났으면 `Judged(note, judgement, signedErrorMs)`를 쏜다. 오차는 부호를 살린다(빠르면 음수). C가 팝업에 쓰고, 나중에 빠름/느림 표시를 붙일 수도 있다.
- 충돌 판정을 쓰지 않는다. `Collider`도 `Physics2D`도 필요 없다. 맞았는지는 곡 박자로만 정한다.

### 테스트 (`HitRoutingTests`)

- 노트 두 개가 다 받을 수 있을 때, 오차가 작은 쪽만 `HitCount`가 오른다
- 오차가 같으면 히트 박이 이른 쪽이 받는다
- 받을 노트가 없을 때 `Press`가 정확도를 건드리지 않는다
- 창을 지난 `Single`이 자동으로 Miss 처리되고 `Active`에서 빠진다

---

## 7. `TimelineDirector`

- `Begin(chart)`에서 `ChartLayout.Build(chart)`를 한 번 호출해 `PhaseWindow` 목록을 받는다. **직접 다시 계산하지 않는다.** 그 공식은 A의 것이고 테스트가 붙어 있다.
- 페이즈에 들어가는 시점에 `PhaseEntered(phase, index)`를 쏜다. C가 배경·피자·틴트를 적용하고 HUD 이름을 바꾼다.
- **페이즈 이름을 보고 분기하지 않는다.** "오븐"인지 "완성"인지 모른 채로 필드만 넘긴다.
- 노트는 `OriginBeat + SpawnBeat(row)`에 스폰한다. 미리 다 만들어 두지 말고 그 박에 만든다. C가 `Spawned`로 뷰를 붙인다.
- 마지막 페이즈의 `ClearedBeat`를 지나면 `ChartFinished`를 쏜다.
- 페이즈 경계는 `PhaseWindow`가 정한다. 실제 노트가 일찍 죽어도 경계를 앞당기지 않는다. 오디오와 어긋나면 안 된다.

---

## 8. `AccuracyGauge`

100에서 시작. `Apply(j, w)`에서 `w.PenaltyOf(j)`만큼 뺀다. 0~100 클램프. 값이 바뀌면 `Changed`를 쏜다.

`MonoBehaviour`가 아니다. 순수 클래스로 만들고 `TimelineDirector`가 들고 있는다. 테스트하기 쉬워진다.

---

## 9. 입력과 자동 재생

`HitSource`는 `Pressed(곡 박)` / `Released(곡 박)` 두 이벤트만 가진다. 누가 보냈는지는 원판이 모른다.

### `PlayerHitSource`

Input System으로 Space, Enter, 마우스 좌클릭을 받는다. `InputSystem_Actions.inputactions`를 고치지 말고 코드에서 직접 잡는다 (`Keyboard.current`, `Mouse.current`).

터치는 **자리만** 연다. 같은 `RaisePressed`를 부르는 경로를 주석으로 남겨 두고 지금은 켜지 않는다.

### `AutoPlayHitSource`

이게 트레일러의 실제 재생 방식이다.

1. `ChartLayout.Build` + 각 행의 `hitBeats`로 **절대 히트 박 목록**을 만든다. `(absBeat, rowKind)` 꼴.
2. `new System.Random(chart.autoSeed)`로 각 히트의 판정을 고른다. `autoGoodRatio` 확률로 Good, 나머지는 Perfect. **시드가 같으면 같은 테이크가 나온다.** 재촬영 때 이게 중요하다.
3. 오차를 정한다. Perfect는 0ms. Good은 **50ms 초과 100ms 이하**에서 고르고 부호도 무작위로 준다. 50이나 100 경계에 정확히 걸면 Perfect로 넘어가거나 Bad가 될 수 있으니 여유를 둔다 (예: 60~95ms).
4. 발화할 박 = `absBeat + chart.BeatsFromMs(signedErrorMs)`. 이 값으로 **정렬한** 뒤, 클락이 그 박을 지날 때 `Pressed`를 쏜다.
5. Long은 `Pressed` 뒤에 `tailBeats`가 끝나는 박에 `Released`를 보낸다. 판정에는 영향이 없지만 손이 붙어 있는 것처럼 보이게 하고, 나중에 홀드 연출을 붙일 자리가 된다.
6. **Bad와 Miss는 절대 내지 않는다.** 오차 계산이 어긋나서 Bad가 한 번이라도 나오면 버그다.

### 테스트 (`AutoPlayTests`)

- 데모 채보를 자동 재생으로 끝까지 돌렸을 때 나온 판정이 Perfect 또는 Good뿐이다
- 같은 시드로 두 번 돌리면 판정 목록이 같다
- 히트 개수와 판정 개수가 같다 (빠뜨린 히트가 없다)
- 정확도가 `100 − 2 × Good 개수`와 같다 (0 아래로는 클램프)

---

## 10. CLI로 확인하는 법

`Docs/작업_계약.md` §3가 전체 규칙이다. 내가 자주 쓸 것만 적는다.

```bash
unity command set_autotick --enable true                     # 처음 한 번
unity command recompile ; unity command recompile_status     # 컴파일 확인

unity command run_tests --mode editor --filter MotionPathTests
unity command run_tests --mode editor --filter HitRoutingTests
unity command run_tests --mode editor --filter AutoPlayTests
```

**핵심은 EditMode 테스트로 최대한 밀어내는 것이다.** 내가 만드는 건 거의 다 순수 로직이라 씬 없이 검증할 수 있다. `ChartClock`만 시간이 걸리는데, 오디오 시각을 주입할 수 있게 인터페이스를 하나 빼 두면 그것도 EditMode에서 돈다.

자동 재생 전체를 한 번 돌려 보는 것도 씬 없이 된다. `NoteWheel`과 `TimelineDirector`를 코드로 만든 `GameObject`에 붙이고, 박을 손으로 밀면서 판정 목록을 모으면 된다. 이게 `AutoPlayTests`의 몸통이다.

`run_tests --mode playmode`는 플레이 모드를 잡아서 C와 겹친다. 꼭 필요할 때만 쓰고, 쓸 거면 사람에게 알린다. `editor_play`는 직접 부르지 않는다.

씬에 컴포넌트를 올려 눈으로 보고 싶으면 C에게 요청한다. 내가 씬을 만들지 않는다.

콘솔 로그는 `unity command console`로 읽을 수 있다. 페이즈 진입 순서를 로그로 찍어 두면 이걸로 확인된다.

---

## 11. 완료 기준

- [ ] `MotionPath` 테스트 통과. 특히 되돌아가는 키
- [ ] `HitRoutingTests` 통과
- [ ] `AutoPlayTests` 통과. Bad·Miss가 한 번도 안 난다
- [ ] 클립 없이 BPM만으로도 채보가 끝까지 돈다
- [ ] 콘솔 로그만으로 `반죽 → 소스 → 치즈 → 페퍼로니 → 오븐 → 완성` 순서와 각 페이즈 진입 박을 확인할 수 있다
- [ ] 페이즈끼리 노트가 겹치지 않는다 (한 프레임에 두 페이즈의 노트가 `Active`에 같이 있지 않다)
- [ ] 코드 어디에도 재료 이름이나 페이즈 이름으로 분기하는 곳이 없다

---

## 12. 하지 않는 것

- 스프라이트, 라인, 메시, 머티리얼, 트레일, 꼬리 렌더 → C
- 배경 전환, 피자 오븐 인·아웃, HUD → C
- 씬 조립, 프리팹 → C
- `SongChart` 필드 추가·삭제, `ChartLayout` 수정 → A
- `Docs/*.md` 수정. 모순을 찾으면 사람에게 보고한다
