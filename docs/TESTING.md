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

GUI 启动、完整交互、任务栏布局、不同 DPI 和真实账号集成尚未完成验证。EXE 未签名，本机曾出现 Windows 应用程序控制启动拦截。

GUI launch, complete interaction, taskbar placement, DPI behavior, and live-account integration remain unverified. The EXE is unsigned, and Windows application control has blocked launches on the test machine.
