# 隐私与运行数据 / Privacy

## 数据请求 / Data requests

CodexUsage 通过本机 Codex `app-server` 请求 `account/read`（`refreshToken: false`）和 `account/rateLimits/read`，获取套餐、额度和重置时间。应用不发起模型对话、额度重置或购买请求，也不自行读取或保存登录凭据；账号响应在内存中处理。

启动参数请求关闭 Codex 分析上报并减少常规日志。联网、登录状态及 Codex 自身文件由已安装的 Codex 处理，具体行为取决于其版本和配置。

CodexUsage reads account and quota information through the local Codex app-server. It does not start model conversations, request quota resets or purchases, or read and store login credentials itself. Responses are processed in memory. Codex handles service access, authentication state, and its own local files.

## 本地文件 / Local files

数据保存在当前 Windows 用户的 `%LOCALAPPDATA%\CodexUsage`：

| 相对数据目录的路径 | 内容 |
| --- | --- |
| `settings.json` | 语言、所选额度窗口和显示设置 |
| `tmp/CodexQuotaLite/` | 子进程工作目录及临时文件 |
| `logs/application.log` | UTC 时间和异常类型 |
| `logs/placement.json` | 任务栏、通知区和小条的位置与尺寸 |
| `logs/CodexQuotaLite/` | Codex 子进程的日志目录 |

应用错误日志不保存令牌、邮箱、原始账号响应、服务错误原文或完整堆栈。Codex 子进程的标准错误输出不保存。移动 EXE 不影响设置。

Settings, temporary files, application error logs and taskbar geometry are stored in `%LOCALAPPDATA%\CodexUsage`. Application error logs omit credentials, raw account responses, service messages, and stack traces. Moving the EXE does not affect settings.

首次运行时，若新位置还没有设置，会从 EXE 旁旧 `env/config/CodexQuotaLite/settings.json` 导入已知设置项。不会迁移日志或其他文件，也不会自动删除旧目录。迁移后，确认旧 `env` 仅用于本应用即可自行删除。

On first run, existing settings beside the EXE are imported if no settings exist at the new location. Logs and unrelated files are not copied, and the old directory is left intact. After migration, the old `env` can be removed if it belongs only to this app.

## 共享资料 / Sharing

分享诊断资料前，请检查并移除个人信息；不要公开凭据或个人运行数据。项目预览使用合成示例数据。

Review diagnostics for personal information before sharing. Keep credentials and personal runtime data private. Project previews use synthetic example values.
