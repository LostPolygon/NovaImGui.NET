using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NCalc;
using NCalc.Exceptions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NovaImGuiNet.Generator;

public static class MathUtility {
    public static long? EvaluateExpressionAsLong(string? expression) {
        if (expression == null)
            return null;

        try {
            return Convert.ToInt64(new Expression(expression).Evaluate());
        } catch (NCalcException) {
            // Ignore
        }

        return null;
    }
}
