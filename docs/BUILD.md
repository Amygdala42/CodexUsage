# 构建 / Build

## 环境 / Requirements

- Windows x64。
- .NET Framework 4.8 或更新的 4.x 版本。
- PowerShell 5.1 或更新版本。
- 系统 C# 编译器：`%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`。

源码使用 C# 5 和 .NET Framework 内置程序集，无需 NuGet 包或现代 .NET SDK。运行程序另需已安装并登录的 Codex。

The build uses C# 5, the Framework64 compiler, and system assemblies. No NuGet packages or modern .NET SDK are required. Running the app also requires an installed, signed-in Codex.

## 编译 / Compile

从仓库根目录运行 / Run from the repository root:

```powershell
./scripts/build.ps1
```

输出：`build/app/CodexUsage.exe`。程序内嵌 `LICENSE` 和 `THIRD_PARTY_NOTICES.md`。该位置是可重新生成的编译目录，不作为历史归档或发布目录。

Output: `build/app/CodexUsage.exe`, with the license and dependency notices embedded. This is a regenerable build directory, not a release archive.

构建、测试和打包入口共用 `output/.package.lock`，同一仓库一次只允许一个独立写入任务；打包内部调用构建和测试时沿用已持有的锁。若提示另一个任务正在运行，请等待其结束后重试。锁文件保留在磁盘上是正常现象，是否占用由打开的文件句柄决定，不需手工删除锁文件。

Build, test and package commands share `output/.package.lock`, allowing one independent writer per repository. Packaging reuses its held lock for nested build and test calls. If another command is running, wait for it to finish and retry. The lock file remains on disk; the open file handle determines ownership, so manual deletion is unnecessary.

## 按日期与批次打包 / Date-based delivery batches

```powershell
# 默认先编译并执行 All，成功后生成程序包
# Build and run All before preparing the delivery
./scripts/package.ps1

# 仅在明确需要时跳过测试；清单会记录此选择
# Explicitly skip tests only when needed; the manifest records this choice
./scripts/package.ps1 -SkipTests
```

每次执行在 `output/YYYY-MM-DD/batch-NNN/` 生成独立批次。日期取打包开始时的 Asia/Shanghai 日期，每日从 `batch-001` 递增；失败批次保留记录，不覆盖、不复用其编号，重跑创建新批次。跨午夜的同一次执行仍属于开始日期。

Each invocation creates a separate `output/YYYY-MM-DD/batch-NNN/` directory. It uses the Asia/Shanghai date at the start, with a daily sequence beginning at `batch-001`. Failed batches retain their records and reserve their numbers; retries create new batches. A run crossing midnight stays under its starting date.

程序和 ZIP 文件名不包含版本号，版本保留在程序内部和批次清单中。批次保存程序包、校验和、构建/测试日志与清单；通过清单核对是否成功、是否跳过测试，再选择交付。此命令不提交源码、不推送 Git、不修改标签或上传 GitHub Release。

Executable and ZIP filenames contain no version number; the executable and batch manifest retain the software version. Each batch stores the package, checksums, build/test logs and a manifest. Check its completion and test status before delivery. Packaging does not commit, push, change tags or upload GitHub releases.

旧包保存在 `history/releases/`，旧版单文件程序保存在 `history/versions/`，迁移时留存的旧编译产物位于 `history/builds/`；它们是历史快照，不由打包命令覆盖。目录迁移记录保存在 `records/organization/`。v1.1.0 包含上述目录与打包约定，文件、程序集和应用清单版本为 1.1.0.0。

Earlier packages live in `history/releases/`, older standalone executables in `history/versions/`, and builds preserved during migration in `history/builds/`; packaging never overwrites these snapshots. Directory migration records live in `records/organization/`. Version 1.1.0 includes these layout and packaging conventions, with file, assembly and application manifest versions set to 1.1.0.0.

## 运行 / Run

运行 EXE。设置和运行数据保存在 `%LOCALAPPDATA%\CodexUsage`，不再写入程序所在目录。

Run the EXE. Settings and runtime data are stored in `%LOCALAPPDATA%\CodexUsage`, outside the program directory.

参见 [测试 / Testing](TESTING.md) 与 [隐私 / Privacy](PRIVACY.md)。

## 文档示意图 / Documentation previews

可选：使用 Python 3、Pillow 和 Windows Microsoft YaHei 字体重绘四张中英文示意图。此步骤不运行应用，版本号从源码读取，数据为合成值；Python/Pillow 不属于应用运行依赖。

Optional: regenerate the four bilingual illustrations with Python 3, Pillow and Windows Microsoft YaHei fonts. The script reads the version from source and uses synthetic data; it does not run the app. Python and Pillow are not app runtime dependencies.

```powershell
python scripts/render-previews.py assets
```

README 中的浮条和主题对照图来自生产绘图代码，可在运行 Theme 检查后复制生成；同样只使用合成数据，不启动原生窗口。

The README widget and theme comparison come from the production renderer. Regenerate them with the Theme suite and copy its synthetic-data output; this does not start a native window.

```powershell
./scripts/test.ps1 -Suite Theme
Copy-Item build/tests/theme-render/widget-dark.png assets/widget-preview.png
Copy-Item build/tests/theme-render/theme-comparison.png assets/appearance-preview.png
```
