using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIBindings.Editor
{
    [CustomEditor( typeof(BinderBase), editorForChildClasses: true)]
    public class BinderEditor : UnityEditor.Editor
    {
        private BinderBase _target;

        private void OnEnable( )
        {
            _target = (BinderBase) target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyPath == "m_Script" || iterator.name == BinderBase.ConvertersFieldName )
                    continue;
                EditorGUILayout.PropertyField(iterator, true);
            }

            //Draw info about source and target
            var binderTypeInfo = BinderBase.GetBinderTypeInfo( target.GetType() );
            var targetType = binderTypeInfo.value;
            var sourceType = GetSourcePropertyType();
            var sourceTypeName = sourceType != null ? sourceType.Name : "null";
            var isBindingValid = IsSourceTargetTypesCompatible( sourceType, targetType, _target.Converters );

            var info = binderTypeInfo.template == typeof(BinderBase<>) ? $"{sourceTypeName} -> {targetType.Name}" : $"{sourceTypeName} <-> {targetType.Name}";
            EditorGUILayout.LabelField( "Binding info", info, isBindingValid ? Resources.DefaultLabelStyle : Resources.ErrorLabelStyle );


            // Draw Converters field last
            var converters = serializedObject.FindProperty( BinderBase.ConvertersFieldName  );
            if (converters != null)
                EditorGUILayout.PropertyField(converters, true);

            //serializedObject.ApplyModifiedProperties();
        }

        [CanBeNull]
        private Type GetSourcePropertyType(  )
        {
            var binder = (BinderBase) target;
            if ( binder.Source && !String.IsNullOrEmpty( binder.Path ) )
            {
                var reflectedProperty = binder.Source.GetType().GetProperty( binder.Path );
                if ( reflectedProperty != null )
                    return reflectedProperty.PropertyType;
            }

            return null;
        }

        private Boolean IsSourceTargetTypesCompatible(Type sourceType, Type targetType, IReadOnlyList<ConverterBase> converters )
        {
            if (sourceType == null || targetType == null)
                return false;

            // If no converters, check direct assignability
            if (converters.Count == 0)
                return targetType == sourceType;

            // Check the chain: sourceType -> [converter1] -> ... -> [converterN] -> targetType
            Type currentType = sourceType;
            for (int i = 0; i < converters.Count; i++)
            {
                var converter     = converters[i];
                if (converter == null)              //Something wrong with converter
                    return false;
                var typeInfo = ConverterBase.GetConverterTypeInfo(converter);
                // The converter's input type must be assignable from the current type
                if (!typeInfo.input.IsAssignableFrom(currentType))
                    return false;
                currentType = typeInfo.output;
            }
            // After all converters, the result type must be assignable to the target type
            return targetType.IsAssignableFrom(currentType);
        }

        private static class Resources
        {
            public static readonly GUIStyle DefaultLabelStyle = new GUIStyle(GUI.skin.label);
            public static readonly GUIStyle ErrorLabelStyle = new GUIStyle(GUI.skin.label)
                                                              {
                                                                      normal  = { textColor = Color.red }, 
                                                                      hover   = { textColor = Color.red }, 
                                                                      focused = { textColor = Color.red }, 
                                                                      active  = { textColor = Color.red },
                                                              };
        }
    }
}

