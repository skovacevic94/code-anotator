using System;
using System.Collections.Generic;
using System.Text;

namespace code_anotator
{
    public class Comment
    {
        public int? LineNumber { get; set; }

        public int? StartIndex { get; set; }

        public int? EndIndex { get; set; }

        private string text;
        public string Text 
        {
            get
            {
                if (text.EndsWith("\\n"))
                {
                    return text.Substring(0, text.Length - 2);
                }
                return text;
            }

            set
            {
                text = value;
            }
        }
    }

    public interface IParserState
    {
        public void Process(ParserStateContext parser, char c, int lineNumber, int index);
    }

    public class ParserStateContext
    {
        public IParserState State { get; set; }

        public List<Comment> ParsedComments = new List<Comment>();

        public ParserStateContext()
        {
            State = new NeutralState();
        }
    }

    public class NeutralState : IParserState
    {
        private char prevC = ' ';
        public void Process(ParserStateContext context, char c, int lineNumber, int index)
        {
            switch(c)
            {
                case '"': context.State = new OpenStringState(); break;
                case '\'': context.State = new OpenStringState(doubleQuotes: false); break;
                case '/':
                    if (prevC == '/') context.State = new SingleLineCommentState(index-1, lineNumber);
                    break;
                case '*':
                    if (prevC == '/') context.State = new MultiLineCommentState(index-1, lineNumber);
                    break;
            }
            prevC = c;
        }
    }

    public class OpenStringState : IParserState
    {
        private char openChar;

        public OpenStringState(bool doubleQuotes = true)
        {
            openChar = doubleQuotes?'"':'\'';
        }

        public void Process(ParserStateContext context, char c, int lineNumber, int index)
        {
            if(c == openChar) // String is closed
            {
                context.State = new NeutralState();
            }
        }
    }

    public class SingleLineCommentState : IParserState
    {
        private StringBuilder builder = new StringBuilder();

        private int startIndex;
        private int startLineNumber;

        public SingleLineCommentState(int startIndex, int startLineNumber)
        {
            this.startIndex = startIndex;
            this.startLineNumber = startLineNumber;
        }

        public void Process(ParserStateContext context, char c, int lineNumber, int index)
        {
            if (c == '\n' || c == '\r')
            {
                context.State = new NeutralState();
                context.ParsedComments.Add(new Comment()
                {
                    StartIndex = startIndex,
                    LineNumber = startLineNumber,
                    EndIndex = index - 1,
                    Text = CommentParser.ProcessComment(builder.ToString())
                });
            }
            else
            {
                builder.Append(c);
            }
        }
    }

    public class MultiLineCommentState : IParserState
    {
        private char prevC = ' ';
        private StringBuilder builder = new StringBuilder();

        private int startIndex;
        private int startLineNumber;

        public MultiLineCommentState(int startIndex, int startLineNumber)
        {
            this.startIndex = startIndex;
            this.startLineNumber = startLineNumber;
        }

        public void Process(ParserStateContext context, char c, int lineNumber, int index)
        {
            if (c == '/' && prevC == '*')
            {
                context.State = new NeutralState();
                context.ParsedComments.Add(new Comment()
                {
                    StartIndex = startIndex,
                    LineNumber = startLineNumber,
                    EndIndex = index,
                    Text = CommentParser.ProcessComment(builder.ToString())
                });
            }
            else
            {
                builder.Append(c);
            }
            prevC = c;
        }
    }

    public static class CommentParser
    {
        public static List<Comment> Parse(string code)
        {
            ParserStateContext context = new ParserStateContext();
            int lineNumber = 1;
            for (int i = 0; i < code.Length; ++i)
            {
                if (code[i] == '\n') lineNumber++;
                context.State.Process(context, code[i], lineNumber, i);
            }

            return context.ParsedComments;
        }

        public static string ProcessComment(string comment)
        {
            string[] lines = comment.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );
            StringBuilder builder = new StringBuilder();
            foreach (var line in lines)
            {
                string strip = FormatLine(line);
                if (strip.Length > 0)
                {
                    builder.Append(strip);
                    builder.Append("\\n");
                }
            }

            return builder.ToString();
        }

        private static string FormatLine(string line)
        {
            string result = line.Trim();
            if (result.StartsWith("/*") || result.StartsWith("//"))
                result = result.Substring(2).Trim();
            if (result.EndsWith("*/"))
                result = result.Substring(0, result.Length - 2).Trim();

            int i = 0;
            while (i < result.Length && (result[i] == '*' || Char.IsWhiteSpace(result[i]) || result[i] == '/')) ++i;
            int j = result.Length - 1;
            while (j > i && (result[j] == '*' || Char.IsWhiteSpace(result[j]) || result[j] == '/')) --j;

            return result.Substring(i, j - i + 1);
        }
    }
}
