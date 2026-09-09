# 测试 / Testing

测试使用合成数据和模拟子进程，无需登录 Codex。

Tests use synthetic data and a fake server; no Codex sign-in is required.

## 命令 / Commands

从仓库根目录运行 / Run from the repository root:

```powershell
# 默认 Domain / Default: Domain
./scripts/test.ps1

./scripts/test.ps1 -Suite Domain
./scripts/test.ps1 -Suite Bridge
./scripts/test.ps1 -Suite All

# 只编译 / Compile only
./scripts/test.ps1 -Suite All -BuildOnly
```

`All` 包含 Domain 和 Bridge。`-BuildOnly` 编译所选套件，不执行测试。

`All` runs Domain and Bridge. `-BuildOnly` compiles the selected suites without running them.

## 覆盖范围 / Coverage

| 套件 / Suite | 覆盖 / Coverage |
| --- | --- |
| Domain | 额度与套餐解析、百分比与重置计算、异常值、安全错误、设置保存与恢复、固定尺寸、语言及运行数据路径 |
| Bridge | 模拟服务握手、通知与响应、安全错误、超时、取消、异常退出及子进程清理 |

Domain covers parsing, calculations, settings, and runtime data paths. Bridge launches a fake server to exercise protocol and process handling.

## 结果 / Results

2026-09-09：**Domain 26 项、Bridge 8 项通过，均为 0 失败。** 新增的六组 Domain 检查覆盖畸形响应、错误字段类型、区域格式、时区与重置秒边界、未知套餐、比例的连续变化及非有限数。本轮重新执行 Domain；Bridge 沿用同日、相同产品源码的前一轮结果。

On 2026-09-09, **26 Domain checks and 8 Bridge checks passed, with no failures.** Six added Domain cases cover malformed responses, wrong field types, regional formats, time zones and reset boundaries, unknown plans, and bounded ratios including non-finite inputs. Domain was rerun; Bridge results are from the earlier run on the same day with unchanged product source.

用户实测补充：当前发布的 1.0.0.0 已在一台 Windows 电脑上启动，运行文件 SHA256 与发行附件一致。截图确认 Pro 额度读取、每周额度与 Codex 显示一致、中英文详情布局以及通知区左侧的额度条位置。截图仅用于本地验收，未上传账号信息。

User testing confirmed that the published 1.0.0.0 executable starts on one Windows machine; its SHA256 matches the release asset. Screenshots confirm Pro usage retrieval, the weekly value matching Codex, Chinese and English detail layouts, and widget placement beside the notification area. Account screenshots were not uploaded.

用户按手动刷新及等待自动刷新的步骤提供了连续截图，成功更新时间的变化符合手动刷新及五分钟定时刷新；界面持续显示正常。本项为用户辅助实测，不是自动化点击验证。

Sequential screenshots supplied during the manual/automatic refresh check show successful update timestamps consistent with a manual refresh and the five-minute schedule, with the UI still displaying normally. This is user-assisted testing, not automated interaction testing.

重启后偏好保存、完整退出、其他 DPI/屏幕环境及真实 Plus 账号仍待验证。EXE 仍未签名；历史构建和部分 UI 检查曾被 Windows 应用控制阻止，本次成功启动不代表这些检查已恢复或所有电脑均能运行。

Preferences after restart, complete shutdown, other DPI/display configurations, and a real Plus account remain unverified. The EXE is still unsigned. Earlier builds and some UI checks were blocked by Windows application control; this successful launch does not validate those checks or guarantee compatibility on every machine.
