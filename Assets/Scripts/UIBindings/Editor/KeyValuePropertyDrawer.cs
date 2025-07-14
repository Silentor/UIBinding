using System;
using UIBindings.Editor.Utils;
using UIBindings.Runtime.Types;
using UnityEditor;
using UnityEngine;

namespace UIBindings.Editor
{
    /// <summary>
    /// Draw enum/int or bool key field. It must know the type of key from binding source property
    /// </summary>
    [CustomPropertyDrawer(typeof(KeyValue<>), true)]
    public class KeyValuePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
        {
            EditorGUI.BeginProperty( position, label, property );

            var keyProp = property.FindPropertyRelative( nameof(KeyValue<bool>.Key) );
            var goProp  = property.FindPropertyRelative( nameof(KeyValue<bool>.Value) );

            var keyType = GetSourceKeyType( property );
            var isWide = !keyType.IsPrimitive ;
            var rects  = GUIUtils.GetHorizontalRects( position, 5, isWide ? 0 : 100, 0 );

            EditorGUI.BeginChangeCheck();
            if( keyType.IsPrimitive )
            {
                if( keyType == typeof(bool) )
                    keyProp.intValue = EditorGUI.Toggle( rects.Item1, keyProp.intValue != 0 ) ? 1 : 0;
                else
                    keyProp.intValue = EditorGUI.IntField( rects.Item1, keyProp.intValue );
            }
            else if( keyType.IsEnum )
            {
                var enumValue = (Enum)Enum.ToObject( keyType, keyProp.intValue );
                enumValue        = EditorGUI.EnumPopup( rects.Item1, enumValue );
                keyProp.intValue = Convert.ToInt32( enumValue );
            }
            else
            {
                EditorGUI.LabelField( rects.Item1, "Key type not supported: " + keyType.Name );
            }

            EditorGUI.PropertyField( rects.Item2, goProp, GUIContent.none);

            EditorGUI.EndProperty();
        }

        private Type GetSourceKeyType( SerializedProperty property )
        {
            var dataBinding = DataBindingEditor.DataBinding;
            var sourceProp = BindingEditorUtils.GetSourceProperty( dataBinding, property.serializedObject.targetObject );
            return sourceProp?.PropertyType ?? typeof(int);
        }
    }
}