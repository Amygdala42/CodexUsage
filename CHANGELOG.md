# Changelog

## 未发布 / Unreleased — 2026-10-03 本地审查修复

以下为未发布的源码修复，版本仍为 1.1.0；2026-10-02 发布的 v1.1.0 下载附件不包含这些修复。
These unreleased source fixes retain version 1.1.0; the v1.1.0 download assets published on 2026-10-02 do not include them.

- 点击详情或菜单的“立即刷新”会立即重新验证公共 reset 公告，保留自动请求的缓存与重试间隔，以及并发和取消保护。
  Refresh from details or the menu immediately revalidates public reset news, preserving automatic cache/retry intervals and concurrent-request/cancellation protections.
- 损坏设置中的字段类型或数值转换失败时回退默认配置，启动阶段统一处理异常；保留合法旧设置。
  Fall back to defaults when settings contain invalid field types or numeric conversions, and handle startup errors consistently while preserving valid legacy settings.
- Codex 响应采用有界流式读取，在收到换行前也能拒绝超大响应，保留分片 UTF-8、取消和超时处理。
  Bound streamed Codex replies before a newline arrives, preserving fragmented UTF-8, cancellation and timeout handling.
- 键盘操作详情时保持显示，恢复鼠标操作后沿用离开半秒收起；副屏全屏不再隐藏未被覆盖的浮条。
  Keep details open during keyboard interaction, restore the half-second pointer-leave behavior on mouse use, and avoid hiding the widget for fullscreen content on a different display.
- 详情刷新状态提供随主题绘制的完整错误提示；移除未使用的滚动面板和重复状态赋值。
  Show complete error details in a themed status tooltip; remove the unused scroll panel and redundant status assignment.
- 构建、测试和打包统一协调共享产物，交付时核对复制后的程序、输入与清单；新增针对性回归及 UiBehavior 套件。
  Coordinate shared outputs across build, test and package commands, verify the copied executable and input manifest, and add targeted regressions plus the UiBehavior suite.

## 1.1.0 — 2026-10-02

- 深色任务栏百分比保留原有近白色 `#E6EDF5`，同步恢复文档示意图。
  Keep the dark taskbar percentage in its original near-white `#E6EDF5` and restore documentation previews.
- 浅色模式原有两种绿色对调：额度饼图、弹窗额度数字及进度条统一为 `#4B885F`；时间饼图、任务栏倒计时、弹窗重置文字及链接统一为 `#25663B`。任务栏上方百分比及加载前的横杠 `—` 改为纯黑 `#000000`，背景和其他状态提示保持原样。
  Swap the original light-mode greens: quota disks, popup quota values and progress bars use `#4B885F`; time disks, taskbar countdowns, popup reset text and links use `#25663B`. Use pure black `#000000` for taskbar percentages and the loading quota placeholder `—`; preserve backgrounds and other status indicators.
- 软件升级为 v1.1.0，文件、程序集和应用清单版本统一为 1.1.0.0；程序与 ZIP 名称继续不含版本号。
  Update the software to v1.1.0 and align file, assembly and manifest versions at 1.1.0.0; executable and ZIP names remain versionless.
- 编译和测试输出迁至 `build/app/`、`build/tests/`；`env/` 仅用于实际环境。交付、历史版本、核验记录和工作树分别归入 `output/`、`history/`、`records/`、`.worktrees/`，保留历史证据原文与迁移映射。
  Move build/test output to `build/app/` and `build/tests/`, reserving `env/` for actual environments. Separate deliveries, history, evidence and worktrees into `output/`, `history/`, `records/` and `.worktrees/`, preserving original evidence and migration mappings.
- 新增 `scripts/package.ps1`，默认构建并执行 All，以 Asia/Shanghai 日期和每日 `batch-NNN` 生成独立软件交付；程序与 ZIP 名称不含版本，不覆盖旧批次，失败状态和显式跳过测试均记录在清单中。
  Add `scripts/package.ps1` to build, run All and create deliveries by Asia/Shanghai date and daily `batch-NNN`. Executable and ZIP names contain no version suffix; existing batches are never replaced, and the manifest records failures and explicit test skips.
- 同步目录规则、构建/测试说明、忽略规则和中英文预览图；以新的 v1.1.0 发行提供程序，保留 v1.0.4 历史附件。
  Update directory rules, build/test documentation, ignore rules and bilingual previews; deliver a new v1.1.0 release while preserving historical v1.0.4 assets.

## 1.0.4 — 2026-09-30（更新 / Updated 2026-10-02）

- 深色模式的任务栏饼图、弹窗额度数字、进度条、倒计时和链接统一采用桌面图标的两种青蓝：主色 #3ABED7、次色 #339AC5。浅色模式、各背景、布局、功能与版本号保持不变。
  Match dark-mode taskbar disks, popup quota values, progress bars, countdowns and links to the desktop icon: primary #3ABED7 and secondary #339AC5. Light mode, backgrounds, layout, functionality and version are unchanged.

- 针对任务栏周期遮挡额度浮条的层级闪烁：监听桌面层级变化，合并排队后仅在任务栏确实位于浮条上方时恢复；保留每秒兜底检查，稳定状态不再反复置顶。
  Respond to desktop reorder events when the taskbar covers the widget, coalesce checks and raise only when needed; retain the one-second fallback without redundant stable-state raises.
- 新增深色／浅色模式切换：详情页按钮与右键“外观”菜单均可操作，立即更新配色并保存偏好；旧配置默认深色。
  Add instant dark/light mode switching from the details button or Appearance menu, with a saved preference and dark defaults for existing settings.
- 语言、主题和额度选择合并为同一行；浅色使用柔和的中性浅灰背景。
  Put language, theme and quota selection on one row. Light mode uses a soft neutral-gray background.
- 固定 Codex 通信输入编码，避免宿主的 UTF-8 BOM 导致握手失败；加强协议错误测试，防止提前断线被误判为通过。
  Make Codex input independent of the host's UTF-8 BOM and ensure protocol tests reach the intended error branch.
- 正确处理公共公告 304 缓存响应、日期形式的重试间隔和取消；修复缓存头大小写/引号解析及坏响应提前延长缓存的问题，新增离线 HTTP 回归套件。
  Handle announcement 304 responses, HTTP-date retry delays and cancellation; accept cache directive case/quotes and avoid extending freshness after invalid responses, with an offline HTTP regression suite.
- 普通定时刷新保留正在操作的额度下拉框，仅在内容或布局改变、窗口移动或隐藏时关闭。
  Keep the usage dropdown open on ordinary refresh; dismiss it when content or layout changes or the window moves or hides.

## 1.0.3 — 2026-09-15

- 移除全部 Spark 额度，旧选择自动回到可用额度。
  Remove Spark quotas and fall back to an available quota window.
- 增加公共重置公告的时间、类型和独立来源链接，支持中英文及本地缓存。
  Add public reset announcements with time, type, a separate source link, bilingual text and caching.
- 移除详情页白色悬浮提示，放大公告文字。
  Remove detail tooltips and enlarge the announcement text.

## 1.0.2 — 2026-09-09

- 详情标题右侧新增版本号和 GitHub 主页链接。
  Add the version and a GitHub project link beside the detail title.
- 调整标题行对齐与链接宽度，完整显示“GITHUB主页”。
  Align the header text and give the project link enough room.

## 1.0.1 — 2026-09-09

- 额度条说明支持未激活窗口悬停显示；移开或按下鼠标立即关闭，点击后需要移出再悬停才重新显示。
  Show the widget tooltip on hover without activation; dismiss it on pointer leave or mouse press, and suppress it until the pointer leaves after a click.

- 前台窗口切换、右键菜单关闭和详情失去焦点后立即检查额度条层级，减少点击任务栏后被短暂遮挡的情况。
  Restore widget ordering after foreground changes, menu dismissal and detail deactivation instead of waiting for the next timer tick.

- 刷新状态使用固定页脚，详情始终贴齐额度条，避免刷新后留下空隙。
  Keep refresh status in a fixed footer and anchor details to the widget.
- 鼠标离开详情和额度条后约半秒自动收起，操作下拉菜单时保持显示。
  Dismiss details after the pointer leaves for half a second, while allowing dropdown interaction.
- 数据改存到用户应用数据目录，首次运行导入旧版设置。
  Store runtime data under Local AppData and import existing settings on first run.
- 刷新时保留已有倒计时，过滤任务栏短暂隐藏信号并减少背景擦除。
  Preserve the countdown during refresh, filter transient taskbar visibility changes, and avoid background erasure.

## 1.0.0 — 2026-09-09

- 显示 Codex 账号套餐及实际返回的额度窗口。
  Display the Codex account plan and available quota windows.
- 任务栏双圆盘展示同一周期的剩余额度和重置倒计时，点击查看详情。
  Show remaining quota and reset time for the same window in a two-disk taskbar widget, with click-to-open details.
- 支持中文与 English 切换、每 5 分钟自动刷新和手动刷新。
  Support Chinese and English, automatic refresh every five minutes, and manual refresh.
- 提供托盘显示与隐藏，设置保存在 EXE 相邻的 `env/`。
  Show or hide the widget from the tray and save settings in `env/` beside the EXE.
