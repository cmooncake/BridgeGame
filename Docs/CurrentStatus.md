# MoonBridgeClient 现状说明

本文记录截至当前工程里**已经落地**的内容：目录、职责、数据流，以及哪些只搭了壳、还没接上。  
工程路径：`C:\git_proj\MoonBridgeClient\MoonBridgeClient`  
Unity 版本：`2022.3.62f3c1`

---

## 1. 目标与分层约定

客户端按学习路线推进：美术 → 场景显示 → 离线玩法 → 接服务器。  
当前做到：**离线发 52 张、四家亮/盖牌、南家可以点一张牌出手**。

禁止 UI 自己改手牌。约定流向：

```text
UI 收集意图（点了哪张牌）
  → Game.Authoritative 立刻改权威状态，产出 GameEvent
  → Presentation.Actions 排队 / 对账（壳已写，尚未驱动画面）
  → Presentation.Animation 播可取消动画（壳已写，无人调用）
  → UI 按结果画画
```

当前实际跑通的是捷径：

```text
点牌 → OfflineTable.Play → TableView.ApplySnapshot → 整桌重画手牌
```

`PresentationDirector` 会收到事件并入队，但没有人 `TryStartNextAction()`，也没有动画，所以表现层等于没转。

---

## 2. 工程与第三方包

| 项 | 现状 |
|---|---|
| Unity | 已从 `2019.4.40f1c1` 升到 `2022.3.62f3c1` |
| UniTask 2.5.10 | UPM git：`com.cysharp.unitask` |
| YooAsset 2.3.19 | UPM git：`com.tuyoogame.yooasset` |
| Spine 4.3 | UPM git：`spine-csharp` + `spine-unity` |
| Animancer 8.3.0 | 嵌入包 `Packages/com.kybernetik.animancer`（从本机斗地主工程拷贝） |
| DOTween（免费版）+ DemiLib | `Assets/Plugins/Demigiant/`（未拷 Pro） |

脚本宏（由 `Assets/Editor/ThirdPartyDefineSymbols.cs` 维护）：

- 必须保留：`DOTWEEN`、`UNITASK_DOTWEEN_SUPPORT`
- 必须去掉：`DOTWEEN_EPO` / `EPO_DOTWEEN`（工程没有 Easy Performant Outline，开了会编不过 `DOTweenModuleEPOOutline.cs`）
- 菜单：`Tools > MoonBridge > Fix DOTween Defines`

DOTween 首次打开后需要：`Tools > Demigiant > DOTween Utility Panel` → Setup，打开 **uGUI**，不要勾 EPO Outline。

这些包目前都还没有接到出牌流程上。

---

## 3. 目录总览

```text
MoonBridgeClient/Assets/
  Art/Cards/
    Base/          卡底、卡背
    Suits/         梅花/方块/红桃/黑桃
    Ranks/Black    黑色点数 A–K、SJ、BJ
    Ranks/Red      红色点数
    Faces/         J/Q/K、小王、大王占位图
  Data/
    CardSpriteLibrary.asset
  Prefabs/
    CardView.prefab
  Scenes/
    SampleScene.unity
  Scripts/
    Domain/                    牌、座位、比较器
    Game/
      OfflineDealService.cs    洗牌发牌
      Authoritative/           权威牌桌
    Presentation/
      Actions/                 表现行动队列
      Animation/               可取消播放
    UI/
      TableView.cs             接线（意图 → 权威 → 快照）
      Cards/                   卡牌显示与对象池
    Testers/
      HandCardViewTester.cs    旧测试脚本，场景已不再使用
  Editor/
    CardViewPrefabBuilder.cs
    ThirdPartyDefineSymbols.cs
  Plugins/Demigiant/           DOTween
```

规则文件：`.cursor/rules/match-state-layers.mdc`

---

## 4. Domain

纯数据，不引用 Unity UI。

| 类型 | 文件 | 说明 |
|---|---|---|
| `CardSuit` | `Domain/CardSuit.cs` | Clubs / Diamonds / Hearts / Spades，另有 SmallJoker / BigJoker |
| `CardRank` | `Domain/CardRank.cs` | Ace=1 … King=13，Joker=14/15 |
| `Card` | `Domain/Card.cs` | `struct`，属性 `Rank`/`Suit`，`IsRed`/`IsJoker` |
| `Seat` | `Domain/Seat.cs` | North / East / South / West |
| `CardComparer` | `Domain/CardComparer.cs` | 显示序：黑桃→红桃→方块→梅花；同花色 A 最大（A 当 14） |

标准桥牌发牌只用 52 张，不含大小王。Joker 素材和枚举保留，发牌器不会生成它们。

---

## 5. Game

### 5.1 `OfflineDealService`

- 命名空间：`MoonBridge.Game`
- 52 张，Fisher–Yates，`seed` 固定则牌面固定
- 按 N→E→S→W 轮发，每家 13 张
- 发完用 `CardComparer.Default` 排序
- 普通 C# 类，不是 `MonoBehaviour`

### 5.2 权威层 `MoonBridge.Game.Authoritative`

| 类型 | 作用 |
|---|---|
| `TableState` | 只读快照：`Sequence`、`Turn`、四家 `Hands`、四家 `Trick` |
| `OptionalCard` | 墩上可能没牌 |
| `PlayCardIntent` | UI 唯一合法意图：`Seat` + `Card` |
| `GameEvent` | `Dealt` / `CardPlayed`，带 `StateAfter` |
| `CommandResult` | `Accepted` / `Error` / `Events` |
| `OfflineTable` | 唯一改数据的地方 |

`OfflineTable` 已实现：

- `Deal(seed)`：发牌，南家先出，墩清空
- `Play(intent)`：必须轮到该座位、牌必须在手里；从手牌移除，写入墩，换下一家

未实现：叫牌、跟牌、比大小、收墩、清墩、三家 AI、局结束。

---

## 6. Presentation（壳）

设计目标：权威可以比画面快，画面也可以先演再对账。

### 6.1 表现行动

`PresentationAction`

- `Kind`：`DealHands` / `PlayCardToTrick`
- `Timing`：`Follow`（跟权威）/ `Lead`（先演）/ `CatchUp`（追上）

`PresentationActionState`

- 阶段：`Idle` / `Playing` / `Blocked` / `Leading`
- `Enqueue` / `TryBeginNext` / `CompleteCurrent` / `CancelCurrent`

`PresentationDirector`

- `HandleAuthoritativeEvent`：入队；若正在 Lead 且对得上则确认，对不上则取消动画
- `BeginLeadPlay`：预测出牌（未使用）
- `TryStartNextAction` / `CompleteCurrentAction`（无人调用）

### 6.2 动画

`AnimationChannel`：各家手牌、各家墩、Global。  
`AnimationPlayHandle`：带 `CancellationToken`。  
`AnimationPlayState.Play(channel, name, play)`：同 Channel 先 Cancel 再播，依赖 UniTask。

没有任何出牌动画调用这里。

---

## 7. UI

### 7.1 `CardView`

预制体 `Assets/Prefabs/CardView.prefab`，尺寸 160×224：

```text
CardView          白底，Raycast Target = 开（接收点击）
  Back            卡背，默认隐藏
  Rank            左上点数
  SmallSuit       左上小花色
  Center          中间大花色 / 人头 / 王
```

子节点 Raycast 必须关，否则 UGUI 点到点数/花色，不会冒泡到根上的 `IPointerClickHandler`。

接口：

- `Bind(card, onClicked)`：记住牌和回调；回收前要 `Bind(default, null)`
- `SetFaceUp` / `SetFaceDown`
- `OnPointerClick`：只上报 `Card`，不改数据

生成菜单：`Tools > MoonBridge > Create CardView Prefab`（会把 `Assets/Art/Cards` 下 PNG 设成 Sprite）。

### 7.2 `CardSpiritLibrary`

ScriptableObject，菜单 `Create > MoonBridge > Card Sprite Library`。  
资源：`Assets/Data/CardSpriteLibrary.asset`。

- 红黑两套点数，下标 `0=A … 12=K，13=SJ，14=BJ`
- 普通牌中间用大花色；J/Q/K 用人头图；王用 joker 图

类名是 `CardSpiritLibrary`（Spirit），资源名是 CardSpriteLibrary。

### 7.3 `CardViewPool`

预热后复用 `CardView`。场景里 `CardPool` 的 `prewarmCount` 为 52。  
`Get(parent)` / `Release` / `ReleaseAll`。  
各 `HandcardView` 只回收自己的牌，不要对共享池调 `ReleaseAll()`。

### 7.4 `HandcardView`

`ShowCards(cards, faceUp, vertical, onClicked)`：

- 横向或纵向排列并居中
- 明牌绑点击，盖牌不绑
- 间距：`HorizontalCardSpacing`、`verticalCardSpacing`

### 7.5 `TableView`

当前唯一接线处，挂在场景 `HandCardView` 上。

- `Start`：`table.Deal(seed)` 后按快照画四家
- 南家点击：`table.Play(new PlayCardIntent(Seat.South, card))`
- 成功则把事件交给 `director`，再 `ApplySnapshot`
- 拒绝则 `Debug.Log(error)`（例如 `not this seat's turn`）

未接：`TrickView`、动画队列消费、三家出牌。

### 7.6 `HandCardViewTester`

旧测试脚本，自己 `Deal` 且不传点击。场景已改挂 `TableView`，这个脚本应视为废弃。

---

## 8. 场景 `SampleScene`

| 物体 | 作用 |
|---|---|
| Canvas | Screen Space Overlay + GraphicRaycaster + CanvasScaler |
| EventSystem | 点击必需 |
| CardPool | `CardViewPool`，Prefab 指向 `CardView.prefab`，预热 52 |
| HandCardView | `TableView` + 南家 `HandcardView`（底边） |
| partnerCardView | 北家，盖牌，横向 |
| leftCardView | 西家，盖牌，纵向 |
| rightCardView | 东家，盖牌，纵向 |

分辨率：CanvasScaler 建议 Scale With Screen Size，参考 1920×1080。Constant Pixel Size 下分辨率越大牌看起来越小。

---

## 9. 美术

`Assets/Art/Cards/`

- Base：`card_front_blank.png`、`card_back_simple.png`
- Suits：club / diamond / heart / spade
- Ranks/Black、Ranks/Red：A–10、J、Q、K、SJ、BJ
- Faces：jack / queen / king、small_joker、big_joker（几何占位，不是精修插画）

点数分红黑两套；黑桃/梅花用黑，红桃/方块用红。

---

## 10. 当前可玩路径

1. Play `SampleScene`
2. 南家 13 张明牌，已按花色排序；其余三家牌背
3. 点南家一张牌：权威移除该牌，手牌重排少一张
4. 再点南家：`not this seat's turn`（轮到西家，还没有自动出牌）
5. 打出的牌还没有墩 UI，只是从手里消失

---

## 11. 未做 / 未接通

| 项 | 状态 |
|---|---|
| `TrickView` 桌中四槽 | 未建 |
| 表现队列真正驱动画面 | Director 入队但未消费 |
| 出牌飞牌动画（DOTween） | 未写 |
| Lead 预测出牌 | API 有，未用 |
| 跟牌、比大小、收墩 | 未写 |
| 叫牌 | 未写 |
| 三家 AI / 自动出 | 未写 |
| 联机 / 协议 / TCP | 学习重置后已去掉 |
| YooAsset / Animancer / Spine | 已导入，业务未用 |
| 对象池 `Release` 清 Bind | `HandcardView.Clear` 会清；池本身 `Release` 未清 |

---

## 12. 建议下一步（按顺序）

1. `TrickView`：按 `TableState.Trick` 画四个槽，仍走快照刷新  
2. 权威：跟牌校验、一墩四人出完后比大小、清墩  
3. 非南家自动出一张合法牌  
4. 把 `TableView` 改成消费 `PresentationDirector` 队列，再用 DOTween 做飞牌  
5. 叫牌阶段  
6. 用同一套意图/事件接服务器
