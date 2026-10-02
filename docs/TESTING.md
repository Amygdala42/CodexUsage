# 测试 / Testing

## 1.1.0 — 2026-10-02

正式发行候选 `output/2026-10-02/batch-008` 通过默认完整打包：构建成功，All **278 项通过、0 失败**（Domain 37、Bridge 14、ResetFeed 46、Theme 27、Layout 121、Stacking 20、Package 13），未跳过测试。程序的文件、程序集和应用清单版本统一为 **1.1.0.0**。打包测试夹具从应用清单读取版本，避免升版后仍引用旧版。

Release candidate `output/2026-10-02/batch-008` completed default full packaging: the build succeeded and All passed **278 checks, zero failures** (Domain 37, Bridge 14, ResetFeed 46, Theme 27, Layout 121, Stacking 20, Package 13), with no tests skipped. Executable file, assembly and application manifest versions are **1.1.0.0**. Packaging fixtures read their version from the application manifest so upgrades do not retain a hard-coded older version.

本次验证覆盖下述认可配色、合成数据位图和打包流程；中英文 Plus/Pro 展示图已更新为 v1.1.0。未执行原生 GUI 全交互、真实 Plus 账号或多屏幕/DPI 验收；Windows PowerShell 5.1 运行兼容性的历史未验范围仍保留。

This run verifies the accepted palette below, synthetic bitmap rendering and packaging. Bilingual Plus/Pro illustrations now show v1.1.0. Native GUI interaction, live Plus accounts and multi-display/DPI acceptance were not performed; the historical Windows PowerShell 5.1 runtime verification limitation remains.

## 2026-10-02 浅色绿色对调验证 / Light green swap verification

深色任务栏百分比保留原有近白色 `#E6EDF5`。浅色额度饼图、弹窗额度数字及进度条精确使用 `#4B885F`；时间饼图、任务栏倒计时、弹窗重置文字及链接精确使用 `#25663B`。任务栏上方百分比与加载前的横杠 `—` 使用纯黑 `#000000`，既有主题测试核对二者的实际渲染像素，以及倒计时与饼图同色。黑色断言在修改前为 26 通过、1 失败，修改后 Theme **27 项通过、0 失败**。浅绿弹窗额度强调的对比度检查仍单独采用 3:1 下限；其他文字仍检查 4.5:1，不声称全部文字达到原目标。浅色最低语义文字／图形对比度均为 3.05:1，两个饼图明暗比为 1.64:1。已查看合成数据位图；未执行本次构建的原生 GUI 交互验收。完整打包的实际结果以相应交付批次的 `manifest.json` 和 `tests.log` 为准。

The dark taskbar percentage retains near-white `#E6EDF5`. Light quota disks, popup quota values and progress bars use `#4B885F`; time disks, taskbar countdowns, popup reset text and links use `#25663B`. Taskbar percentages and the loading quota placeholder `—` use pure black `#000000`. Existing Theme checks inspect their rendered pixels and confirm that countdowns still match the time disks. The black-text assertion produced 26 passes and 1 failure before the change, and all **27 pass** afterward. Only the light popup quota accent retains a 3:1 contrast floor; other text retains 4.5:1 checks. This does not establish the original target for every text colour. Light minimum text and graphical contrast are both 3.05:1, with a 1.64:1 disk-to-disk ratio. Synthetic previews were inspected; native GUI interaction was not performed. Each delivery's `manifest.json` and `tests.log` record its full packaging result.

## 2026-10-02 目录迁移验证 / Layout migration verification

在新位置使用 PowerShell 7.6.5 执行构建与完整打包，All **278 项通过、0 失败**（原应用 265 项、打包回归 13 项）。`output/2026-10-02/batch-001` 完整测试通过；第二批通过旧打包入口调用、显式跳过重复测试，仅用于验证编号递增和第一批不被覆盖。两批的程序、ZIP、校验和及 39 个输入文件哈希均已核对。程序已从第一批新位置恢复运行，设置文件内容未变；这不等同于 GUI 全交互验收。

Build and full packaging passed all **278 checks, zero failures** under PowerShell 7.6.5 at the new location. Batch 001 completed all tests. Batch 002 explicitly skipped repeated tests to verify the legacy forwarding entry, batch increment and preservation of batch 001. Both deliveries passed artifact, ZIP and 39-file input-hash checks. Restoring the application process with unchanged settings is not full GUI acceptance.

Windows PowerShell 5.1 的 25 个当前/适配脚本通过语法解析；其执行策略为 Restricted，未改变策略，也未运行这些脚本。该宿主的运行兼容性仍未验证。详细本地证据在 `records/organization/2026-10-02/batch-001/`，不随源码上传。

Windows PowerShell 5.1 parsed 25 current/adapted scripts without syntax errors. Its Restricted policy was preserved, so runtime compatibility on that host remains unverified. Detailed local evidence is retained under `records/organization/2026-10-02/batch-001/` and excluded from source publication.

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
./scripts/test.ps1 -Suite Package
./scripts/test.ps1 -Suite All

# 只编译 / Compile only
./scripts/test.ps1 -Suite All -BuildOnly
```

`All` 包含 Domain、Bridge、ResetFeed、Theme、Layout、Stacking 和 Package。`-BuildOnly` 只编译所选 C# 套件，不执行测试；Package 是 PowerShell 套件，指定 `-BuildOnly` 时跳过。Bridge 包含真实 35 秒超时检查，ResetFeed 包含真实 HTTP 超时检查，请等待最终统计。Theme 将合成数据的真实绘图产物写入 `build/tests/theme-render/`；Layout 将逐项字体测量写入 `build/tests/layout-compact/`。

`All` runs Domain, Bridge, ResetFeed, Theme, Layout, Stacking and Package. `-BuildOnly` compiles the selected C# suites without running tests; the PowerShell Package suite is skipped. Bridge checks the real 35-second deadline, and ResetFeed exercises HTTP timeouts; wait for the final totals. Theme writes production-renderer bitmaps using synthetic data to `build/tests/theme-render/`; Layout writes individual font measurements to `build/tests/layout-compact/`.

`scripts/package.ps1` 默认执行构建与 `All`，并将日志和执行状态保存在独立的 `output/YYYY-MM-DD/batch-NNN/`。`-SkipTests` 是显式跳过，不代表测试通过。目录迁移前的原始测试产物与记录保存在 `records/verification/`，发布核验在 `records/publishing/`；历史记录中的旧绝对路径保持原文，不作为当前命令入口。

`scripts/package.ps1` builds and runs `All` by default, storing logs and execution status in a separate `output/YYYY-MM-DD/batch-NNN/`. `-SkipTests` is an explicit skip, not a passing test result. Original pre-migration test products and records are retained in `records/verification/`, with publication evidence in `records/publishing/`. Old absolute paths in historical evidence remain unchanged and are not current command entry points.

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
| Package | 独立文件夹内的构建/测试输出、任意工作目录、上海日期边界、默认执行 All、文件校验、输入追溯、ZIP 完整性、每日批次递增、显式跳过测试、失败留痕及互斥锁 |

Domain covers parsing, calculations, settings, and runtime data paths. Bridge launches a fake server to exercise protocol and process handling. ResetFeed uses a loopback server to test the production HTTP and cache implementation without external service dependencies.

Domain additionally checks old/invalid theme preferences and light/dark persistence without changing other settings. Theme checks real bitmap output and semantic text contrast in both palettes. Its 100/150/200% image scaling checks do not establish native Windows DPI, menu or window-layout behavior.

Layout 使用实际生产布局与 GDI 字体测量检查 Plus/Pro 中英文文案，按钮每边额外预留 4 个逻辑像素。它不创建原生控件，也不代表真实 Plus/Pro 账号登录、窗口点击或 Windows DPI 验收。未知超长额度名称继续省略显示；没有承诺任意标签都能完整显示。

Layout uses production geometry and GDI font measurements for Chinese/English Plus and Pro labels, with an extra four logical pixels reserved at each button edge. It does not create native controls or establish live-account, click or Windows DPI acceptance. Unknown long quota labels retain ellipsis; arbitrary labels are not guaranteed to fit in full.

Stacking 使用纯委托测试实际生产的层级决策、事件筛选与队列合并；不创建窗口或调用原生置顶。已有进程的只读窗口状态与事件采样是独立现场证据，不等同于修复后视觉验收。

Stacking tests production ordering decisions, event filtering and queue coalescing with delegates; it creates no windows and performs no native raises. Read-only observation of the existing process is separate live evidence, not post-fix visual acceptance.

Package 的 13 项检查使用 `build/tests/package/run-<唯一编号>/` 下的隔离源码与脚本夹具，验证打包流程，不启动应用 GUI、不访问真实账号。夹具中的测试入口用于检查 All 调用及失败传播，不递归运行 Package 本身。程序原有六个套件共 265 项，新增打包回归 13 项（含非公历区域设置）；数量表示套件组成，本轮运行是否通过以实际日志为准，不能沿用下方历史通过记录。

Package defines 13 checks using isolated source and script fixtures under `build/tests/package/run-<unique-id>/`. It verifies packaging without starting the application GUI or accessing real accounts. Its fixture test command checks the All invocation and failure propagation without recursively running Package. The six existing application suites contain 265 checks, with 13 additional packaging checks including non-Gregorian host cultures; these counts describe suite composition. Results for a new run must come from its actual logs, not the historical passing results below.

## 结果 / Results

2026-10-02 深色青蓝配色发布前复验：在最终生产源码上重新运行 All，**265 项通过、0 失败**。随后仅整理正式发行文档和包内说明；EXE 沿用已验证的 1.0.4.0 构建，SHA-256 为 `62640448BF70090A43F0A157747B4538EBBFCABF5B5C337885BA446081C69C99`。以下本地修订记录保留为历史验证过程，原生 GUI 未验范围不变。

The 2026-10-02 dark cyan-blue release check reran All against the final production source: **265 checks passed, zero failures**. Subsequent changes only prepare release documentation and package notes; the release reuses the verified 1.0.4.0 executable with the SHA-256 above. Local revision records below describe historical validation, and native GUI verification limits are unchanged.

2026-10-02 本地深色配色修订（仍为 1.0.4）：任务栏与弹窗的主次强调统一为桌面图标的 #3ABED7 / #339AC5。深色用实际像素与弹窗主题色的精确色值断言替换之前自设的色相/明暗差距约束，浅色区分度断言保持不变；旧配色为 26 通过/1 失败，更新后 Theme 27 项全过，构建及 All **265 项通过、0 失败**。深色最低语义文字对比度 4.65:1、图形对比度 3.61:1，两色明暗比 1.45:1；浅色仍为 4.77:1、3.05:1、1.64:1。浅色浮条、浅色托盘位图和桌面 ICO 与改前逐字节一致，背景、布局和功能不变；预览已检查，原生 GUI 交互未执行。

The 2026-10-02 local dark-palette revision retains version 1.0.4 and uses desktop icon colours #3ABED7 / #339AC5 for taskbar and popup accents. Exact rendered-pixel and popup-colour assertions replace the previous custom dark hue/separation targets; light-mode checks are unchanged. The old palette gave 26 passes/1 failure; Theme now passes all 27 checks, and the build and All passed **265 checks, zero failures**. Dark minima are 4.65:1 text and 3.61:1 graphics, with a 1.45:1 disk-to-disk ratio; light minima remain 4.77:1 and 3.05:1, with a 1.64:1 pair ratio. Light widget/glyph bitmaps and the desktop ICO are byte-identical to the previous revision. Backgrounds, layout and functionality are unchanged; previews were inspected and native GUI interaction was not executed.

2026-09-30 v1.0.4 配色更新发布前复验：在最终双蓝/双绿生产源码上重新运行 All，**265 项通过、0 失败**（Domain 37、Bridge 14、ResetFeed 46、Theme 27、Layout 121、Stacking 20）。本次随后仅整理发行文档与程序包说明，复用已验证的 1.0.4.0 EXE，SHA-256 为 `D7E44FC40230ADE214CDCC0AAE39472A5B5F67B9428C93A6AAB3BC97CD4FA8A3`。以下本地修订记录保留为历史验证过程。

The 2026-09-30 v1.0.4 palette-update release check reran All against the final blue/green production source: **265 checks passed, zero failures** (Domain 37, Bridge 14, ResetFeed 46, Theme 27, Layout 121, Stacking 20). Subsequent changes only prepare release documentation and package notes; the release reuses the verified 1.0.4.0 executable with the SHA-256 above. The local revision records below document historical validation steps.

2026-09-30 本地蓝绿配色色差修订（仍为 1.0.4）：深色双蓝为 #3E86E3 / #86B4F2，浅色双绿为 #25663B / #4B885F。加严既有实际像素区分度断言后，上一配色为 25 通过/2 失败，新版 Theme 27 项全过；构建和 All **265 项通过、0 失败**。两填色的明暗比分别从约 1.20/1.08 提升到 1.72/1.64；这是本次视觉区分目标，不是无障碍标准。图形对轨道/背景最低为深色 3.15:1、浅色 3.05:1，语义文字仍为 4.79:1/4.77:1。背景、文字和布局保持不变，进度条随额度主色更新；已检查位图并完成独立源码审查，未执行原生 GUI 交互。

The local blue/green separation revision on 2026-09-30 retains version 1.0.4. Dark disks use #3E86E3 / #86B4F2; light disks use #25663B / #4B885F. Tightened rendered-pixel separation assertions produced 25 passes/2 failures on the previous palette and 27 passes on this palette. The build and All passed **265 checks, zero failures**. Disk-to-disk lightness ratios increased from about 1.20/1.08 to 1.72/1.64; these are design targets, not accessibility standards. Graphical contrast against tracks/surfaces is at least 3.15:1 dark and 3.05:1 light; tested text minima remain 4.79:1/4.77:1. Backgrounds, text and layout are unchanged; progress bars follow the quota accent. Bitmaps and source were reviewed; native GUI interaction was not executed.

2026-09-30 本地蓝绿配色修订（仍为 1.0.4）：在统一配色基础上，深色饼图改为更鲜明的 #3E86E3 / #5294F0，浅色改为 #388353 / #47875E；额度文字、倒计时、链接与菜单同步采用各自蓝色或绿色色系。背景、布局和功能不变，文档示意图同步重绘。构建及 All **265 项通过、0 失败**（Domain 37、Bridge 14、ResetFeed 46、Theme 27、Layout 121、Stacking 20），其中 Plus/Pro 双语字体与布局检查继续通过。实际渲染图形最低对比度为深色 3.15:1、浅色 3.11:1，所测语义文字最低为深色 4.79:1、浅色 4.77:1。已查看位图并独立审查配色应用范围；未执行本构建的原生 GUI 交互。

The local blue/green revision on 2026-09-30 retains version 1.0.4. Dark disks use richer blues #3E86E3 / #5294F0; light disks use greens #388353 / #47875E. Quota text, countdowns, links and menus use matching blue or green shades. Backgrounds, layout and functionality are unchanged, and documentation illustrations were regenerated. The build and All passed **265 checks, zero failures** (Domain 37, Bridge 14, ResetFeed 46, Theme 27, Layout 121, Stacking 20), including bilingual Plus/Pro font and layout checks. Minimum measured graphical contrast is 3.15:1 dark and 3.11:1 light; tested semantic text minima are 4.79:1 dark and 4.77:1 light. Bitmaps were inspected and palette usage independently reviewed; native GUI interaction with this build was not executed.

2026-09-30 本地统一配色修订（仍为 1.0.4）：重整深浅主题的背景、正文、主次蓝色强调与警告色，进度条使用额度饼图主色，下拉活动行增加强调边框。新增图形对比度回归并替换过时的固定色样断言，旧版为 23 通过/4 失败（包括深色图形 1.38:1、浅色 2.43:1）；最终构建和 All **265 项通过、0 失败**：Domain 37、Bridge 14、ResetFeed 46、Theme 27、Layout 121、Stacking 20。实际渲染填色相对轨道/背景的最低对比度为深色 4.80:1、浅色 3.32:1，所测正文/语义文字最低为深色 4.80:1、浅色 5.01:1。已查看位图和完成独立配色/源码审查；本构建仍未运行原生 GUI 交互。

The local unified-palette revision on 2026-09-30 retains version 1.0.4. Surfaces, text, blue accents and warning colours are coordinated; progress bars use the quota disk accent and active dropdown rows gain an accent outline. New graphical-contrast regressions and updated hue checks produced 23 passes/4 failures on the previous palette, including graphical ratios of 1.38:1 dark and 2.43:1 light. The final build and All passed **265 checks, zero failures**: Domain 37, Bridge 14, ResetFeed 46, Theme 27, Layout 121, Stacking 20. Minimum measured rendered-fill contrast against tracks/surfaces is 4.80:1 dark and 3.32:1 light; tested semantic text minima are 4.80:1 dark and 5.01:1 light. Bitmaps and code were independently reviewed; native GUI interaction with this build was not executed.

检查目标为文字至少 4.5:1、有效图形至少 3:1，参考 W3C 的[文字对比度说明](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html)与[非文字对比度说明](https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast.html)。检查对象为本套件覆盖的颜色组合和绘图产物，不是整个原生应用的无障碍认证。

The checks target 4.5:1 text and 3:1 meaningful graphical contrast, following the linked W3C guidance. They cover the tested colour combinations and rendered graphics, not full accessibility certification of the native application.

2026-09-30 本地四色配色修订（仍为 1.0.4，未替换发行附件）：深色饼图 #191970/#0047AB，浅色 #1E90FF/#87CEEB；详情进度条分别为 #0047AB/#87CEEB。其余文字、背景、布局和原过期状态颜色保持不变。All 263 项通过后，按最终要求保留原倒计时文字色并更新详情进度条，重新构建与 Theme 25/25 检查均通过。最终最低文字对比度为深色 6.24:1、浅色 4.81:1；已检查配色位图，原生 GUI 交互未执行。指定的 MidnightBlue 饼图本身偏暗，文字对比度检查不代表图形对比度达到同一数值。

The local four-colour revision on 2026-09-30 retains version 1.0.4 and has not replaced release assets. Dark disks use #191970/#0047AB; light disks use #1E90FF/#87CEEB. Detail progress bars use #0047AB/#87CEEB. Other text, backgrounds, layout and stale-state colours are unchanged. After All passed 263 checks, the final countdown text was kept unchanged and detail progress fills updated; the final build and Theme 25/25 passed again. Minimum text contrast is 6.24:1 dark and 4.81:1 light. Rendered bitmaps were inspected; native GUI interaction was not executed. The specified MidnightBlue disk is deliberately dark; text contrast results do not describe graphical contrast.

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
