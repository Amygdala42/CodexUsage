using System;
using System.Text.RegularExpressions;

namespace CodexQuotaLite
{
    internal static class UiText
    {
        private static string language = "zh";
        internal static string Language { get { return language; } set { language = value == "en" ? "en" : "zh"; } }
        internal static bool English { get { return language == "en"; } }
        internal static string T(string chinese, string english) { return English ? english : chinese; }
        internal static string AppName { get { return "CodexUsage"; } }
        internal static string WindowLabel(string label)
        {
            if (!English || String.IsNullOrEmpty(label)) return label ?? String.Empty;
            string result = label.Replace("未知周期额度", "Unknown window").Replace("未知周期", "Unknown window")
                .Replace("每周额度", "Weekly").Replace("每周", "Weekly");
            result = Regex.Replace(result, @"(\d+)\s*小时(?:额度)?", "$1h");
            result = Regex.Replace(result, @"(\d+)\s*分钟(?:额度)?", "$1 min");
            result = Regex.Replace(result, @"(\d+)\s*天(?:额度)?", "$1d");
            return result.Replace("示例窗口", "Sample window").Replace("示例额度", "Sample window");
        }
        internal static string Plan(string label)
        { return label == "未知套餐" ? T("未知套餐", "Unknown plan") : label; }
        internal static string Error(string message)
        {
            if (!English || String.IsNullOrWhiteSpace(message)) return message;
            if (message.StartsWith("设置")) return "Settings could not be saved. Changes may be lost after restart.";
            if (message.Contains("超时")) return "Request timed out. Check your connection and refresh.";
            if (message.Contains("未找到") || message.Contains("不存在")) return "Codex was not found. Install Codex and sign in, then refresh.";
            if (message.Contains("启动 Codex") || message.Contains("连接已退出")) return "Could not start Codex. Check that Codex opens normally.";
            if (message.Contains("目录") || message.Contains("写入")) return "Access was denied. Move the app to a writable folder and try again.";
            if (message.Contains("登录方式") || message.Contains("不能读取订阅")) return "Sign in to Codex with a ChatGPT account to read subscription usage.";
            if (message.Contains("登录") && !message.Contains("网络")) return "Sign in to Codex with your ChatGPT account, then refresh.";
            if (message.Contains("取消")) return "Request cancelled. Refresh to try again.";
            if (message.Contains("无法识别")) return "Codex returned an unreadable response. Refresh or update Codex.";
            if (message.Contains("连接已断开")) return "The Codex connection closed. Refresh to reconnect.";
            if (message.Contains("响应") || message.Contains("通知过多")) return "The usage response could not be read. Please refresh later.";
            if (message.Contains("已过期")) return "The previous result is out of date. Refresh to update it.";
            for (int i = 0; i < message.Length; i++)
                if (message[i] >= 0x4e00 && message[i] <= 0x9fff) return "Usage is unavailable. Check your Codex sign-in and connection, then refresh.";
            return message;
        }
    }
}
