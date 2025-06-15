using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UIBindings.Adapters;
using UIBindings.Converters;
using UIBindings.Editor.Utils;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Search;
using Object = System.Object;

namespace UIBindings.Editor
{
    [CustomPropertyDrawer( typeof(DataBinding), true )]
    public class DataBindingEditor : PropertyDrawer
    {
        /// <summary>
        /// Sometimes embedded property drawers want to know what is the type of source property.
        /// </summary>
        public static Type SourcePropertyType { get; private set; } 

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {
            using ( new EditorGUI.PropertyScope( position, label, property ) ) ;

            position.height = EditorGUIUtility.singleLineHeight;
            var labelRect = position;

            //Draw property label
            labelRect.width = EditorGUIUtility.labelWidth;
            property.isExpanded = EditorGUI.Foldout( labelRect, property.isExpanded, label, true );

            //Draw main content line = enabled toggle + main binding info label
            var mainLineContentPosition = position;
            mainLineContentPosition.xMin += EditorGUIUtility.labelWidth;
            var rects = GUIUtils.GetHorizontalRects( mainLineContentPosition, 2, 0, 20 );

            //Draw Enabled toggle
            var enabledProp = property.FindPropertyRelative( nameof(BindingBase.Enabled) );
            var isEnabled = enabledProp.boolValue;
            EditorGUI.PropertyField( rects.Item2, enabledProp, GUIContent.none );

            using ( new EditorGUI.DisabledGroupScope( !isEnabled ) )
            {
                //Draw main binding info label + folded content (if unfolded)
                var binding           = (DataBinding)fieldInfo.GetValue( property.serializedObject.targetObject );
                var (sourceType, sourceName)   = GetSourceType( binding );
                var sourceProperty    = GetSourceProperty( binding );
                var sourcePropType    = sourceProperty != null ? sourceProperty.PropertyType : null;
                var sourceTypeName    = sourcePropType != null ? sourcePropType.Name : "null";
                var sourcePropName    = binding.Path;
                var sourceDisplayName = sourceType != null ? $"{sourceTypeName} {sourceName}.{sourcePropName}" : sourceName;
                Predicate<Type> isTypeSupported        = binding.IsCompatibleWith;
                var isTwoWayBinding   = binding.IsTwoWay;
                var sourceAdapterType = PropertyAdapter.GetAdaptedType( sourcePropType );
                var validationReport  = String.Empty;
                var isValid           = !isEnabled || ( IsSourceValid( binding, sourceProperty, out validationReport ) && IsSourceTargetTypesCompatible( sourceAdapterType, isTypeSupported, isTwoWayBinding, binding.Converters, out validationReport ));

                string mainTextStr;
                if ( Application.isPlaying )
                {
                    var propValue = (binding.SourceObject != null && sourceProperty != null) ? sourceProperty.GetValue( binding.SourceObject ).ToString() : "?";
                    mainTextStr = $"{binding.GetBindingSourceInfo()} <{propValue}> {binding.GetBindingDirection()} {binding.GetBindingTargetInfo()} <{binding.GetBindingState()}>";
                    isValid  = isValid && (binding.IsRuntimeValid || !binding.Enabled);
                }
                else
                {
                    var convertersCount = binding.Converters.Count > 0 ? $"[{binding.Converters.Count}]" : String.Empty;
                    var arrowStr        = isTwoWayBinding ? $"<-{convertersCount}->" : $"-{convertersCount}->";
                    mainTextStr = $"{sourceDisplayName} {arrowStr} {fieldInfo.Name}";
                }

                //Draw main binding info label itself
                GUI.Label( rects.Item1, new GUIContent(mainTextStr, tooltip: validationReport), isValid ? Resources.DefaultLabel : Resources.ErrorLabel );

                //Draw expanded content
                if ( property.isExpanded )
                {
                    SourcePropertyType = sourcePropType; //Hack, set static property for embedded drawers

                    using ( new EditorGUI.IndentLevelScope( 1 ) )
                    {
                        position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
                        //var sourceProp = property.FindPropertyRelative( nameof(Binding.Source) );
                        //EditorGUI.PropertyField( position, sourceProperty );
                        DrawSourceField( position, property );

                        position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
                        var pathProp   = property.FindPropertyRelative( nameof(BindingBase.Path) );
                        //EditorGUI.PropertyField( position, pathProperty );
                        DrawPathField( position, pathProp, binding );

                        position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
                        var updateProp = property.FindPropertyRelative( nameof(DataBinding.Update) );
                        EditorGUI.PropertyField( position, updateProp );

                        using ( new EditorGUI.DisabledScope( sourceProperty == null ) )
                        {
                            position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
                            var convertersProp = property.FindPropertyRelative( DataBinding.ConvertersPropertyName );
                            //EditorGUI.PropertyField( position, convertersProperty );
                            DrawConvertersField( position, convertersProp, binding, sourceAdapterType, isTypeSupported );
                        }
                    }
                }
            }
        }

        private void DrawSourceField(  Rect position, SerializedProperty property )
        {
            EditorGUI.BeginChangeCheck();

            var sourceObjectProp = property.FindPropertyRelative( nameof(BindingBase.Source) );
            var sourceTypeStrProp = property.FindPropertyRelative( nameof(BindingBase.SourceType) );
            var bindTypeProp = property.FindPropertyRelative( nameof(BindingBase.BindToType) );

            position = EditorGUI.PrefixLabel( position, new GUIContent( bindTypeProp.boolValue ? "Source type" : "Source reference" ));

            var rects = GUIUtils.GetHorizontalRects( position, 1, 0, 50 );

            //Button "object reference" or "type" for source
            var bindType = bindTypeProp.boolValue;
            var nextModeLabel = bindType ? new GUIContent("Ref", "Click to switch to Unity object source mode") : new GUIContent("Type", "Click to switch to C# any type source mode");
            if ( GUI.Button( rects.Item2, nextModeLabel ) )
            {
                bindType = !bindType;
                bindTypeProp.boolValue = bindType;
            }

            if ( bindType )         //Type source
            {
                var currentType = sourceTypeStrProp.stringValue;
                //hack around Search window
                if( _currentSelectedTypeFromSearchWindow != null && currentType != _currentSelectedTypeFromSearchWindow )
                {
                    currentType = _currentSelectedTypeFromSearchWindow;
                    sourceTypeStrProp.stringValue = currentType;
                    _currentSelectedTypeFromSearchWindow = null; //Reset after use
                    sourceTypeStrProp.serializedObject.ApplyModifiedProperties();
                }
                var (content, style) = GetSourceTypeFieldContent( currentType );
                if ( GUI.Button( rects.Item1, content, style ) )
                {
                    //Show search service window to select type
                    var provider2 = new TypeSearchProvider2( typeof(System.Object) );
                    var context = SearchService.CreateContext(provider2, "type:");
                    var state = new SearchViewState(context)
                                {
                                        title               = "Type",
                                        queryBuilderEnabled = true,
                                        hideTabs            = true,
                                        selectHandler       = (a, b) => _currentSelectedTypeFromSearchWindow = ((Type)a.data).AssemblyQualifiedName,
                                        flags = SearchViewFlags.TableView                |
                                                SearchViewFlags.DisableBuilderModeToggle |
                                                SearchViewFlags.DisableInspectorPreview
                                };
                    var view = SearchService.ShowPicker(state);

                    /*
                    var typeSearchProvider = SearchService.GetProvider( TypeSearchProvider.Id );
                    var searchContext = SearchService.CreateContext(typeSearchProvider);
                    //var searchPosition  = EditorGUIUtility.GUIToScreenPoint( Event.current.mousePosition );
                    var viewArgs = new SearchViewState( searchContext, SearchViewFlags.Borderless | SearchViewFlags.CompactView)
                                   {       
                                           selectHandler = (si, b) =>
                                           {
                                               Debug.Log( Event.current.type );
                                               _currentSelectedTypeFromSearchWindow = ((Type)si.data).AssemblyQualifiedName;
                                               _currentTypeSearchWindow.Close();
                                           },
                                            trackingHandler = si =>
                                            {
                                                _currentSelectedTypeFromSearchWindow = ((Type)si.data).AssemblyQualifiedName;
                                            },
                                            //position = new Rect( searchPosition, new Vector2( 300, 400 ) )
                                   };
                    _currentTypeSearchWindow = SearchService.ShowWindow(  viewArgs );
                    */
                }
            }
            else
            {
                var oldSource = sourceObjectProp.objectReferenceValue;
                var controlRect = rects.Item1;
                controlRect.xMin -= EditorGUI.indentLevel * 15f;
                EditorGUI.PropertyField( controlRect, sourceObjectProp, GUIContent.none );
                if ( EditorGUI.EndChangeCheck() )
                {
                    //Process some automatization for first time object selection
                    if( !oldSource && sourceObjectProp.objectReferenceValue )
                    {
                        //Autosearch for monobeh components or components with INotifyPropertyChanged
                        if( sourceObjectProp.objectReferenceValue is GameObject sourceGO )
                        {
                            var components = sourceGO.GetComponents<MonoBehaviour>();
                            if( components.Length > 0 )
                                sourceObjectProp.objectReferenceValue = components[0];
                        
                            foreach (var component in components)
                                if ( component is INotifyPropertyChanged )
                                {
                                    sourceObjectProp.objectReferenceValue = component;
                                    break;
                                }

                            sourceObjectProp.serializedObject.ApplyModifiedProperties();
                        } 
                    }
                }
            }

            (GUIContent, GUIStyle) GetSourceTypeFieldContent( String typeString )
            {
                if ( String.IsNullOrEmpty( typeString ) )
                    return (new GUIContent( "click to select source type from Search window" ), Resources.PlaceholderTextField);

                var type = Type.GetType( typeString );
                if( type != null
)                {
                    var typeName = type.Name;
                    if ( type.IsGenericType )
                        typeName = $"{typeName}<{string.Join( ", ", type.GetGenericArguments().Select( t => t.Name ) )}>";
                    return new (new GUIContent( typeName, tooltip: typeString ), Resources.TextField);
                }
                else
                {
                    return new (new GUIContent( $"Type '{typeString}' not found. Click to select correct type from Search window", tooltip: typeString ), Resources.ErrorTextField);
                }  
            }
        }

        private void DrawPathField(  Rect position, SerializedProperty pathProp, BindingBase binding )
        {
            position = EditorGUI.PrefixLabel( position, new GUIContent( pathProp.displayName ) );

            var (sourceType, _) = GetSourceType( binding );
            if ( sourceType != null )
            {
                var propInfo = GetSourceProperty( binding );

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
                    GUI.Label( position, "(Source not set)", Resources.DisabledTextField );
                }
            }
        }


        private void DrawConvertersField( Rect position, SerializedProperty convertersProp, DataBinding binding, Type sourceType, Predicate<Type> targetType )
        {
            _convertersFieldHeight = 0;
            var labelRect = position;
            labelRect.width = EditorGUIUtility.labelWidth;
            var mainContentPosition = position;
            mainContentPosition.xMin += EditorGUIUtility.labelWidth;

            convertersProp = convertersProp.FindPropertyRelative( nameof(DataBinding.ConvertersList.Converters) );
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
                            var converterHeight = DrawConverterField( ref position, i, convertersProp, prevType, binding );
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
                    AppendConverter( binding, convertersProp );
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

        private static (Type, String) GetSourceType(BindingBase binding )
        {
            if ( binding.BindToType )
            {
                var bindType = Type.GetType( binding.SourceType, false );
                return (bindType, bindType != null ? bindType.Name : "source type missed");
            }
            else if ( binding.Source )
                return (binding.Source.GetType(), $"{binding.Source.name}");

            return (null, "source object missed");
        }

        public static PropertyInfo GetSourceProperty( BindingBase binding )
        {
            var (sourceType, _) = GetSourceType( binding );
            if ( sourceType == null )
                return null;

            var propertyPath = binding.Path;
            if ( string.IsNullOrEmpty( propertyPath ) )
                return null;

            var sourceProperty = sourceType.GetProperty( propertyPath );
            return sourceProperty;
        }

        public static Type GetSourcePropertyType( BindingBase binding )
        {
            var sourceProperty = GetSourceProperty( binding );
            return sourceProperty != null ? sourceProperty.PropertyType : null;
        }

        private static bool IsSourceValid( DataBinding binding, PropertyInfo sourceProperty, out string report )
        {
            report = String.Empty;

            if ( !binding.BindToType && !binding.Source )
            {
                report = "Source is not set";
                return false;
            }

            if ( binding.BindToType )
            {
                if ( String.IsNullOrEmpty( binding.SourceType ) )
                {
                    report = "Source type is not set";
                    return false;
                }
                else if( Type.GetType( binding.SourceType, throwOnError: false ) == null )
                {
                    report = "Source type is not found.";
                    return false;
                } 
                else if( Application.isPlaying && binding.SourceObject == null )
                {
                    report = "Source object is not set in runtime";
                    return false;
                } 
            }

            if ( sourceProperty == null )
            {
                report = $"Source property '{binding.Path}' is not found on {binding.Source.GetType().Name}";
                return false;
            }

            if( !sourceProperty.CanWrite && binding.IsTwoWay )
            {
                report = $"Source property '{binding.Path}' is read-only, but binding is two-way.";
                return false;
            }

            if ( !sourceProperty.CanRead )
            {
                report = $"Write only source property '{binding.Path}' is not supported.";
                return false;   
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

        private ISearchView _currentTypeSearchWindow;
        private String      _currentSelectedTypeFromSearchWindow;  //Store here because if stored from Search window callback, it cause ugly repaint of Search window

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

        private static Type GetConverterTypeToAppend( BindingBase binding, IReadOnlyList<ConverterBase> converters )
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

        private static void AppendConverter( DataBinding binding, SerializedProperty convertersProp )
        {
            var converters = binding.Converters;
            var convertFromType = GetConverterTypeToAppend( binding, converters );
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

        private static Single DrawConverterField( ref Rect position, int index, SerializedProperty convertersProp, Type prevType, DataBinding binding) 
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
                AppendConverter( binding, convertersProp );
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
                                                                hover = { textColor = Color.gray },
                                                                focused = { textColor = Color.gray },
                                                        };
            public static GUIStyle ErrorTextField => new GUIStyle( TextField )
            {
                normal = { textColor = Color.red },
                hover = { textColor = Color.red },
                focused = { textColor = Color.red }
            };

            public static GUIStyle PlaceholderTextField => new GUIStyle( TextField )
            { 
                fontStyle = FontStyle.Italic,
                normal = { textColor = Color.gray },
                hover = { textColor = Color.gray },
                focused = { textColor = Color.gray }
            };

            public static readonly float LineHeightWithMargin = EditorGUIUtility.singleLineHeight + 2;

            public static readonly GUIContent AddButtonContent = new GUIContent( "+", "Add compatible converter" );
            public static readonly GUIContent RemoveBtnContent = new GUIContent( "-", "Remove converter" );
        }
    }
}