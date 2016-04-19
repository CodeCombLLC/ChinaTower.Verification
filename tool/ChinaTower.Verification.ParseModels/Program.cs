using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace ChinaTower.Verification.ParseModels
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("请输入欲转换为Headers数组的模型源码路径:");
            var path = Console.ReadLine();
            var files = Directory.GetFiles(path);
            var sb = new StringBuilder();
            foreach (var f in files)
            {
                if (Path.GetExtension(f) != ".cs")
                    continue;
                var txt = File.ReadAllText(f);
                var PullTable = new Regex("(?<=TableName = \").*(?=\"\\)])");
                var matchTable = PullTable.Match(txt);
                if (!matchTable.Success)
                    continue;
                var tableName = matchTable.Value;
                Console.WriteLine($"找到模型{tableName}...");
                var pullFields = new Regex("(?<=ColumnName = \").*(?=\"\\)])");
                var matchFields = pullFields.Matches(txt);
                var fields = new List<string>();
                foreach (Match m in matchFields)
                    fields.Add(m.Value);
                Console.WriteLine(string.Join(",", fields));
                sb.AppendLine("{ FormType." + tableName + ", new string[] { \"" + string.Join("\",\"", fields) + "\" } },");
            }
            Console.WriteLine("完成，按任意键退出...");
            File.WriteAllText(Path.Combine(path, "output.txt"), sb.ToString());
            Console.Read();
        }
    }
}
