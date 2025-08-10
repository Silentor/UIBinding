using System;
using System.Collections.Generic;
using System.Reflection;

namespace UIBindings.Editor
{
    /// <summary>
    /// Editor property path parser
    /// </summary>
    public class PathParser
    {
        public class TokenInfo
        {
            public string Token { get; set; }
            public int StartIndex { get; set; }
            public int EndIndex { get; set; }
            public Type PropertyType { get; set; }
            public Type SourceType { get; set; } // The type that owns this property
        }

        public IReadOnlyList<TokenInfo> Tokens => _tokens;
        public Type SourceType => _sourceType;
        public string PropertyPath => _propertyPath;

        
        public PathParser(Type sourceType, string propertyPath)
        {
            _sourceType = sourceType;
            _propertyPath = propertyPath;
            _tokens = ParsePath();
        }

        /// <summary>
        /// Returns the token at the specified character position in the property path, or null if none.
        /// </summary>
        /// <param name="charPosition">The character position in the property path.</param>
        public bool TryGetTokenAtPosition(int charPosition, out TokenInfo token )
        {
            foreach (var t in _tokens)
            {
                if ( charPosition >= t.StartIndex && charPosition <= t.EndIndex )
                {
                    token = t;
                    return true;
                }
            }

            token = null;
            return false;
        }

        private readonly Type            _sourceType;
        private readonly string          _propertyPath;
        private readonly List<TokenInfo> _tokens;


        private List<TokenInfo> ParsePath()
        {
            if (string.IsNullOrEmpty(_propertyPath))
                return new List<TokenInfo>()
                       {
                               new (){StartIndex = 0, EndIndex = 0, Token = "", PropertyType = null, SourceType = _sourceType}
                       };

            var tokens = new List<TokenInfo>();
            Type currentType = _sourceType;
            string[] parts = _propertyPath.Split('.');
            int index = 0;
            foreach (var part in parts)
            {
                int tokenStart = index;
                int tokenEnd = index + part.Length;
                Type propertyType = null;
                Type tokenSourceType = currentType;
                if (currentType != null)
                {
                    var prop = currentType.GetProperty(part, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null)
                    {
                        propertyType = prop.PropertyType;
                        currentType = propertyType;
                    }
                    else
                    {
                        currentType = null;
                    }
                }
                tokens.Add(new TokenInfo
                {
                    Token = part,
                    StartIndex = tokenStart,
                    EndIndex = tokenEnd,
                    PropertyType = propertyType,
                    SourceType = tokenSourceType
                });
                index += part.Length + 1; // +1 for the dot
            }
            return tokens;
        }
    }
}