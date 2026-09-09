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

将 EXE 放在可写目录。设置和运行数据保存在 EXE 相邻的 `env/`；直接运行构建产物时，对应位置为 `env/build/env/`。

Place the EXE in a writable directory. Settings and runtime data are stored in the adjacent `env/` directory.

参见 [测试 / Testing](TESTING.md) 与 [隐私 / Privacy](PRIVACY.md)。
