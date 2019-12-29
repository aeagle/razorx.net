using HtmlAgilityPack;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RazorX.ViewEngine
{
    public class RazorXParser
    {
        const string COMPONENT_TAG_PREFIX = "component";
        const string PARTIAL_SPLIT_TOKEN = "@Model.children";
        const string PARTIAL_SPLIT_CHAR = "¬";

        public static readonly Regex componentTagRegex =
            new Regex(
                $@"<{COMPONENT_TAG_PREFIX}-(.+?)(| (.+?))>(.+?)</{COMPONENT_TAG_PREFIX}-\1>", 
                RegexOptions.Singleline | RegexOptions.Compiled
            );

        public static string ProcessFile(string text)
        {
            if (text.IndexOf($"<{COMPONENT_TAG_PREFIX}-") >= 0)
            {
                MatchEvaluator processComponent = null;
                processComponent =
                    (match) =>
                    {
                        var parsedDoc = new HtmlDocument();
                        parsedDoc.LoadHtml($"<p {match.Groups[2].Value}></p>");
                        var node = parsedDoc.DocumentNode.ChildNodes.FindFirst("p");
                        var propsGuid = Guid.NewGuid().ToString().Replace("-", "");

                        StringBuilder dynamicObject = new StringBuilder("@{ ");

                        dynamicObject.Append($"dynamic props{propsGuid}start = new System.Dynamic.ExpandoObject();");
                        dynamicObject.Append($"props{propsGuid}start.renderTop = true;");
                        foreach (var attribute in node.Attributes)
                        {
                            if (attribute.Value.StartsWith("@"))
                            {
                                dynamicObject.Append($"props{propsGuid}start.{attribute.Name} = {attribute.Value.Substring(1)};");
                            }
                            else
                            {
                                dynamicObject.Append($"props{propsGuid}start.{attribute.Name} = \"{attribute.Value}\";");
                            }
                        }

                        dynamicObject.Append($"dynamic props{propsGuid}end = new System.Dynamic.ExpandoObject();");
                        dynamicObject.Append($"props{propsGuid}end.renderTop = false;");
                        foreach (var attribute in node.Attributes)
                        {
                            if (attribute.Value.StartsWith("@"))
                            {
                                dynamicObject.Append($"props{propsGuid}end.{attribute.Name} = {attribute.Value.Substring(1)};");
                            }
                            else
                            {
                                dynamicObject.Append($"props{propsGuid}end.{attribute.Name} = \"{attribute.Value}\";");
                            }
                        }
                        dynamicObject.Append("}\r\n");
                        dynamicObject.AppendLine($"@Html.Partial(\"{match.Groups[1]}\", (object)props{propsGuid}start)");

                        if (match.Groups[4].Value.IndexOf($"<{COMPONENT_TAG_PREFIX}-") >= 0)
                        {
                            // Recursively process component tags
                            dynamicObject.AppendLine(
                                componentTagRegex.Replace(
                                    match.Groups[4].Value,
                                    processComponent
                                )
                            );
                        }
                        else
                        {
                            dynamicObject.AppendLine(
                                match.Groups[4].Value
                            );
                        }

                        dynamicObject.AppendLine($"@Html.Partial(\"{match.Groups[1]}\", (object)props{propsGuid}end)");

                        var replacement = dynamicObject.ToString();
                        return replacement;
                    };

                text = componentTagRegex.Replace(text, processComponent);
            }

            if (text.IndexOf(PARTIAL_SPLIT_TOKEN) >= 0)
            {
                var renderParts = 
                    text
                        .Replace(PARTIAL_SPLIT_TOKEN, PARTIAL_SPLIT_CHAR)
                        .Split(PARTIAL_SPLIT_CHAR.ToCharArray());

                StringBuilder newText = new StringBuilder();
                newText.AppendLine("@if (Model.renderTop) {");
                newText.AppendLine(string.Join("\r\n", renderParts[0].Split("\r\n".ToCharArray()).Select(x => x.Trim().StartsWith("@") ? x : $"@:{x}")));
                newText.AppendLine("}");
                
                if (renderParts.Length > 1)
                {
                    newText.AppendLine("@if (!Model.renderTop) {");
                    newText.AppendLine(string.Join("\r\n", renderParts[1].Split("\r\n".ToCharArray()).Select(x => x.Trim().StartsWith("@") ? x : $"@:{x}")));
                    newText.AppendLine("}");
                }
                
                text = newText.ToString();
            }

            return text;
        }
    }
}
