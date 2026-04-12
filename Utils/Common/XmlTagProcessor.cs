using System.Text.RegularExpressions;

namespace VPetLLM.Utils.Common
{
    public enum XmlTagFilterMode
    {
        ConvertToCommand,
        StripTag,
        KeepContent
    }

    public class PluginTagRegistration
    {
        public string PluginName { get; set; }
        public string TagName { get; set; }
        public string CommandType { get; set; }
        public XmlTagFilterMode FilterMode { get; set; } = XmlTagFilterMode.StripTag;
    }

    public static class XmlTagProcessor
    {
        private static readonly HashSet<string> SupportedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "execute_action",
            "say",
            "talk",
            "action",
            "happy",
            "health",
            "exp",
            "move",
            "buy",
            "use_item",
            "plugin",
            "tool",
            "record",
            "record_modify",
            "vpet_settings",
            "play"
        };

        private static readonly Dictionary<string, string> TagToCommandTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "execute_action", "action" },
            { "say", "say" },
            { "talk", "talk" },
            { "action", "action" },
            { "happy", "happy" },
            { "health", "health" },
            { "exp", "exp" },
            { "move", "move" },
            { "buy", "buy" },
            { "use_item", "use_item" },
            { "plugin", "plugin" },
            { "tool", "tool" },
            { "record", "record" },
            { "record_modify", "record_modify" },
            { "vpet_settings", "vpet_settings" },
            { "play", "play" }
        };

        private static readonly Dictionary<string, XmlTagFilterMode> TagFilterModes = new Dictionary<string, XmlTagFilterMode>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, List<PluginTagRegistration>> _pluginRegistrations = new Dictionary<string, List<PluginTagRegistration>>(StringComparer.OrdinalIgnoreCase);

        private static readonly Regex _xmlTagRegex = new Regex(@"<\s*(\w+)(?:\s[^>]*)?\s*>(.*?)<\s*/\s*\1\s*>", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex _xmlDetectRegex = new Regex(@"<\s*(\w+)\s*>", RegexOptions.Compiled);

        public static bool ContainsXmlTags(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return _xmlDetectRegex.IsMatch(text);
        }

        public static string ProcessXmlTags(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            Logger.Log($"XmlTagProcessor: 开始处理XML标签，原始文本长度: {text.Length}");

            var result = new StringBuilder();
            int currentIndex = 0;

            var matches = _xmlTagRegex.Matches(text);

            foreach (Match match in matches)
            {
                string tagName = match.Groups[1].Value.Trim();
                string tagContent = match.Groups[2].Value.Trim();
                int matchStart = match.Index;
                int matchLength = match.Length;

                if (matchStart > currentIndex)
                {
                    result.Append(text.Substring(currentIndex, matchStart - currentIndex));
                }

                if (SupportedTags.Contains(tagName))
                {
                    string convertedCommand = ConvertXmlTagToCommand(tagName, tagContent);
                    result.Append(convertedCommand);
                    Logger.Log($"XmlTagProcessor: 处理已知标签 <{tagName}>，转换为: {convertedCommand.Substring(0, Math.Min(convertedCommand.Length, 50))}...");
                }
                else
                {
                    var filterMode = GetFilterMode(tagName);
                    switch (filterMode)
                    {
                        case XmlTagFilterMode.KeepContent:
                            result.Append(tagContent);
                            Logger.Log($"XmlTagProcessor: 保留未知标签 <{tagName}> 的内容: {tagContent.Substring(0, Math.Min(tagContent.Length, 30))}...");
                            break;
                        case XmlTagFilterMode.StripTag:
                        default:
                            Logger.Log($"XmlTagProcessor: 过滤未知标签 <{tagName}>");
                            break;
                    }
                }

                currentIndex = matchStart + matchLength;
            }

            if (currentIndex < text.Length)
            {
                result.Append(text.Substring(currentIndex));
            }

            var finalResult = result.ToString();
            Logger.Log($"XmlTagProcessor: 处理完成，结果长度: {finalResult.Length}");
            return finalResult;
        }

        public static string FilterPluginResultXml(string pluginResult)
        {
            if (string.IsNullOrEmpty(pluginResult))
                return pluginResult;

            if (!ContainsXmlTags(pluginResult))
                return pluginResult;

            Logger.Log($"XmlTagProcessor: 过滤插件结果中的XML标签，原始长度: {pluginResult.Length}");

            var result = new StringBuilder();
            int currentIndex = 0;

            var matches = _xmlTagRegex.Matches(pluginResult);

            foreach (Match match in matches)
            {
                string tagName = match.Groups[1].Value.Trim();
                string tagContent = match.Groups[2].Value.Trim();
                int matchStart = match.Index;
                int matchLength = match.Length;

                if (matchStart > currentIndex)
                {
                    result.Append(pluginResult.Substring(currentIndex, matchStart - currentIndex));
                }

                if (SupportedTags.Contains(tagName))
                {
                    string convertedCommand = ConvertXmlTagToCommand(tagName, tagContent);
                    result.Append(convertedCommand);
                    Logger.Log($"XmlTagProcessor: 插件结果中已知标签 <{tagName}> 已转换");
                }
                else
                {
                    var filterMode = GetFilterMode(tagName);
                    switch (filterMode)
                    {
                        case XmlTagFilterMode.KeepContent:
                            result.Append(tagContent);
                            Logger.Log($"XmlTagProcessor: 插件结果中保留标签 <{tagName}> 的内容");
                            break;
                        case XmlTagFilterMode.StripTag:
                        default:
                            Logger.Log($"XmlTagProcessor: 插件结果中过滤未知标签 <{tagName}>");
                            break;
                    }
                }

                currentIndex = matchStart + matchLength;
            }

            if (currentIndex < pluginResult.Length)
            {
                result.Append(pluginResult.Substring(currentIndex));
            }

            return result.ToString();
        }

        private static XmlTagFilterMode GetFilterMode(string tagName)
        {
            if (TagFilterModes.TryGetValue(tagName, out var mode))
                return mode;

            return XmlTagFilterMode.StripTag;
        }

        private static string ConvertXmlTagToCommand(string tagName, string tagContent)
        {
            if (TagToCommandTypeMap.TryGetValue(tagName, out string commandType))
            {
                string processedContent = ProcessTagContent(tagName, tagContent);
                return $"<|{commandType}_begin|> {processedContent} <|{commandType}_end|>";
            }
            return string.Empty;
        }

        private static string ProcessTagContent(string tagName, string content)
        {
            content = content.Trim();

            switch (tagName.ToLower())
            {
                case "execute_action":
                    return ProcessExecuteActionContent(content);
                case "say":
                    return ProcessSayContent(content);
                default:
                    return content;
            }
        }

        private static string ProcessExecuteActionContent(string content)
        {
            var actionTypeMatch = Regex.Match(content, @"<\s*action_type\s*>(.*?)<\s*/\s*action_type\s*>", RegexOptions.Singleline);

            if (actionTypeMatch.Success)
            {
                string actionType = actionTypeMatch.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(actionType))
                {
                    return actionType;
                }
            }

            return content;
        }

        private static string ProcessSayContent(string content)
        {
            content = content.Trim();

            if (content.StartsWith("\"") && content.EndsWith("\""))
                return content;

            if (content.StartsWith("\u201C") && content.EndsWith("\u201D"))
                return content;

            return $"\"{content}\"";
        }

        public static string[] GetSupportedTags()
        {
            return SupportedTags.ToArray();
        }

        public static void RegisterTag(string tagName, string commandType)
        {
            if (string.IsNullOrEmpty(tagName) || string.IsNullOrEmpty(commandType))
                return;

            SupportedTags.Add(tagName);
            TagToCommandTypeMap[tagName] = commandType;
            Logger.Log($"XmlTagProcessor: 注册新标签 <{tagName}> -> {commandType}");
        }

        public static void RegisterTag(string tagName, string commandType, XmlTagFilterMode filterMode)
        {
            if (string.IsNullOrEmpty(tagName))
                return;

            if (!string.IsNullOrEmpty(commandType))
            {
                SupportedTags.Add(tagName);
                TagToCommandTypeMap[tagName] = commandType;
            }

            TagFilterModes[tagName] = filterMode;
            Logger.Log($"XmlTagProcessor: 注册标签 <{tagName}> -> {commandType ?? "(无映射)"}, 过滤模式: {filterMode}");
        }

        public static void RegisterPluginTags(string pluginName, IEnumerable<PluginTagRegistration> registrations)
        {
            if (string.IsNullOrEmpty(pluginName) || registrations is null)
                return;

            var regList = registrations.ToList();
            _pluginRegistrations[pluginName] = regList;

            foreach (var reg in regList)
            {
                if (!string.IsNullOrEmpty(reg.CommandType))
                {
                    SupportedTags.Add(reg.TagName);
                    TagToCommandTypeMap[reg.TagName] = reg.CommandType;
                }

                TagFilterModes[reg.TagName] = reg.FilterMode;
                Logger.Log($"XmlTagProcessor: 插件 {pluginName} 注册标签 <{reg.TagName}> -> {reg.CommandType ?? "(无映射)"}, 过滤模式: {reg.FilterMode}");
            }
        }

        public static void UnregisterPluginTags(string pluginName)
        {
            if (string.IsNullOrEmpty(pluginName))
                return;

            if (pluginName == "__all__")
            {
                foreach (var kvp in _pluginRegistrations.ToList())
                {
                    UnregisterPluginTags(kvp.Key);
                }
                return;
            }

            if (!_pluginRegistrations.TryGetValue(pluginName, out var registrations))
                return;

            foreach (var reg in registrations)
            {
                if (SupportedTags.Contains(reg.TagName) && !IsBuiltInTag(reg.TagName))
                {
                    SupportedTags.Remove(reg.TagName);
                    TagToCommandTypeMap.Remove(reg.TagName);
                }

                TagFilterModes.Remove(reg.TagName);
                Logger.Log($"XmlTagProcessor: 插件 {pluginName} 注销标签 <{reg.TagName}>");
            }

            _pluginRegistrations.Remove(pluginName);
        }

        private static bool IsBuiltInTag(string tagName)
        {
            var builtInTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "execute_action", "say", "talk", "action", "happy", "health",
                "exp", "move", "buy", "use_item", "plugin", "tool",
                "record", "record_modify", "vpet_settings", "play"
            };
            return builtInTags.Contains(tagName);
        }

        public static bool IsTagRegistered(string tagName)
        {
            return SupportedTags.Contains(tagName);
        }
    }
}
