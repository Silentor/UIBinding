using System;
using UIBindings.Runtime.Types;
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
            var sourcePropertyType = GetSourcePropertyType( bindingProp );
            EditorGUILayout.PropertyField( gosProp );

            //Editor tools
            if ( sourcePropertyType.IsEnum )
            {
                if ( GUILayout.Button( "Generate enum entries", GUILayout.Width( 200 ) ) )
                {
                    var enumValues = Enum.GetValues( sourcePropertyType );
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
                var keyProp = gameObjectsProp.GetArrayElementAtIndex( i ).FindPropertyRelative( nameof(KeyValue<bool>.Key) );
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
            newElement.FindPropertyRelative( nameof(KeyValue<bool>.Key) ).intValue = key;
            newElement.FindPropertyRelative( nameof(KeyValue<bool>.Value) ).objectReferenceValue = go;
        }

        private Type GetSourcePropertyType( SerializedProperty bindingProperty )
        {
            var propertyInfo = DataBindingEditor.GetSourceProperty( bindingProperty );
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
}