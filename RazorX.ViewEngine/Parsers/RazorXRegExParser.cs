using HtmlAgilityPack;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RazorX.ViewEngine
{
    public class RazorXRegExParser : IRazorXParser
    {
        const string PARTIAL_SPLIT_CHAR = "¬";

        public static readonly Regex componentTagRegex =
            new Regex(
                $@"<{RazorXViewEngine.COMPONENT_TAG_PREFIX}-(.+?)(| (.+?))>(.+?)</{RazorXViewEngine.COMPONENT_TAG_PREFIX}-\1>",
                RegexOptions.Singleline | RegexOptions.Compiled
            );

        public string Process(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                return Process(reader.ReadToEnd());
            }
        }

        public string Process(string text)
        {
            // Process component tags
            if (text.IndexOf($"<{RazorXViewEngine.COMPONENT_TAG_PREFIX}-") >= 0)
            {
                string processComponent(Match match)
                {
                    var parsedDoc = new HtmlDocument();
                    parsedDoc.LoadHtml($"<p {match.Groups[2].Value}></p>");
                    var node = parsedDoc.DocumentNode.ChildNodes.FindFirst("p");
                    var propsGuid = Guid.NewGuid().ToString().Replace("-", "");

                    StringBuilder dynamicObject = new StringBuilder();
                    dynamicObject.Append($"@Html.Partial(\"{match.Groups[1]}\", (object)RazorX.ViewEngine.RazorXProps.Create().Add(\"renderTop\", true)");
                    addProps(dynamicObject, node.Attributes);
                    dynamicObject.AppendLine($".Build())");

                    if (match.Groups[4].Value.IndexOf($"<{RazorXViewEngine.COMPONENT_TAG_PREFIX}-") >= 0)
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

                    dynamicObject.Append($"@Html.Partial(\"{match.Groups[1]}\", (object)RazorX.ViewEngine.RazorXProps.Create().Add(\"renderTop\", false)");
                    addProps(dynamicObject, node.Attributes);
                    dynamicObject.AppendLine($".Build())");

                    var replacement = dynamicObject.ToString();
                    return replacement;
                };

                text = componentTagRegex.Replace(text, processComponent);
            }

            // Process partials with @Model.children
            if (text.IndexOf($"@{RazorXViewEngine.PARTIAL_SPLIT_TOKEN}") >= 0)
            {
                var renderParts =
                    text
                        .Replace($"@{RazorXViewEngine.PARTIAL_SPLIT_TOKEN}", PARTIAL_SPLIT_CHAR)
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

        private static void addProps(StringBuilder dynamicObject, HtmlAttributeCollection attributes)
        {
            foreach (var attribute in attributes)
            {
                if (attribute.Value.StartsWith("@"))
                {
                    dynamicObject.Append($".Add(\"{attribute.Name}\", {attribute.Value.Substring(1)})");
                }
                else
                {
                    dynamicObject.Append($".Add(\"{attribute.Name}\", \"{attribute.Value}\")");
                }
            }
        }
    }
}
