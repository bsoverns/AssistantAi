using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AssistantAi.Helpers
{
    /// <summary>
    /// Owns all writing into the AI output RichTextBox: plain text, fenced code
    /// blocks with C# syntax colouring, and inline images.
    /// </summary>
    public class ResponseRenderer
    {
        private readonly RichTextBox _target;
        private readonly ErrorLog _log;

        public ResponseRenderer(RichTextBox target, ErrorLog log)
        {
            _target = target;
            _log = log;
        }

        public bool IsEmpty => _target.Document.Blocks.Count == 0;

        public void Clear()
        {
            _target.Document.Blocks.Clear();
        }

        public void ScrollToEnd()
        {
            _target.ScrollToEnd();
        }

        /// <summary>
        /// Appends a labelled response ("Me: ", "Chat GPT: ") to the output window,
        /// splitting out a ``` fenced block for syntax highlighting when one is present.
        /// </summary>
        public void AppendResponse(string typeResponse, string response)
        {
            response = (response ?? string.Empty).Trim();

            try
            {
                // Only a single fenced block is handled; text with several fences
                // treats everything between the first and last fence as one block.
                int firstIndex = response.IndexOf("```", StringComparison.Ordinal);
                int lastIndex = response.LastIndexOf("```", StringComparison.Ordinal);

                if (firstIndex != -1 && lastIndex != -1 && firstIndex != lastIndex)
                {
                    string beforeCode = response.Substring(0, firstIndex);
                    Append(beforeCode);

                    string code = response.Substring(firstIndex + 3, lastIndex - firstIndex - 3);
                    Append(code, isCodeBlock: true);

                    string afterCode = response.Substring(lastIndex + 3);
                    Append(afterCode);
                }

                else
                {
                    Append(typeResponse + " " + response, false, true);
                }

                _target.ScrollToEnd();
            }

            catch (Exception ex)
            {
                _log.Write(ex);
                Append("Error: " + ex.Message);
                _target.ScrollToEnd();
            }
        }

        /// <summary>Appends the image at <paramref name="fileLocation"/> as a 400x400 inline preview.</summary>
        public void AppendImage(string fileLocation)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage(new Uri(fileLocation, UriKind.Absolute));
                Image imageControl = new Image
                {
                    Source = bitmap,
                    Width = 400,
                    Height = 400,
                    Stretch = Stretch.Uniform
                };

                InlineUIContainer container = new InlineUIContainer(imageControl);
                _target.Document.Blocks.Add(new Paragraph(container));
                _target.ScrollToEnd();
            }

            catch (Exception ex)
            {
                _log.Write(ex);
                Append("Error: " + ex.Message);
                _target.ScrollToEnd();
            }
        }

        /// <summary>Appends raw text, optionally as a highlighted code block.</summary>
        public void Append(string text, bool isCodeBlock = false, bool appendToLastParagraph = false)
        {
            Paragraph? paragraph;

            if (appendToLastParagraph && _target.Document.Blocks.Count > 0)
            {
                paragraph = _target.Document.Blocks.LastBlock as Paragraph;
                if (paragraph == null)
                {
                    paragraph = new Paragraph();
                    _target.Document.Blocks.Add(paragraph);
                }
            }

            else
            {
                paragraph = new Paragraph();
                _target.Document.Blocks.Add(paragraph);
            }

            if (isCodeBlock)
            {
                paragraph.FontFamily = new FontFamily("Courier");
                paragraph.Background = Brushes.LightGray;
                paragraph.Padding = new Thickness(5);
                HighlightCode(paragraph, text);
            }

            else
            {
                paragraph.Inlines.Add(new Run(text));
            }
        }

        private static readonly SolidColorBrush KeywordColor = Brushes.Blue;
        private static readonly SolidColorBrush StringColor = Brushes.Brown;
        private static readonly SolidColorBrush CommentColor = Brushes.Green;
        private static readonly SolidColorBrush NormalTextColor = Brushes.Black;

        private static readonly HashSet<string> CSharpKeywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool",
            "break", "byte", "case", "catch",
            "char", "checked", "class", "const",
            "continue", "decimal", "default", "delegate",
            "do", "double", "else", "enum",
            "event", "explicit", "extern", "false",
            "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit",
            "in", "int", "interface", "internal",
            "is", "lock", "long", "namespace",
            "new", "null", "object", "operator",
            "out", "override", "params", "private",
            "protected", "public", "readonly", "ref",
            "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string",
            "struct", "switch", "this", "throw",
            "true", "try", "typeof", "uint",
            "ulong", "unchecked", "unsafe", "ushort",
            "using", "virtual", "void", "volatile",
            "while"
        };

        /// <summary>
        /// Whitespace-splitting C# colouriser. It only recognises tokens that are
        /// separated by spaces, so "int x=1;" or a multi-word string literal won't
        /// colour correctly — good enough for a preview, not a real lexer.
        /// </summary>
        private static void HighlightCode(Paragraph paragraph, string code)
        {
            string[] lines = code.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                Span span = new Span();

                foreach (var token in line.Split(' '))
                {
                    Run run = new Run(token + " ") { Foreground = NormalTextColor };

                    if (CSharpKeywords.Contains(token))
                        run.Foreground = KeywordColor;

                    else if (token.StartsWith("//", StringComparison.Ordinal))
                        run.Foreground = CommentColor;

                    else if (token.StartsWith("\"", StringComparison.Ordinal) && token.EndsWith("\"", StringComparison.Ordinal))
                        run.Foreground = StringColor;

                    span.Inlines.Add(run);
                }

                paragraph.Inlines.Add(span);
                paragraph.Inlines.Add(new LineBreak());
            }
        }
    }
}
