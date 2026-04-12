using System;

namespace VPetLLM.Utils.Tests
{
    public static class XmlTagProcessorTests
    {
        public static void RunTests()
        {
            Console.WriteLine("=== XmlTagProcessor 测试开始 ===\n");

            Test1_BasicXmlTagConversion();
            Test2_UnknownTagFiltering();
            Test3_ExecuteActionProcessing();
            Test4_SayTagProcessing();
            Test5_MixedContent();
            Test6_PluginResultFiltering();
            Test7_PluginTagRegistration();
            Test8_KeepContentFilterMode();

            Console.WriteLine("\n=== XmlTagProcessor 测试完成 ===");
        }

        private static void Test1_BasicXmlTagConversion()
        {
            Console.WriteLine("测试1: 基本XML标签转换");
            string input = "<say>你好！</say>";
            string result = Common.XmlTagProcessor.ProcessXmlTags(input);
            
            Console.WriteLine($"输入: {input}");
            Console.WriteLine($"输出: {result}");
            Console.WriteLine($"结果: {(result.Contains("<|say_begin|>") ? "✅ 通过" : "❌ 失败")}\n");
        }

        private static void Test2_UnknownTagFiltering()
        {
            Console.WriteLine("测试2: 未知标签过滤");
            string input = "<unknown_tag>这会被过滤</unknown_tag><say>这不会被过滤</say>";
            string result = Common.XmlTagProcessor.ProcessXmlTags(input);
            
            Console.WriteLine($"输入: {input}");
            Console.WriteLine($"输出: {result}");
            Console.WriteLine($"结果: {(result.Contains("这不会被过滤") && !result.Contains("这会被过滤") ? "✅ 通过" : "❌ 失败")}\n");
        }

        private static void Test3_ExecuteActionProcessing()
        {
            Console.WriteLine("测试3: execute_action标签处理");
            string input = @"<execute_action>
     <action_type>PetInteraction</action_type>
     <target_emotion>Curiosity</target_emotion>
     <action_description>Gently petting the user's arm in response to the touch.</action_description>
 </execute_action>";
            string result = Common.XmlTagProcessor.ProcessXmlTags(input);
            
            Console.WriteLine($"输入包含 execute_action 标签");
            Console.WriteLine($"输出: {result.Substring(0, Math.Min(result.Length, 100))}...");
            Console.WriteLine($"结果: {(result.Contains("<|action_begin|>") ? "✅ 通过" : "❌ 失败")}\n");
        }

        private static void Test4_SayTagProcessing()
        {
            Console.WriteLine("测试4: say标签处理（包含中文引号）");
            string input = "<say>\u201C嗯？你叫我了？\u201D</say><say>\u201C怎么了？是不是想我了呀？\u201D</say>";
            string result = Common.XmlTagProcessor.ProcessXmlTags(input);
            
            Console.WriteLine($"输入: {input}");
            Console.WriteLine($"输出: {result}");
            Console.WriteLine($"结果: {(result.Contains("嗯？你叫我了？") && result.Contains("怎么了？是不是想我了呀？") ? "✅ 通过" : "❌ 失败")}\n");
        }

        private static void Test5_MixedContent()
        {
            Console.WriteLine("测试5: 混合内容处理");
            string input = @"普通文本
<execute_action>
     <action_type>PetInteraction</action_type>
     <target_emotion>Curiosity</target_emotion>
     <action_description>Gently petting the user's arm</action_description>
 </execute_action>
<say>\u201C嗯？你叫我了？\u201D</say>
<unknown_tag>被过滤</unknown_tag>
<say>\u201C怎么了？是不是想我了呀？\u201D</say>
更多普通文本";
            string result = Common.XmlTagProcessor.ProcessXmlTags(input);
            
            Console.WriteLine($"输入包含混合内容");
            Console.WriteLine($"输出包含有效标签: {(result.Contains("<|action_begin|>") && result.Contains("<|say_begin|>") ? "是" : "否")}");
            Console.WriteLine($"输出不包含未知标签: {(!result.Contains("unknown_tag") ? "是" : "否")}");
            Console.WriteLine($"结果: {(result.Contains("<|action_begin|>") && result.Contains("<|say_begin|>") && !result.Contains("unknown_tag") ? "✅ 通过" : "❌ 失败")}\n");
        }

        private static void Test6_PluginResultFiltering()
        {
            Console.WriteLine("测试6: 插件结果XML过滤");
            string input = "[Plugin Result: Weather] <weather>晴天</weather><temperature>25度</temperature><say>今天天气不错！</say>";
            string result = Common.XmlTagProcessor.FilterPluginResultXml(input);
            
            Console.WriteLine($"输入: {input}");
            Console.WriteLine($"输出: {result}");
            bool pass = result.Contains("<|say_begin|>") && !result.Contains("<weather>") && !result.Contains("<temperature>");
            Console.WriteLine($"结果: {(pass ? "✅ 通过" : "❌ 失败")}\n");
        }

        private static void Test7_PluginTagRegistration()
        {
            Console.WriteLine("测试7: 插件标签注册");
            
            Common.XmlTagProcessor.RegisterPluginTags("WeatherPlugin", new[]
            {
                new Common.PluginTagRegistration
                {
                    PluginName = "WeatherPlugin",
                    TagName = "weather_report",
                    CommandType = null,
                    FilterMode = Common.XmlTagFilterMode.KeepContent
                }
            });

            string input = "<weather_report>北京：晴，25°C</weather_report><unknown_data>被过滤</unknown_data>";
            string result = Common.XmlTagProcessor.ProcessXmlTags(input);
            
            Console.WriteLine($"输入: {input}");
            Console.WriteLine($"输出: {result}");
            bool pass = result.Contains("北京：晴，25°C") && !result.Contains("unknown_data") && !result.Contains("<weather_report>");
            Console.WriteLine($"结果: {(pass ? "✅ 通过" : "❌ 失败")}");

            Common.XmlTagProcessor.UnregisterPluginTags("WeatherPlugin");
            Console.WriteLine($"插件标签已注销\n");
        }

        private static void Test8_KeepContentFilterMode()
        {
            Console.WriteLine("测试8: KeepContent过滤模式");
            
            Common.XmlTagProcessor.RegisterTag("custom_data", null, Common.XmlTagFilterMode.KeepContent);

            string input = "<custom_data>重要信息</custom_data><random_tag>被过滤</random_tag>";
            string result = Common.XmlTagProcessor.ProcessXmlTags(input);
            
            Console.WriteLine($"输入: {input}");
            Console.WriteLine($"输出: {result}");
            bool pass = result.Contains("重要信息") && !result.Contains("被过滤");
            Console.WriteLine($"结果: {(pass ? "✅ 通过" : "❌ 失败")}\n");
        }
    }
}
