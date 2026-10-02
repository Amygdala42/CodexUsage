# CodexUsage

[简体中文](README.md) | [English](README.en.md)

轻量 Windows Codex 额度小窗，在任务栏通知区旁查看剩余额度和重置倒计时。

**[下载 v1.1.0 · CodexUsage.exe](https://github.com/Amygdala42/CodexUsage/releases/download/v1.1.0/CodexUsage.exe)** · [ZIP 程序包](https://github.com/Amygdala42/CodexUsage/releases/download/v1.1.0/CodexUsage-Windows-x64.zip) · [更新说明](https://github.com/Amygdala42/CodexUsage/releases/tag/v1.1.0)

适用于 Windows x64。

v1.1.0 配色（2026-10-02）：深色采用深蓝灰背景与桌面图标一致的双青蓝强调，深色任务栏百分比保留近白色。浅色采用柔和浅灰背景：额度饼图、弹窗额度文字和进度条统一使用 #4B885F；时间饼图、任务栏倒计时、弹窗重置文字和链接统一使用 #25663B。浅色任务栏上方额度百分比与加载前占位横杠使用纯黑 #000000；普通正文仍使用深色文字，过期与错误状态保留中性灰和琥珀色语义提示。

![CodexUsage 任务栏小条](assets/widget-preview.png)

## 功能

- 上行圆盘和百分比表示剩余额度，下行圆盘和倒计时表示同一周期的剩余时间。
- 按实际返回显示额度卡片；Plus、Pro 使用相同规则，已移除 Spark 额度。
- 每 5 分钟自动刷新，也可手动刷新。
- 显示最近公共重置公告的时间和类型，点击来源查看原文；公告不代表个人账号到账。
- 中英文切换并保存偏好；应用尺寸固定 100%，跟随 Windows 系统 DPI。
- 深色／浅色模式即时切换，自动记住选择；旧设置默认沿用深色。

## 界面示例

以下为 v1.1.0 布局示意图，使用示例数据，并非真实账号截图；额度项目以账号实际返回为准。

### Plus

![Plus 界面示例](assets/plus-preview-zh.png)

### Pro

![Pro 界面示例](assets/pro-preview-zh.png)

### 深色与浅色

下图使用程序实际绘图代码和示例数据，展示两种主题及正常、过期、失败、更新中状态。

![深色与浅色绘图预览](assets/appearance-preview.png)

## 下载与运行

需要 Windows x64、.NET Framework 4.8 或更新的 4.x 版本，以及已安装并登录订阅账号的 Codex。

1. 在 Releases 中下载 `CodexUsage.exe`，或下载 `CodexUsage-Windows-x64.zip` 后解压。
2. 运行 `CodexUsage.exe`。点击额度条查看详情，鼠标移开后自动收起。

悬停额度条可查看简要说明，移开或点击即关闭；右键打开菜单。详情顶部可查看版本号，并打开 GitHub 项目主页。

切换外观：点击详情页中的“浅色模式／深色模式”按钮，或在额度条／托盘图标上右键，选择“外观 → 深色模式／浅色模式”。语言、主题和额度选择位于同一行。深色以深蓝灰背景配与桌面图标一致的双青蓝饼图（#3ABED7 / #339AC5），任务栏百分比保留近白色。浅色以柔和浅灰背景配两种绿色：额度饼图、弹窗额度文字和进度条统一为 #4B885F；时间饼图、任务栏倒计时、弹窗重置文字和链接统一为 #25663B。任务栏上方额度百分比及加载前占位横杠为纯黑 #000000；普通正文保持深色文字，过期和错误状态保留中性灰与琥珀色提示。主题切换无需重启；下次启动恢复上次选择。

升级时先从托盘退出旧版，再替换 EXE。设置保存在 `%LOCALAPPDATA%\CodexUsage`，首次运行会导入同目录旧版的设置。

验证范围见 [测试说明](docs/TESTING.md)。

## 开发

当前版本为 **v1.1.0**（2026-10-02），EXE 文件与程序集版本为 **1.1.0.0**。包含深浅主题切换、单行设置布局、通信输入编码与公告缓存修复、任务栏遮挡恢复，以及浅色配色更新。完整打包验证中 All **278 项通过**；原生 GUI、真实 Plus 账号及其他多屏幕/DPI 场景的验证范围见 [测试说明](docs/TESTING.md)。

```powershell
./scripts/build.ps1
./scripts/test.ps1 -Suite All
# 编译、执行全部测试并生成独立交付批次
./scripts/package.ps1
```

临时编译与测试产物分别位于 `build/app/`、`build/tests/`；软件交付位于 `output/YYYY-MM-DD/batch-NNN/`，按 Asia/Shanghai 日期和当日递增批次生成，不覆盖已有批次。程序名为 `CodexUsage.exe`，当前版本为 1.1.0.0。`env/` 只用于实际环境，不放交付、报告或 Git 工作树；目前构建使用系统环境，无需在仓库中安装独立环境。

目录约定见 [项目规则](PROJECT_RULES.md)。

[构建说明](docs/BUILD.md) · [测试说明](docs/TESTING.md) · [隐私说明](docs/PRIVACY.md)

## 许可

[MIT](LICENSE) · Amygdala42
