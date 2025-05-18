using System;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using System.Linq;
using System.Reflection;
using UIBindings.Editor.Utils;
using UIBindings.Runtime;
using Object = UnityEngine.Object;

namespace UIBindings.Editor
{
    [CustomPropertyDrawer( typeof(SourcePath) )]
    public class SourcePathPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Draw prefix label and get the rect for the button
            position = EditorGUI.PrefixLabel(position, label);

            // Get the parent object and find the 'Source' field
            var sourceObject = GetSourceObject( property );

            if ( sourceObject )
            {
                var sourceType = sourceObject.GetType();
                property = property.FindPropertyRelative( nameof(SourcePath.Path) );
                var propInfo = sourceObject.GetType().GetProperty( property.stringValue );

                //Draw select bindable property button
                var isSelectPropertyPressed = false;
                String selectedProperty;
                if ( propInfo != null )
                {
                    var displayName = $"{propInfo.Name} ({propInfo.PropertyType.Name})";
                    isSelectPropertyPressed = GUI.Button( position, displayName, Resources.TextFieldStyle );
                    selectedProperty = property.stringValue;
                }
                else
                {
                    var displayName = $"{property.stringValue} (missed property)";
                    using ( GUIUtils.ChangeContentColor( Color.red ) )
                    {
                        isSelectPropertyPressed = GUI.Button( position, displayName, Resources.TextFieldStyle );
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
                        string propName = prop.Name;
                        menu.AddItem(new GUIContent(propName), propName == selectedProperty, () =>
                        {
                            property.stringValue = propName;
                            property.serializedObject.ApplyModifiedProperties();
                        });
                    }
                    menu.DropDown(position);
                }
            }
            else
            {
                GUI.Label( position, "Source not set", Resources.TextFieldStyle );
            }

            EditorGUI.EndProperty();
        }

        private static Object GetSourceObject(SerializedProperty property )
        {
            var targetObject = property.serializedObject.targetObject;
            var sourceField  = targetObject.GetType().GetField("Source");
            Object sourceObject = null;
            if ( sourceField != null )
            {
                sourceObject = sourceField.GetValue(targetObject) as UnityEngine.Object;
            }

            return sourceObject;
        }

        private static class Resources
        {
            public static GUIStyle TextFieldStyle => GUI.skin.textField;
        }
    }
}

