using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NestedDropdownMenuSystem.Sample.Runtime
{
    public static class SyntaxHighlighter
    {
        private static readonly Dictionary<string, List<string>> Patterns = new()
        {
            ["type"] = new List<string>
            {
                "(?<!\\.)Range",
                "(?<!\\.)List",
                "Func",
                "T",
                nameof(Enumerable),
                
                "(?<!\\.)Header",
                "(?<!\\.)Space",
                "Multiline",
                "NonReorderable",
                "NonSerialized",
                "HideInInspector",
                nameof(Vector2),
                nameof(Color),
                nameof(Screen),
                nameof(Debug),
            },
            ["method"] = new List<string>
            {
                nameof(ToString),
                nameof(string.ToUpper),
                nameof(string.ToLower),
                nameof(Enumerable.Range),
                nameof(Enumerable.Select),
                nameof(Debug.Log),
                nameof(NestedDropdownMenu.AddItem),
                nameof(NestedDropdownMenu.AddDisabledItem),
                nameof(NestedDropdownMenu.AddSeparator),
            },
            ["keyword"] = new List<string>
            {
                "public",
                "protected",
                "private",
                "readonly",
                "class",
                "struct",
                "return",
                "void",
                "bool",
                "int",
                "uint",
                "float",
                "string",
                "new",
                "nameof",
                "true",
                "false",
                "null",
                "var",
                "using",
                "this",
            },
            ["symbol"] = new List<string>
            {
                @"[{}()=;,+\-*/<>|\[\]]+",
            },
            ["digit"] = new List<string>
            {
                @"(?<![a-zA-Z_])[+-]?[0-9]+\.?[0-9]?(([eE][+-]?)?[0-9]+)?f?"
            },
            ["str"] = new List<string>
            {
                "(\\$?\"[^\"\\n]*?\")"
            },
            ["parameterName"] = new List<string>
            {
                "([a-zA-Z_]+[0-9a-zA-Z_]*:)"
            },
            ["comment"] = new List<string>
            {
                @"/\*[\s\S]*?\*/|//.*"
            }
        };

        private static readonly Dictionary<string, string> ColorTable = new()
        {
            { "field", "#66C3CC"},
            { "type", "#C191FF" },
            { "method", "#39CC8F" },
            { "keyword", "#6C95EB" },
            { "symbol", "#BDBDBD" },
            { "digit", "#ED94C0" },
            { "str", "#C9A26D" },
            { "parameterName", "#787878" },
            { "comment", "#85C46C" },
        };

        private static Regex _regex;

        private static Regex CreateRegex()
        {
            const string forwardSeparator = "(?<![0-9a-zA-Z_])";
            const string backwardSeparator = "(?![0-9a-zA-Z_])";
            const string format1 = "(?<{0}>({1}))";
            var format2 = string.Format("(?<{0}>{2}({1}){3})", "{0}", "{1}", forwardSeparator, backwardSeparator);

            var nameAndFormats = new[]
            {
                ("comment", format1),
                ("type", format2),
                ("method", format2),
                ("keyword", format2),
                ("symbol", format1),
                ("digit", format1),
                ("str", format1),
                ("parameterName", format1),
            };

            var patterns = nameAndFormats.Select((pair) =>
            {
                var (name, formatStr) = pair;
                return string.Format(formatStr, name, string.Join("|", Patterns[name]));
            });
            
            var combinedPattern = $"({string.Join("|", patterns)})";
            
            return new Regex(combinedPattern, RegexOptions.Compiled);
        }
        
        private static string ToColoredCode(string code, string color) => $"<color={color}>{code}</color>";

        public static void AddPattern(string name, string pattern)
        {
            Patterns[name].Add(pattern);
            _regex = null;
        }
        
        public static string Highlight(string code)
        {
            _regex ??= CreateRegex();
            return ToColoredCode(_regex.Replace(code, Evaluator), ColorTable["field"]);
            
            
            string Evaluator(Match match)
            {
                foreach (var pair in ColorTable.Where(pair => match.Groups[pair.Key].Success))
                {
                    return ToColoredCode(match.Value, pair.Value);
                }

                return match.Value;
            }
        }
    }
}