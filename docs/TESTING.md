# 测试 / Testing

## 1.0.4 — 2026-09-30

本版继续使用 1.0.4 版本号，并加入深色／浅色模式切换。主题选择经真实设置文件保存/加载测试；配色、图标和额度条经纯位图渲染验证，不创建 Form、Control 或托盘窗口，不等同于交互式 GUI 验收。

The version remains 1.0.4 with dark/light mode switching. Theme preference checks use real settings-file saves and loads. Palette, glyph and widget checks render only bitmaps: they do not create Forms, Controls or tray windows, and are not interactive GUI acceptance tests.

本版新增公告 HTTP 离线回归，并加强通信首字节及错误阶段断言。非 GUI 测试覆盖输入编码、子进程取消/超时、公告 304、缓存策略、重试日期及取消后恢复。测试使用合成账号和仅监听本机的 HTTP 服务；不会登录、退出或修改真实账号。

This update adds offline announcement HTTP regressions and checks protocol bytes and error stages. Non-GUI tests cover encoding, child-process cancellation/timeouts, 304 responses, cache policy, retry dates and recovery after cancellation. Fixtures use synthetic accounts and a loopback-only HTTP server; tests do not sign in, sign out or modify a real account.

GUI、真实 Plus、多屏幕/DPI 和浏览器点击仍需单独验收。构建通过及非 GUI 测试通过不能替代这些验收；历史受系统应用控制阻止的 GUI 专项未恢复。

GUI behavior, live Plus accounts, multiple displays/DPI settings and browser clicks still require separate acceptance testing. A build and non-GUI checks do not establish those results; historical GUI checks blocked by application control have not been restored.

编码回归已覆盖有控制台的 UTF-8 BOM、UTF-16（有/无 BOM），以及本机无控制台、系统代码页 CP936 的情况。无控制台且系统默认编码本身带 BOM（例如 UTF-8 系统区域设置）的环境尚未验收；.NET Framework 的控制台编码 setter 在该环境可能失败，不能据此声称兼容所有宿主编码。

Encoding regressions cover console hosts using UTF-8 with a BOM and UTF-16 with/without a BOM, plus a detached console on this machine with code page CP936. A detached console whose system default encoding includes a BOM, such as a UTF-8 system locale, remains unverified: the .NET Framework console encoding setter may fail there. These results do not establish compatibility with every host encoding.

## 1.0.3

新增检查覆盖 Spark 过滤、公共重置记录与预告区分、未知类型、安全来源链接、中英文缓存文案、损坏缓存及取消。用户已认可本地公告布局；GUI、真实Plus、其他DPI和新增HTTP异常分支尚未全面独立验证。

New checks cover Spark filtering, executed versus scheduled reset records, unknown types, safe source links, bilingual cache labels, corrupt caches and cancellation. The user accepted the local announcement layout. GUI, live Plus accounts, other DPI settings and new HTTP failure paths have not been comprehensively verified independently.

测试使用合成数据和模拟子进程，无需登录 Codex。

Tests use synthetic data and a fake server; no Codex sign-in is required.

## 命令 / Commands

从仓库根目录运行 / Run from the repository root:

```powershell
# 默认 Domain / Default: Domain
./scripts/test.ps1

./scripts/test.ps1 -Suite Domain
./scripts/test.ps1 -Suite Bridge
./scripts/test.ps1 -Suite ResetFeed
./scripts/test.ps1 -Suite Theme
./scripts/test.ps1 -Suite Layout
./scripts/test.ps1 -Suite Stacking
./scripts/test.ps1 -Suite All

# 只编译 / Compile only
./scripts/test.ps1 -Suite All -BuildOnly
```

`All` 包含 Domain、Bridge、ResetFeed、Theme、Layout 和 Stacking。`-BuildOnly` 编译所选套件，不执行测试。Bridge 包含真实 35 秒超时检查，ResetFeed 包含真实 HTTP 超时检查，请等待最终统计。Theme 将合成数据的真实绘图产物写入 `env/tests/theme-render/`；Layout 将逐项字体测量写入 `env/tests/layout-compact/`。

`All` runs Domain, Bridge, ResetFeed, Theme, Layout and Stacking. `-BuildOnly` compiles the selected suites without running them. Bridge checks the real 35-second deadline, and ResetFeed exercises HTTP timeouts; wait for the final totals. Theme writes production-renderer bitmaps using synthetic data to `env/tests/theme-render/`; Layout writes individual font measurements to `env/tests/layout-compact/`.

脚本需在本机策略允许的 PowerShell 中运行。若系统明确拒绝脚本或测试程序，停止该入口并记录错误，不通过更改策略、改名或换宿主绕过。2026-09-30 本机 PowerShell 7.6.5 可运行原测试入口；Windows PowerShell 5.1 的脚本入口被执行策略拒绝，该环境未完成运行验证。

Use a PowerShell installation in which local policy permits these scripts. If a script or executable is explicitly blocked, stop that entry point and record the error; do not bypass it by changing policy, renaming or switching hosts. On the audited machine on 2026-09-30, the original entry point ran under PowerShell 7.6.5, while Windows PowerShell 5.1 refused the script under its execution policy; execution in that environment was not verified.

## 覆盖范围 / Coverage

| 套件 / Suite | 覆盖 / Coverage |
| --- | --- |
| Domain | 额度与套餐解析、百分比与重置计算、异常值、安全错误、设置保存与恢复、固定尺寸、语言及运行数据路径 |
| Bridge | 模拟服务握手、通知与响应、安全错误、超时、取消、异常退出及子进程清理 |
| ResetFeed | 本机 HTTP 服务、304、ETag、缓存策略、秒数/日期重试、坏响应、取消、超时及原子缓存 |
| Theme | 深浅切换与恢复、额度条/图标真实像素、100/150/200% 绘图比例、透明圆角、文字对比度、双语状态及额度数据不变 |
| Layout | Plus/Pro 合成响应经生产解析器生成的标签、共享布局几何、同一行控件无重叠、真实字体宽高、选择器箭头及加宽下拉列表的文字/勾选预留空间 |
| Stacking | 层级按需修复、桌面事件筛选、排队合并、菜单/隐藏/退出门控，以及失败后重试；测试通过委托隔离原生写操作 |

Domain covers parsing, calculations, settings, and runtime data paths. Bridge launches a fake server to exercise protocol and process handling. ResetFeed uses a loopback server to test the production HTTP and cache implementation without external service dependencies.

Domain additionally checks old/invalid theme preferences and light/dark persistence without changing other settings. Theme checks real bitmap output and semantic text contrast in both palettes. Its 100/150/200% image scaling checks do not establish native Windows DPI, menu or window-layout behavior.

Layout 使用实际生产布局与 GDI 字体测量检查 Plus/Pro 中英文文案，按钮每边额外预留 4 个逻辑像素。它不创建原生控件，也不代表真实 Plus/Pro 账号登录、窗口点击或 Windows DPI 验收。未知超长额度名称继续省略显示；没有承诺任意标签都能完整显示。

Layout uses production geometry and GDI font measurements for Chinese/English Plus and Pro labels, with an extra four logical pixels reserved at each button edge. It does not create native controls or establish live-account, click or Windows DPI acceptance. Unknown long quota labels retain ellipsis; arbitrary labels are not guaranteed to fit in full.

Stacking 使用纯委托测试实际生产的层级决策、事件筛选与队列合并；不创建窗口或调用原生置顶。已有进程的只读窗口状态与事件采样是独立现场证据，不等同于修复后视觉验收。

Stacking tests production ordering decisions, event filtering and queue coalescing with delegates; it creates no windows and performs no native raises. Read-only observation of the existing process is separate live evidence, not post-fix visual acceptance.

## 结果 / Results

发布前复验：2026-09-30 在最终 v1.0.4 生产源码上重新运行 All，Domain 37、Bridge 14、ResetFeed 46、Theme 25、Layout 121、Stacking 20，共 **263 项通过、0 失败**。随后仅整理发布文档、示例图和包内说明，不改动生产代码；发行 EXE 沿用已验证的 1.0.4.0 构建。

Pre-release verification on 2026-09-30 reran All against the final v1.0.4 production source: Domain 37, Bridge 14, ResetFeed 46, Theme 25, Layout 121 and Stacking 20 — **263 checks passed, zero failures**. Subsequent changes only prepare release documentation, illustrations and package notes; production code is unchanged and the release reuses the verified 1.0.4.0 executable.

2026-09-30 浅色蓝色样修订（仍为 1.0.4）：从用户两张色样读取到 #7BBDFF / #1C8DFF，仅用于浅色模式的额度/时间饼图；浅色倒计时文字使用 #2056AA。深色配色、各背景、边框和尺寸不变。构建与 All 通过 **263 项、0 失败**（Domain 37、Bridge 14、ResetFeed 46、Theme 25、Layout 121、Stacking 20）。更新后的主题断言在修改前为 23 通过/2 失败，修改后为 25/25；深色浮条和图标 PNG 与上一份绿色构建逐字节一致。最低语义文字对比度仍为深色 6.24:1、浅色 4.81:1。已查看实际位图预览；未执行本构建的原生 GUI 交互。

The 2026-09-30 light-mode swatch revision retains version 1.0.4. The supplied images were sampled as #7BBDFF / #1C8DFF for the light quota/time disks, with #2056AA countdown text. Dark colours, backgrounds, borders and geometry are unchanged. Build and All passed **263 checks, zero failures** (Domain 37, Bridge 14, ResetFeed 46, Theme 25, Layout 121, Stacking 20). Updated theme assertions first produced 23 passes and 2 failures, then passed 25/25. Dark widget and glyph PNGs are byte-for-byte identical to the preceding green build. Minimum semantic text contrast remains 6.24:1 dark and 4.81:1 light. Rendered bitmaps were inspected; native GUI interaction with this build was not executed.

2026-09-30 浅绿配色修订（仍为 1.0.4）：两个饼图改为 #6CCF91 与更浅的 #A8E5BC，背景、边框和布局保持不变。浅色模式倒计时文字单独使用 #246D40，避免浅绿文字在灰色背景上难以辨认。最终构建通过；All 共 **263 项通过、0 失败**（Domain 37、Bridge 14、ResetFeed 46、Theme 25、Layout 121、Stacking 20）。更新后的主题断言先为 22 通过/3 失败，配色修改后为 25/25；所测语义文字最低对比度为深色 6.24:1、浅色 4.81:1。已检查实际渲染位图；本轮未启动绿色构建的原生 GUI。

另对用户已启动的上一份 1626483 层级修复程序做 20 秒只读采样：636 次中浮条均可见且处于任务栏上方，位置尺寸稳定，未采到此前的周期性遮挡。这是上一份程序的窗口元数据观测，不代表绿色构建或所有原生交互已验收。

The 2026-09-30 green revision remains version 1.0.4. Its pies use #6CCF91 and the lighter #A8E5BC; backgrounds, borders and geometry are unchanged. Light-mode countdown text separately uses #246D40 for legibility. Build and All passed **263 checks, zero failures** (Domain 37, Bridge 14, ResetFeed 46, Theme 25, Layout 121, Stacking 20). Updated theme assertions first produced 22 passes and 3 failures, then passed 25/25 after the palette change. Minimum tested semantic text contrast is 6.24:1 dark and 4.81:1 light. Actual rendered bitmaps were inspected; native GUI interaction with the green build was not executed.

A separate 20-second read-only observation of the user's running 1626483 stacking build found the widget visible and above the taskbar in all 636 samples, with stable bounds and no sampled recurrence of the earlier periodic occlusion. This is window metadata from the preceding executable, not native acceptance of the green build or every interaction.

2026-09-30 浮条层级闪烁修订（仍为 1.0.4）：最终构建通过；All 为 Domain 37、Bridge 14、ResetFeed 46、Theme 25、Layout 121、Stacking 20，共 **263 项通过、0 失败**。Stacking 对旧的无条件置顶行为为 1 通过/11 失败，事件过滤与排队中间版本为 14 通过/6 失败，最终 20/20；稳定状态连续 100 次检查由 100 次原生写入降为零。

对用户正在运行的上一份 763834c 程序做只读采样：637 次中可见标志、置顶标志和位置尺寸均稳定，相对任务栏却每秒交替落到下方；20 个完整下方区间估计为 313–378ms，中位数 345ms。另一次关联记录中，20 条 Desktop/OBJID_CLIENT(-4)/CHILDID_SELF(0) 重排事件对应 10 次下方和 10 次上方状态。这些是离散窗口元数据，不是逐帧屏幕测量。

最终 EXE 内的生产 ForegroundMonitor 和 TaskbarStacking.Repair 还进行了 10 秒被动监听验证：20 次实际桌面回调，10 次下方判为需要恢复、10 次上方判为无需写入。恢复委托仅计数，实际原生写入为零；未创建 Form、Control、托盘或替换用户运行程序。本轮修复后的视觉效果、菜单与全屏交互、Explorer 重启仍需切换新版后验收，不能用纯逻辑及被动观测代替。

The 2026-09-30 stacking revision remains version 1.0.4. Build and All passed **263 checks, zero failures**: Domain 37, Bridge 14, ResetFeed 46, Theme 25, Layout 121 and Stacking 20. The original unconditional-raise seam failed 11 of 12 checks; the intermediate event/queue implementation failed 6 of 20; the final suite passes 20/20. One hundred stable checks now issue zero writes instead of 100.

Read-only observation of the prior 763834c process found stable visibility, topmost flags and bounds across 637 samples, but recurring relative taskbar occlusion. Twenty complete below-taskbar intervals were estimated at 313–378ms (median 345ms). A separate correlated log recorded 20 Desktop/OBJID_CLIENT/CHILDID_SELF reorder notifications, alternating between below and above. These are sampled window metadata, not frame-level visual measurements.

A passive probe using ForegroundMonitor and TaskbarStacking.Repair from the final executable received 20 actual desktop callbacks in 10 seconds: 10 below-taskbar repair decisions and 10 above-taskbar no-write decisions. Its write delegate only counted intent; native writes were zero. No Form, Control or tray was created and the running user program was not replaced. Post-fix visual behavior, menu/fullscreen interaction and Explorer restart remain pending desktop acceptance after switching builds.

2026-09-30 单行布局与浅灰/双蓝修订（仍为 1.0.4）：最终构建通过；All 为 Domain 37/37、Bridge 14/14、ResetFeed 46/46、Theme 25/25、Layout 121/121，共 **243 项、0 失败**。语言、主题和额度选择同排，详情页高度减少 36 个逻辑像素；下拉列表单独加宽并限制在工作区宽度内。

Layout 对真实解析器生成的 Plus、Pro 和多额度桶中英文标签完成 624 条 GDI 字体测量，覆盖 75/100/125/150/175/200% 几何比例及两种主题按钮文案，全部解析为 Microsoft YaHei UI 字体。宽高均无超限；75% 最小横向余量 1 像素，100–200% 至少 7 像素。最初的行宽方案出现 14 项失败，下拉列表宽度另出现 8 项失败，调整生产共用几何后全部通过。未知长标签仍使用单行省略号；字体替换、低于 75% 的极小工作区及原生 Windows DPI 行为不在已验证范围。

浅色背景/卡片/额度条分别为 #E5E5E5/#EFEFEF/#E8E8E8；两主题的两个饼图均使用深浅不同的蓝色。主题位图检查为 25/25；最终新增的中性灰和双蓝断言在调整前出现 3 项失败，调整后通过。所测语义文字最低对比度为深色 4.71:1、浅色 4.81:1。本轮独立审查未发现剩余阻断问题；未执行原生 GUI 或真实 Plus 登录。

The final 2026-09-30 compact gray/blue revision still uses version 1.0.4. Build and All passed: Domain 37, Bridge 14, ResetFeed 46, Theme 25 and Layout 121 — **243 checks, zero failures**. Shared production geometry puts all three controls on one row, removes 36 logical pixels of height and gives the popup a separate width bounded by the work area.

Layout recorded 624 real GDI font measurements for parser-generated Plus, Pro and multi-bucket Chinese/English labels at 75/100/125/150/175/200 percent, with both theme captions. All resolved to Microsoft YaHei UI; none exceeded its width or height budget. Minimum horizontal spare was 1 pixel at 75 percent and at least 7 pixels at 100–200 percent. The first row allocation failed 14 checks; the narrow popup failed another 8 before repair. Long unknown labels retain ellipsis. Fallback fonts, scales below 75 percent and native Windows DPI behavior remain unverified.

Light background/card/widget are #E5E5E5/#EFEFEF/#E8E8E8, with two blue pie colors in both themes. Theme checks passed 25/25 after three new neutral-gray/two-blue assertions failed on the prior palette. Minimum tested semantic text contrast is 4.71:1 dark and 4.81:1 light. Independent review found no remaining blockers; no native GUI or live Plus login was performed.

2026-09-30 上一轮主题版（提交 e3f2cd8，本轮浅灰/双蓝配色调整前，版本仍为 1.0.4）：最终构建通过；All 为 Domain 37/37、Bridge 14/14、ResetFeed 46/46、Theme 21/21，共 118 项、0 失败。新增主题设置检查先出现 3 项失败，修复后通过；最终主题位图套件对旧版为 12 通过、9 失败，对新版为 21 通过。恢复深色后额度条及图标 PNG 与旧版逐字节一致。所测正文/语义颜色组合的最低对比度为深色 5.20:1、浅色 4.81:1。

独立代码审查覆盖两种入口、菜单状态、已有控件重着色、主题持久化与托盘图标释放；审查中发现并修正了自绘提示框默认单行绘制导致原有换行丢失的问题，随后最终构建通过。本轮没有运行原生窗口、菜单或提示框交互测试；下面的真实服务记录来自此前修复版，不能当作主题版 GUI 验收。

The earlier 2026-09-30 theme build (commit e3f2cd8, before this neutral-gray/two-blue revision) retains version 1.0.4. The final build succeeded and All passed Domain 37/37, Bridge 14/14, ResetFeed 46/46 and Theme 21/21: 118 checks, zero failures. The three new preference checks failed before implementation. The final bitmap suite produced 12 passes and 9 failures on the old renderer, then 21 passes on the new one. Restoring dark mode reproduces the original widget and glyph PNG bytes. The minimum tested semantic text contrast is 5.20:1 in dark mode and 4.81:1 in light mode.

Independent review covered both entry points, menu state, existing-control recoloring, persistence and tray-icon disposal. It caught and resolved a single-line drawing default that would discard the tooltip's existing line break, followed by a successful final build. Native window, menu and tooltip interaction were not executed. The live-service evidence below belongs to the earlier repair build and is not GUI acceptance of the theme build.

2026-09-30：1.0.4 修复版在 Windows x64、.NET Framework 4.8、PowerShell 7.6.5 上编译通过（C# 5，警告视为错误）。`-Suite All` 最终运行 Domain 34/34、Bridge 14/14、ResetFeed 46/46，共 94 项，0 失败。修复前强化的 Bridge 检查为 2 通过、10 失败，公告检查为 30 通过、16 失败；修复后全部通过，Bridge 另补充了启动失败恢复与并发启动两项。

最终 1.0.4.0 EXE 的非 GUI 真实服务验证通过：UTF-8 BOM 宿主下额度读取约 3.67 秒，Pro 套餐、每周窗口、使用比例及重置时间与 Codex 应用工具一致；公共公告 HTTPS 请求约 0.38 秒成功并写入隔离测试缓存。真实额度会随账号使用变化，这些结果只说明验证当时的一致性。GUI 交互及上方列明的其它环境仍未验收。

On 2026-09-30, the 1.0.4 repair build compiled on Windows x64 with .NET Framework 4.8 and PowerShell 7.6.5 (C# 5, warnings as errors). The final All run passed Domain 34/34, Bridge 14/14 and ResetFeed 46/46: 94 checks with zero failures. Strengthened pre-fix Bridge checks had 2 passes and 10 failures; announcement checks had 30 passes and 16 failures. All pass after repair, with two additional Bridge checks for failed-start restoration and concurrent starts.

The final 1.0.4.0 EXE also passed non-GUI live-service checks: quota retrieval under a UTF-8 BOM host took about 3.67 seconds and agreed with the Codex app tool on Pro plan, weekly window, usage and reset time. The announcement HTTPS request succeeded in about 0.38 seconds and wrote an isolated test cache. Live usage changes over time; this establishes agreement at verification time. GUI interaction and the other environments listed above remain unverified.

1.0.2：标题栏链接、版本文本及对齐修订已编译通过；用户授权发布。未独立执行浏览器点击或新版 GUI 测试，下面的历史结果不作为本次完整 GUI 验证。

1.0.2 compiles with the header link, version text and alignment changes, and the user approved release. Browser-launch and GUI interactions were not independently tested; earlier results below are not full GUI verification of this update.

2026-09-09：1.0.1 编译通过，用户确认新版提示框交互正常并同意发布。这是用户辅助实测，不是自动化 GUI 验证；不能据此认定所有屏幕、套餐或任务栏场景均已覆盖。

On 2026-09-09, version 1.0.1 compiled successfully and the user confirmed the updated tooltip interaction before approving release. This is user-assisted testing, not automated GUI verification, and does not cover every display, plan or taskbar scenario.

开发阶段的 Domain 32 项与 Bridge 8 项检查通过，0 失败，覆盖设置迁移、数据目录、鼠标移出状态与通信处理。后续原生窗口事件和提示框修改仅做编译检查，没有将旧测试结果当作新 GUI 验证。

Development checks passed: 32 Domain and 8 Bridge checks, with no failures. They cover settings migration, data paths, pointer-leave state and protocol handling. Later native-window and tooltip changes were compiled; those earlier results are not presented as GUI verification.

以下为 1.0.0 的历史实测 / Earlier user testing on 1.0.0:
用户实测补充：当前发布的 1.0.0.0 已在一台 Windows 电脑上启动，运行文件 SHA256 与发行附件一致。截图确认 Pro 额度读取、每周额度与 Codex 显示一致、中英文详情布局以及通知区左侧的额度条位置。截图仅用于本地验收，未上传账号信息。

User testing confirmed that the published 1.0.0.0 executable starts on one Windows machine; its SHA256 matches the release asset. Screenshots confirm Pro usage retrieval, the weekly value matching Codex, Chinese and English detail layouts, and widget placement beside the notification area. Account screenshots were not uploaded.

用户按手动刷新及等待自动刷新的步骤提供了连续截图，成功更新时间的变化符合手动刷新及五分钟定时刷新；界面持续显示正常。本项为用户辅助实测，不是自动化点击验证。

Sequential screenshots supplied during the manual/automatic refresh check show successful update timestamps consistent with a manual refresh and the five-minute schedule, with the UI still displaying normally. This is user-assisted testing, not automated interaction testing.

用户另确认：通过托盘 Exit 退出后额度条和托盘图标消失，重新打开同一 EXE 后仍为英文，且能够重新读取套餐和额度。本项为用户操作反馈，不代表对所有退出后的子进程做过独立检查。

The user also confirmed that Exit removes the widget and tray icon, and that reopening the same EXE preserves English and retrieves the plan and usage again. This is user-reported interaction testing, not an independent inspection of every child process after exit.

其他 DPI/屏幕环境及真实 Plus 账号仍待验证。EXE 仍未签名；历史构建和部分 UI 检查曾被 Windows 应用控制阻止，本次成功启动不代表这些检查已恢复或所有电脑均能运行。

Other DPI/display configurations and a real Plus account remain unverified. The EXE is still unsigned. Earlier builds and some UI checks were blocked by Windows application control; this successful launch does not validate those checks or guarantee compatibility on every machine.
