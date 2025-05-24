using System;
using System.Linq;
using System.Reflection;
using UIBindings.Editor.Utils;
using UIBindings.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIBindings.Editor
{
    [CustomPropertyDrawer( typeof(Binding<>), true )]
    public class BindingEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {
            using (  new EditorGUI.PropertyScope( position, label, property ) ) ;

            position.height = EditorGUIUtility.singleLineHeight;
            var labelRect = position;

            //Draw label
            labelRect.width = EditorGUIUtility.labelWidth;
            property.isExpanded = EditorGUI.Foldout( labelRect, property.isExpanded, label );

            //Draw main content
            var mainLineContentPosition = position;
            mainLineContentPosition.xMin += EditorGUIUtility.labelWidth;

            var binding = (Binding)property.boxedValue;
            var bindingType    = GetBindingTypeInfo( binding );
            var sourceType     = GetSourcePropertyType( binding );
            var sourceTypeName = sourceType            != null ? sourceType.Name : "null";
            var targetTypeName = bindingType.valueType != null ? bindingType.valueType.Name : "null";
            var isValid = sourceType != null; 

            GUI.Label( mainLineContentPosition, $"{sourceTypeName} -> {targetTypeName}", isValid ? Resources.DefaultLabel : Resources.ErrorLabel );
            
            //Draw expanded content
            if ( property.isExpanded )
            {
                using ( new EditorGUI.IndentLevelScope( 1 ) )
                {
                    position = position.Translate( new Vector2( 0, EditorGUIUtility.singleLineHeight + 2) );
                    var sourceProperty = property.FindPropertyRelative( nameof(Binding.Source) );
                    //EditorGUI.PropertyField( position, sourceProperty );
                    DrawSourceField( position, sourceProperty );

                    position = position.Translate( new Vector2( 0, EditorGUIUtility.singleLineHeight + 2) );
                    var pathProperty   = property.FindPropertyRelative( nameof(Binding.Path) );
                    //EditorGUI.PropertyField( position, pathProperty );
                    DrawPathField( position, pathProperty, binding );

                    position = position.Translate( new Vector2( 0, EditorGUIUtility.singleLineHeight + 2) );
                    var convertersProperty = property.FindPropertyRelative( Binding.ConvertersPropertyName);
                    EditorGUI.PropertyField( position, convertersProperty );
                }

            }

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
                    //Autosearch for components with notify property changed
                    if( sourceProp.objectReferenceValue is GameObject sourceGO )
                    {
                        var components = sourceGO.GetComponents<MonoBehaviour>();
                        if( components.Length > 0 )
                            sourceProp.objectReferenceValue = components[0];
                        
                        foreach (var component in components)
                            if ( component is INotifyPropertyChanged )
                            {
                                sourceProp.objectReferenceValue = component;
                                break;
                            }

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
                pathProp = pathProp.FindPropertyRelative( nameof(SourcePath.Path) );
                var propInfo = sourceObject.GetType().GetProperty( pathProp.stringValue );

                //Draw select bindable property button
                var isSelectPropertyPressed = false;
                String selectedProperty;
                if ( propInfo != null )
                {
                    var displayName = $"{propInfo.Name} ({propInfo.PropertyType.Name})";
                    isSelectPropertyPressed = GUI.Button( position, displayName, Resources.TextField );
                    selectedProperty = pathProp.stringValue;
                }
                else
                {
                    var displayName = pathProp.stringValue == String.Empty 
                            ? $"(property not set)"
                            : $"{pathProp.stringValue} (missed property on Source)";
                    //using ( GUIUtils.ChangeContentColor( Color.red ) )
                    {
                        isSelectPropertyPressed = GUI.Button( position, displayName, Resources.ErrorTextField );
                    }
                    selectedProperty = null;
                }

                //Select bindable property from list
                if ( isSelectPropertyPressed )
                {
                    var props = sourceType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                          .Where(p => p.CanRead)
                                          .ToArray();
                    var menu             = new GenericMenu();
                    foreach (var prop in props)
                    {
                        var isBaseProp = prop.DeclaringType != sourceType;
                        string propDisplayName = isBaseProp ? $"Base/{prop.Name}" : prop.Name;
                        string propName = prop.Name;
                        menu.AddItem(new GUIContent(propDisplayName), propName == selectedProperty, () =>
                        {
                            pathProp.stringValue = propName;
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

        private void DrawConvertersField( Rect position, SerializedProperty convertersProp )
        {
            if ( convertersProp.isArray && convertersProp.arraySize > 0 )
            {
                for ( int i = 0; i < convertersProp.arraySize; i++ )
                {
                    var converter = convertersProp.GetArrayElementAtIndex( i );
                    EditorGUI.PropertyField( position, converter );
                    position = position.Translate( new Vector2( 0, EditorGUIUtility.singleLineHeight + 2 ) );
                }
            }
            else
            {
                using ( new EditorGUI.DisabledScope() )
                {
                    GUI.TextField( position, "(No converters)", Resources.DisabledTextField );
                }
            }
        }

        public override Single GetPropertyHeight(SerializedProperty property, GUIContent label )
        {
            if( property.isExpanded )
                return EditorGUIUtility.singleLineHeight + EditorGUI.GetPropertyHeight( property.FindPropertyRelative( nameof(Binding.Source) )  ) 
                                                     + EditorGUI.GetPropertyHeight( property.FindPropertyRelative( nameof(Binding.Path) ) ) 
                                                     + EditorGUI.GetPropertyHeight( property.FindPropertyRelative( Binding.ConvertersPropertyName ) );
            else
                return EditorGUIUtility.singleLineHeight;
        }

        private static (Type valueType, Type templateType) GetBindingTypeInfo ( Binding binding )
        {
            return Binding.GetBinderTypeInfo( binding.GetType() );
        }

        private static Type GetSourcePropertyType( Binding binding )
        {
            if ( !binding.Source )
                return null;

            var propertyPath = binding.Path;
            if ( string.IsNullOrEmpty( propertyPath ) )
                return null;

            var sourceType = binding.Source.GetType();
            var sourceProperty   = sourceType.GetProperty( propertyPath );

            if ( sourceProperty == null )
                return null;

            return sourceProperty.PropertyType;
        }

        private static class Resources
        {
            public static readonly GUIStyle DefaultLabel = new GUIStyle( GUI.skin.label );
            public static readonly GUIStyle ErrorLabel = new GUIStyle( DefaultLabel )
            {
                normal = { textColor = Color.red },
                hover = { textColor = Color.red },
                focused =  { textColor = Color.red }
            };
            public static GUIStyle TextField => new GUIStyle( GUI.skin.textField );

            public static GUIStyle DisabledTextField => new GUIStyle( TextField )
                                                        {
                                                                normal = { textColor = Color.gray },
                                                        };
            public static GUIStyle ErrorTextField => new GUIStyle( TextField )
            {
                normal = { textColor = Color.red },
                hover = { textColor = Color.red },
                focused = { textColor = Color.red }
            };
        }
    }
}