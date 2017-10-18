using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AutoDI.Fody
{
    internal class Matcher<T>
    {
        private const string RegexPrefix = "regex:";

        private readonly Regex _regex;
        private readonly Func<T, string> _valueProvider;
        private readonly string _replacement;
        private readonly List<Variable> _variables = new List<Variable>();

        public Matcher(Func<T, string> valueProvider, string pattern, string replacement = null)
        {
            _valueProvider = valueProvider ?? throw new ArgumentNullException(nameof(valueProvider));
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));
            _replacement = replacement;
            if (pattern.StartsWith(RegexPrefix, StringComparison.OrdinalIgnoreCase))
            {
                _regex = new Regex(pattern.Substring(RegexPrefix.Length));
            }
            else
            {
                pattern = Regex.Escape(pattern);
                pattern = pattern.Replace("*", ".*");
                _regex = new Regex(pattern);
            }
        }

        public void AddVariable(string name, Func<T, string> parameterValueProvider)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (parameterValueProvider == null) throw new ArgumentNullException(nameof(parameterValueProvider));
            _variables.Add(new Variable(name, parameterValueProvider));
        }

        public bool TryMatch(T input, out string replacement)
        {
            string providedValue = _valueProvider(input);

            if (GetReplacement(providedValue, out replacement) ||
                GetReplacement(providedValue.Replace('/', '+'), out replacement))
            {
                replacement = replacement?.Replace('+', '/');
                return true;
            }

            return false;

            bool GetReplacement(string inputValue, out string replace)
            {
                if (_regex.IsMatch(inputValue))
                {
                    replace = _replacement != null ? _regex.Replace(inputValue, _replacement) : null;
                    return true;
                }
                replace = null;
                return false;
            }
        }

        public override string ToString()
        {
            if (_replacement != null)
            {
                return $"'{_regex}' => '{_replacement}'";
            }
            return _regex.ToString();
        }

        private class Variable
        {
            public Variable(string name, Func<T, string> valueProvider)
            {
                Name = name;
                ValueProvider = valueProvider;
            }

            public string Name { get; }
            public Func<T, string> ValueProvider { get; }
        }
    }
}