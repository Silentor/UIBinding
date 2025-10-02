using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UIBindings.Editor
{
    /// <summary>
    /// Editor property path parser
    /// TODO use PathProcessor from runtime, its up to date. PathParser is property-centric
    /// </summary>
    public class PathParser
    {
        public class TokenInfo
        {
            public string Token { get; set; }
            public int StartIndex { get; set; }
            public int EndIndex { get; set; }
            public Type SourceType { get; set; } // The type that owns this property

            //If token is property
            public Type PropertyType { get; set; }
            public PropertyInfo PropertyInfo { get; set; } // The PropertyInfo for this token, if valid

            //If token is method
            public MethodInfo MethodInfo { get; set; } // The MethodInfo for this token, if valid
        }

        public IReadOnlyList<TokenInfo> Tokens => _tokens;
        public Type SourceType => _sourceType;
        public string PropertyPath => _propertyPath;

        public PropertyInfo LastProperty
        {
            get
            {
                if (_tokens.Count == 0)
                    return null;

                var lastToken = _tokens[^1]; // Get the last token
                return lastToken.PropertyInfo; // Return the PropertyInfo of the last token
            }
        }

        public MethodInfo LastMethod
        {
            get
            {
                if (_tokens.Count == 0)
                    return null;

                var lastToken = _tokens[^1];
                return lastToken.MethodInfo;
            }
        }

        
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
                               new (){StartIndex = 0, EndIndex = 0, Token = "", SourceType = _sourceType}
                       };

            var tokens = new List<TokenInfo>();
            Type currentType = _sourceType;
            string[] parts = _propertyPath.Split('.');
            int index = 0;
            foreach ( var part in parts )
            {
                int          tokenStart      = index;
                int          tokenEnd        = index + part.Length;
                Type         propertyType    = null;
                Type         tokenSourceType = currentType;
                PropertyInfo propertyInfo    = null;
                MethodInfo   methodInfo      = null;
                if (currentType != null)
                {
                    var prop = currentType.GetProperty(part, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null)
                    {
                        propertyType = prop.PropertyType;
                        propertyInfo = prop;
                        currentType  = propertyType;
                    }
                    else
                    {
                        var methods = CallBindingEditor.GetCompatibleMethods( currentType );
                        var method = methods.FirstOrDefault( mi => mi.Name == part );
                        if (method != null)
                        {
                            methodInfo    = method;
                        }

                        currentType = null;
                    }
                }

                tokens.Add( new TokenInfo
                            {
                                    Token        = part,
                                    StartIndex   = tokenStart,
                                    EndIndex     = tokenEnd,
                                    PropertyType = propertyType,
                                    SourceType   = tokenSourceType,
                                    PropertyInfo = propertyInfo,
                                    MethodInfo   = methodInfo,
                            } );
                index += part.Length + 1; // +1 for the dot
            }
            return tokens;
        }
    }
}