using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GitForce
{
    /// <summary>
    /// Light-weight syntax highlighter for C#, C, C++ and Python source lines.
    /// It recognizes comments, strings, keywords, numbers and preprocessor lines, which is what
    /// matters to read code; it is not a parser. Multi-line comments and strings are followed
    /// from one line to the next with a state value.
    /// </summary>
    public class ClassSyntax
    {
        public enum Kind { Text, Keyword, Type, String, Comment, Number, Preprocessor }

        /// <summary>
        /// A colored piece of a line
        /// </summary>
        public struct Token
        {
            public int Start, Length;
            public Kind Kind;
        }

        /// <summary>
        /// Construct that continues on the next line
        /// </summary>
        public enum State { Normal, BlockComment, VerbatimString, RawString, TripleSingle, TripleDouble }

        private enum Lang { CSharp, C, Cpp, Python }

        private readonly Lang lang;
        private readonly HashSet<string> keywords;
        private readonly HashSet<string> types;

        private static readonly string[] CKeywords = {
            "auto", "break", "case", "const", "continue", "default", "do", "else", "enum", "extern",
            "for", "goto", "if", "inline", "register", "restrict", "return", "sizeof", "static",
            "struct", "switch", "typedef", "union", "volatile", "while", "_Alignas", "_Alignof",
            "_Atomic", "_Bool", "_Generic", "_Noreturn", "_Static_assert", "_Thread_local", "NULL",
            "true", "false" };

        private static readonly string[] CTypes = {
            "char", "double", "float", "int", "long", "short", "signed", "unsigned", "void", "bool",
            "size_t", "ssize_t", "ptrdiff_t", "int8_t", "int16_t", "int32_t", "int64_t", "uint8_t",
            "uint16_t", "uint32_t", "uint64_t", "uintptr_t", "intptr_t", "wchar_t", "FILE" };

        private static readonly string[] CppKeywords = {
            "alignas", "alignof", "and", "and_eq", "asm", "bitand", "bitor", "catch", "class",
            "compl", "concept", "consteval", "constexpr", "constinit", "const_cast", "co_await",
            "co_return", "co_yield", "decltype", "delete", "dynamic_cast", "explicit", "export",
            "final", "friend", "mutable", "namespace", "new", "noexcept", "not", "not_eq", "nullptr",
            "operator", "or", "or_eq", "override", "private", "protected", "public",
            "reinterpret_cast", "requires", "static_assert", "static_cast", "template", "this",
            "thread_local", "throw", "try", "typeid", "typename", "using", "virtual", "xor", "xor_eq" };

        private static readonly string[] CppTypes = {
            "char8_t", "char16_t", "char32_t", "std", "string", "vector", "map", "set", "unique_ptr",
            "shared_ptr", "auto" };

        private static readonly string[] CSharpKeywords = {
            "abstract", "as", "base", "break", "case", "catch", "checked", "class", "const",
            "continue", "default", "delegate", "do", "else", "enum", "event", "explicit", "extern",
            "false", "finally", "fixed", "for", "foreach", "goto", "if", "implicit", "in",
            "interface", "internal", "is", "lock", "namespace", "new", "null", "operator", "out",
            "override", "params", "private", "protected", "public", "readonly", "ref", "return",
            "sealed", "sizeof", "stackalloc", "static", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "unchecked", "unsafe", "using", "virtual", "volatile", "while", "add",
            "async", "await", "get", "set", "init", "value", "var", "when", "where", "yield", "record",
            "partial", "remove", "global", "nameof", "required", "with", "not", "and", "or" };

        private static readonly string[] CSharpTypes = {
            "bool", "byte", "char", "decimal", "double", "dynamic", "float", "int", "long", "object",
            "sbyte", "short", "string", "uint", "ulong", "ushort", "void", "nint", "nuint", "String",
            "Int32", "Int64", "Boolean", "Object", "List", "Dictionary", "IEnumerable", "Task" };

        private static readonly string[] PythonKeywords = {
            "False", "None", "True", "and", "as", "assert", "async", "await", "break", "class",
            "continue", "def", "del", "elif", "else", "except", "finally", "for", "from", "global",
            "if", "import", "in", "is", "lambda", "nonlocal", "not", "or", "pass", "raise", "return",
            "try", "while", "with", "yield", "match", "case", "self", "cls" };

        private static readonly string[] PythonTypes = {
            "int", "float", "str", "bool", "list", "dict", "set", "tuple", "bytes", "object",
            "print", "len", "range", "open", "isinstance", "super", "type", "Exception" };

        private ClassSyntax(Lang lang)
        {
            this.lang = lang;
            switch (lang)
            {
                case Lang.CSharp:
                    keywords = new HashSet<string>(CSharpKeywords);
                    types = new HashSet<string>(CSharpTypes);
                    break;
                case Lang.C:
                    keywords = new HashSet<string>(CKeywords);
                    types = new HashSet<string>(CTypes);
                    break;
                case Lang.Cpp:
                    keywords = new HashSet<string>(CKeywords);
                    keywords.UnionWith(CppKeywords);
                    types = new HashSet<string>(CTypes);
                    types.UnionWith(CppTypes);
                    break;
                default:
                    keywords = new HashSet<string>(PythonKeywords);
                    types = new HashSet<string>(PythonTypes);
                    break;
            }
        }

        /// <summary>
        /// Returns a highlighter for a file name, or null if its language is not supported
        /// </summary>
        public static ClassSyntax ForFile(string file)
        {
            switch (Path.GetExtension(file ?? string.Empty).ToLowerInvariant())
            {
                case ".cs": case ".csx":
                    return new ClassSyntax(Lang.CSharp);
                case ".c":
                    return new ClassSyntax(Lang.C);
                case ".h": case ".cpp": case ".cc": case ".cxx": case ".c++": case ".hpp": case ".hh":
                case ".hxx": case ".h++": case ".inl": case ".ino":
                    return new ClassSyntax(Lang.Cpp);
                case ".py": case ".pyw": case ".pyi":
                    return new ClassSyntax(Lang.Python);
            }
            return null;
        }

        /// <summary>
        /// Tokenize all the lines of a text, following multi-line constructs
        /// </summary>
        public List<Token>[] TokenizeAll(IList<string> lines)
        {
            List<Token>[] result = new List<Token>[lines.Count];
            State state = State.Normal;
            for (int i = 0; i < lines.Count; i++)
                result[i] = Tokenize(lines[i] ?? string.Empty, ref state);
            return result;
        }

        /// <summary>
        /// Split one line into colored tokens. Text that is not part of any token keeps the default color.
        /// </summary>
        public List<Token> Tokenize(string line, ref State state)
        {
            List<Token> tokens = new List<Token>();
            int i = 0, n = line.Length;

            // Finish a construct started on a previous line
            if (state != State.Normal)
            {
                int end = FindEnd(line, 0, state);
                tokens.Add(new Token { Start = 0, Length = (end < 0 ? n : end), Kind = state == State.BlockComment ? Kind.Comment : Kind.String });
                if (end < 0)
                    return tokens;
                i = end;
                state = State.Normal;
            }

            // C family preprocessor line (C# directives as well)
            if (lang != Lang.Python && i == 0)
            {
                int k = 0;
                while (k < n && char.IsWhiteSpace(line[k])) k++;
                if (k < n && line[k] == '#')
                {
                    int comment = line.IndexOf("//", k, StringComparison.Ordinal);
                    int stop = comment >= 0 ? comment : n;
                    tokens.Add(new Token { Start = k, Length = stop - k, Kind = Kind.Preprocessor });
                    if (comment >= 0)
                        tokens.Add(new Token { Start = comment, Length = n - comment, Kind = Kind.Comment });
                    return tokens;
                }
            }

            while (i < n)
            {
                char c = line[i];

                // Comments
                if (lang == Lang.Python ? c == '#' : (c == '/' && i + 1 < n && line[i + 1] == '/'))
                {
                    tokens.Add(new Token { Start = i, Length = n - i, Kind = Kind.Comment });
                    break;
                }
                if (lang != Lang.Python && c == '/' && i + 1 < n && line[i + 1] == '*')
                {
                    int end = line.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    if (end < 0)
                    {
                        tokens.Add(new Token { Start = i, Length = n - i, Kind = Kind.Comment });
                        state = State.BlockComment;
                        break;
                    }
                    tokens.Add(new Token { Start = i, Length = end + 2 - i, Kind = Kind.Comment });
                    i = end + 2;
                    continue;
                }

                // Strings
                int stringStart = i;
                if (IsStringStart(line, ref i, out State multi, out char quote))
                {
                    int end;
                    if (multi != State.Normal)
                    {
                        end = FindEnd(line, i, multi);
                        if (end < 0)
                        {
                            tokens.Add(new Token { Start = stringStart, Length = n - stringStart, Kind = Kind.String });
                            state = multi;
                            break;
                        }
                    }
                    else
                        end = FindQuote(line, i, quote);
                    tokens.Add(new Token { Start = stringStart, Length = end - stringStart, Kind = Kind.String });
                    i = end;
                    continue;
                }

                // Numbers
                if (char.IsDigit(c) || (c == '.' && i + 1 < n && char.IsDigit(line[i + 1])))
                {
                    int start = i;
                    while (i < n && (char.IsLetterOrDigit(line[i]) || line[i] == '.' || line[i] == '_' || line[i] == '\''))
                        i++;
                    tokens.Add(new Token { Start = start, Length = i - start, Kind = Kind.Number });
                    continue;
                }

                // Identifiers and keywords
                if (char.IsLetter(c) || c == '_' || (c == '@' && lang == Lang.CSharp && i + 1 < n && char.IsLetter(line[i + 1])))
                {
                    int start = i;
                    i++;
                    while (i < n && (char.IsLetterOrDigit(line[i]) || line[i] == '_'))
                        i++;
                    string word = line.Substring(start, i - start);
                    if (keywords.Contains(word))
                        tokens.Add(new Token { Start = start, Length = i - start, Kind = Kind.Keyword });
                    else if (types.Contains(word))
                        tokens.Add(new Token { Start = start, Length = i - start, Kind = Kind.Type });
                    else if (lang == Lang.Python && start > 0 && line[start - 1] == '@')
                        tokens.Add(new Token { Start = start - 1, Length = i - start + 1, Kind = Kind.Preprocessor });  // Decorator
                    continue;
                }
                i++;
            }
            return tokens;
        }

        /// <summary>
        /// Recognize the opening of a string at position i. On success, i is moved after the
        /// opening quotes and multi tells if the string can span lines.
        /// </summary>
        private bool IsStringStart(string line, ref int i, out State multi, out char quote)
        {
            multi = State.Normal;
            quote = '\0';
            int n = line.Length;
            int k = i;

            if (lang == Lang.Python)
            {
                // Optional prefix: r, b, f, u and their combinations
                while (k < n && k - i < 2 && "rRbBfFuU".IndexOf(line[k]) >= 0) k++;
                if (k >= n || (line[k] != '"' && line[k] != '\''))
                    return false;
                quote = line[k];
                if (k + 2 < n && line[k + 1] == quote && line[k + 2] == quote)
                {
                    multi = quote == '"' ? State.TripleDouble : State.TripleSingle;
                    i = k + 3;
                    return true;
                }
                i = k + 1;
                return true;
            }

            if (lang == Lang.CSharp)
            {
                // Raw string literal """...""" (possibly with $), verbatim @"..." / $@"..." / @$"..."
                while (k < n && k - i < 2 && (line[k] == '$' || line[k] == '@')) k++;
                if (k < n && line[k] == '"')
                {
                    string prefix = line.Substring(i, k - i);
                    if (k + 2 < n && line[k + 1] == '"' && line[k + 2] == '"' && !prefix.Contains("@"))
                    {
                        multi = State.RawString;
                        i = k + 3;
                        return true;
                    }
                    if (prefix.Contains("@"))
                    {
                        multi = State.VerbatimString;
                        i = k + 1;
                        return true;
                    }
                    quote = '"';
                    i = k + 1;
                    return true;
                }
                k = i;
            }
            else
            {
                // C/C++ prefixes: L, u, U, u8, R; a prefix not followed by a quote is an identifier
                while (k < n && k - i < 3 && "LuU8R".IndexOf(line[k]) >= 0) k++;
                if (k >= n || (line[k] != '"' && line[k] != '\''))
                    k = i;
            }

            if (k < n && (line[k] == '"' || line[k] == '\''))
            {
                quote = line[k];
                i = k + 1;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the position after the closing quote of a single-line string (or the line end)
        /// </summary>
        private static int FindQuote(string line, int i, char quote)
        {
            while (i < line.Length)
            {
                if (line[i] == '\\')
                    i += 2;
                else if (line[i] == quote)
                    return i + 1;
                else
                    i++;
            }
            return line.Length;
        }

        /// <summary>
        /// Returns the position after the end of a multi-line construct, or -1 if it does not end on this line
        /// </summary>
        private static int FindEnd(string line, int i, State state)
        {
            switch (state)
            {
                case State.BlockComment:
                {
                    int end = line.IndexOf("*/", i, StringComparison.Ordinal);
                    return end < 0 ? -1 : end + 2;
                }
                case State.VerbatimString:
                    while (i < line.Length)
                    {
                        if (line[i] == '"')
                        {
                            if (i + 1 < line.Length && line[i + 1] == '"')
                                i += 2;     // Doubled quote inside a verbatim string
                            else
                                return i + 1;
                        }
                        else
                            i++;
                    }
                    return -1;
                case State.RawString:
                case State.TripleDouble:
                case State.TripleSingle:
                {
                    string close = state == State.TripleSingle ? "'''" : "\"\"\"";
                    for (int k = i; k + 3 <= line.Length; k++)
                    {
                        if (state != State.RawString && line[k] == '\\')
                        {
                            k++;
                            continue;
                        }
                        if (string.CompareOrdinal(line, k, close, 0, 3) == 0)
                            return k + 3;
                    }
                    return -1;
                }
            }
            return -1;
        }

        #region Painting

        /// <summary>
        /// Color of a token kind in the current theme
        /// </summary>
        public static Color ColorOf(Kind kind, Color text)
        {
            bool dark = ClassTheme.IsDark;
            switch (kind)
            {
                case Kind.Keyword: return dark ? Color.FromArgb(86, 156, 214) : Color.FromArgb(0, 0, 255);
                case Kind.Type: return dark ? Color.FromArgb(78, 201, 176) : Color.FromArgb(43, 145, 175);
                case Kind.String: return dark ? Color.FromArgb(214, 157, 133) : Color.FromArgb(163, 21, 21);
                case Kind.Comment: return dark ? Color.FromArgb(87, 166, 74) : Color.FromArgb(0, 128, 0);
                case Kind.Number: return dark ? Color.FromArgb(181, 206, 168) : Color.FromArgb(9, 134, 88);
                case Kind.Preprocessor: return dark ? Color.FromArgb(155, 155, 155) : Color.FromArgb(111, 0, 138);
            }
            return text;
        }

        /// <summary>
        /// Paint a grid cell holding a line of code: background, then the text piece by piece in
        /// the colors of its tokens. The text is drawn from the left edge plus a prefix width,
        /// which lets the unified diff view draw its "+ " / "- " marker first.
        /// </summary>
        public static void PaintCell(DataGridViewCellPaintingEventArgs e, string text, List<Token> tokens, string prefix = "")
        {
            bool selected = (e.State & DataGridViewElementStates.Selected) != 0;
            Color back = selected ? e.CellStyle.SelectionBackColor : e.CellStyle.BackColor;
            Color fore = selected ? e.CellStyle.SelectionForeColor : e.CellStyle.ForeColor;
            using (SolidBrush brush = new SolidBrush(back))
                e.Graphics.FillRectangle(brush, e.CellBounds);

            Font font = e.CellStyle.Font;
            const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix |
                                          TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                                          TextFormatFlags.PreserveGraphicsClipping;
            Rectangle r = new Rectangle(e.CellBounds.X + 2, e.CellBounds.Y, e.CellBounds.Width - 2, e.CellBounds.Height);
            int x = r.X;

            Action<string, Color> draw = (piece, color) =>
            {
                if (piece.Length == 0 || x > e.CellBounds.Right)
                    return;
                Size size = TextRenderer.MeasureText(e.Graphics, piece, font, new Size(int.MaxValue, r.Height), flags);
                TextRenderer.DrawText(e.Graphics, piece, font, new Rectangle(x, r.Y, size.Width + 1, r.Height), color, flags);
                x += size.Width;
            };

            draw(prefix, fore);
            int pos = 0;
            if (tokens != null)
            {
                foreach (Token t in tokens)
                {
                    if (t.Start < pos || t.Start >= text.Length)
                        continue;
                    draw(text.Substring(pos, t.Start - pos), fore);
                    int len = Math.Min(t.Length, text.Length - t.Start);
                    draw(text.Substring(t.Start, len), ColorOf(t.Kind, fore));
                    pos = t.Start + len;
                }
            }
            if (pos < text.Length)
                draw(text.Substring(pos), fore);
            e.Handled = true;
        }

        #endregion
    }
}
