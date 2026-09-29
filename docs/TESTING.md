# 测试 / Testing

## 1.0.4（未发布 / Unreleased）

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
./scripts/test.ps1 -Suite All

# 只编译 / Compile only
./scripts/test.ps1 -Suite All -BuildOnly
```

`All` 包含 Domain、Bridge 和 ResetFeed。`-BuildOnly` 编译所选套件，不执行测试。Bridge 包含真实 35 秒超时检查，ResetFeed 包含真实 HTTP 超时检查，请等待最终统计。

`All` runs Domain, Bridge and ResetFeed. `-BuildOnly` compiles the selected suites without running them. Bridge checks the real 35-second deadline, and ResetFeed exercises HTTP timeouts; wait for the final totals.

脚本需在本机策略允许的 PowerShell 中运行。若系统明确拒绝脚本或测试程序，停止该入口并记录错误，不通过更改策略、改名或换宿主绕过。2026-09-30 本机 PowerShell 7.6.5 可运行原测试入口；Windows PowerShell 5.1 的脚本入口被执行策略拒绝，该环境未完成运行验证。

Use a PowerShell installation in which local policy permits these scripts. If a script or executable is explicitly blocked, stop that entry point and record the error; do not bypass it by changing policy, renaming or switching hosts. On the audited machine on 2026-09-30, the original entry point ran under PowerShell 7.6.5, while Windows PowerShell 5.1 refused the script under its execution policy; execution in that environment was not verified.

## 覆盖范围 / Coverage

| 套件 / Suite | 覆盖 / Coverage |
| --- | --- |
| Domain | 额度与套餐解析、百分比与重置计算、异常值、安全错误、设置保存与恢复、固定尺寸、语言及运行数据路径 |
| Bridge | 模拟服务握手、通知与响应、安全错误、超时、取消、异常退出及子进程清理 |
| ResetFeed | 本机 HTTP 服务、304、ETag、缓存策略、秒数/日期重试、坏响应、取消、超时及原子缓存 |

Domain covers parsing, calculations, settings, and runtime data paths. Bridge launches a fake server to exercise protocol and process handling. ResetFeed uses a loopback server to test the production HTTP and cache implementation without external service dependencies.

## 结果 / Results

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
