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

            if( converters.arraySize == 0 )
            {
                var rects = GUIUtils.GetHorizontalRects( position, 5, new (), new (30) );
                GUI.Label( rects.Item1, "none" );
                if ( GUI.Button( rects.Item2, Resources.AddButtonContent ) )
                {
                    AddConverter( converters );
                }
            }
            else
            {
                var prevType = sourceType;
                for ( int i = 0; i < converters.arraySize; i++ )
                {
                    var converterName = String.Empty;
                    var isValid = true;
                    var converter = (ConverterBase)converters.GetArrayElementAtIndex( i ).managedReferenceValue;
                    Type converterOutput = null;
                    Type converterInput = null;
                    if ( converter == null )
                    {
                        isValid = false;
                        converterName = "null";
                        prevType = null;
                    }
                    else
                    {
                        var converterTypeInfo = ConverterBase.GetConverterTypeInfo( converter );
                        converterOutput = converterTypeInfo.output;
                        converterInput = converterTypeInfo.input;
                        converterName = $"{converter.GetType().Name.Replace( "Converter", "" )} {(converter.ReverseMode ? "(R)" : "")} -> {converterOutput.Name}";
                        if ( converterInput != prevType )
                        {
                            isValid = false;
                        }

                        prevType = converterOutput;
                    }

                    //Draw each converter line
                    var labelStyle = isValid ? Resources.DefaultLabelStyle : Resources.ErrorLabelStyle;
                    Rect labelRect, addBtnRect, removeBtnRect;
                    if ( i == converters.arraySize - 1 )
                    {
                        var rects = GUIUtils.GetHorizontalRects( position, 5, new (), new (30), new (30) );
                        labelRect = rects.Item1;
                        addBtnRect = rects.Item2;
                        removeBtnRect = rects.Item3;
                    }
                    else
                    {
                        var rects = GUIUtils.GetHorizontalRects( position, 5, new (), new (30) );
                        labelRect     = rects.Item1;
                        addBtnRect = default;
                        removeBtnRect = rects.Item2;
                    }

                    GUI.Label( labelRect, converterName, labelStyle );
                    if ( addBtnRect != default && GUI.Button( addBtnRect, Resources.AddButtonContent ) )
                    {
                        AddConverter( converters );
                        break;
                    }
                    if( GUI.Button( removeBtnRect, Resources.RemoveBtnContent ) )
                    {
                        RemoveConverter( converters, i );
                        break;
                    }

                    position = position.Translate( new Vector2( 0, EditorGUIUtility.singleLineHeight ) );
                }
            }

            EditorGUI.EndProperty();
        }

        private static void RemoveConverter(SerializedProperty converters, Int32 i )
        {
            converters.DeleteArrayElementAtIndex( i );
            converters.serializedObject.ApplyModifiedProperties();
        }

        private void AddConverter( SerializedProperty converters )
        {
            var currentOutputType = GetCurrentOutputType(converters);
            var compatibleTypes   = GetCompartibleConverters( currentOutputType );

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
                    converters.serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
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
                return ConverterBase.GetConverterTypeInfo( converter ).output;
            }

            return null;
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
            return EditorGUIUtility.singleLineHeight 
                   * Math.Max( property.FindPropertyRelative( nameof(BinderBase.ConvertersList.Converters) ).arraySize, 1 );
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

