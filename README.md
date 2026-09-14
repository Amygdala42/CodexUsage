# CodexUsage

[简体中文](README.md) | [English](README.en.md)

轻量 Windows Codex 额度小窗，在任务栏通知区旁查看剩余额度和重置倒计时。

**[下载 CodexUsage.exe](https://github.com/Amygdala42/CodexUsage/releases/download/v1.0.3/CodexUsage.exe)** · [发行版与更新说明](https://github.com/Amygdala42/CodexUsage/releases)

适用于 Windows x64。

![CodexUsage 任务栏小条](assets/widget-preview.png)

## 功能

- 上行青色圆盘和百分比表示剩余额度，下行蓝色圆盘和倒计时表示同一周期的剩余时间。
- 按实际返回显示额度卡片；Plus、Pro 使用相同规则，已移除 Spark 额度。
- 每 5 分钟自动刷新，也可手动刷新。
- 显示最近公共重置公告的时间和类型，点击来源查看原文；公告不代表个人账号到账。
- 中英文切换并保存偏好；应用尺寸固定 100%，跟随 Windows 系统 DPI。

## 界面示例

以下为 v1.0.3 布局示意图，使用示例数据；额度项目以账号实际返回为准。

### Plus

![Plus 界面示例](assets/plus-preview-zh.png)

### Pro

![Pro 界面示例](assets/pro-preview-zh.png)

## 下载与运行

需要 Windows x64、.NET Framework 4.8 或更新的 4.x 版本，以及已安装并登录订阅账号的 Codex。

1. 在 Releases 中下载 `CodexUsage.exe`，或下载 `CodexUsage-Windows-x64.zip` 后解压。
2. 运行 `CodexUsage.exe`。点击额度条查看详情，鼠标移开后自动收起。

悬停额度条可查看简要说明，移开或点击即关闭；右键打开菜单。详情顶部可查看版本号，并打开 GitHub 项目主页。

升级时先从托盘退出旧版，再替换 EXE。设置保存在 `%LOCALAPPDATA%\CodexUsage`，首次运行会导入同目录旧版的设置。

验证范围见 [测试说明](docs/TESTING.md)。

## 开发

```powershell
./scripts/build.ps1
```

[构建说明](docs/BUILD.md) · [测试说明](docs/TESTING.md) · [隐私说明](docs/PRIVACY.md)

## 许可

[MIT](LICENSE) · Amygdala42
