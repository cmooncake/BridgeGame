# 牌局运行时结构

本文是改代码的目标说明，不是现状说明书。  
场景：`SampleScene`（牌桌场景）。根物体：`MatchRuntime`。

单向约定不变：UI 只收集意图，不改手牌或墩。

```text
UI 收集意图
  → ActionList.Emit
  → 状态机 Trigger
  → 权威来源按规则更新牌桌
  → 权威来源发出更新消息
  → ActionList.Emit(权威事件)
  → 状态机更新表现状态
  → UI 按表现结果画画
```

---

## 1. 实体一览

共六块。根物体本身不算“运行时内容”，它只负责在场景开始时把下面五块装起来。

| 实体 | 目标名 | 是什么 | 不是什么 |
|---|---|---|---|
| 根物体 | `MatchRuntime` | 牌桌场景里的构造器 / 持有者 | 规则、出牌、画画 |
| 状态机 | `MatchStateMachine` | 权威状态 + 表现状态 + Update + Trigger | 跟牌公式、UGUI |
| 权威来源 | `IAuthoritativeSource` | 通知权威更新；本地由 Domain 实现 | UI、输入、动画 |
| ActionList | `ActionRuntime` | 事件与回调的唯一分发器 | 决定出哪张、改桌面 |
| 牌桌 | `Table` | 四座位、四玩家、手牌/墩等数据 | 点牌、飞牌 |
| UI | `MoonBridge.UI` | 显示 + 把点击变成意图 | 跟牌、轮转、删牌 |

人机、联机、本地，都不进牌桌，也不进权威来源。它们只是不同的意图来源，最后都变成同一种意图进 ActionList。

---

## 2. 依赖关系

只允许实线方向。禁止反向、禁止跨层抄近路。

```text
                    牌桌场景
                        │
                        ▼
                  MatchRuntime
                  （构造 / 持有 / 销毁）
                        │
          ┌─────────────┼─────────────┬──────────────┐
          ▼             ▼             ▼              ▼
   MatchStateMachine  ActionList   IAuthoritative   Table
          │             ▲          Source            ▲
          │             │             │              │
          │        只 Emit / On       │              │
          ├─────────────┤             │              │
          │             │             │   本地实现时写入
          │             │             └──────────────┘
          │             │
          │             │  UI / 自动座位 / 以后的网络适配
          │             │  只注册回调或 Emit 意图
          ▼             │
   Presentation 状态     │
   （行动队列、动画）    │
                        │
          MoonBridge.UI ┘
          读：表现结果 / 需要画的快照
          写：无（只能 Emit 意图）
```

### 2.1 谁可以引用谁

| 从 | 可以依赖 | 禁止依赖 |
|---|---|---|
| `MatchRuntime` | 五块运行时内容；Unity 场景生命周期 | Domain 规则细节、具体 `CardView` |
| `MatchStateMachine` | `Table`、`ActionList`、`IAuthoritativeSource`、Presentation 状态 | `TableView`、`HandcardView`、Unity UGUI |
| `IAuthoritativeSource`（本地） | Domain 规则、`Table` | ActionList 的“谁注册了回调”、UI、Presentation |
| `ActionList` | 意图 / 事件的数据类型 | `Table`、Domain 规则、UI、状态机实现 |
| `Table` | Domain 数据类型（`Card`、`Seat`、`Player`） | 规则判定入口以外的 UI / ActionList / 网络 |
| Domain | 无 Unity、无 UI | `MatchRuntime`、`ActionList`、Presentation |
| UI | `ActionList`（Emit / 订阅）；只读快照或表现结果 | `Table` 的可写接口、`OfflineTable.Play`、跟牌函数 |

本地权威来源**可以改 `Table`**，因为现在没有服务器，牌桌数据就在本机。  
联机后权威来源改为服务器适配：它不再改本地 `Table`，只把服务器消息变成权威事件；由状态机 Trigger 把事件写进本地 `Table` 的只读镜像。接口不变。

### 2.2 ActionList 上的消息

现在就要分清两类，避免再按参数类型撞车：

| Action | 方向 | 载荷 | 谁 Emit | 谁注册回调 |
|---|---|---|---|---|
| `DealHands` | 意图 | `int` seed | 根物体开局 | 状态机 Trigger |
| `MakeCall` | 意图 | `BidIntent` | UI、自动座位 | 状态机 Trigger |
| `PlayCard` | 意图 | `PlayCardIntent` | UI、自动座位、以后的网络适配 | 状态机 Trigger |
| `AuthoritativeEvent` | 权威更新 | `GameEvent` | 状态机在 `Submit` 成功后转发 | 状态机、UI |
| （以后）`PresentationReady` 等 | 表现 | 行动 id | 状态机 Update | UI |

其他人只 `+=` 回调，或 `Emit` 自己职责内的那一种消息。  
禁止：UI 直接调 `Table.Play`；权威来源直接调 `HandcardView.ShowCards`；A 回调里去调 B 的私有方法代替 Emit。

---

## 3. 各实体职责与边界

### 3.1 根物体 `MatchRuntime`

**职责**

1. 牌桌场景 `Awake` 时构造运行时内容（或确认场景里已挂好的引用）。
2. 按固定顺序接线：先 `Table` 与 `ActionList`，再权威来源，再状态机，最后让 UI 来注册。
3. 场景卸载 / `OnDestroy` 时按反序拆掉：退订、停动画、丢掉单例引用。

**不做**

- 不写跟牌、比大小、发牌算法。
- 不画牌。
- 不在根上堆 `if (seat == South)` 推进牌局。

**现状**

已有场景根物体 `MatchRuntime`，也已是牌局级单例（不 `DontDestroyOnLoad`）。  
但它现在还在自己的 `HandleDeal` / `HandlePlayIntent` 里执行出牌，并持有 `SeatIntentRouter`。这些应下沉到状态机与意图来源，根物体只保留构造。

---

### 3.2 状态机 `MatchStateMachine`

同时持有两类状态，以及两类函数。

#### 状态

| 层 | 已有/目标类型 | 含义 |
|---|---|---|
| 权威 | `Table` 的当前数据，或只读 `TableState` 快照 | 牌局真相。不等人看完动画。 |
| 表现行动 | `PresentationActionState` | 正在播哪条行动、队列里还有什么；Follow / Lead / CatchUp。 |
| 表现动画 | `AnimationPlayState` | 哪个 Channel 在播，必须能 `Cancel`。 |

权威可以比画面快；画面也可以先 Lead 再对账。对不上就取消动画，按权威纠偏。这三层已经写过壳，状态机是把它们收成一个可 Tick、可 Trigger 的对象。

#### 函数

**Trigger（离散）**

由 ActionList 回调进来，或由动画结束回调进来。只做状态转移，不直接碰 UGUI。

| 触发 | 状态机做什么 |
|---|---|
| `DealHands` / `PlayCard` | 把意图交给 `IAuthoritativeSource.Submit`。不在这里认人机。 |
| 权威来源返回拒绝 | 不改权威状态；可记一条可观察错误（现有 `CommandResult.Error`）。 |
| 权威来源产生 `GameEvent` | 更新对权威快照的引用；给表现层入队；`AuthoritativeEvent.Emit`（若来源自己不发）。 |
| 表现行动开始 / 结束 | 改 `PresentationActionState` 的 Phase；必要时开下一条。 |
| 动画 Cancel / Complete | 清 Channel；决定 CatchUp 还是 Idle。 |

**Update（每帧）**

由根物体 `Update` 转交给状态机。只推进表现，不推进权威。

- 动画是否结束。
- `TryStartNextAction`：队列不空且 Channel 空闲则开始下一条。
- 权威已超前、画面 Blocked 时的 CatchUp。

权威本身**没有**按帧 Update。本地权威是意图进来立刻结算。

**不做**

- 不实现 `TrickRules`。
- 不 `ShowCards`。
- 不判断“这是 AI 所以出 `hand[0]`”。当前座位若需要自动意图，由绑在该座位上的 `IIntentSource` 自己 `Emit(PlayCard)`。

**现状**

`PresentationDirector` + `PresentationActionState` + `AnimationPlayState` 是表现半边。  
`OfflineTable.Current` 是权威半边。  
还没有统一的 Trigger/Update 入口，也没有人每帧 `TryStartNextAction`。

---

### 3.3 权威状态来源 `IAuthoritativeSource`

**职责**

通知本局权威更新。输入是意图，输出是接受/拒绝 + `GameEvent[]`。

```text
Submit(intent) → CommandResult
                 Accepted + Events     权威已变
                 Reject + Error        权威不变
```

它描述的是**抽象规则下的权威变迁**，不是画面。

**本地实现（现在）**

由 Domain 接管：发牌、跟牌、A=14、赢墩、清墩、轮转。  
实现类可以叫 `LocalAuthoritativeSource`。它拿着 `Table`，用 Domain 规则改数据，再交出 `GameEvent`。

这就是现在的 `OfflineTable.Deal` / `Play`，要从“又是桌又是规则引擎”里拆出来：桌是数据，来源是规则入口。

**联机实现（以后）**

`ServerAuthoritativeSource`：把意图编码发出去，把服务器下行解码成同样的 `GameEvent`。  
状态机和 UI 仍然只认 `Submit` 与 `AuthoritativeEvent`，不认 TCP。

**不做**

- 不持有 `CardView`、不读点击。
- 不知道谁是人、谁是 AI。只认 `PlayCardIntent(Seat, Card)`。
- 不排队动画、不对账 Lead。

**和 Domain 的关系**

| Domain | 权威来源 |
|---|---|
| 牌、座位、规则函数（`TrickRules`、以后的叫牌规则） | 把意图应用到一局 `Table` 上，并对外通知 |
| 无生命周期，无 Unity | 一局一个实例，随根物体生灭 |
| 不发消息 | 发 `GameEvent` |

跟牌公式继续放 Domain，不放权威来源类里，也不放 UI。

---

### 3.4 ActionList `ActionRuntime`

**职责**

牌局里唯一的“事件名 → 回调列表”。

- 用具体 action 区分（`PlayCard`、`DealHands`），不用参数类型当键。
- 注册：`action += callback`
- 分发：`action.Emit(payload)`，内部拷一份再调，避免回调里 `+=`/`-=` 改正在遍历的列表。
- 其他人只注册，不负责“执行这条流水线”。执行发生在各自回调里，且回调只做自己那一层的事。

**不做**

- 不调用 `Table` / Domain。
- 不保存手牌。
- 不当通用全局 EventBus（不要把按钮音效、场景加载塞进来）。

**现状**

`ActionRuntime` + `RuntimeAction<T>` 已按 action 列表实现。  
缺口：状态机还没成为唯一的意图回调；`MatchRuntime` 自己在执行 Deal/Play。

---

### 3.5 牌桌 `Table`

**职责**

一局牌的数据，被状态机引用。

必须能表达：

- 四个 `Seat`：North / East / South / West。
- 四个玩家位：每个座位一个 `Player`（标识、座位；以后可加昵称）。玩家不是 AI 类。
- 卡牌：各家手牌、当前墩、首攻花色、墩是否出齐、当前轮到谁、序号。

权威来源（本地）写它；状态机读它做快照；UI **不持有可写 `Table`**。

**和现在的 `OfflineTable` / `TableState`**

| 现有 | 拆完以后 |
|---|---|
| `OfflineTable` 数据 + `Deal`/`Play` | 数据 → `Table`；`Deal`/`Play` → `LocalAuthoritativeSource` |
| `TableState` 只读快照 | 保留。状态机 / 事件仍带 `StateAfter`，UI 只读这个 |
| `OptionalCard` | 留在牌桌数据一侧（空槽是桌状态，不是规则） |
| `PlayFirstCardOfCurrentSeat` | 已删，不要回来 |

**不做**

- 不 `Emit`。
- 不创建 `CardView`。
- 不实现“西家自动出牌”。

---

### 3.6 UI

**职责**

- 显示：手牌、墩、以后的飞牌，只根据**当前表现结果**（现在可以暂时等于权威快照）。
- 收集意图：点击 → `PlayCardIntent` → `ActionList.PlayCard.Emit`。发牌按钮/开局 → `DealHands.Emit`。

**不做**

- 不 `hand.Remove`。
- 不写跟牌、轮转、赢墩。
- 不 `new OfflineTable()`，不直接 `source.Submit`。

**现状**

`TableView` 已改为只 Emit / 订阅。  
仍有两处越界：

1. `Start` 里发 `DealHands` 可以保留（开局意图），但 seed 属于开局配置，最终应归根物体或开局流程，不该永久躺在 View 上。
2. 收到 `AuthoritativeEvent` 立刻整桌重画，等于跳过表现状态机。在表现 Update 接通之前，这是暂时允许的捷径；接通后 UI 应听“可以画的表现结果”，而不是权威事件本身。

`HandcardView` / `CardView` / `TrickcardView` 只显示，并把点击变成 `Card`。

---

### 3.7 意图来源（附属，不是五块之一）

`IIntentSource` / `AutoPlayIntentSource` / `SeatIntentRouter` 不是权威，也不是牌桌。

它们的唯一合法动作：看只读状态 → 造 `PlayCardIntent` → `ActionList.PlayCard.Emit`。

和 UI 点牌是同一条路。状态机看不到“这是自动的”。

南家不绑自动源，就等人点。换联机时，远端座位改绑网络源，或干脆不绑、只收服务器权威事件。

---

## 4. 生存周期

牌局级，跟场景走。不是整个客户端进程单例。

```text
进入牌桌场景
  1. MatchRuntime.Awake
       构造 Table（空桌）
       构造 ActionList
       构造 LocalAuthoritativeSource(Table)
       构造 MatchStateMachine(Table, Source, ActionList)
       状态机订阅 ActionList（意图 Trigger）
       绑定各座位 IIntentSource（可选）
  2. UI.Awake / OnEnable
       订阅 AuthoritativeEvent（或以后的表现事件）
  3. 开局（UI.Start 或根物体 Start）
       DealHands.Emit(seed)
  4. 状态机 Trigger → Source.Submit(Deal)
       Table 被写入
       AuthoritativeEvent.Emit
       表现入队；UI 画发牌结果
  5. 对局中
       意图 Emit → Trigger → Submit → 事件 Emit → 表现 Update → UI
  6. 离开场景 / MatchRuntime.OnDestroy
       状态机退订、CancelAll 动画
       UI 退订（OnDisable / OnDestroy）
       丢掉 Source、Table、ActionList
       Instance = null
```

### 4.1 构造顺序（必须）

1. `Table`
2. `ActionList`
3. `IAuthoritativeSource`（需要 `Table`）
4. `MatchStateMachine`（需要前三者，并在构造里 `+=` 意图回调）
5. 座位意图源（需要 `ActionList`，只读状态来自 `Table` / 快照）
6. UI 订阅（场景里的 MonoBehaviour，晚于根物体；根物体 `DefaultExecutionOrder(-100)` 已保证）

UI 若在根物体之前 `Awake`，必须 `MatchRuntime.Ensure()`，但不能在 Ensure 里发 Deal。Deal 放 Start，保证订阅已挂上。

### 4.2 一局内对象活多久

| 对象 | 出生 | 死亡 | 一局中能重建吗 |
|---|---|---|---|
| `MatchRuntime` | 进场景 | 出场景 | 否。一场景一份 |
| `ActionList` | 根构造 | 根销毁 | 否。回调表跟着局走 |
| `Table` | 根构造（空） | 根销毁 | `Deal` 可重置内容，不换实例 |
| `IAuthoritativeSource` | 根构造 | 根销毁 | 本地否；以后切联机是换实现，不是中途 new 一个混用 |
| `MatchStateMachine` | 根构造 | 根销毁 | 否。`Deal` 时清表现队列 |
| `IIntentSource` | 根构造时按座位绑 | 根销毁或重绑 | 可换绑定，不换 Action 类型 |
| `TableView` 等 UI | 场景摆好 | 出场景 | 与运行时解耦，只通过 ActionList 相连 |
| `CardView`（池里） | 池预热 | 回池 / 出场景 | 与规则无关 |

编辑器域重载：继续用 `RuntimeInitializeOnLoadMethod(SubsystemRegistration)` 把静态 `Instance` 置空，避免指向已毁物体。

### 4.3 一墩 / 一手的生命周期（数据，不是 MonoBehaviour）

| 数据 | 开始 | 结束 |
|---|---|---|
| 手牌列表 | `Deal` | 打出或重新发牌 |
| 当前墩 | 首攻写入第一张 | 下一墩出手时清（满墩先留着给表现看） |
| `HasLeadSuit` | 墩上第一张 | 清墩 |
| `TrickComplete` | 第四张合法打出 | 清墩 |
| `GameEvent` | `Submit` 成功当场产生 | 事件对象只读，用完即弃；序号留在 `Table` |
| `PresentationAction` | 权威事件入队 | Complete 或 Cancel |
| 动画 Handle | `Play(channel)` | Complete / Cancel |

---

## 5. 一次出牌怎么走

以南家点一张黑桃为例（本地、无将）。

```text
1. CardView 点击 → TableView 得到 Card
2. UI：ActionList.PlayCard.Emit(PlayCardIntent(South, card))
3. 状态机 Trigger：source.Submit(intent)
4. LocalAuthoritativeSource：
     查 Table.Turn、手牌、TrickRules.IsLegalFollow
     合法：手牌删这张，写入墩，可能结算赢家
     非法：Reject，Table 不变
5. 成功则产生 GameEvent(CardPlayed, StateAfter)
6. ActionList.AuthoritativeEvent.Emit(event)
7. 状态机：记下权威快照，Enqueue PlayCardToTrick
8. UI（现阶段）：按 StateAfter 重画
   以后：等 Update 开始这条行动，再飞牌，结束再定格
9. 若当前座位绑了 IIntentSource：
     它看到 Turn 变了 → 再 Emit 一个 PlayCardIntent
     回到步骤 3。状态机仍然不认这是 AI
```

拒绝时：只 `Debug.Log`（或以后的提示 action）。不发 `AuthoritativeEvent`，画面不动。

---

## 6. 目标目录

```text
Assets/Scripts/
  Domain/                         纯规则与数据类型
    Card, CardRank, CardSuit
    Seat, Player
    CardComparer, TrickRules
    （以后叫牌规则也在这里）

  Runtime/                        根 + 总线 + 状态机
    MatchRuntime                  根物体
    MatchStateMachine             Trigger / Update
    ActionRuntime, RuntimeAction  ActionList
    SeatIntentRouter              座位 → 意图源（附属）

  Game/
    Authoritative/
      IAuthoritativeSource
      LocalAuthoritativeSource    现 OfflineTable 的 Deal/Play
      Table                       现 OfflineTable 的数据
      TableState, OptionalCard
      PlayCardIntent, GameEvent, CommandResult
    AutoPlayIntentSource          意图源，不是权威
    OfflineDealService            洗牌，被本地权威来源使用

  Presentation/
    Actions/                      表现行动状态（状态机持有）
    Animation/                    可取消动画状态（状态机持有）

  UI/                             只显示与收集意图
```

命名可以在落地时微调，层次不要调。

---

## 7. 现状对照（改代码时按这个拆）

| 现有类型 | 目标归属 | 要做的事 |
|---|---|---|
| `MatchRuntime` | 根物体 | 去掉 Deal/Play 执行；只构造、转 Update、销毁 |
| `ActionRuntime` | ActionList | 保留；意图回调改挂到状态机 |
| `OfflineTable` | 拆成 `Table` + `LocalAuthoritativeSource` | 数据与 `Submit` 分离 |
| `TableState` / `GameEvent` / `PlayCardIntent` | 权威数据与消息 | 保留 |
| `TrickRules` | Domain | 已在 Domain，保持 |
| `PresentationDirector` | 并进状态机或成为其内部 | 不要再和 TableView 直连 |
| `PresentationActionState` / `AnimationPlayState` | 状态机持有的表现状态 | 接上 Update |
| `SeatIntentRouter` / `AutoPlayIntentSource` | 意图源 | 保留在权威/UI 之外 |
| `TableView` | UI | 继续只 Emit / 订阅；seed 以后挪走 |
| `HandcardView` 等 | UI | 不变 |

`CurrentStatus.md` 里“未做跟牌 / 未做自动出 / 墩 UI 未建”已经过时。规则、自动意图、墩显示已经在跑；缺的是按本文把结构收拢，以及表现队列真正驱动画面。

---

## 8. 验收（结构收拢完成时）

1. `OfflineTable` 不再同时身兼数据与规则入口。
2. `MatchRuntime` 里没有 `table.Play` / `table.Deal`。
3. 所有出牌、发牌都经过 `ActionList` 对应 action。
4. 状态机是意图的唯一执行回调；UI 与自动源只 Emit。
5. Domain / 本地权威来源零引用 UI。
6. UI 零引用可写 `Table`。
7. 进 `SampleScene` 仍能发牌、南家点牌、三家跟牌、满墩定赢家。行为不变，只是接线变了。

未完成：飞牌动画、叫牌 AI（三家目前一律 Pass）、服务器。

叫牌与将牌已落地：`AuctionRules` / `Call` / `Contract` 在 Domain；`LocalAuthoritativeSource.SubmitBid` 结算定约；打牌用定约将牌比大小。
