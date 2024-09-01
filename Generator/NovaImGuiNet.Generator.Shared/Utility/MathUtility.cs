using org.matheval;

namespace NovaImGuiNet.Generator;

public static class MathUtility {
    public static long? EvaluateExpressionAsLong(string? expression) {
        if (expression == null)
            return null;

        try {
            return Convert.ToInt64(new Expression(expression).Eval());
        } catch (Exception) {
            // Ignore
        }

        return null;
    }
}
