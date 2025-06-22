using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UIBindings.Adapters;
using UIBindings.Converters;
using UIBindings.Editor.Utils;
using UIBindings.Runtime;
using UIBindings.Runtime.Utils;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Search;
using Object = System.Object;

namespace UIBindings.Editor
{
    [CustomPropertyDrawer( typeof(DataBinding), true )]
    public class DataBindingEditor : BindingBaseEditor
    {
        /// <summary>
        /// Sometimes embedded property drawers want to know what is the type of source property.
        /// </summary>
        public static Type SourcePropertyType { get; private set; } 

        protected override BindingMainString GetMainString( SerializedProperty property )
        {
            //Calculate if source valid and get report if not valid
            var binding           = GetBindingObject<DataBinding>( property );
            var (sourceObjType, sourceObject)   = GetSourceTypeAndObject( property );
            var sourceProperty    = GetSourceProperty( property );
            var sourcePropType    = sourceProperty?.PropertyType;
            Predicate<Type> isTypeSupported        = binding.IsCompatibleWith;
            var isTwoWayBinding   = binding.IsTwoWay;
            var sourceAdapterType = PropertyAdapter.GetAdaptedType( sourcePropType );
            var validationReport  = String.Empty;
            var isValid           = IsSourceValid( sourceObject, sourceObjType, sourceProperty, property, out validationReport ) && IsSourceTargetTypesCompatible( sourceAdapterType, isTypeSupported, isTwoWayBinding, binding.Converters, out validationReport );

            //Get main string for binding property. Also get values for runtime mode 
            string mainTextStr;
            if ( Application.isPlaying )
            {
                var propValueString = GetSourcePropertyValueString( binding.SourceObject, sourceProperty );
                mainTextStr = $"{binding.GetBindingSourceInfo()} <{propValueString}> {binding.GetBindingDirection()} {binding.GetBindingTargetInfo()} <{binding.GetBindingState()}>";
                isValid  = isValid && binding.IsRuntimeValid;
            }
            else            //A similar output for editor mode (without runtime values)
            {
                var sourcePropTypeName    = sourcePropType != null ? sourcePropType.GetPrettyName() : "?";
                var sourcePropPath    = !String.IsNullOrEmpty(binding.Path) ? binding.Path : "?";
                var sourceDisplayName = sourceObject ? $"{sourcePropTypeName} '{sourceObject.name}'.{sourcePropPath}" : sourceObjType != null ? $"{sourcePropTypeName} '{sourceObjType.Name}'.{sourcePropPath}" : "?";
                var convertersCount = binding.Converters.Count > 0 ? $"[{binding.Converters.Count}]" : String.Empty;
                var arrowStr        = isTwoWayBinding ? $"<-{convertersCount}->" : $"-{convertersCount}->";
                mainTextStr = $"{sourceDisplayName} {arrowStr} {fieldInfo.FieldType.GetPrettyName()} {fieldInfo.Name}";
            }

            return new BindingMainString()
                   {
                           MainText         = mainTextStr,
                           IsValid          = isValid,
                           ValidationReport = validationReport,
                   };
        }

        private static String GetSourcePropertyValueString( Object sourceObject, PropertyInfo sourceProperty )
        {
            var propValueString = "?";
            if ( sourceObject != null && sourceProperty != null )
            {
                try
                {
                    var propValue = sourceProperty.GetValue( sourceObject );
                    propValueString = propValue.ToString();
                }
                catch ( TargetInvocationException e )
                {
                    propValueString = e.InnerException.GetType().Name;
                }
                catch ( Exception e )
                {
                    propValueString = e.GetType().Name;
                }
            }

            return propValueString;
        }

        protected override void DrawPathField(  Rect position, SerializedProperty property )
        {
            var pathProp = property.FindPropertyRelative( nameof(BindingBase.Path) );

            var (sourceType, _) = GetSourceTypeAndObject( property );
            if ( sourceType != null )
            {
                position = EditorGUI.PrefixLabel( position, GUIUtility.GetControlID(FocusType.Keyboard), new GUIContent( pathProp.displayName )) ;
                var propInfo = GetSourceProperty( property );

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
                //using ( new EditorGUI.DisabledScope( true ) )
                {
                    EditorGUI.LabelField( position, new GUIContent(pathProp.displayName), new GUIContent("(Source not set)"), Resources.DisabledTextField );
                }
            }
        }

        protected override void DrawAdditionalFields(Rect position, SerializedProperty property )
        {
            base.DrawAdditionalFields( position, property );

            position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
            var updateProp = property.FindPropertyRelative( nameof(DataBinding.Update) );
            EditorGUI.PropertyField( position, updateProp );

            position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
            var dataBinding = GetBindingObject<DataBinding>( property );
            var sourcePropertyType = GetSourceProperty( property )?.PropertyType;
            var sourceType = PropertyAdapter.GetAdaptedType( sourcePropertyType );
            SourcePropertyType = sourceType;
            Predicate<Type> targetTypePredicate = dataBinding.IsCompatibleWith;
            DrawConvertersField( position, property, dataBinding, sourceType, targetTypePredicate );
        }

        private void DrawConvertersField( Rect position, SerializedProperty bindingProp, DataBinding binding, Type sourceType, Predicate<Type> targetType )
        {
            _convertersFieldHeight = 0;
            var labelRect = position;
            labelRect.width = EditorGUIUtility.labelWidth;
            var mainContentPosition = position;
            mainContentPosition.xMin += EditorGUIUtility.labelWidth;

            var convertersProp = bindingProp.FindPropertyRelative( DataBinding.ConvertersPropertyName );
            if ( convertersProp.isArray && convertersProp.arraySize > 0 )
            {
                convertersProp.isExpanded = EditorGUI.Foldout( labelRect, convertersProp.isExpanded, convertersProp.displayName );
                var isValid = IsSourceTargetTypesCompatible( sourceType, targetType, binding.IsTwoWay, binding.Converters, out var report );
                GUI.Label( mainContentPosition, new GUIContent( $"Count {convertersProp.arraySize}", tooltip: report), isValid ? Resources.DefaultLabel : Resources.ErrorLabel );
                position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
                _convertersFieldHeight += Resources.LineHeightWithMargin;

                if ( convertersProp.isExpanded )
                {
                    //Draw every converter
                    Type prevType = sourceType;
                    using ( new EditorGUI.IndentLevelScope(1) )
                    {
                        for ( int i = 0; i < convertersProp.arraySize; i++ )
                        {
                            var converterHeight = DrawConverterField( ref position, i, bindingProp, convertersProp, prevType, binding );
                            _convertersFieldHeight += converterHeight;
                            var converter = binding.Converters[ i ];
                            prevType = converter != null ? ConverterBase.GetConverterTypeInfo( converter ).output : null;
                            //position = position.Translate( new Vector2( 0, converterHeight ) );
                        }
                    }
                }
            }
            else
            {
                //No converters present, show message and button to add converter
                GUI.Label( EditorGUI.IndentedRect( labelRect ), convertersProp.displayName);
                var rects = GUIUtils.GetHorizontalRects( mainContentPosition, 2, 0, 20 );
                GUI.Label( rects.Item1, "No converters" );
                if ( GUI.Button( rects.Item2, Resources.AddButtonContent ) )
                    AppendConverter( binding, bindingProp, convertersProp );
            }
        }

        public override Single GetPropertyHeight(SerializedProperty property, GUIContent label )
        {
            if ( property.isExpanded )
                return Resources.LineHeightWithMargin * 4 //Main line + Source + Path
                       + Math.Max( _convertersFieldHeight, Resources.LineHeightWithMargin);        
            else
                return Resources.LineHeightWithMargin;
        }

        public static PropertyInfo GetSourceProperty( SerializedProperty bindingProp )
        {
            var (sourceType, _) = GetSourceTypeAndObject( bindingProp );
            if ( sourceType == null )
                return null;

            var propertyPath = bindingProp.FindPropertyRelative( nameof(BindingBase.Path) ).stringValue;
            if ( string.IsNullOrEmpty( propertyPath ) )
                return null;

            var sourceProperty = sourceType.GetProperty( propertyPath );
            return sourceProperty;
        }

        private static bool IsSourceValid( UnityEngine.Object sourceObject, Type sourceType, PropertyInfo sourceProperty, SerializedProperty bindingProp, out string report )
        {
            report = String.Empty;

            if ( bindingProp.FindPropertyRelative( nameof(BindingBase.BindToType) ).boolValue )
            {
                if( sourceType == null )
                {
                    report = "Source type is not set";
                    return false;
                }
            }
            else
            {
                if ( !sourceObject )
                {
                    report = "Source object is not set";
                    return false;
                }
            }

            var propPath = bindingProp.FindPropertyRelative( nameof(BindingBase.Path) ).stringValue;
            if ( sourceProperty == null )
            {
                if ( !String.IsNullOrEmpty( propPath ) )
                {
                    report = $"Property '{propPath}' is not found on source";
                    return false;
                }
                else
                {
                    report = "Source property is not set";
                    return false;
                }
            }
            else
            {
                if ( !sourceProperty.CanRead )
                {
                    report = $"Property '{propPath}' is not readable";
                    return false;   
                }
                // if( !sourceProperty.CanWrite && binding.IsTwoWay )
                // {
                //     report = $"Property '{propPath}' is read-only, but binding is two-way";
                //     return false;
                // }
            }
            
            return true;
        }

        private static Boolean IsSourceTargetTypesCompatible(Type sourcePropertyType, Predicate<Type> targetTypeCheck, Boolean isTwoWayBinding, IReadOnlyList<ConverterBase> converters, out string report )
        {
            report = String.Empty;

            if ( sourcePropertyType == null || targetTypeCheck == null )
            {
                report = "Source or target type is not defined";
                return false;
            }

            // If no converters, check direct assignability
            if ( converters.Count == 0 )
            {
                if ( targetTypeCheck( sourcePropertyType ) || ImplicitConversion.IsConversionSupported( sourcePropertyType, targetTypeCheck ) )
                    return true;
                else
                {
                    report = $"Source type {sourcePropertyType.Name} is not compatible with target type.";
                    return false;
                }
            }

            // Check the chain: sourceType -> [converter1] -> ... -> [converterN] -> targetType
            for (int i = 0; i < converters.Count; i++)
            {
                var converter     = converters[i];
                if ( converter == null )              //Something wrong with converter
                {
                    report = $"Converter at index {i} is null.";
                    return false;
                }

                if ( !IsConverterValid( sourcePropertyType, converter, isTwoWayBinding, out report ) )
                    return false;

                sourcePropertyType = converter.OutputType;
            }

            // After all converters, the result type must be assignable to the target type
            if ( targetTypeCheck(sourcePropertyType) || ImplicitConversion.IsConversionSupported( sourcePropertyType, targetTypeCheck ) )
                return true;
            else
            {
                report = $"Final last converter's type {sourcePropertyType.Name} is not compatible with target type.";
                return false;
            }
        }

        private static Boolean IsConverterValid(Type prevType, ConverterBase converter, bool isBindingTwoWay, out string report )
        {
            if( prevType == null )
            {
                report = "Previous type is null.";
                return false;
            }

            if( converter == null )
            {
                report = "Converter is null.";
                return false;
            }

            if ( isBindingTwoWay && !converter.IsTwoWay )
            {
                report = "Cannot use one-way converter for two-way binding.";
                return false;
            }

            var isAssignable = prevType == converter.InputType || ImplicitConversion.IsConversionSupported( prevType, converter.InputType );
            if ( !isAssignable )
            {
                report = $"Converter {converter.GetType().Name} input type {converter.InputType.Name} is not compatible with previous type {prevType.Name}.";
                return false;
            }

            report = null;
            return true;
        }

#region Converters stuff

        private static readonly IReadOnlyList<ConverterTypeInfo> AllConverterTypes = PrepareTypeCache();
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

        private static Type GetConverterTypeToAppend( SerializedProperty bindingProp, IReadOnlyList<ConverterBase> converters )
        {
            if ( converters.Count > 0 )
                return GetLastConverterOutputType( converters );
            return PropertyAdapter.GetAdaptedType( GetSourceProperty( bindingProp )?.PropertyType );
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

        private static void AppendConverter( DataBinding binding, SerializedProperty bindingProp, SerializedProperty convertersProp )
        {
            var converters = binding.Converters;
            var convertFromType = GetConverterTypeToAppend( bindingProp, converters );
            var compatibleTypes   = GetCompatibleConverters( convertFromType, binding.IsTwoWay );

            if( compatibleTypes.Count == 0 )
            {
                var menu = new GenericMenu();
                menu.AddDisabledItem( new GUIContent($"No compatible converters for type {convertFromType.Name}") );
                menu.ShowAsContext();
            }
            else
            {
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
        }

        private static void RemoveConverter(SerializedProperty convertersProp, Int32 index )
        {
            if ( index < 0 || index >= convertersProp.arraySize )
            {
                Debug.LogError( $"[{nameof(DataBindingEditor)}] Invalid converter index {index} for removal." );
                return;
            }

            convertersProp.DeleteArrayElementAtIndex( index );
            convertersProp.serializedObject.ApplyModifiedProperties();
        }

        private static IReadOnlyList<ConverterType> GetCompatibleConverters ( Type sourceType, bool isTwoWayBinding )
        {
            var result = new List<ConverterType>();
            foreach ( var converter in AllConverterTypes )
            {
                var isConverterTwoWay = converter.TemplateType == typeof(SimpleConverterTwoWayBase<,>);

                if( isTwoWayBinding && !isConverterTwoWay )
                    continue;                       //Skip one way converters in two way binding

                if ( converter.InputType == sourceType || ImplicitConversion.IsConversionSupported( sourceType, converter.InputType ))
                {
                    result.Add( new ConverterType( converter ) );       //Direct mode
                }
                else if ( isConverterTwoWay && (converter.OutputType == sourceType || ImplicitConversion.IsConversionSupported( sourceType, converter.OutputType )))
                {
                    result.Add( new ConverterType( converter ) { IsReverseMode = true } );  //Reverse mode
                }
            }

            return result;
        }

        private static Single DrawConverterField( ref Rect position, int index, SerializedProperty bindingProp, SerializedProperty convertersProp, Type prevType, DataBinding binding) 
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
                EditorGUI.LabelField( titleRect, $"Converter {index}", "(null)", Resources.ErrorLabel );
                if ( GUI.Button( removeBtnRect, Resources.RemoveBtnContent ) )
                {
                    RemoveConverter( convertersProp, index );
                    GUIUtility.ExitGUI();       //Exit immediately to avoid issues with modified converters list
                }

                position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
                return position.height;
            }

            var title =  converter.GetType().Name.Replace( "Converter", "" ) ;
            if( converter.ReverseMode )
                title += " (R)";

            var typeInfo  = ConverterBase.GetConverterTypeInfo( converter );
            var direction = converter.IsTwoWay ? "<->" : "->";
            var isValid = IsConverterValid( prevType, converter, binding.IsTwoWay, out var report );
            var infoStr = $"{typeInfo.input.Name} {direction} {typeInfo.output.Name}";
            var info = new GUIContent( infoStr, tooltip: !isValid ? report : null );
        
            EditorGUI.LabelField( position, new GUIContent(title), info, isValid ? Resources.DefaultLabel : Resources.ErrorLabel );
            if( isLastConverter && GUI.Button( appendBtnRect, Resources.AddButtonContent ) )
            {
                AppendConverter( binding, bindingProp, convertersProp );
            }

            if ( GUI.Button( removeBtnRect, Resources.RemoveBtnContent ) )
            {
                RemoveConverter( convertersProp, index );
                GUIUtility.ExitGUI();       //Exit immediately to avoid issues with modified converters list
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
    }
}