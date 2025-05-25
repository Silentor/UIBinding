using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
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
            property.isExpanded = EditorGUI.Foldout( labelRect, property.isExpanded, label, true );

            //Draw main content
            var mainLineContentPosition = position;
            mainLineContentPosition.xMin += EditorGUIUtility.labelWidth;
            var rects = GUIUtils.GetHorizontalRects( mainLineContentPosition, 2, 0, 20 );
            var enabledProp = property.FindPropertyRelative( nameof(Binding.Enabled) );

            var isEnabled = enabledProp.boolValue;
            var binding = (Binding)property.boxedValue;
            var bindingTypeInfo    = GetBindingTypeInfo( binding );
            var sourceType     = GetSourcePropertyType( binding );
            var sourceTypeName = sourceType            != null ? sourceType.Name : "null";
            var sourceName = binding.Source != null ? binding.Source.name : "";
            var sourcePropName = binding.Path.IsAssigned ? $".{binding.Path.Path}" : "";
            var sourceDisplayName = $"{sourceName}{sourcePropName} {sourceTypeName}";
            var targetType = bindingTypeInfo.valueType;
            var targetTypeName = targetType != null ? targetType.Name : "null";
            var isValid = !isEnabled || IsSourceTargetTypesCompatible( sourceType, targetType, binding.Converters );
            var arrowStr = bindingTypeInfo.templateType == typeof(BindingTwoWay<>) ? " <-> " : " -> ";
            var convertersCount = binding.Converters.Count > 0 ? $" ({binding.Converters.Count} cnvs)" : String.Empty;

            //Draw Enabled toggle
            EditorGUI.PropertyField( rects.Item2, enabledProp, GUIContent.none );

            using ( new EditorGUI.DisabledGroupScope( !isEnabled ) )
            {
                var mainText = $"{sourceDisplayName} {arrowStr} {targetTypeName} {convertersCount}";
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

                        position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
                        var convertersProperty = property.FindPropertyRelative( Binding.ConvertersPropertyName );
                        //EditorGUI.PropertyField( position, convertersProperty );
                        DrawConvertersField( position, convertersProperty, binding, sourceType, targetType );
                    }
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

        private void DrawConvertersField( Rect position, SerializedProperty convertersProp, Binding binding, Type sourceType, Type targetType )
        {
            _convertersFieldHeight = 0;
            var labelRect = position;
            labelRect.width = EditorGUIUtility.labelWidth;
            var mainContentPosition = position;
            mainContentPosition.xMin += EditorGUIUtility.labelWidth;

            convertersProp = convertersProp.FindPropertyRelative( nameof(Binding.ConvertersList.Converters) );
            if ( convertersProp.isArray && convertersProp.arraySize > 0 )
            {
                convertersProp.isExpanded = EditorGUI.Foldout( labelRect, convertersProp.isExpanded, convertersProp.displayName );
                var isValid = IsSourceTargetTypesCompatible( sourceType, targetType, binding.Converters );
                GUI.Label( mainContentPosition, $"Count {convertersProp.arraySize}", isValid ? Resources.DefaultLabel : Resources.ErrorLabel );
                position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
                _convertersFieldHeight += Resources.LineHeightWithMargin;

                if ( convertersProp.isExpanded )
                {
                    //Draw every converter
                    Type prevType = GetSourcePropertyType( binding );
                    using ( new EditorGUI.IndentLevelScope(1) )
                    {
                        for ( int i = 0; i < convertersProp.arraySize; i++ )
                        {
                            var converterHeight = DrawConverterField( ref position, i, convertersProp, prevType, binding );
                            _convertersFieldHeight += converterHeight;
                            prevType = ConverterBase.GetConverterTypeInfo( binding.Converters[ i ] ).output;
                            //position = position.Translate( new Vector2( 0, converterHeight ) );
                        }
                    }
                }
            }
            else
            {
                //No converters present, show message and button to add converter
                GUI.Label( labelRect, convertersProp.displayName);
                var rects = GUIUtils.GetHorizontalRects( mainContentPosition, 2, 0, 20 );
                GUI.Label( rects.Item1, "No converters" );
                if ( GUI.Button( rects.Item2, Resources.AddButtonContent ) )
                    AppendConverter( binding, convertersProp );
            }
        }

        public override Single GetPropertyHeight(SerializedProperty property, GUIContent label )
        {
            if ( property.isExpanded )
                return Resources.LineHeightWithMargin * 3 //Main line + Source + Path
                       + Math.Max( _convertersFieldHeight, Resources.LineHeightWithMargin);        
            else
                return Resources.LineHeightWithMargin;
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

        private static Boolean IsSourceTargetTypesCompatible(Type sourceType, Type targetType, IReadOnlyList<ConverterBase> converters )
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

#region Converters stuff

        private static readonly IReadOnlyList<ConverterTypeInfo> ConverterTypes = PrepareTypeCache();
        private float _convertersFieldHeight;

        private static IReadOnlyList<ConverterTypeInfo> PrepareTypeCache( )
        {
            var allConverters = TypeCache.GetTypesDerivedFrom<ConverterBase>();
            var result        = new List<ConverterTypeInfo>( allConverters.Count );
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

        private static Type GetConverterTypeToAppend( Binding binding, IReadOnlyList<ConverterBase> converters )
        {
            if ( converters.Count > 0 )
                return GetLastConverterOutputType( converters );
            return GetSourcePropertyType( binding );
        }

        private static Type GetLastConverterOutputType( IReadOnlyList<ConverterBase> converters )
        {
            if ( converters.Count > 0 )
            {
                var lastConverter = converters[^1];
                return ConverterBase.GetConverterTypeInfo( lastConverter ).output;
            }

            return null;
        }

        private static void AppendConverter( Binding binding, SerializedProperty convertersProp )
        {
            var converters = binding.Converters;
            var currentOutputType = GetConverterTypeToAppend( binding, converters );
            var compatibleTypes   = GetCompatibleConverters( currentOutputType );

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
                    var newIndex     = convertersProp.arraySize;
                    var newConverter = (ConverterBase)Activator.CreateInstance(typeInfo.TypeInfo.FullType);
                    newConverter.ReverseMode = typeInfo.IsReverseMode;
                    convertersProp.InsertArrayElementAtIndex(newIndex);
                    convertersProp.GetArrayElementAtIndex(newIndex).managedReferenceValue = newConverter;
                    convertersProp.serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }

        private static void RemoveConverter(SerializedProperty convertersProp, Int32 index )
        {
            if ( index < 0 || index >= convertersProp.arraySize )
            {
                Debug.LogError( $"[{nameof(BindingEditor)}] Invalid converter index {index} for removal." );
                return;
            }

            convertersProp.DeleteArrayElementAtIndex( index );
            convertersProp.serializedObject.ApplyModifiedProperties();
        }

        private static IReadOnlyList<ConverterType> GetCompatibleConverters ( Type sourceType )
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

        private static Single DrawConverterField( ref Rect position, int index, SerializedProperty convertersProp, Type prevType, Binding binding) 
        {
            var isLastConverter = index == convertersProp.arraySize - 1;
            var converterProp = convertersProp.GetArrayElementAtIndex( index );

            //Draw converter title 
            Rect titleRect, appendBtnRect = default, removeBtnRect;
            if ( isLastConverter )
                (titleRect, appendBtnRect, removeBtnRect) = GUIUtils.GetHorizontalRects( position, 3, 0, 20, 20 );
            else
                (titleRect, removeBtnRect) = GUIUtils.GetHorizontalRects( position, 3, 0, 20 );
            var converter = (ConverterBase)converterProp.boxedValue;
            if ( converter == null )
            {
                GUI.Label( titleRect, "(null)", Resources.ErrorLabel );
                position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
                return position.height;
            }

            var title =  converter.GetType().Name.Replace( "Converter", "" ) ;
            if( converter.ReverseMode )
                title += " (R)";

            var typeInfo  = ConverterBase.GetConverterTypeInfo( converter );
            var direction = $"{typeInfo.input.Name} -> {typeInfo.output.Name}";
        
            var isValid = !(prevType != null && typeInfo.input != prevType);
        
            EditorGUI.LabelField( position, title, direction, isValid ? Resources.DefaultLabel : Resources.ErrorLabel );
            if( isLastConverter && GUI.Button( appendBtnRect, Resources.AddButtonContent ) )
            {
                AppendConverter( binding, convertersProp );
            }

            if ( GUI.Button( removeBtnRect, Resources.RemoveBtnContent ) )
            {
                RemoveConverter( convertersProp, index );
            }

            position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
            var converterHeight = Resources.LineHeightWithMargin;

            //Draw converter properties (if any)
            var isChanged = false;
            var insideIterator = converterProp.Copy();
            var rootDepth = insideIterator.depth;
            if ( insideIterator.Next( true ) && insideIterator.depth > rootDepth )
            {
                using (new EditorGUI.IndentLevelScope(1 ))
                {
                    do
                    {
                        //Skip reserved properties
                        if( insideIterator.name == nameof(ConverterBase.ReverseMode) ) continue;

                        EditorGUI.BeginChangeCheck();
                        EditorGUI.PropertyField(position, insideIterator, true);
                        isChanged |= EditorGUI.EndChangeCheck();

                        var propHeight = EditorGUI.GetPropertyHeight( insideIterator, true );
                        converterHeight += propHeight;
                        position        =  position.Translate( new Vector2( 0, propHeight ) );
                                    
                    }
                    while( insideIterator.NextVisible( false ) && insideIterator.depth > rootDepth );
                }
            }

            return converterHeight;
        }

       
        public readonly struct ConverterTypeInfo
        {
            public readonly Type InputType;
            public readonly Type OutputType;
            public readonly Type TemplateType;
            public readonly Type FullType;

            public ConverterTypeInfo(Type inputType, Type outputType, Type templateType, Type fullType)
            {
                InputType    = inputType;
                OutputType   = outputType;
                TemplateType = templateType;
                FullType     = fullType;
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

#endregion

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

            public static readonly float LineHeightWithMargin = EditorGUIUtility.singleLineHeight + 2;

            public static readonly GUIContent AddButtonContent = new GUIContent( "+", "Add compatible converter" );
            public static readonly GUIContent RemoveBtnContent = new GUIContent( "-", "Remove converter" );
        }
    }
}