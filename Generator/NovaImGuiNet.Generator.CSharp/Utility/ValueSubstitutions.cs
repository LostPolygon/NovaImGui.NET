namespace NovaImGuiNet.Generator.CSharp;

public class ValueSubstitutions<T>(Func<T, string> valueToStringConverter)  {
    private readonly List<KeyValuePair<string, T>> _substitutions = [];

    public void AddRange(IEnumerable<KeyValuePair<string, T>> substitutions) {
        _substitutions.AddRange(substitutions);
        _substitutions.Sort((a, b) => String.CompareOrdinal(a.Key, b.Key));
    }

    public string Substitute(string expression, out (bool found, T? value) exactMatch) {
        exactMatch = FindExactMatch(expression);
        if (exactMatch.found) {
            return valueToStringConverter(exactMatch.value!);
        }

        exactMatch = (false, default);
        return _substitutions.Aggregate(
            expression,
            (current, substitution) => current.Replace(substitution.Key, valueToStringConverter(substitution.Value))
        );
    }

    private (bool found, T? value) FindExactMatch(string key) {
        foreach (KeyValuePair<string, T> substitution in _substitutions) {
            if (substitution.Key == key)
                return (true, substitution.Value);
        }

        return (false, default!);
    }
}
