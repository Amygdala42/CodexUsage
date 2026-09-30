# Changelog

## 1.0.4 本地配色修订 / Local palette revision — Unreleased

- 统一本地深浅模式配色：深色为深蓝灰底与更鲜明的双蓝（#3E86E3 / #5294F0），浅色为柔和浅灰底与双绿（#388353 / #47875E）；额度数字、饼图、倒计时、进度条与菜单在各自主题中保持同一色系，文字使用更易读的同系色阶，警告使用独立琥珀色。下拉列表活动行增加强调边框，验证文字与图形对比度；布局、功能与版本号不变。
  Unify dark/light palettes: dark slate surfaces with richer blues (#3E86E3 / #5294F0), and gentle gray surfaces with greens (#388353 / #47875E). Quota values, disks, countdowns, progress bars and menus share a colour family within each theme, with readable text shades and separate amber warnings. Add an accent outline to active dropdown rows and verify text/graphic contrast; layout, functionality and version are unchanged.

## 1.0.4 — 2026-09-30

- 针对任务栏周期遮挡额度浮条的层级闪烁：监听桌面层级变化，合并排队后仅在任务栏确实位于浮条上方时恢复；保留每秒兜底检查，稳定状态不再反复置顶。
  Respond to desktop reorder events when the taskbar covers the widget, coalesce checks and raise only when needed; retain the one-second fallback without redundant stable-state raises.
- 新增深色／浅色模式切换：详情页按钮与右键“外观”菜单均可操作，立即更新配色并保存偏好；旧配置默认深色。
  Add instant dark/light mode switching from the details button or Appearance menu, with a saved preference and dark defaults for existing settings.
- 语言、主题和额度选择合并为同一行；浅色使用柔和的中性浅灰背景，两个饼图采用浅蓝 #7BBDFF 与蓝色 #1C8DFF，深色采用浅绿与更浅绿配色。
  Put language, theme and quota selection on one row. Light mode uses a soft neutral-gray background with #7BBDFF and #1C8DFF blue disks; dark mode uses light green and lighter green disks.
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
