using System;
using System.Text.RegularExpressions;

namespace AutoDI.Fody
{
    internal class Matcher
    {
        private const string RegexPrefix = "regex:";

        private readonly Regex _regex;
        private readonly string _replacement;

        public Matcher(string pattern, string replacement = null)
        {
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

        public bool TryMatch(string input, out string replacement)
        {
            if (_regex.IsMatch(input))
            {
                replacement = _replacement != null ? _regex.Replace(input, _replacement) : null;
                return true;
            }
            replacement = "";
            return false;
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