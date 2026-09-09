using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using CodexQuotaLite;

internal static class DomainTests
{
    private static int failures;
    private static int testsRun;

    public static int Main()
    {
        Run("多桶优先主额度且不混入旧字段", MultiBucketUsesCodexFirstAndIgnoresLegacy);
        Run("Plus 显示实际普通双窗口且不补造 Spark", PlusUsesReturnedStandardWindowsWithoutSpark);
        Run("Plus 缺少窗口或字段时不按套餐补造数据", PlusMissingWindowsRemainMissing);
        Run("Pro 的 Spark 来自响应且套餐名不决定窗口内容", PlanNameDoesNotInferOrFilterReturnedWindows);
        Run("多桶中只有 codex ID 能成为主桶", OnlyCodexBucketCanBePrimary);
        Run("主次窗口对调后仍按时长排序并保留稳定 ID", SwappedSlotsSortByDurationAndKeepDistinctIds);
        Run("数字缺失、零值、越界和非法日期保持正确语义", NumericNullZeroClampingAndInvalidDates);
        Run("没有多桶时使用旧单桶并按时长命名", LegacyFallbackUsesDurationLabels);
        Run("额度和剩余时间计算在边界处收敛", QuotaWindowCalculatesRemainingAndResetState);
        Run("空账号给出可操作的安全错误", NullAccountIsRejectedSafely);
        Run("API Key 账号给出可操作的安全错误", ApiKeyAccountIsRejectedSafely);
        Run("JSON-RPC 错误不会泄露原始响应", RpcErrorIsRejectedWithoutLeakingPayload);
        Run("缺少套餐名时使用明确回退文案", MissingPlanUsesFallbackLabel);
        Run("缺失或损坏的设置返回默认值", MissingAndCorruptSettingsReturnDefaults);
        Run("设置保存后会清理和规范化值", SettingsRoundTripSanitizesValues);
        Run("旧尺寸恢复100且语言偏好可保存", LegacyScaleAndLanguagePreference);
        Run("设置替换失败时保留原文件", FailedAtomicSavePreservesExistingFile);
        Run("便携目录支持空格与非英文名称", PortablePathsSupportSpacesAndUnicode);
        Run("便携设置不使用父目录的开发环境", PortablePathsIgnoreAncestorWorkspace);
        Run("运行目录仅在程序相邻位置创建", PortablePreparationStaysBesideExecutable);
        Run("畸形响应只返回安全错误", MalformedEnvelopesStaySafe);
        Run("错误字段类型不伪造额度", WrongFieldTypesRemainUnknown);
        Run("区域格式不改变数字含义", NumericParsingIsCultureIndependent);
        Run("等价时区和重置秒边界一致", EquivalentTimeZonesAndResetSeconds);
        Run("未知套餐保留名称且不增加窗口", UnknownPlanKeepsActualWindows);
        Run("额度与时间比例连续变化且有界", RemainingValuesAreBoundedAndMonotonic);

        Console.WriteLine("RESULT: {0} passed, {1} failed", testsRun - failures, failures);
        return failures == 0 ? 0 : 1;
    }

    private static void MalformedEnvelopesStaySafe()
    {
        string[] invalid = { "", "null", "[]", "42", "{", "{\"result\":null}",
            "{\"result\":[]}", "{\"error\":{\"message\":\"fixture-private-message\"},\"result\":{}}" };
        foreach (string value in invalid)
        {
            InvalidOperationException error = ThrowsInvalidOperation(delegate {
                QuotaParser.Parse(Account("chatgpt", "plus"), value, DateTimeOffset.UtcNow);
            });
            DoesNotContain(error.Message, "fixture-private-message", "原始错误不可回显");
        }
    }

    private static void WrongFieldTypesRemainUnknown()
    {
        string[] invalid = { "true", "false", "[]", "{}", "\"25\"", "null" };
        foreach (string value in invalid)
        {
            string data = "{\"result\":{\"rateLimits\":{\"primary\":{\"usedPercent\":" + value +
                ",\"windowDurationMins\":" + value + ",\"resetsAt\":" + value + "}}}}";
            QuotaWindow window = QuotaParser.Parse(Account("chatgpt", "plus"), data, DateTimeOffset.UtcNow).Windows[0];
            False(window.RemainingPercent.HasValue, "错误类型不能显示为100%或0%");
            False(window.WindowMinutes.HasValue, "错误类型不能生成时长");
            False(window.ResetsAtUtc.HasValue, "错误类型不能生成日期");
        }
    }

    private static void NumericParsingIsCultureIndependent()
    {
        CultureInfo original = System.Threading.Thread.CurrentThread.CurrentCulture;
        try
        {
            foreach (string culture in new[] { "zh-CN", "en-US", "de-DE", "fr-FR", "tr-TR" })
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
                string data = "{\"result\":{\"rateLimits\":{\"primary\":{\"usedPercent\":12.5,\"windowDurationMins\":300,\"resetsAt\":1800000000}}}}";
                QuotaWindow window = QuotaParser.Parse(Account("chatgpt", "plus"), data, DateTimeOffset.UtcNow).Windows[0];
                Equal(87.5, window.RemainingPercent.Value, culture + "小数含义");
                Equal(300, window.WindowMinutes.Value, culture + "周期含义");
            }
        }
        finally { System.Threading.Thread.CurrentThread.CurrentCulture = original; }
    }

    private static void EquivalentTimeZonesAndResetSeconds()
    {
        DateTimeOffset now = Utc(2026, 9, 9, 0, 0, 0);
        QuotaWindow window = new QuotaWindow { WindowMinutes = 300, ResetsAtUtc = now.AddHours(1) };
        foreach (int hours in new[] { -12, -5, 0, 8, 14 })
        {
            DateTimeOffset equivalent = now.ToOffset(TimeSpan.FromHours(hours));
            Equal(20.0, window.GetTimeRemainingPercent(equivalent).Value, "同一时刻不受时区影响");
            False(window.IsResetPending(equivalent.AddHours(1).AddSeconds(-1)), "重置前一秒");
            True(window.IsResetPending(equivalent.AddHours(1)), "重置当秒");
            Equal(0.0, window.GetTimeRemainingPercent(equivalent.AddHours(1).AddSeconds(1)).Value, "重置后一秒");
        }
    }

    private static void UnknownPlanKeepsActualWindows()
    {
        QuotaSnapshot snapshot = QuotaParser.Parse(Account("chatgpt", "future-plan"), EmptyLimits(), DateTimeOffset.UtcNow);
        Equal("future-plan", snapshot.PlanLabel, "未知套餐名不能伪装成Plus或Pro");
        Equal(0, snapshot.Windows.Count, "未知套餐不能补造默认窗口");
    }

    private static void RemainingValuesAreBoundedAndMonotonic()
    {
        DateTimeOffset now = Utc(2026, 9, 9, 0, 0, 0);
        QuotaWindow window = new QuotaWindow { WindowMinutes = 300, ResetsAtUtc = now.AddMinutes(300) };
        double previousQuota = 100, previousTime = 100;
        for (int step = -100; step <= 1000; step++)
        {
            window.UsedPercent = step / 5.0;
            double quota = window.RemainingPercent.Value;
            double time = window.GetTimeRemainingPercent(now.AddMinutes(step)).Value;
            True(quota >= 0 && quota <= 100 && quota <= previousQuota, "用量增加时剩余额度不可增加或越界");
            True(time >= 0 && time <= 100 && time <= previousTime, "时间推进时剩余比例不可增加或越界");
            previousQuota = quota; previousTime = time;
        }
        foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            window.UsedPercent = invalid;
            False(window.RemainingPercent.HasValue, "非有限数不可变成可用额度");
        }
    }

    private static void PortablePathsSupportSpacesAndUnicode()
    {
        string folder = Path.Combine(Path.GetTempPath(), "portable path 示例", "application");
        AppPaths paths = AppPaths.Resolve(folder);
        Equal(Path.Combine(Path.GetFullPath(folder), "env"), paths.SupportDirectory, "Support directory follows the executable");
        Equal(Path.Combine(paths.SupportDirectory, "config", "CodexQuotaLite", "settings.json"), paths.SettingsFile, "Settings remain portable");
    }

    private static void PortablePathsIgnoreAncestorWorkspace()
    {
        string root = CreateTestDirectory();
        try
        {
            File.WriteAllText(Path.Combine(root, "PROJECT_RULES.md"), "test fixture");
            Directory.CreateDirectory(Path.Combine(root, "env"));
            string folder = Path.Combine(root, "nested application");
            Directory.CreateDirectory(folder);
            Equal(Path.Combine(folder, "env"), AppPaths.Resolve(folder).SupportDirectory, "Parent workspace must not capture settings");
            string second = Path.Combine(root, "other application");
            Equal(Path.Combine(second, "env"), AppPaths.Resolve(second).SupportDirectory, "Each portable copy has its own settings location");
        }
        finally { Directory.Delete(root, true); }
    }

    private static void PortablePreparationStaysBesideExecutable()
    {
        string root = CreateTestDirectory();
        try
        {
            string folder = Path.Combine(root, "application 示例");
            AppPaths paths = AppPaths.Resolve(folder);
            paths.Prepare();
            True(Directory.Exists(Path.GetDirectoryName(paths.SettingsFile)), "Settings directory is created");
            True(Directory.Exists(Path.GetDirectoryName(paths.ErrorLog)), "Log directory is created");
            True(Directory.Exists(Path.Combine(folder, "env", "tmp", "CodexQuotaLite")), "Temporary directory is beside executable");
            False(Directory.Exists(Path.Combine(root, "env")), "No ancestor environment is created");
        }
        finally { Directory.Delete(root, true); }
    }

    private static void PlusUsesReturnedStandardWindowsWithoutSpark()
    {
        string limits = @"{""result"":{""rateLimitsByLimitId"":{""codex"":{
          ""primary"":{""usedPercent"":21,""windowDurationMins"":300,""resetsAt"":1800000000},
          ""secondary"":{""usedPercent"":61,""windowDurationMins"":10080,""resetsAt"":1800600000}
        }}}}";

        QuotaSnapshot snapshot = QuotaParser.Parse(Account("chatgpt", "plus"), limits, Utc(2026, 9, 9, 0, 0, 0));

        Equal("Plus", snapshot.PlanLabel, "套餐必须来自账号，不能固定为 Pro");
        Equal(2, snapshot.Windows.Count, "只显示响应的两个普通窗口");
        Equal("codex:primary", snapshot.Windows[0].Id, "默认首项为主桶 5 小时窗口");
        Equal("5 小时额度", snapshot.Windows[0].Label, "普通 5 小时标签");
        Equal(300, snapshot.Windows[0].WindowMinutes.Value, "短期时长来自响应");
        Equal(79.0, snapshot.Windows[0].RemainingPercent.Value, "短期剩余额度来自响应");
        Equal("codex:secondary", snapshot.Windows[1].Id, "第二项为主桶每周窗口");
        Equal("每周额度", snapshot.Windows[1].Label, "普通每周标签");
        Equal(10080, snapshot.Windows[1].WindowMinutes.Value, "每周时长来自响应");
        Equal(39.0, snapshot.Windows[1].RemainingPercent.Value, "每周剩余额度来自响应");
        foreach (QuotaWindow window in snapshot.Windows)
        {
            True(window.Id.StartsWith("codex:", StringComparison.Ordinal), "没有返回其它额度桶时不能补造");
            DoesNotContain(window.Label, "Spark", "没有返回 Spark 时不能显示 Spark");
        }
    }

    private static void PlusMissingWindowsRemainMissing()
    {
        DateTimeOffset now = Utc(2026, 9, 9, 0, 0, 0);
        string account = Account("chatgptAuthTokens", "plus");
        string weeklyOnly = @"{""result"":{""rateLimitsByLimitId"":{""codex"":{
          ""primary"":null,
          ""secondary"":{""usedPercent"":40,""windowDurationMins"":10080,""resetsAt"":1800600000}
        }}}}";
        QuotaSnapshot weekly = QuotaParser.Parse(account, weeklyOnly, now);

        Equal("Plus", weekly.PlanLabel, "缺少一个窗口仍保留 Plus 套餐");
        Equal(1, weekly.Windows.Count, "缺少 5 小时窗口时不能根据 Plus 补造");
        Equal("codex:secondary", weekly.Windows[0].Id, "保留实际返回的每周窗口");
        Equal("每周额度", weekly.Windows[0].Label, "首项不能固定命名为 5 小时");
        Equal(60.0, weekly.Windows[0].RemainingPercent.Value, "每周剩余额度");

        string incompletePrimary = @"{""result"":{""rateLimitsByLimitId"":{""codex"":{
          ""primary"":{""usedPercent"":null,""windowDurationMins"":300,""resetsAt"":null}
        }}}}";
        QuotaSnapshot partial = QuotaParser.Parse(account, incompletePrimary, now);
        Equal(1, partial.Windows.Count, "缺失 secondary 时不能补造每周窗口");
        False(partial.Windows[0].RemainingPercent.HasValue, "未知额度不能按 Plus 推测");
        False(partial.Windows[0].GetTimeRemainingPercent(now).HasValue, "未知重置时间不能按 Plus 推测");

        QuotaSnapshot empty = QuotaParser.Parse(account, EmptyLimits(), now);
        Equal("Plus", empty.PlanLabel, "没有额度时仍可显示实际套餐");
        Equal(0, empty.Windows.Count, "空额度响应不能生成默认 5 小时、每周或 Spark 窗口");
    }

    private static void PlanNameDoesNotInferOrFilterReturnedWindows()
    {
        string limits = @"{""result"":{""rateLimitsByLimitId"":{
          ""codex_bengalfox"":{""limitName"":""回归示例 Spark"",
            ""primary"":{""usedPercent"":8,""windowDurationMins"":300,""resetsAt"":1800000100}},
          ""codex"":{
            ""primary"":{""usedPercent"":24,""windowDurationMins"":300,""resetsAt"":1800000000},
            ""secondary"":{""usedPercent"":58,""windowDurationMins"":10080,""resetsAt"":1800600000}}
        }}}";
        DateTimeOffset now = Utc(2026, 9, 9, 0, 0, 0);
        QuotaSnapshot pro = QuotaParser.Parse(Account("chatgpt", "pro"), limits, now);

        Equal("Pro", pro.PlanLabel, "Pro 套餐标签");
        Equal(3, pro.Windows.Count, "响应实际包含两个主窗口和一个 Spark 窗口");
        Equal("codex:primary", pro.Windows[0].Id, "Spark 在响应中靠前也不能替代主额度");
        Equal("codex:secondary", pro.Windows[1].Id, "主桶窗口排在 Spark 前");
        Equal("codex_bengalfox:primary", pro.Windows[2].Id, "保留实际 Spark 桶 ID");
        Equal("回归示例 Spark · 5小时", pro.Windows[2].Label, "名称来自响应而非写死模型名称");
        Equal(92.0, pro.Windows[2].RemainingPercent.Value, "Spark 的百分比来自其自己的窗口");

        // This synthetic Plus+Spark pairing tests protocol independence only;
        // it does not assert that a real Plus subscription is entitled to Spark.
        QuotaSnapshot plus = QuotaParser.Parse(Account("chatgpt", "plus"), limits, now);
        Equal("Plus", plus.PlanLabel, "替换账号套餐时更新显示标签");
        Equal(pro.Windows.Count, plus.Windows.Count, "不能依据套餐名过滤响应中的窗口");
        for (int i = 0; i < pro.Windows.Count; i++)
        {
            Equal(pro.Windows[i].Id, plus.Windows[i].Id, "更换套餐名不改变窗口标识和排序");
            Equal(pro.Windows[i].Label, plus.Windows[i].Label, "更换套餐名不改变响应标签");
            Equal(pro.Windows[i].RemainingPercent, plus.Windows[i].RemainingPercent, "更换套餐名不重算额度");
            Equal(pro.Windows[i].WindowMinutes, plus.Windows[i].WindowMinutes, "更换套餐名不推断周期");
            Equal(pro.Windows[i].ResetsAtUtc, plus.Windows[i].ResetsAtUtc, "更换套餐名不推断重置时间");
        }

        QuotaSnapshot emptyPro = QuotaParser.Parse(Account("chatgpt", "pro"), EmptyLimits(), now);
        Equal(0, emptyPro.Windows.Count, "Pro 本身不能触发创建 Spark 或其它默认窗口");
    }

    private static void OnlyCodexBucketCanBePrimary()
    {
        string limits = @"{""result"":{""rateLimitsByLimitId"":{
          ""legacy"":{""limitName"":""AAA"",""primary"":{""usedPercent"":1,""windowDurationMins"":300,""resetsAt"":1800000000}},
          ""codex"":{""limitName"":null,""primary"":{""usedPercent"":2,""windowDurationMins"":10080,""resetsAt"":1800000000}}
        }}}";

        QuotaSnapshot snapshot = QuotaParser.Parse(Account("chatgpt", "pro"), limits, Utc(2026, 9, 8, 0, 0, 0));

        Equal("codex:primary", snapshot.Windows[0].Id, "只有 codex 是主桶");
        Equal("AAA · 5小时", snapshot.Windows[1].Label, "名为 legacy 的多桶仍需显示桶名");
    }

    private static void MultiBucketUsesCodexFirstAndIgnoresLegacy()
    {
        string account = Account("chatgpt", "pro");
        string limits = @"{
          ""jsonrpc"":""2.0"",""id"":2,""result"":{
            ""rateLimitsByLimitId"":{
              ""codex_bengalfox"":{
                ""limitId"":""codex_bengalfox"",""limitName"":""GPT-5.3-Codex-Spark"",
                ""primary"":{""usedPercent"":11,""windowDurationMins"":300,""resetsAt"":1800000000},
                ""secondary"":{""usedPercent"":22,""windowDurationMins"":10080,""resetsAt"":1800600000}
              },
              ""codex"":{
                ""limitId"":""codex"",""limitName"":null,
                ""primary"":{""usedPercent"":33,""windowDurationMins"":10080,""resetsAt"":1800700000},
                ""secondary"":null
              }
            },
            ""rateLimits"":{
              ""primary"":{""usedPercent"":99,""windowDurationMins"":300,""resetsAt"":1800000000}
            }
          }
        }";

        QuotaSnapshot snapshot = QuotaParser.Parse(account, limits, Utc(2026, 9, 8, 1, 2, 3));

        Equal("Pro", snapshot.PlanLabel, "套餐显示");
        Equal(3, snapshot.Windows.Count, "多桶窗口数");
        Equal("codex:primary", snapshot.Windows[0].Id, "主额度桶必须优先");
        Equal("每周额度", snapshot.Windows[0].Label, "主桶标签");
        Equal(33.0, snapshot.Windows[0].UsedPercent.Value, "不得混入旧单桶");
        Equal("codex_bengalfox:primary", snapshot.Windows[1].Id, "其它桶窗口顺序");
        Equal("GPT-5.3-Codex-Spark · 5小时", snapshot.Windows[1].Label, "其它桶标签需可区分");
        Equal(TimeSpan.Zero, snapshot.FetchedAtUtc.Offset, "抓取时间必须为 UTC");
    }

    private static void SwappedSlotsSortByDurationAndKeepDistinctIds()
    {
        string limits = @"{""result"":{""rateLimitsByLimitId"":{""model_x"":{
          ""limitId"":""model_x"",""limitName"":""模型 X"",
          ""primary"":{""usedPercent"":20,""windowDurationMins"":10080,""resetsAt"":1800000000},
          ""secondary"":{""usedPercent"":10,""windowDurationMins"":300,""resetsAt"":1800000001}
        }}}}";

        QuotaSnapshot snapshot = QuotaParser.Parse(Account("chatgptAuthTokens", "plus"), limits, Utc(2026, 9, 8, 0, 0, 0));

        Equal(2, snapshot.Windows.Count, "窗口数");
        Equal("model_x:secondary", snapshot.Windows[0].Id, "5 小时窗口应排在同桶每周窗口前");
        Equal("model_x:primary", snapshot.Windows[1].Id, "字段槽位必须保留在 ID 中");
        NotEqual(snapshot.Windows[0].Id, snapshot.Windows[1].Id, "ID 必须不同");
        Equal(300, snapshot.Windows[0].WindowMinutes.Value, "窗口时长不能依赖字段顺序");
        Equal("模型 X · 5小时", snapshot.Windows[0].Label, "5 小时标签");
        Equal("模型 X · 每周", snapshot.Windows[1].Label, "每周标签");
    }

    private static void NumericNullZeroClampingAndInvalidDates()
    {
        string limits = @"{""result"":{""rateLimitsByLimitId"":{
          ""codex"":{""primary"":{""usedPercent"":0,""windowDurationMins"":300,""resetsAt"":253402300799}},
          ""high"":{""primary"":{""usedPercent"":140,""windowDurationMins"":0,""resetsAt"":253402300800}},
          ""low"":{""primary"":{""usedPercent"":-12,""windowDurationMins"":60,""resetsAt"":-62135596801}},
          ""missing"":{""primary"":{""usedPercent"":null,""windowDurationMins"":null,""resetsAt"":null}},
          ""text"":{""primary"":{""usedPercent"":""NaN"",""windowDurationMins"":""300"",""resetsAt"":""1800000000""}}
        }}}";

        QuotaSnapshot snapshot = QuotaParser.Parse(Account("chatgpt", "plus"), limits, Utc(2026, 9, 8, 0, 0, 0));
        Dictionary<string, QuotaWindow> byId = Index(snapshot.Windows);

        Equal(0.0, byId["codex:primary"].UsedPercent.Value, "零使用量不能当成缺失");
        Equal(100.0, byId["codex:primary"].RemainingPercent.Value, "零使用量的剩余额度");
        True(byId["codex:primary"].ResetsAtUtc.HasValue, "最大合法 Unix 秒应可解析");
        Equal(100.0, byId["high:primary"].UsedPercent.Value, "上界限制");
        False(byId["high:primary"].WindowMinutes.HasValue, "非正窗口时长无效");
        False(byId["high:primary"].ResetsAtUtc.HasValue, "越过上界的 Unix 秒无效");
        Equal(0.0, byId["low:primary"].UsedPercent.Value, "下界限制");
        False(byId["low:primary"].ResetsAtUtc.HasValue, "越过下界的 Unix 秒无效");
        False(byId["missing:primary"].UsedPercent.HasValue, "null 使用量必须保留");
        False(byId["text:primary"].UsedPercent.HasValue, "文本不能冒充数字");
        False(byId["text:primary"].WindowMinutes.HasValue, "文本时长无效");
        False(byId["text:primary"].ResetsAtUtc.HasValue, "文本日期无效");
    }

    private static void LegacyFallbackUsesDurationLabels()
    {
        string limits = @"{""result"":{""rateLimits"":{
          ""primary"":{""usedPercent"":70,""windowDurationMins"":1440,""resetsAt"":1800000000},
          ""secondary"":{""usedPercent"":20,""windowDurationMins"":90,""resetsAt"":1800000100}
        }}}";

        QuotaSnapshot snapshot = QuotaParser.Parse(Account("chatgpt", "team"), limits, Utc(2026, 9, 8, 0, 0, 0));

        Equal(2, snapshot.Windows.Count, "旧字段窗口数");
        Equal("legacy:secondary", snapshot.Windows[0].Id, "其它时长按实际长度排序");
        Equal("90 分钟额度", snapshot.Windows[0].Label, "分钟标签");
        Equal("1 天额度", snapshot.Windows[1].Label, "天标签");
    }

    private static void QuotaWindowCalculatesRemainingAndResetState()
    {
        DateTimeOffset now = Utc(2026, 9, 8, 0, 0, 0);
        QuotaWindow window = new QuotaWindow();
        window.UsedPercent = 25;
        window.WindowMinutes = 300;
        window.ResetsAtUtc = now.AddMinutes(150);

        Equal(75.0, window.RemainingPercent.Value, "剩余额度");
        Equal(50.0, window.GetTimeRemainingPercent(now).Value, "半周期剩余时间");
        Equal(100.0, window.GetTimeRemainingPercent(now.AddMinutes(-500)).Value, "剩余时间上限");
        Equal(0.0, window.GetTimeRemainingPercent(now.AddMinutes(151)).Value, "剩余时间下限");
        False(window.IsResetPending(now), "重置前");
        True(window.IsResetPending(now.AddMinutes(150)), "重置边界开始等待更新");

        window.WindowMinutes = null;
        False(window.GetTimeRemainingPercent(now).HasValue, "缺时长时不能猜测时间比例");
        window.UsedPercent = null;
        False(window.RemainingPercent.HasValue, "缺使用量时不能猜测剩余额度");
    }

    private static void NullAccountIsRejectedSafely()
    {
        InvalidOperationException error = ThrowsInvalidOperation(delegate
        {
            QuotaParser.Parse(@"{""result"":{""account"":null}}", EmptyLimits(), Utc(2026, 9, 8, 0, 0, 0));
        });
        Contains(error.Message, "登录", "空账号提示应说明登录操作");
    }

    private static void ApiKeyAccountIsRejectedSafely()
    {
        InvalidOperationException error = ThrowsInvalidOperation(delegate
        {
            QuotaParser.Parse(Account("apiKey", null), EmptyLimits(), Utc(2026, 9, 8, 0, 0, 0));
        });
        Contains(error.Message, "ChatGPT", "API Key 提示应说明所需账号类型");
        Contains(error.Message, "登录", "API Key 提示应给出操作");
    }

    private static void RpcErrorIsRejectedWithoutLeakingPayload()
    {
        InvalidOperationException error = ThrowsInvalidOperation(delegate
        {
            QuotaParser.Parse(Account("chatgpt", "pro"),
                @"{""error"":{""code"":401,""message"":""unauthorized sk-secret-value""}}",
                Utc(2026, 9, 8, 0, 0, 0));
        });
        Contains(error.Message, "额度", "接口错误应说明读取额度失败");
        DoesNotContain(error.Message, "secret", "安全错误不得回显响应内容");
        DoesNotContain(error.Message, "401", "安全错误不得回显接口细节");
    }

    private static void MissingPlanUsesFallbackLabel()
    {
        QuotaSnapshot snapshot = QuotaParser.Parse(Account("chatgpt", null), EmptyLimits(), Utc(2026, 9, 8, 0, 0, 0));
        Equal("未知套餐", snapshot.PlanLabel, "缺套餐名回退");
        Equal(0, snapshot.Windows.Count, "无额度窗口时返回空集合");
    }

    private static void MissingAndCorruptSettingsReturnDefaults()
    {
        string dir = CreateTestDirectory();
        try
        {
            string file = Path.Combine(dir, "settings.json");
            AssertDefaults(new SettingsStore(file).Load(), "缺失文件");

            File.WriteAllText(file, "{ definitely not json", System.Text.Encoding.UTF8);
            AssertDefaults(new SettingsStore(file).Load(), "损坏文件");
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    private static void SettingsRoundTripSanitizesValues()
    {
        string dir = CreateTestDirectory();
        try
        {
            string file = Path.Combine(dir, "settings.json");
            AppSettings input = new AppSettings();
            input.AlwaysOnTop = false;
            input.ScalePercent = 999;
            input.SelectedWindowId = "  codex:primary  ";
            input.X = -120;
            input.Y = 456;

            True(new SettingsStore(file).Save(input), "保存应成功");
            AppSettings loaded = new SettingsStore(file).Load();

            False(loaded.AlwaysOnTop, "置顶值");
            Equal(100, loaded.ScalePercent, "非法缩放回到默认值");
            Equal("codex:primary", loaded.SelectedWindowId, "窗口 ID 去除首尾空白");
            Equal(-120, loaded.X.Value, "X 坐标");
            Equal(456, loaded.Y.Value, "Y 坐标");
            Equal(0, Directory.GetFiles(dir, "*.tmp").Length, "原子保存不遗留临时文件");

            AppSettings supported = new AppSettings();
            supported.ScalePercent = 150;
            True(new SettingsStore(file).Save(supported), "支持的缩放应保存");
            Equal(100, new SettingsStore(file).Load().ScalePercent, "新版固定100，旧150不再影响显示");
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    private static void LegacyScaleAndLanguagePreference()
    {
        string dir = CreateTestDirectory();
        try
        {
            string file = Path.Combine(dir, "settings.json");
            File.WriteAllText(file, "{\"ScalePercent\":125,\"Language\":\"en\",\"SelectedWindowId\":\"codex:primary\"}");
            AppSettings value = new SettingsStore(file).Load();
            Equal(100, value.ScalePercent, "旧125固定为100");
            PropertyInfo language = typeof(AppSettings).GetProperty("Language");
            True(language != null, "有语言设置属性");
            Equal("en", (string)language.GetValue(value, null), "读取英文偏好");
            True(new SettingsStore(file).Save(value), "保存英文偏好");
            Equal("en", (string)language.GetValue(new SettingsStore(file).Load(), null), "重启后英文偏好保留");
            File.WriteAllText(file, "{\"ScalePercent\":150,\"Language\":\"unsupported\"}");
            value = new SettingsStore(file).Load();
            Equal(100, value.ScalePercent, "旧150固定为100");
            Equal("zh", (string)language.GetValue(value, null), "非法语言回退中文");
        }
        finally { Directory.Delete(dir, true); }
    }

    private static void FailedAtomicSavePreservesExistingFile()
    {
        string dir = CreateTestDirectory();
        try
        {
            string file = Path.Combine(dir, "settings.json");
            const string original = "{\"AlwaysOnTop\":true,\"ScalePercent\":125}";
            File.WriteAllText(file, original, new System.Text.UTF8Encoding(false));

            bool saved;
            using (FileStream locked = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                AppSettings replacement = new AppSettings();
                replacement.AlwaysOnTop = false;
                replacement.ScalePercent = 150;
                saved = new SettingsStore(file).Save(replacement);
            }

            False(saved, "被锁定时保存应报告失败");
            Equal(original, File.ReadAllText(file, System.Text.Encoding.UTF8), "原设置必须保留");
            Equal(0, Directory.GetFiles(dir, "*.tmp").Length, "失败后清理临时文件");
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    private static string Account(string type, string plan)
    {
        string planJson = plan == null ? "null" : "\"" + plan + "\"";
        return "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"account\":{\"type\":\"" + type + "\",\"planType\":" + planJson + "}}}";
    }

    private static string EmptyLimits()
    {
        return @"{""jsonrpc"":""2.0"",""id"":2,""result"":{""rateLimitsByLimitId"":{},""rateLimits"":null}}";
    }

    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute, int second)
    {
        return new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);
    }

    private static Dictionary<string, QuotaWindow> Index(List<QuotaWindow> windows)
    {
        Dictionary<string, QuotaWindow> result = new Dictionary<string, QuotaWindow>(StringComparer.Ordinal);
        foreach (QuotaWindow window in windows)
        {
            result.Add(window.Id, window);
        }
        return result;
    }

    private static string CreateTestDirectory()
    {
        string baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string path = Path.Combine(baseDirectory, "settings-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void AssertDefaults(AppSettings settings, string context)
    {
        True(settings.AlwaysOnTop, context + "默认置顶");
        Equal(100, settings.ScalePercent, context + "默认尺寸");
        Equal(null, settings.SelectedWindowId, context + "默认窗口");
        False(settings.X.HasValue, context + "默认 X");
        False(settings.Y.HasValue, context + "默认 Y");
    }

    private static InvalidOperationException ThrowsInvalidOperation(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException ex)
        {
            return ex;
        }
        throw new Exception("预期 InvalidOperationException，但没有抛出。");
    }

    private static void Run(string name, Action test)
    {
        testsRun++;
        try
        {
            test();
            Console.WriteLine("PASS: " + name);
        }
        catch (Exception ex)
        {
            failures++;
            Console.WriteLine("FAIL: {0} -- {1}", name, ex.Message);
        }
    }

    private static void Equal<T>(T expected, T actual, string context)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new Exception(string.Format(CultureInfo.InvariantCulture,
                "{0}: expected <{1}>, actual <{2}>", context, expected, actual));
        }
    }

    private static void NotEqual<T>(T left, T right, string context)
    {
        if (EqualityComparer<T>.Default.Equals(left, right))
        {
            throw new Exception(context + ": values should differ");
        }
    }

    private static void True(bool value, string context)
    {
        if (!value)
        {
            throw new Exception(context + ": expected true");
        }
    }

    private static void False(bool value, string context)
    {
        if (value)
        {
            throw new Exception(context + ": expected false");
        }
    }

    private static void Contains(string actual, string expectedPart, string context)
    {
        if (actual == null || actual.IndexOf(expectedPart, StringComparison.Ordinal) < 0)
        {
            throw new Exception(context + ": expected text containing <" + expectedPart + ">");
        }
    }

    private static void DoesNotContain(string actual, string forbiddenPart, string context)
    {
        if (actual != null && actual.IndexOf(forbiddenPart, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            throw new Exception(context + ": forbidden text was present");
        }
    }
}
