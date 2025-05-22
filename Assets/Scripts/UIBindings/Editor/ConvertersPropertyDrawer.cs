using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework;
using UIBindings.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace UIBindings.Editor
{
    [CustomPropertyDrawer( typeof(BinderBase.ConvertersList), true )]
    public class ConvertersPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {
            EditorGUI.BeginProperty( position, label, property );

            position = EditorGUI.PrefixLabel( position, label );
            position.height = EditorGUIUtility.singleLineHeight;
            var converters = property.FindPropertyRelative( nameof(BinderBase.ConvertersList.Converters) );

            var sourceType = GetSourcePropertyType( converters );
            var sourceTypeName = sourceType != null ? sourceType.Name : "null";
            var targetType = GetTargetValueType( converters );

            //Draw main line
            //Draw label
            var rects = GUIUtils.GetHorizontalRects( position, 5, new (), new (30), new (30) );
            var infoRect = rects.Item1;
            var isCompatible = IsSourceTargetTypesCompatible( sourceType, targetType, converters );
            GUI.Label( infoRect, $"{sourceTypeName} -> {targetType.Name}", 
                isCompatible ? Resources.DefaultLabelStyle : Resources.ErrorLabelStyle );

            //Draw add converter button
            var addBtnRect = rects.Item2;
            if (GUI.Button(addBtnRect, Resources.AddButtonContent))
            {
                var currentOutputType = GetCurrentOutputType(converters);
                var compatibleTypes = GetCompartibleConverters( currentOutputType );

                if( compatibleTypes.Count == 0 )
                {
                    Debug.LogWarning( $"No compatible converter found for {currentOutputType}" );
                    return;
                }

                var menu = new GenericMenu();
                foreach (var typeInfo in compatibleTypes)
                {
                    menu.AddItem(new GUIContent(typeInfo.TypeInfo.FullType.Name), false, () =>
                    {
                        var newIndex     = converters.arraySize;
                        var newConverter = (ConverterBase)Activator.CreateInstance(typeInfo.TypeInfo.FullType);
                        newConverter.ReverseMode = typeInfo.IsReverseMode;
                        converters.InsertArrayElementAtIndex(newIndex);
                        converters.GetArrayElementAtIndex(newIndex).managedReferenceValue = newConverter;
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }
                menu.ShowAsContext();
            }

            //Draw clear all button
            var clearAllRect = rects.Item3;
            if ( GUI.Button( clearAllRect, Resources.ClearAllContent ) )
            {
                while ( converters.arraySize > 0 )
                {
                    converters.DeleteArrayElementAtIndex( 0 );
                }
                converters.serializedObject.ApplyModifiedProperties();
            }

            //Draw converters list
            var converterRects = GUIUtils.GetHorizontalRects( position, 5, new (), new (30) );
            for ( int i = 0; i < converters.arraySize; i++ )
            {
                converterRects = converterRects.Translate( new Vector2( 0, EditorGUIUtility.singleLineHeight ) );
                var converterObject = (ConverterBase)converters.GetArrayElementAtIndex( i ).managedReferenceValue;
                var converterTypes = ConverterBase.GetConverterTypeInfo( converterObject );
                var converterText = converterObject.ReverseMode 
                        ? $"{converterObject.GetType().Name} {converterTypes.output.Name} -> {converterTypes.input.Name}"
                        :$"{converterObject.GetType().Name} {converterTypes.input.Name} -> {converterTypes.output.Name}";
                GUI.Label( converterRects.Item1, converterText );
                if( GUI.Button( converterRects.Item2, Resources.RemoveBtnContent ))
                {
                    converters.DeleteArrayElementAtIndex( i );
                    converters.serializedObject.ApplyModifiedProperties();
                    break;
                }
            }

            EditorGUI.EndProperty();
        }

        private Boolean IsSourceTargetTypesCompatible(Type sourceType, Type targetType, SerializedProperty converters )
        {
            if (sourceType == null || targetType == null)
                return false;

            // If no converters, check direct assignability
            if (converters.arraySize == 0)
                return targetType.IsAssignableFrom(sourceType);

            // Check the chain: sourceType -> [converter1] -> ... -> [converterN] -> targetType
            Type currentType = sourceType;
            for (int i = 0; i < converters.arraySize; i++)
            {
                var converterProp = converters.GetArrayElementAtIndex(i);
                var converter = converterProp.managedReferenceValue as ConverterBase;
                if (converter == null)
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

        private Type GetCurrentOutputType( SerializedProperty converters )
        {
            if ( converters.arraySize > 0 )
                return GetLastConverterOutputType( converters );
            return GetSourcePropertyType( converters );
        }

        [CanBeNull]
        private Type GetSourcePropertyType( SerializedProperty converters )
        {
            var binder = (BinderBase) converters.serializedObject.targetObject;
            if ( binder.Source && !String.IsNullOrEmpty( binder.Path ) )
            {
                var reflectedProperty = binder.Source.GetType().GetProperty( binder.Path );
                if ( reflectedProperty != null )
                    return reflectedProperty.PropertyType;
            }

            return null;
        }

        private Type GetLastConverterOutputType( SerializedProperty converters )
        {
            if ( converters.arraySize > 0 )
            {
                var lastConverter = converters.GetArrayElementAtIndex( converters.arraySize - 1 );
                var converter = (ConverterBase)lastConverter.managedReferenceValue;
                var converterTypeInfo = ConverterBase.GetConverterTypeInfo( converter );
                return converterTypeInfo.output;
            }

            return null;
        }

        private Type GetTargetValueType( SerializedProperty converters )
        {
            var binder = (BinderBase) converters.serializedObject.targetObject;
            var typeInfo = BinderBase.GetBinderTypeInfo( binder.GetType() );
            return typeInfo.value;
        }


        private static IReadOnlyList<ConverterTypeInfo> PrepareTypeCache( )
        {
            var allConverters = TypeCache.GetTypesDerivedFrom<ConverterBase>();
            var result = new List<ConverterTypeInfo>( allConverters.Count );
            foreach ( var converter in allConverters )
            {
                if ( !converter.IsAbstract )
                {
                    var typeInfo = ConverterBase.GetConverterTypeInfo( converter );
                    result.Add( new ConverterTypeInfo(
                        typeInfo.input,
                        typeInfo.output,
                        typeInfo.template,
                        converter
                    ));
                }
            }

            return result;
        }

        private IReadOnlyList<ConverterType> GetCompartibleConverters ( Type sourceType )
        {
            var result = new List<ConverterType>();
            foreach ( var converter in ConverterTypes )
            {
                if ( converter.InputType == sourceType )
                {
                    result.Add( new ConverterType( converter ) );
                }
                else if ( converter.TemplateType == typeof(ConverterTwoWayBase<,>) && converter.OutputType == sourceType )
                {
                    result.Add( new ConverterType( converter ) { IsReverseMode = true } );
                }
            }

            return result;
        }

        
        public override Single GetPropertyHeight(SerializedProperty property, GUIContent label )
        {
            return EditorGUIUtility.singleLineHeight * ( property.FindPropertyRelative( nameof(BinderBase.ConvertersList.Converters) ).arraySize + 1 );
        }
        

        private static readonly IReadOnlyList<ConverterTypeInfo> ConverterTypes = PrepareTypeCache();

        public readonly struct ConverterTypeInfo
        {
            public readonly Type InputType;
            public readonly Type OutputType;
            public readonly Type TemplateType;
            public readonly Type FullType;

            public ConverterTypeInfo(Type inputType, Type outputType, Type templateType, Type fullType)
            {
                InputType = inputType;
                OutputType = outputType;
                TemplateType = templateType;
                FullType = fullType;
            }
        }

        public struct ConverterType
        {
            public readonly ConverterTypeInfo TypeInfo;
            public          bool              IsReverseMode;

            public ConverterType( ConverterTypeInfo typeInfo ) : this()
            {
                TypeInfo = typeInfo;
            }
        }

        private static class Resources
        {
            public static readonly GUIStyle DefaultLabelStyle = new GUIStyle(GUI.skin.label);
            public static readonly GUIStyle ErrorLabelStyle = new GUIStyle(GUI.skin.label)
                                                              {
                                                                      normal = { textColor = Color.red }, 
                                                                      hover = { textColor = Color.red }, 
                                                                      focused = { textColor = Color.red }, 
                                                                      active = { textColor = Color.red },
                                                              };

            public static readonly GUIContent AddButtonContent = new GUIContent( "+", "Add compartible converter" );
            public static readonly GUIContent ClearAllContent = new GUIContent( "X", "Remove all converters" );
            public static readonly GUIContent RemoveBtnContent = new GUIContent( "-", "Remove converter" );

        }
    }
}

