using System;
using UIBindings.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace UIBindings.Editor
{
    [CustomEditor( typeof(GameObjectSelectBinder) )]
    public class GameObjectSelectBinderEditor : UnityEditor.Editor
    {
        private GameObjectSelectBinder _target;

        private void OnEnable( )
        {
            _target = target as GameObjectSelectBinder;
        }

        public override void OnInspectorGUI( )
        {
            serializedObject.Update();

            var bindingProp = serializedObject.FindProperty( nameof(_target.SelectorBinding) );
            EditorGUILayout.PropertyField( bindingProp );

            var gosProp = serializedObject.FindProperty( nameof(_target.GameObjects) );
            KeyGameObjectDrawer.KeyType = GetSourcePropertyType( bindingProp );
            EditorGUILayout.PropertyField( gosProp );

            //Editor tools
            if ( KeyGameObjectDrawer.KeyType.IsEnum )
            {
                if ( GUILayout.Button( "Generate enum entries", GUILayout.Width( 200 ) ) )
                {
                    var enumValues = Enum.GetValues( KeyGameObjectDrawer.KeyType );
                    foreach ( var enumValue in enumValues )
                    {
                        var key = Convert.ToInt32( enumValue );
                        if ( !IsKeyContains( gosProp, key ) )
                        {
                            AddGameObject( gosProp, key, null );
                        }
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private bool IsKeyContains( SerializedProperty gameObjectsProp, int key )
        {
            for ( int i = 0; i < gameObjectsProp.arraySize; i++ )
            {
                var keyProp = gameObjectsProp.GetArrayElementAtIndex( i ).FindPropertyRelative( nameof(GameObjectSelectBinder.KeyGameObject.Key) );
                if ( keyProp.intValue == key )
                {
                    return true;
                }
            }

            return false;
        }

        private void AddGameObject( SerializedProperty gameObjectsProp, int key, GameObject go )
        {
            gameObjectsProp.arraySize++;
            var newElement = gameObjectsProp.GetArrayElementAtIndex( gameObjectsProp.arraySize - 1 );
            newElement.FindPropertyRelative( nameof(GameObjectSelectBinder.KeyGameObject.Key) ).intValue = key;
            newElement.FindPropertyRelative( nameof(GameObjectSelectBinder.KeyGameObject.GameObject) ).objectReferenceValue = go;
        }

        private Type GetSourcePropertyType( SerializedProperty bindingProperty )
        {
            var bindingInstance = (DataBinding)bindingProperty.boxedValue;
            var propertyInfo = DataBindingEditor.GetSourceProperty( bindingInstance );
            Type keyType;
            if ( propertyInfo != null )
            {
                keyType = propertyInfo.PropertyType;
            }
            else
            {
                keyType = typeof(int);
            }

            return keyType;
        }
    }

    [CustomPropertyDrawer(typeof(GameObjectSelectBinder.KeyGameObject))]
    public class KeyGameObjectDrawer : PropertyDrawer
    {
        public static Type KeyType ;

        public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
        {
            EditorGUI.BeginProperty( position, label, property );

            var keyProp = property.FindPropertyRelative( nameof(GameObjectSelectBinder.KeyGameObject.Key) );
            var goProp = property.FindPropertyRelative( nameof(GameObjectSelectBinder.KeyGameObject.GameObject) );

            var isWide = !KeyType.IsPrimitive ;
            var rects = GUIUtils.GetHorizontalRects( position, 5, isWide ? 0 : 100, 0 );

            EditorGUI.BeginChangeCheck();
            if( KeyType.IsPrimitive )
            {
                if( KeyType == typeof(bool) )
                    keyProp.intValue = EditorGUI.Toggle( rects.Item1, keyProp.intValue != 0 ) ? 1 : 0;
                else
                    keyProp.intValue = EditorGUI.IntField( rects.Item1, keyProp.intValue );
            }
            else if( KeyType.IsEnum )
            {
                var enumValue = (Enum)Enum.ToObject( KeyType, keyProp.intValue );
                enumValue = EditorGUI.EnumPopup( rects.Item1, enumValue );
                keyProp.intValue = Convert.ToInt32( enumValue );
            }
            else
            {
                EditorGUI.LabelField( rects.Item1, "Key type not supported: " + KeyType.Name );
            }

            EditorGUI.PropertyField( rects.Item2, goProp, GUIContent.none);

            EditorGUI.EndProperty();
        }

        // private Type GetSourceType( SerializedProperty property )
        // {
        //     var binderObject = ()property.serializedObject.targetObject
        // }
    }
}