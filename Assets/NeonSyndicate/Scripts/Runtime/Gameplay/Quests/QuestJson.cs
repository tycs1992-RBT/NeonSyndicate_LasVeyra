using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace NeonSyndicate
{
    public enum JsonKind
    {
        Null,
        Bool,
        Number,
        String,
        Array,
        Object
    }

    /// <summary>A parsed JSON value with the line and column it started at (both counted from 1).</summary>
    public sealed class JsonNode
    {
        public JsonKind Kind;
        public bool Bool;
        public double Number;
        /// <summary>The string value, or the number exactly as written.</summary>
        public string Text = "";
        public readonly List<JsonNode> Items = new List<JsonNode>();
        /// <summary>Object members in file order.</summary>
        public readonly List<KeyValuePair<string, JsonNode>> Members = new List<KeyValuePair<string, JsonNode>>();
        public int Line;
        public int Column;

        /// <summary>The member with this exact key, or null.</summary>
        public JsonNode Get(string key)
        {
            for (int i = 0; i < Members.Count; i++) if (Members[i].Key == key) return Members[i].Value;
            return null;
        }
    }

    /// <summary>Compares objects by reference (the trace maps bound objects back to their place in the file).</summary>
    public sealed class ReferenceComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceComparer Instance = new ReferenceComparer();

        public new bool Equals(object a, object b)
        {
            return ReferenceEquals(a, b);
        }

        public int GetHashCode(object o)
        {
            return RuntimeHelpers.GetHashCode(o);
        }
    }

    /// <summary>Where each bound object came from, so validation messages can point at a line.</summary>
    public sealed class QuestJsonTrace
    {
        private readonly Dictionary<object, int> _lines = new Dictionary<object, int>(ReferenceComparer.Instance);

        public void Record(object o, int line)
        {
            if (o != null) _lines[o] = line;
        }

        /// <summary>The line of the object's opening brace, or 0 when unknown.</summary>
        public int LineOf(object o)
        {
            int l;
            return o != null && _lines.TryGetValue(o, out l) ? l : 0;
        }
    }

    /// <summary>
    /// A small, strict JSON reader in plain C# (no engine calls), so the quest and event files can be checked in the
    /// EditMode tests and offline. Read binds the text onto the same classes JsonUtility fills in the game and reports
    /// what JsonUtility would silently accept: unknown or misspelled field names (case matters), wrong value types,
    /// nulls, duplicate keys, trailing commas and comments.
    /// </summary>
    public static class QuestJson
    {
        private const int MaxDepth = 64;

        /// <summary>Parses and binds a whole file. Returns null (with errors) when the text is not valid JSON.</summary>
        public static T Read<T>(string text, string fileName, List<string> errors, QuestJsonTrace trace) where T : class, new()
        {
            JsonNode root = Parse(text, fileName, errors);
            if (root == null) return null;
            object bound = Bind(root, typeof(T), "", fileName, errors, trace);
            return bound as T;
        }

        // ================================================================== parsing

        /// <summary>Parses JSON text into a tree. Returns null and adds one error (with line and column) on bad syntax.</summary>
        public static JsonNode Parse(string text, string fileName, List<string> errors)
        {
            Parser p = new Parser(text ?? "", fileName ?? "json");
            try
            {
                p.SkipBom();
                p.SkipSpace();
                JsonNode root = p.Value(0);
                p.SkipSpace();
                if (!p.AtEnd) p.Fail("unexpected text after the end of the document");
                return root;
            }
            catch (JsonSyntaxError e)
            {
                if (errors != null) errors.Add(e.Message);
                return null;
            }
        }

        private sealed class JsonSyntaxError : Exception
        {
            public JsonSyntaxError(string message) : base(message) { }
        }

        private sealed class Parser
        {
            private readonly string _s;
            private readonly string _file;
            private int _i;
            private int _line = 1;
            private int _col = 1;

            public Parser(string s, string file)
            {
                _s = s;
                _file = file;
            }

            public bool AtEnd { get { return _i >= _s.Length; } }

            public void Fail(string message)
            {
                throw new JsonSyntaxError(_file + ":" + _line + ":" + _col + ": " + message);
            }

            private char Peek { get { return _i < _s.Length ? _s[_i] : '\0'; } }

            private char Next()
            {
                char c = _s[_i++];
                if (c == '\n')
                {
                    _line++;
                    _col = 1;
                }
                else _col++;
                return c;
            }

            public void SkipBom()
            {
                if (_i < _s.Length && _s[_i] == '\uFEFF') _i++;
            }

            public void SkipSpace()
            {
                while (_i < _s.Length)
                {
                    char c = _s[_i];
                    if (c == ' ' || c == '\t' || c == '\r' || c == '\n') Next();
                    else if (c == '/') Fail("comments are not allowed in data files (JsonUtility cannot read them)");
                    else break;
                }
            }

            private void Expect(char c)
            {
                if (AtEnd) Fail("expected '" + c + "' but the file ended");
                if (Peek != c) Fail("expected '" + c + "' but found '" + Peek + "'");
                Next();
            }

            public JsonNode Value(int depth)
            {
                if (depth > MaxDepth) Fail("nesting is too deep");
                if (AtEnd) Fail("expected a value but the file ended");
                JsonNode n = new JsonNode { Line = _line, Column = _col };
                char c = Peek;
                if (c == '{') ObjectInto(n, depth);
                else if (c == '[') ArrayInto(n, depth);
                else if (c == '"')
                {
                    n.Kind = JsonKind.String;
                    n.Text = StringValue();
                }
                else if (c == 't') { Word("true"); n.Kind = JsonKind.Bool; n.Bool = true; }
                else if (c == 'f') { Word("false"); n.Kind = JsonKind.Bool; n.Bool = false; }
                else if (c == 'n') { Word("null"); n.Kind = JsonKind.Null; }
                else if (c == '-' || (c >= '0' && c <= '9')) NumberInto(n);
                else Fail("unexpected character '" + c + "'");
                return n;
            }

            private void Word(string w)
            {
                for (int k = 0; k < w.Length; k++)
                {
                    if (AtEnd || Peek != w[k]) Fail("expected '" + w + "'");
                    Next();
                }
            }

            private void ObjectInto(JsonNode n, int depth)
            {
                n.Kind = JsonKind.Object;
                Expect('{');
                SkipSpace();
                if (Peek == '}')
                {
                    Next();
                    return;
                }
                HashSet<string> seen = new HashSet<string>();
                while (true)
                {
                    SkipSpace();
                    if (Peek != '"') Fail(Peek == '}' ? "trailing comma before '}'" : "expected a field name in double quotes");
                    int keyLine = _line, keyCol = _col;
                    string key = StringValue();
                    if (!seen.Add(key)) throw new JsonSyntaxError(_file + ":" + keyLine + ":" + keyCol + ": duplicate field '" + key + "'");
                    SkipSpace();
                    Expect(':');
                    SkipSpace();
                    JsonNode v = Value(depth + 1);
                    n.Members.Add(new KeyValuePair<string, JsonNode>(key, v));
                    SkipSpace();
                    if (Peek == ',')
                    {
                        Next();
                        continue;
                    }
                    if (Peek == '}')
                    {
                        Next();
                        return;
                    }
                    Fail(AtEnd ? "the file ended inside an object" : "expected ',' or '}' after a field");
                }
            }

            private void ArrayInto(JsonNode n, int depth)
            {
                n.Kind = JsonKind.Array;
                Expect('[');
                SkipSpace();
                if (Peek == ']')
                {
                    Next();
                    return;
                }
                while (true)
                {
                    SkipSpace();
                    if (Peek == ']') Fail("trailing comma before ']'");
                    n.Items.Add(Value(depth + 1));
                    SkipSpace();
                    if (Peek == ',')
                    {
                        Next();
                        continue;
                    }
                    if (Peek == ']')
                    {
                        Next();
                        return;
                    }
                    Fail(AtEnd ? "the file ended inside a list" : "expected ',' or ']' in a list");
                }
            }

            private string StringValue()
            {
                Expect('"');
                StringBuilder sb = new StringBuilder();
                while (true)
                {
                    if (AtEnd) Fail("the file ended inside a string");
                    char c = Next();
                    if (c == '"') return sb.ToString();
                    if (c < ' ') Fail("control character inside a string (use \\n for a new line)");
                    if (c != '\\')
                    {
                        sb.Append(c);
                        continue;
                    }
                    if (AtEnd) Fail("the file ended inside an escape");
                    char e = Next();
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                        {
                            int code = 0;
                            for (int k = 0; k < 4; k++)
                            {
                                if (AtEnd) Fail("the file ended inside a \\u escape");
                                char h = Next();
                                int d = h >= '0' && h <= '9' ? h - '0' : (h >= 'a' && h <= 'f' ? h - 'a' + 10 : (h >= 'A' && h <= 'F' ? h - 'A' + 10 : -1));
                                if (d < 0) Fail("bad \\u escape");
                                code = code * 16 + d;
                            }
                            sb.Append((char)code);
                            break;
                        }
                        default: Fail("unknown escape '\\" + e + "'"); break;
                    }
                }
            }

            private void NumberInto(JsonNode n)
            {
                int start = _i;
                if (Peek == '-') Next();
                if (Peek == '0') Next();
                else if (Peek >= '1' && Peek <= '9') { while (Peek >= '0' && Peek <= '9') Next(); }
                else Fail("bad number");
                if (Peek == '.')
                {
                    Next();
                    if (!(Peek >= '0' && Peek <= '9')) Fail("a decimal point needs digits after it");
                    while (Peek >= '0' && Peek <= '9') Next();
                }
                if (Peek == 'e' || Peek == 'E')
                {
                    Next();
                    if (Peek == '+' || Peek == '-') Next();
                    if (!(Peek >= '0' && Peek <= '9')) Fail("an exponent needs digits");
                    while (Peek >= '0' && Peek <= '9') Next();
                }
                n.Kind = JsonKind.Number;
                n.Text = _s.Substring(start, _i - start);
                n.Number = double.Parse(n.Text, NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }

        // ================================================================== binding

        /// <summary>
        /// Builds an object of the given type from a node, the way JsonUtility would, and reports every mismatch. Fields
        /// missing from the JSON keep the values the class initialises them with.
        /// </summary>
        public static object Bind(JsonNode node, Type type, string path, string fileName, List<string> errors, QuestJsonTrace trace)
        {
            string where = fileName + ":" + node.Line + ":" + node.Column + ": " + (string.IsNullOrEmpty(path) ? "(root)" : path);
            if (node.Kind == JsonKind.Null)
            {
                Add(errors, where + ": null is not allowed (leave the field out instead)");
                return null;
            }
            if (type == typeof(string))
            {
                if (node.Kind != JsonKind.String) { Add(errors, where + ": expected text in double quotes"); return ""; }
                return node.Text;
            }
            if (type == typeof(int))
            {
                if (node.Kind != JsonKind.Number) { Add(errors, where + ": expected a whole number"); return 0; }
                if (node.Number != Math.Floor(node.Number) || node.Text.IndexOf('.') >= 0 || node.Text.IndexOf('e') >= 0 || node.Text.IndexOf('E') >= 0)
                {
                    Add(errors, where + ": expected a whole number, found " + node.Text);
                    return 0;
                }
                if (node.Number > int.MaxValue || node.Number < int.MinValue) { Add(errors, where + ": number out of range"); return 0; }
                return (int)node.Number;
            }
            if (type == typeof(float))
            {
                if (node.Kind != JsonKind.Number) { Add(errors, where + ": expected a number"); return 0f; }
                return (float)node.Number;
            }
            if (type == typeof(double))
            {
                if (node.Kind != JsonKind.Number) { Add(errors, where + ": expected a number"); return 0.0; }
                return node.Number;
            }
            if (type == typeof(bool))
            {
                if (node.Kind != JsonKind.Bool) { Add(errors, where + ": expected true or false"); return false; }
                return node.Bool;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type item = type.GetGenericArguments()[0];
                IList list = (IList)Activator.CreateInstance(type);
                if (node.Kind != JsonKind.Array)
                {
                    Add(errors, where + ": expected a list in [ ]");
                    return list;
                }
                for (int i = 0; i < node.Items.Count; i++)
                {
                    object v = Bind(node.Items[i], item, path + "[" + i + "]", fileName, errors, trace);
                    if (v != null) list.Add(v);
                }
                return list;
            }
            if (type.IsClass)
            {
                object o = Activator.CreateInstance(type);
                if (node.Kind != JsonKind.Object)
                {
                    Add(errors, where + ": expected an object in { }");
                    return o;
                }
                if (trace != null) trace.Record(o, node.Line);
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                for (int m = 0; m < node.Members.Count; m++)
                {
                    string key = node.Members[m].Key;
                    JsonNode value = node.Members[m].Value;
                    FieldInfo f = null;
                    for (int k = 0; k < fields.Length; k++) if (fields[k].Name == key) { f = fields[k]; break; }
                    string childPath = string.IsNullOrEmpty(path) ? key : path + "." + key;
                    if (f == null)
                    {
                        Add(errors, fileName + ":" + value.Line + ":" + value.Column + ": " + childPath + ": unknown field '" + key + "' on " + type.Name + Suggest(key, fields));
                        continue;
                    }
                    object bound = Bind(value, f.FieldType, childPath, fileName, errors, trace);
                    if (bound != null) f.SetValue(o, bound);
                }
                return o;
            }
            Add(errors, where + ": type " + type.Name + " cannot be read from JSON");
            return null;
        }

        private static void Add(List<string> errors, string message)
        {
            if (errors != null) errors.Add(message);
        }

        /// <summary>" (did you mean 'X'?)" for a near miss, otherwise the list of valid fields.</summary>
        private static string Suggest(string key, FieldInfo[] fields)
        {
            string best = null;
            int bestD = int.MaxValue;
            for (int i = 0; i < fields.Length; i++)
            {
                int d = string.Equals(fields[i].Name, key, StringComparison.OrdinalIgnoreCase) ? 0 : Distance(fields[i].Name.ToLowerInvariant(), key.ToLowerInvariant());
                if (d < bestD)
                {
                    bestD = d;
                    best = fields[i].Name;
                }
            }
            if (best != null && bestD <= 2) return " (did you mean '" + best + "'?)";
            StringBuilder sb = new StringBuilder(" (valid fields: ");
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(fields[i].Name);
            }
            return sb.Append(')').ToString();
        }

        /// <summary>Edit distance between two short strings (used for "did you mean" hints).</summary>
        public static int Distance(string a, string b)
        {
            if (a == null) a = "";
            if (b == null) b = "";
            int[] prev = new int[b.Length + 1];
            int[] cur = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++) prev[j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                cur[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                }
                int[] t = prev;
                prev = cur;
                cur = t;
            }
            return prev[b.Length];
        }
    }
}
