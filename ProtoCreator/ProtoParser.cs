using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class ProtoParser
{
    public ProtoDetail Parse(string filePath)
    {
        var protoDetail = new ProtoDetail();
        var content = File.ReadAllText(filePath);

        // 解析 syntax 信息
        var syntaxMatch = Regex.Match(content, @"syntax\s*=\s*""([^""]+)"";");
        if (syntaxMatch.Success)
        {
            protoDetail.Syntax = syntaxMatch.Groups[1].Value;
        }

        // 解析 package 名称
        var packageMatch = Regex.Match(content, @"package\s+([\w\.]+);");
        if (packageMatch.Success)
        {
            protoDetail.PackageName = packageMatch.Groups[1].Value;
        }

        // 解析 import 信息
        var importPattern = new Regex(@"import\s+""([^""]+)"";");
        foreach (Match match in importPattern.Matches(content))
        {
            protoDetail.Imports.Add(match.Groups[1].Value);
        }

        // 解析 message 定义
        var messagePattern = new Regex(@"message\s+(\w+)\s*\{(.*?)\}", RegexOptions.Singleline);
        foreach (Match match in messagePattern.Matches(content))
        {
            var messageName = match.Groups[1].Value;
            var messageBody = match.Groups[2].Value;

            var messageDetail = new MessageDetail { Name = messageName };
            messageDetail.Fields = ParseFields(messageBody);
            protoDetail.Messages.Add(messageDetail);
        }

        // 解析 service 定义
        var servicePattern = new Regex(@"service\s+(\w+)\s*\{(.*?)\}", RegexOptions.Singleline);
        foreach (Match match in servicePattern.Matches(content))
        {
            var serviceName = match.Groups[1].Value;
            var serviceBody = match.Groups[2].Value;

            var serviceDetail = new ServiceDetail { Name = serviceName };
            serviceDetail.Methods = ParseMethods(serviceBody);
            protoDetail.Services.Add(serviceDetail);
        }

        // 解析 enum 定义
        var enumPattern = new Regex(@"enum\s+(\w+)\s*\{(.*?)\}", RegexOptions.Singleline);
        foreach (Match match in enumPattern.Matches(content))
        {
            var enumName = match.Groups[1].Value;
            var enumBody = match.Groups[2].Value;

            var enumDetail = new EnumDetail { Name = enumName };
            enumDetail.Values = ParseEnumValues(enumBody);
            protoDetail.Enums.Add(enumDetail);
        }

        return protoDetail;
    }
    private List<FieldDetail> ParseFields(string messageBody)
    {
        var fields = new List<FieldDetail>();
        var fieldPattern = new Regex(@"(\brequired\b|\boptional\b|\brepeated\b)?\s*(map<\s*([\w\.]+)\s*,\s*([\w\.]+)\s*>|[\w\.]+)\s+(\w+)\s*=\s*(\d+);");

        foreach (Match match in fieldPattern.Matches(messageBody))
        {
            var modifier = match.Groups[1].Value; // required, optional, repeated
            var type = match.Groups[2].Value; // 完整类型名称或 map 类型
            var name = match.Groups[5].Value;
            var number = int.Parse(match.Groups[6].Value);

            // 处理 map 类型
            if (type.StartsWith("map<"))
            {
                var keyType = match.Groups[3].Value;
                var valueType = match.Groups[4].Value;
                type = $"map<{keyType}, {valueType}>";
            }

            fields.Add(new FieldDetail
            {
                Modifier = modifier,
                Type = type,
                Name = name,
                Number = number
            });
        }

        return fields;
    }

    private List<MethodDetail> ParseMethods(string serviceBody)
    {
        var methods = new List<MethodDetail>();
        var methodPattern = new Regex(@"rpc\s+(\w+)\s*\(([\w\.]+)\)\s*returns\s*\(([\w\.]+)\);");
        foreach (Match match in methodPattern.Matches(serviceBody))
        {
            methods.Add(new MethodDetail
            {
                Name = match.Groups[1].Value,
                InputType = match.Groups[2].Value,
                OutputType = match.Groups[3].Value
            });
        }
        return methods;
    }

    private List<EnumValueDetail> ParseEnumValues(string enumBody)
    {
        var values = new List<EnumValueDetail>();
        var valuePattern = new Regex(@"(\w+)\s*=\s*(\d+);");
        foreach (Match match in valuePattern.Matches(enumBody))
        {
            values.Add(new EnumValueDetail
            {
                Name = match.Groups[1].Value,
                Value = int.Parse(match.Groups[2].Value)
            });
        }
        return values;
    }

    public void GenerateProtoFile(ProtoDetail protoDetail, string outputPath)
    {
        using (var writer = new StreamWriter(outputPath))
        {
            // 写入 syntax
            if (!string.IsNullOrEmpty(protoDetail.Syntax))
            {
                writer.WriteLine($"syntax = \"{protoDetail.Syntax}\";");
                writer.WriteLine();
            }

            // 写入 package
            if (!string.IsNullOrEmpty(protoDetail.PackageName))
            {
                writer.WriteLine($"package {protoDetail.PackageName};");
                writer.WriteLine();
            }

            // 写入 imports
            if (protoDetail.Imports.Count > 0)
            {
                foreach (var import in protoDetail.Imports)
                {
                    writer.WriteLine($"import \"{import}\";");
                }
                writer.WriteLine();
            }

            // 写入 messages
            foreach (var message in protoDetail.Messages)
            {
                writer.WriteLine($"message {message.Name} {{");
                foreach (var field in message.Fields)
                {
                    if (field.Type.StartsWith("map<"))
                    {
                        writer.WriteLine($"  {field.Modifier} {field.Type} {field.Name} = {field.Number};");
                    }
                    else
                    {
                        writer.WriteLine($"  {field.Modifier} {field.Type} {field.Name} = {field.Number};");
                    }
                }
                writer.WriteLine("}");
                writer.WriteLine();
            }

            // 写入 services
            foreach (var service in protoDetail.Services)
            {
                writer.WriteLine($"service {service.Name} {{");
                foreach (var method in service.Methods)
                {
                    writer.WriteLine($"  rpc {method.Name}({method.InputType}) returns ({method.OutputType});");
                }
                writer.WriteLine("}");
                writer.WriteLine();
            }

            // 写入 enums
            foreach (var enumDetail in protoDetail.Enums)
            {
                writer.WriteLine($"enum {enumDetail.Name} {{");
                foreach (var value in enumDetail.Values)
                {
                    writer.WriteLine($"  {value.Name} = {value.Value};");
                }
                writer.WriteLine("}");
                writer.WriteLine();
            }
        }
    }
}
public class ProtoDetail
{
    public string Syntax { get; set; } // 存储 syntax 信息
    public string PackageName { get; set; }
    public List<string> Imports { get; set; } = new List<string>(); // 存储 import 信息
    public List<MessageDetail> Messages { get; set; } = new List<MessageDetail>();
    public List<ServiceDetail> Services { get; set; } = new List<ServiceDetail>();
    public List<EnumDetail> Enums { get; set; } = new List<EnumDetail>();
}

public class MessageDetail
{
    public string Name { get; set; }
    public List<FieldDetail> Fields { get; set; } = new List<FieldDetail>();
}

public class FieldDetail
{
    public string Modifier { get; set; }
    public string Type { get; set; }
    public string Name { get; set; }
    public int Number { get; set; }
}

public class ServiceDetail
{
    public string Name { get; set; }
    public List<MethodDetail> Methods { get; set; } = new List<MethodDetail>();
}

public class MethodDetail
{
    public string Name { get; set; }
    public string InputType { get; set; }
    public string OutputType { get; set; }
}

public class EnumDetail
{
    public string Name { get; set; }
    public List<EnumValueDetail> Values { get; set; } = new List<EnumValueDetail>();
}

public class EnumValueDetail
{
    public string Name { get; set; }
    public int Value { get; set; }
}

