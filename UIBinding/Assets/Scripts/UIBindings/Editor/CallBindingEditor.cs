using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UIBindings.Editor.Utils;
using UIBindings.Runtime;
using UIBindings.Runtime.Utils;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace UIBindings.Editor
{
    [CustomPropertyDrawer(typeof(CallBinding))]
    public class CallBindingEditor : BindingBaseEditor
    {
        private Int32 _additionalLinesCount;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded)
                return Resources.LineHeightWithMargin * 3 + _additionalLinesCount * Resources.LineHeightWithMargin;
            else
                return EditorGUIUtility.singleLineHeight;
        }

        public static Boolean IsProperMethod(MethodInfo method)
        {
            if (method == null)
                return false;

            //Check if method is not a property getter or setter
            if ( method.IsSpecialName )
                return false;

            //Check if method is not obsolete
            if (method.GetCustomAttribute<ObsoleteAttribute>() != null)
                return false;

            //Check result type                       
            if ( method.ReturnType != typeof(void)
                 && method.ReturnType.GetMethod( "GetAwaiter" ) == null )
                return false;

            //Check params
            var paramz = method.GetParameters();
            if (paramz.Length > 2 )
                return false;

            foreach ( var parameterInfo in paramz )
            {
                var paramType = parameterInfo.ParameterType;
                if( paramType != typeof(int) && 
                    paramType != typeof(float) && 
                    paramType != typeof(bool) && 
                    paramType != typeof(string) && 
                    !typeof(UnityEngine.Object).IsAssignableFrom(paramType) )
                {
                    return false;
                }
            }

            return true;
        }

        protected override string GetMainString(SerializedProperty property )
        {
            String mainText = null;
            if ( Application.isPlaying  )
            {
                var (binding, _) = BindingEditorUtils.GetBindingObject<CallBinding>( property );
                mainText = binding.GetFullRuntimeInfo();
                return mainText;
            }
            else
            {
                var (binding, bindingHost) = BindingEditorUtils.GetBindingObject<CallBinding>( property );
                var bindingSourceInfo = BindingEditorUtils.GetBindingSourceInfo( binding, bindingHost );
                var bindingDirection = BindingEditorUtils.GetBindingDirection( binding );
                var bindingTargetInfo = BindingEditorUtils.GetBindingTargetInfo( binding, fieldInfo, bindingHost, false );
                var resultString = $"{bindingSourceInfo} {bindingDirection} {bindingTargetInfo}";
                return resultString;
            }
        }


        protected override void DrawPathField( Rect position, SerializedProperty property, BindingBase binding, UnityEngine.Object host )
        {
            var pathProp = property.FindPropertyRelative( nameof(BindingBase.Path) );

            var (sourceType, _) = BindingUtils.GetSourceTypeAndObject( binding, host );
            if ( sourceType != null )
            {
                position = EditorGUI.PrefixLabel( position, GUIUtility.GetControlID( FocusType.Keyboard ), new GUIContent( pathProp.displayName ) ) ;
                var pathString  = pathProp.stringValue;
                var parser      = new PathParser( sourceType, pathString );
                var tokens      = parser.Tokens;
                var isValidPath = tokens.Count > 0 && parser.LastMethod != null;

                GUI.SetNextControlName( "PathTextField" );
                var isFocused = GUI.GetNameOfFocusedControl() == "PathTextField";
                String displayPath = pathString;
                if ( isFocused )
                {
                    if ( Event.current.type == EventType.KeyDown )
                    {
                        if ( Event.current.keyCode == KeyCode.DownArrow ) //Show suggestions list on arrow down
                        {
                            Event.current.Use();  //Prevent further processing of this event
                            if ( EditorGUIUtils.TryGetCursorPositionInTextField( out var cursorPosition ) &&
                                 parser.TryGetTokenAtPosition( cursorPosition, out var token )            &&
                                 token.SourceType != null )
                            {
                                //Show suggestion for given source type in the token
                                var properties = GetCompatibleProperties( token.SourceType );
                                var methods     = GetCompatibleMethods( token.SourceType );
                                var menu             = new GenericMenu();
                                foreach ( var method in methods )
                                {
                                    var    isBaseMethod      = method.DeclaringType != token.SourceType;
                                    string methodDisplayName = isBaseMethod ? $"Base()/{method.Name}()" : $"{method.Name}()";
                                    string methodName        = method.Name;
                                    menu.AddItem( new GUIContent( methodDisplayName ), methodName == token.Token, ( ) =>
                                    {
                                        token.Token          = methodName;
                                        pathProp.stringValue = tokens.Select( t => t.Token ).JoinToString( "." );
                                        pathProp.serializedObject.ApplyModifiedProperties();
                                    } );
                                }
                                foreach ( var prop in properties )
                                {
                                    var    isBaseProp      = prop.DeclaringType != token.SourceType;
                                    string propDisplayName = isBaseProp ? $"Base/{prop.Name}" : prop.Name;
                                    string propName        = prop.Name;
                                    menu.AddItem( new GUIContent( propDisplayName ), propName == token.Token, ( ) =>
                                    {
                                        token.Token          = propName;
                                        pathProp.stringValue = tokens.Select( t => t.Token ).JoinToString( "." );
                                        pathProp.serializedObject.ApplyModifiedProperties();
                                    } );
                                }

                                menu.DropDown( position );
                            }
                        }
                        else if ( Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.Escape ) 
                        {
                            Event.current.Use();  
                            GUI.FocusControl( null );
                        }
                    }
                }
                else
                {
                    //When not focues, show property type also
                    if( tokens.Last().PropertyType != null )
                        displayPath += $" ({tokens.Last().PropertyType.Name})";
                } 

                var newValue = GUI.TextField( position, displayPath, isValidPath ? Resources.TextField : Resources.ErrorTextField );
                if ( isFocused )
                    pathProp.stringValue = newValue;

            }
            else
            {
                position = EditorGUI.PrefixLabel( position, GUIUtility.GetControlID( FocusType.Passive ), new GUIContent( pathProp.displayName ) ) ;
                GUI.Label( position, "Source not set", Resources.ErrorLabel );
            }
        }

        protected override void DrawAdditionalFields( Rect position, SerializedProperty property, BindingBase binding, UnityEngine.Object host )
        {
            base.DrawAdditionalFields( position, property, binding, host );

            var methodInfo = GetSourceMethod( binding, host );
            if ( methodInfo != null )
            {
                position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
                var paramProperty = property.FindPropertyRelative(nameof(CallBinding.Params));
                DrawMethodParameters( position, paramProperty, methodInfo );
            }
        }

        public static MethodInfo[] GetCompatibleMethods(Type sourceType )
        {
            return sourceType.GetMethods( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic )
                             .Where( mi => mi.DeclaringType != typeof(Object) )
                             .GroupBy( mi => mi.Name )
                             .Where( g => g.Count() == 1 ) //Don't want to bother with overloaded methods bc I want to store only method name without signature
                             .SelectMany( g => g )
                             .Where( IsProperMethod )
                             .ToArray();
        }

        private static PropertyInfo[] GetCompatibleProperties(Type sourceType)
        {
            return sourceType.GetProperties( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic )
                             .Where( pi => pi.DeclaringType != typeof(Object) )
                             .Where( pi => pi.GetIndexParameters().Length == 0 ) //Exclude indexed properties
                             .Where( pi => HasCompatibleMethodsRecursive( pi.PropertyType ) )
                             .ToArray();
        }

        private static MethodInfo GetSourceMethod( BindingBase binding, UnityEngine.Object host )
        {
            var (sourceType, _) = BindingUtils.GetSourceTypeAndObject( binding, host );
            if ( sourceType != null )
            {
                var pathParser = new PathParser( sourceType, binding.Path );
                return pathParser.LastMethod;
            }

            return null;
        }

        private static string GetPrettyMethodName(MethodInfo method)
        {
            if (method == null)
                return "(null)";

            var name = method.Name;
            // if (method.ReturnType != typeof(void))
            // {
            //     name += " -> " + method.ReturnType.Name;
            // }

            var paramz = method.GetParameters();
            if (paramz.Length > 0)
            {
                name += "(" + String.Join( ", ", paramz.Select( p => $"{p.ParameterType.Name}" ) ) + ")";
            }
            else
            {
                name += "()";
            }

            return name;
        }

        private void DrawMethodParameters(Rect position, SerializedProperty paramsProp, MethodInfo method )
        {
            _additionalLinesCount = 0;
            var isChanged = false;

            var paramz = method.GetParameters();
            if( paramsProp.arraySize < paramz.Length )
            {
                paramsProp.arraySize = paramz.Length;
                isChanged = true;
            }

            //Draw all parameters
            for ( int i = 0; i < paramz.Length; i++ )
            {
                var param = paramz[i];
                var paramProp = paramsProp.GetArrayElementAtIndex(i);
                position.height = EditorGUIUtility.singleLineHeight;
                var paramName = $"{param.Name} ({param.ParameterType.Name})";
                var paramObject = (SerializableParam)paramProp.boxedValue;

                //Draw parameter value field
                if ( param.ParameterType == typeof(int) )
                {
                    EditorGUI.BeginChangeCheck();
                    var newValue = EditorGUI.IntField( position, paramName, paramObject.GetInt() );
                    if (EditorGUI.EndChangeCheck() )
                    {
                        paramProp.FindPropertyRelative( SerializableParam.PrimitiveFieldName).intValue = newValue;
                        paramProp.FindPropertyRelative( SerializableParam.ValueTypeFieldName ).intValue = (Int32)SerializableParam.EType.Int;
                        isChanged = true;
                    }
                }
                else if ( param.ParameterType == typeof(float) )
                {
                    EditorGUI.BeginChangeCheck();
                    var newValue = EditorGUI.FloatField( position, paramName, paramObject.GetFloat() );
                    if (EditorGUI.EndChangeCheck() )
                    {
                        paramProp.FindPropertyRelative(SerializableParam.PrimitiveFieldName).intValue = UnsafeUtility.As<float, int>(ref newValue);
                        paramProp.FindPropertyRelative( SerializableParam.ValueTypeFieldName ).intValue = (Int32)SerializableParam.EType.Float;
                        isChanged = true;
                    }
                }
                else if ( param.ParameterType == typeof(bool) )
                {
                    EditorGUI.BeginChangeCheck();
                    var newValue = EditorGUI.Toggle( position, paramName, paramObject.GetBool() );
                    if (EditorGUI.EndChangeCheck() )
                    {
                        paramProp.FindPropertyRelative(SerializableParam.PrimitiveFieldName).intValue = newValue ? 1 : 0;
                        paramProp.FindPropertyRelative( SerializableParam.ValueTypeFieldName ).intValue = (Int32)SerializableParam.EType.Bool;
                        isChanged = true;
                    }
                }
                else if ( param.ParameterType == typeof(string) )
                {
                    EditorGUI.BeginChangeCheck();
                    var newValue = EditorGUI.TextField( position, paramName, paramObject.GetString(), Resources.TextField );
                    if (EditorGUI.EndChangeCheck() )
                    {
                        paramProp.FindPropertyRelative(SerializableParam.StringFieldName).stringValue = newValue;
                        paramProp.FindPropertyRelative( SerializableParam.ValueTypeFieldName ).intValue = (Int32)SerializableParam.EType.String;
                        isChanged = true;
                    }
                }
                else if ( typeof(UnityEngine.Object).IsAssignableFrom(param.ParameterType) )
                {
                    EditorGUI.BeginChangeCheck();
                    var newValue = EditorGUI.ObjectField( position, paramName, paramObject.GetObject(), param.ParameterType, true );
                    if (EditorGUI.EndChangeCheck() )
                    {
                        paramProp.FindPropertyRelative(SerializableParam.ObjectFieldName).objectReferenceValue = newValue;
                        paramProp.FindPropertyRelative( SerializableParam.ValueTypeFieldName ).intValue = (Int32)SerializableParam.EType.Object;
                        isChanged = true;
                    }
                }
                else
                {
                    //Unsupported parameter type
                    using ( new EditorGUI.DisabledScope() )
                    {
                        EditorGUI.LabelField( position, paramName, $"(unsupported type {param.ParameterType.Name})", Resources.ErrorTextField );
                    }
                }

                position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
                _additionalLinesCount++;
            }

            if ( isChanged )
                paramsProp.serializedObject.ApplyModifiedProperties();
        }

        public static bool HasCompatibleMethodsRecursive(Type typeToCheck, HashSet<Type> visitedTypes = null)
        {
            if (typeToCheck == null)
                return false;

            visitedTypes ??= new HashSet<Type>();
            if (!visitedTypes.Add(typeToCheck))
                return false; // Prevent infinite recursion

            // 1. Check if this type has compatible methods
            if (GetCompatibleMethods(typeToCheck).Length > 0)
                return true;

            // 2. Check properties of this type recursively
            var properties = typeToCheck.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var prop in properties)
            {
                var propType = prop.PropertyType;
                if (propType == typeToCheck)
                    continue; // Prevent self-recursion

                if (HasCompatibleMethodsRecursive(propType, visitedTypes))
                    return true;
            }

            return false;
        }
    }
}

