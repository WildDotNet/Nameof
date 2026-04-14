using System;
using System.Text;

namespace Nameof.Internal.Support;

internal sealed class CodeWriter
{
    private readonly StringBuilder _builder = new();
    private int _indent;

    public void Line()
    {
        _builder.AppendLine();
    }

    public void Line(string text)
    {
        if (text.Length == 0)
        {
            _builder.AppendLine();
            return;
        }

        _builder.Append(' ', _indent * 4);
        _builder.AppendLine(text);
    }

    public void OpenBlock(string header)
    {
        Line(header);
        Line("{");
        _indent++;
    }

    public void CloseBlock()
    {
        _indent = Math.Max(0, _indent - 1);
        Line("}");
    }

    public override string ToString()
    {
        return _builder.ToString();
    }
}
