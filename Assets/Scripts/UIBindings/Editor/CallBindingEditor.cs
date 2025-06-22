using System;
using System.Linq;
using System.Reflection;
using UIBindings.Editor.Utils;
using UIBindings.Runtime;
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

        private static Boolean IsProperMethod(MethodInfo method)
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

        protected override BindingMainString GetMainString(SerializedProperty property )
        {
            var validationReport = String.Empty;
            var (sourceType, sourceObject) = GetSourceTypeAndObject( property );
            var sourceName       = sourceObject ? $"{sourceObject.GetType().Name} {sourceObject.name}" : sourceType?.Name ?? "null";
            var sourceMethod     = GetSourceMethod( property );
            var sourceMethodName = sourceMethod != null ? sourceMethod.Name : "null";
            var isValid          = sourceMethod != null && IsProperMethod( sourceMethod );
            var paramsString = sourceMethod != null && sourceMethod.GetParameters().Length > 0
                    ? String.Concat("(", String.Join( ", ", sourceMethod.GetParameters().Select( p => p.ParameterType.Name ) ), ")")
                    : "()";
            var isAwaitable = sourceMethod != null && sourceMethod.ReturnType.GetMethod( "GetAwaiter" ) != null;
            var taskString  = isAwaitable ? "task " : String.Empty;

            var mainText = $"{taskString}{sourceName}.{sourceMethodName} {paramsString}";

            if ( Application.isPlaying  )
            {
                var callBindingObject = GetBindingObject<CallBinding>( property );
                if ( !callBindingObject.IsRuntimeValid )
                {
                    isValid = false;
                    validationReport += "CallBinding initialization failed.";
                }
            }

            return new BindingMainString()
            {
                MainText = mainText,
                IsValid = isValid,
                ValidationReport = validationReport 
            };
        }

        protected override void DrawPathField(Rect position, SerializedProperty property )
        {
            var pathProp = property.FindPropertyRelative( nameof(BindingBase.Path) );
            var pathName = pathProp.stringValue;
            position = EditorGUI.PrefixLabel( position, new GUIContent( pathProp.displayName ) );

            var (sourceType, _) = GetSourceTypeAndObject( property ); 
            if ( sourceType != null )
            {
                var compatibleMethods = GetCompartibleMethods( sourceType );
                var methodInfo = compatibleMethods.FirstOrDefault( mi => mi.Name == pathProp.stringValue );

                //Draw select method button
                var isSelectPropertyPressed = false;
                String selectedProperty;
                if ( methodInfo != null )
                {
                    var displayName = GetPrettyMethodName( methodInfo );
                    isSelectPropertyPressed = GUI.Button( position, displayName, Resources.TextField );
                    selectedProperty = pathName;
                }
                else
                {
                    var displayName = pathName == String.Empty 
                            ? "(method not set)"
                            : $"(missed method {pathName} on Source)";
                    //using ( GUIUtils.ChangeContentColor( Color.red ) )
                    {
                        isSelectPropertyPressed = GUI.Button( position, displayName, Resources.ErrorTextField );
                    }
                    selectedProperty = null;
                }

                //Show select method menu
                if ( isSelectPropertyPressed )
                {
                    var menu             = new GenericMenu();
                    foreach (var method in compatibleMethods)
                    {
                        var isBaseMethod = method.DeclaringType != sourceType;
                        string propDisplayName = isBaseMethod ? $"Base/{method.Name}" : method.Name;
                        var capturedName = method.Name;
                        menu.AddItem(new GUIContent(propDisplayName), capturedName == selectedProperty, () =>
                        {
                            pathProp.stringValue = capturedName;
                            pathProp.serializedObject.ApplyModifiedProperties();
                        });
                    }
                    menu.DropDown(position);
                }
            }
            else
            {
                using ( new EditorGUI.DisabledScope() )
                {
                    GUI.TextField( position, $"{pathName} (Source not set)", Resources.DisabledTextField );
                }
            }
        }

        protected override void DrawAdditionalFields(Rect position, SerializedProperty property )
        {
            base.DrawAdditionalFields( position, property );

            var methodInfo = GetSourceMethod( property );
            if ( methodInfo != null )
            {
                position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
                var paramProperty = property.FindPropertyRelative(nameof(CallBinding.Params));
                DrawMethodParameter( position, paramProperty, methodInfo );
            }
        }

        private static MethodInfo[] GetCompartibleMethods(Type sourceType )
        {
            return sourceType.GetMethods( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic )
                             .Where( mi => mi.DeclaringType != typeof(Object) )
                             .GroupBy( mi => mi.Name )
                             .Where( g => g.Count() == 1 ) //Don't want to bother with overloaded methods bc I want to store only method name without signature
                             .SelectMany( g => g )
                             .Where( IsProperMethod )
                             .ToArray();
        }

        private static MethodInfo GetSourceMethod( SerializedProperty property )
        {
            var (sourceType, _) = GetSourceTypeAndObject( property );
            if ( sourceType != null )
            {
                var methods = GetCompartibleMethods( sourceType );
                var methodName = property.FindPropertyRelative( nameof(BindingBase.Path) ).stringValue;
                var method = methods.FirstOrDefault( mi => mi.Name == methodName );
                return method;
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

        private void DrawMethodParameter(Rect position, SerializedProperty paramsProp, MethodInfo method )
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
    }
}

