using System;
using System.Text.RegularExpressions;

namespace AutoDI.Build
{
    internal class Matcher<T>
    {
        private const string RegexPrefix = "regex:";

        private readonly Regex _regex;
        private readonly Func<T, string> _valueProvider;
        private readonly string _replacement;

        public Matcher(Func<T, string> valueProvider, string pattern, string replacement = null)
        {
            _valueProvider = valueProvider ?? throw new ArgumentNullException(nameof(valueProvider));
            if (pattern is null) throw new ArgumentNullException(nameof(pattern));

            if (pattern.StartsWith(RegexPrefix, StringComparison.OrdinalIgnoreCase))
            {
                _regex = new Regex(pattern.Substring(RegexPrefix.Length));
            }
            else
            {
                pattern = Regex.Escape(pattern);
                pattern = pattern.Replace(@"\*", "(.*)");
                _regex = new Regex(pattern);
            }

            if (replacement?.StartsWith(RegexPrefix, StringComparison.OrdinalIgnoreCase) == true)
            {
                _replacement = replacement.Substring(RegexPrefix.Length);
            }
            else if (replacement != null)
            {
                _replacement = replacement.Replace(@"*", "$1");
            }
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
    }
}