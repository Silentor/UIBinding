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
    public class CallBindingEditor : PropertyDrawer
    {
        private Int32 _additionalLinesCount;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (  new EditorGUI.PropertyScope( position, label, property ) ) ;

            _additionalLinesCount = 0;
            position.height = EditorGUIUtility.singleLineHeight;
            var labelRect = position;

            //Draw label
            labelRect.width     = EditorGUIUtility.labelWidth;
            property.isExpanded = EditorGUI.Foldout( labelRect, property.isExpanded, label, true );

            //Draw main content
            var mainLineContentPosition = position;
            mainLineContentPosition.xMin += EditorGUIUtility.labelWidth;
            var rects       = GUIUtils.GetHorizontalRects( mainLineContentPosition, 2, 0, 20 );
            var enabledProp = property.FindPropertyRelative( nameof(Binding.Enabled) );

            var isEnabled         = enabledProp.boolValue;
            var binding           = (CallBinding)property.boxedValue;
            var sourceName        = binding.Source ? binding.Source.name : "null";
            var sourceMethod      = GetSourceMethod( binding );
            var sourceMethodName  = sourceMethod != null ? sourceMethod.Name : "null";
            var isValid           = !isEnabled || (sourceMethod != null && IsProperMethod( sourceMethod ));
            var paramsString = sourceMethod != null && sourceMethod.GetParameters().Length > 0
                ? String.Concat("(", String.Join( ", ", sourceMethod.GetParameters().Select( p => p.ParameterType.Name ) ), ")")
                : "()";

            //Draw Enabled toggle
            EditorGUI.PropertyField( rects.Item2, enabledProp, GUIContent.none );

            using ( new EditorGUI.DisabledGroupScope( !isEnabled ) )
            {
                var mainText = $"{sourceName}.{sourceMethodName} {paramsString}";
                GUI.Label( rects.Item1, mainText, isValid ? Resources.DefaultLabel : Resources.ErrorLabel );

                //Draw expanded content
                if ( property.isExpanded )
                {
                    using ( new EditorGUI.IndentLevelScope( 1 ) )
                    {
                        position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
                        var sourceProperty = property.FindPropertyRelative( nameof(Binding.Source) );
                        //EditorGUI.PropertyField( position, sourceProperty );
                        DrawSourceField( position, sourceProperty );

                        position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
                        var pathProperty   = property.FindPropertyRelative( nameof(Binding.Path) );
                        //EditorGUI.PropertyField( position, pathProperty );
                        var methodInfo = DrawPathField( position, pathProperty, binding );

                        // position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
                        // var convertersProperty = property.FindPropertyRelative( Binding.ConvertersPropertyName );
                        // //EditorGUI.PropertyField( position, convertersProperty );
                        // DrawConvertersField( position, convertersProperty, binding, sourceType, targetType );

                        if ( methodInfo != null )
                        {
                            position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
                            var paramProperty = property.FindPropertyRelative(nameof(CallBinding.Params));
                            DrawMethodParameter( position, paramProperty, methodInfo, binding );
                        }
                    }
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded)
                return Resources.LineHeightWithMargin * 3 + _additionalLinesCount * Resources.LineHeightWithMargin;
            else
                return EditorGUIUtility.singleLineHeight;
        }

        private static MethodInfo GetSourceMethod(CallBinding binding)
        {
            if (!binding.Source || String.IsNullOrEmpty(binding.Path))
                return null;

            var sourceType = binding.Source.GetType();
            var method = sourceType.GetMethod(binding.Path, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (IsProperMethod( method ))
                return method;

            return null;
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

        private void DrawSourceField(  Rect position, SerializedProperty sourceProp )
        {
            var oldSource = sourceProp.objectReferenceValue;
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField( position, sourceProp );
            if ( EditorGUI.EndChangeCheck() )
            {
                if( !oldSource && sourceProp.objectReferenceValue )
                {
                    //Autosearch for components with methods todo implement
                    if( sourceProp.objectReferenceValue is GameObject sourceGO )
                    {
                        var components = sourceGO.GetComponents<MonoBehaviour>();
                        if( components.Length > 0 )
                            sourceProp.objectReferenceValue = components[0];
                        
                        // foreach (var component in components)
                        //     if ( component is INotifyPropertyChanged )
                        //     {
                        //         sourceProp.objectReferenceValue = component;
                        //         break;
                        //     }

                        sourceProp.serializedObject.ApplyModifiedProperties();
                    } 
                }
            }
        }

        private MethodInfo DrawPathField(  Rect position, SerializedProperty pathProp, Binding binding )
        {
            position = EditorGUI.PrefixLabel( position, new GUIContent( pathProp.displayName ) );

            var sourceObject = binding.Source;
            if ( sourceObject )
            {
                var sourceType = sourceObject.GetType();
                var compatibleMethods = sourceType.GetMethods( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic )
                                                        .Where( IsProperMethod )
                                                        .ToArray();
                MethodInfo methodInfo = null;
                var methodName = pathProp.stringValue;
                if ( !String.IsNullOrEmpty( methodName ) )
                {
                    methodInfo = compatibleMethods.FirstOrDefault( mi => mi.Name == methodName );
                }

                //Draw select method button
                var isSelectPropertyPressed = false;
                String selectedProperty;
                if ( methodInfo != null )
                {
                    var displayName = $"{methodName}()";
                    isSelectPropertyPressed = GUI.Button( position, displayName, Resources.TextField );
                    selectedProperty = methodName;
                }
                else
                {
                    var displayName = methodName == String.Empty 
                            ? $"(method not set)"
                            : $"(missed method {methodName} on Source)";
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

                return methodInfo;
            }
            else
            {
                using ( new EditorGUI.DisabledScope() )
                {
                    GUI.TextField( position, "(Source not set)", Resources.DisabledTextField );
                }

                return null;
            }
        }

        private void DrawMethodParameter(Rect position, SerializedProperty paramsProp, MethodInfo method, CallBinding binding )
        {
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

        private static class Resources
        {
            public static readonly GUIStyle DefaultLabel = new GUIStyle( GUI.skin.label );
            public static readonly GUIStyle ErrorLabel = new GUIStyle( DefaultLabel )
                                                         {
                                                                 normal  = { textColor  = Color.red },
                                                                 hover   = { textColor  = Color.red },
                                                                 focused =  { textColor = Color.red }
                                                         };
            public static GUIStyle TextField => new GUIStyle( GUI.skin.textField );

            public static GUIStyle DisabledTextField => new GUIStyle( TextField )
                                                        {
                                                                normal = { textColor = Color.gray },
                                                        };
            public static GUIStyle ErrorTextField => new GUIStyle( TextField )
                                                     {
                                                             normal  = { textColor = Color.red },
                                                             hover   = { textColor = Color.red },
                                                             focused = { textColor = Color.red }
                                                     };

            public static readonly float LineHeightWithMargin = EditorGUIUtility.singleLineHeight + 2;

            public static readonly GUIContent AddButtonContent = new GUIContent( "+", "Add compatible converter" );
            public static readonly GUIContent RemoveBtnContent = new GUIContent( "-", "Remove converter" );
        }
    }
}

