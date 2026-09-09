# Changelog

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
