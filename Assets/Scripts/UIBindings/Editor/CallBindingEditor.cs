using System;
using System.Linq;
using System.Reflection;
using UIBindings.Editor.Utils;
using UIBindings.Runtime;
using UnityEditor;
using UnityEngine;

namespace UIBindings.Editor
{
    [CustomPropertyDrawer(typeof(CallBinding))]
    public class CallBindingEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (  new EditorGUI.PropertyScope( position, label, property ) ) ;

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
            var isValid           = !isEnabled || sourceMethod != null;

            //Draw Enabled toggle
            EditorGUI.PropertyField( rects.Item2, enabledProp, GUIContent.none );

            using ( new EditorGUI.DisabledGroupScope( !isEnabled ) )
            {
                var mainText = $"{sourceName}.{sourceMethodName} ()";
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
                        DrawPathField( position, pathProperty, binding );

                        // position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
                        // var convertersProperty = property.FindPropertyRelative( Binding.ConvertersPropertyName );
                        // //EditorGUI.PropertyField( position, convertersProperty );
                        // DrawConvertersField( position, convertersProperty, binding, sourceType, targetType );
                    }
                }
            }

           
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded)
                return Resources.LineHeightWithMargin * 3;
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

            //Check if method has no parameters and returns void
            if (method.GetParameters().Length != 0 )
                return false;

            //Check if method is not a property getter or setter
            if ( method.IsSpecialName )
                return false;

            //Check if method is not obsolete
            if (method.GetCustomAttribute<ObsoleteAttribute>() != null)
                return false;

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

        private void DrawPathField(  Rect position, SerializedProperty pathProp, Binding binding )
        {
            position = EditorGUI.PrefixLabel( position, new GUIContent( pathProp.displayName ) );

            var sourceObject = binding.Source;
            if ( sourceObject )
            {
                var sourceType = sourceObject.GetType();
                var methodInfo = sourceType.GetMethod( pathProp.stringValue, Array.Empty<Type>() );

                //Draw select bindable property button
                var isSelectPropertyPressed = false;
                String selectedProperty;
                if ( methodInfo != null )
                {
                    var displayName = $"{methodInfo.Name}";
                    isSelectPropertyPressed = GUI.Button( position, displayName, Resources.TextField );
                    selectedProperty = pathProp.stringValue;
                }
                else
                {
                    var displayName = pathProp.stringValue == String.Empty 
                            ? $"(method not set)"
                            : $"(missed method {pathProp.stringValue} on Source)";
                    //using ( GUIUtils.ChangeContentColor( Color.red ) )
                    {
                        isSelectPropertyPressed = GUI.Button( position, displayName, Resources.ErrorTextField );
                    }
                    selectedProperty = null;
                }

                //Select compatible call method from list
                if ( isSelectPropertyPressed )
                {
                    var methods = sourceType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                          .Where( IsProperMethod )
                                          .ToArray();
                    var menu             = new GenericMenu();
                    foreach (var method in methods)
                    {
                        var isBaseMethod = method.DeclaringType != sourceType;
                        string propDisplayName = isBaseMethod ? $"Base/{method.Name}" : method.Name;
                        string methodName = method.Name;
                        menu.AddItem(new GUIContent(propDisplayName), methodName == selectedProperty, () =>
                        {
                            pathProp.stringValue = methodName;
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
                    GUI.TextField( position, "(Source not set)", Resources.DisabledTextField );
                }
            }
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

