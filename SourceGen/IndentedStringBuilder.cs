using System.Text;

namespace SourceGen;

public class IndentedStringBuilder
{
    private readonly StringBuilder _builder     = new();
    private          int           _indentLevel = 0;
    
    public IndentedStringBuilder AddIndent()
    {
        _indentLevel++;
        return this;
    }
    
    public IndentedStringBuilder RemoveIndent()
    {
        if (_indentLevel > 0)
            _indentLevel--;
        return this;
    }
    
    public IndentedStringBuilder AppendLine( )
    {
        for (int i = 0; i < _indentLevel; i++)                
            _builder.Append("    "); // 4 spaces for each indent level
        _builder.AppendLine();
        return this;
    }

    public IndentedStringBuilder AppendLine(string value)
    {
        for (int i = 0; i < _indentLevel; i++)                
            _builder.Append("    "); // 4 spaces for each indent level
        _builder.AppendLine(value);
        return this;
    }
    
    public override string ToString() => _builder.ToString();
}