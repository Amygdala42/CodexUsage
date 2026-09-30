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

输出：`env/build/CodexUsage.exe`。程序内嵌 `LICENSE` 和 `THIRD_PARTY_NOTICES.md`。

Output: `env/build/CodexUsage.exe`, with the license and dependency notices embedded.

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
Copy-Item env/tests/theme-render/widget-dark.png assets/widget-preview.png
Copy-Item env/tests/theme-render/theme-comparison.png assets/appearance-preview.png
```
