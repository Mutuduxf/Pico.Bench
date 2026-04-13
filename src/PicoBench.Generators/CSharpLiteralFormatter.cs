namespace PicoBench.Generators;

internal static class CSharpLiteralFormatter
{
    internal static string FormatConstant(TypedConstant constant, ITypeSymbol? targetType)
    {
        if (constant.IsNull)
            return "null";

        if (targetType is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
            return FormatEnumLiteral(enumType, constant.Value);

        var value = constant.Value;
        return value switch
        {
            string s => FormatStringLiteral(s),
            bool b => b ? "true" : "false",
            char c => FormatCharLiteral(c),
            float f => FormatFloatLiteral(f),
            double d => FormatDoubleLiteral(d),
            decimal m => m.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m",
            long l => l.ToString(System.Globalization.CultureInfo.InvariantCulture) + "L",
            ulong ul => ul.ToString(System.Globalization.CultureInfo.InvariantCulture) + "UL",
            uint ui => ui.ToString(System.Globalization.CultureInfo.InvariantCulture) + "U",
            _ => value?.ToString() ?? "default"
        };
    }

    private static string FormatEnumLiteral(INamedTypeSymbol enumType, object? value)
    {
        var underlyingValue = value is null ? "0" : FormatPrimitiveNumericLiteral(value);
        return $"({enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){underlyingValue}";
    }

    private static string FormatPrimitiveNumericLiteral(object value)
    {
        return value switch
        {
            byte b => b.ToString(System.Globalization.CultureInfo.InvariantCulture),
            sbyte sb => sb.ToString(System.Globalization.CultureInfo.InvariantCulture),
            short s => s.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ushort us => us.ToString(System.Globalization.CultureInfo.InvariantCulture),
            int i => i.ToString(System.Globalization.CultureInfo.InvariantCulture),
            uint ui => ui.ToString(System.Globalization.CultureInfo.InvariantCulture) + "U",
            long l => l.ToString(System.Globalization.CultureInfo.InvariantCulture) + "L",
            ulong ul => ul.ToString(System.Globalization.CultureInfo.InvariantCulture) + "UL",
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "0"
        };
    }

    private static string FormatStringLiteral(string s)
    {
        var sb = new StringBuilder("\"");
        foreach (var c in s)
        {
            sb.Append(
                c switch
                {
                    '"' => "\\\"",
                    '\\' => "\\\\",
                    '\n' => "\\n",
                    '\r' => "\\r",
                    '\t' => "\\t",
                    '\0' => "\\0",
                    _ when c < 0x20 => $"\\u{(int)c:X4}",
                    _ => c.ToString()
                }
            );
        }

        sb.Append('"');
        return sb.ToString();
    }

    private static string FormatCharLiteral(char c)
    {
        return c switch
        {
            '\'' => "'\\''",
            '\\' => "'\\\\'",
            '\n' => "'\\n'",
            '\r' => "'\\r'",
            '\t' => "'\\t'",
            '\0' => "'\\0'",
            _ when c < 0x20 || c > 0x7E => $"'\\u{(int)c:X4}'",
            _ => $"'{c}'"
        };
    }

    private static string FormatFloatLiteral(float f)
    {
        if (float.IsNaN(f))
            return "float.NaN";
        if (float.IsPositiveInfinity(f))
            return "float.PositiveInfinity";
        if (float.IsNegativeInfinity(f))
            return "float.NegativeInfinity";
        return f.ToString("G", System.Globalization.CultureInfo.InvariantCulture) + "F";
    }

    private static string FormatDoubleLiteral(double d)
    {
        if (double.IsNaN(d))
            return "double.NaN";
        if (double.IsPositiveInfinity(d))
            return "double.PositiveInfinity";
        if (double.IsNegativeInfinity(d))
            return "double.NegativeInfinity";
        return d.ToString("G", System.Globalization.CultureInfo.InvariantCulture) + "D";
    }
}
