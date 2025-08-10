using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
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
        /// Sometimes embedded property drawers want to know binding context.
        /// </summary>
        public static DataBinding DataBinding { get; private set; } 

        protected override string GetMainString( SerializedProperty property )
        {
            var (binding, bindingHost) = BindingEditorUtils.GetBindingObject<DataBinding>( property );

            //Get main string for binding property.
            string mainTextStr;
            if ( Application.isPlaying )    //For runtime, binding itself provides all debug info
            {
                return binding.GetFullRuntimeInfo();
            }
            else            //A similar output for editor mode too (without runtime values)
            {
                var bindingSourceInfo = BindingEditorUtils.GetBindingSourceInfo( binding, bindingHost );
                var bindingDirection = BindingEditorUtils.GetBindingDirection( binding );
                var bindingTargetInfo = BindingEditorUtils.GetBindingTargetInfo( binding, fieldInfo, bindingHost );
                mainTextStr = $"{bindingSourceInfo} {bindingDirection} {bindingTargetInfo}";
                return mainTextStr;
            }
        }

        protected override void DrawPathField(   Rect position, SerializedProperty property, BindingBase binding, UnityEngine.Object host )
        {
            var pathProp = property.FindPropertyRelative( nameof(BindingBase.Path) );

            var (sourceType, _) = BindingUtils.GetSourceTypeAndObject( binding, host );
            if ( sourceType != null )
            {
                position = EditorGUI.PrefixLabel( position, GUIUtility.GetControlID( FocusType.Keyboard ), new GUIContent( pathProp.displayName ) ) ;
                var pathString  = pathProp.stringValue;
                var parser      = new PathParser( sourceType, pathString );
                var tokens      = parser.Tokens;
                var isValidPath = tokens.Count > 0 && tokens.All( p => p.PropertyType != null );

                GUI.SetNextControlName( "PathTextField" );
                var isFocused = GUI.GetNameOfFocusedControl() == "PathTextField";
                String displayPath = pathString;
                if ( isFocused )
                {
                    if ( Event.current.type == EventType.KeyDown )
                    {
                        if ( Event.current.keyCode == KeyCode.DownArrow ) //Show suggestions list on arrow down
                        {
                            Event.current.Use();  //Prevent further processing of this event
                            if ( EditorGUIUtils.TryGetCursorPositionInTextField( out var cursorPosition ) &&
                                 parser.TryGetTokenAtPosition( cursorPosition, out var token )            &&
                                 token.SourceType != null )
                            {
                                //Show suggestion for given source type in the token
                                var properties = token
                                                .SourceType
                                                .GetProperties( BindingFlags.Instance | BindingFlags.Public ) //todo also support methods for CallBinding
                                                .Where( p => p.CanRead )
                                                .ToArray();
                                var menu             = new GenericMenu();
                                foreach ( var prop in properties )
                                {
                                    var    isBaseProp      = prop.DeclaringType != token.SourceType;
                                    string propDisplayName = isBaseProp ? $"Base/{prop.Name}" : prop.Name;
                                    string propName        = prop.Name;
                                    menu.AddItem( new GUIContent( propDisplayName ), propName == token.Token, ( ) =>
                                    {
                                        token.Token          = propName;
                                        pathProp.stringValue = tokens.Select( t => t.Token ).JoinToString( "." );
                                        pathProp.serializedObject.ApplyModifiedProperties();
                                    } );
                                }

                                menu.DropDown( position );
                            }
                        }
                        else if ( Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.Escape ) 
                        {
                            Event.current.Use();  
                            GUI.FocusControl( null );
                        }
                    }
                }
                else
                {
                    //When not focues, show property type also
                    if( tokens.Last().PropertyType != null )
                        displayPath += $" ({tokens.Last().PropertyType.Name})";
                } 

                var newValue = GUI.TextField( position, displayPath, isValidPath ? Resources.TextField : Resources.ErrorTextField );
                if ( isFocused )
                    pathProp.stringValue = newValue;

            }
            else
            {
                position = EditorGUI.PrefixLabel( position, GUIUtility.GetControlID( FocusType.Passive ), new GUIContent( pathProp.displayName ) ) ;
                GUI.Label( position, "Source not set", Resources.ErrorLabel );
            }
        }

        protected override void DrawAdditionalFields( Rect position, SerializedProperty property, BindingBase binding, UnityEngine.Object host )
        {
            base.DrawAdditionalFields( position, property, binding, host );

            position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
            var updateProp = property.FindPropertyRelative( nameof(UIBindings.DataBinding.Update) );
            EditorGUI.PropertyField( position, updateProp );

            position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
            var dataBinding = (DataBinding)binding;
            DataBinding = dataBinding;
            DrawConvertersField( position, property, dataBinding, host );
        }

        private void DrawConvertersField( Rect position, SerializedProperty bindingProp, DataBinding binding, UnityEngine.Object host )
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
                var isValid = BindingEditorUtils.IsConvertersValid( binding, host );
                GUI.Label( mainContentPosition, new GUIContent( $"Count {convertersProp.arraySize}", tooltip: isValid.ErrorMessage), isValid ? Resources.DefaultLabel : Resources.ErrorLabel );
                position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
                _convertersFieldHeight += Resources.LineHeightWithMargin;

                if ( convertersProp.isExpanded )
                {
                    var sourcePropertyType = BindingEditorUtils.GetSourceProperty( binding, host )?.PropertyType;
                    var sourceType         = PropertyAdapter.GetAdaptedType( sourcePropertyType );
  
                    //Draw every converter
                    Type prevType = sourceType;
                    using ( new EditorGUI.IndentLevelScope(1) )
                    {
                        for ( int i = 0; i < convertersProp.arraySize; i++ )
                        {
                            var converterHeight = DrawConverterField( ref position, i, convertersProp, prevType, binding, host );
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
                var rects = EditorGUIUtils.GetHorizontalRects( mainContentPosition, 2, 0, 20 );
                GUI.Label( rects.Item1, "No converters" );
                if ( GUI.Button( rects.Item2, Resources.AddButtonContent ) )
                    AppendConverter( binding, host, convertersProp );
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

        private static Type GetConverterTypeToAppend( DataBinding binding, UnityEngine.Object host, IReadOnlyList<ConverterBase> converters )
        {
            if ( converters.Count > 0 )
                return GetLastConverterOutputType( converters );
            return PropertyAdapter.GetAdaptedType( BindingEditorUtils.GetSourceProperty( binding, host )?.PropertyType );
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

        private static void AppendConverter( DataBinding binding, UnityEngine.Object host, SerializedProperty convertersProp )
        {
            var converters = binding.Converters;
            var convertFromType = GetConverterTypeToAppend( binding, host, converters);
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

        private static Single DrawConverterField( ref Rect position, int index, SerializedProperty convertersProp, Type prevType, DataBinding binding, UnityEngine.Object host ) 
        {
            var isLastConverter = index == convertersProp.arraySize - 1;
            var converterProp = convertersProp.GetArrayElementAtIndex( index );

            //Draw converter title 
            Rect titleRect, appendBtnRect = default, removeBtnRect;
            if ( isLastConverter )
                (titleRect, appendBtnRect, removeBtnRect) = EditorGUIUtils.GetHorizontalRects( position, 3, 0, 20, 20 );
            else
                (titleRect, removeBtnRect) = EditorGUIUtils.GetHorizontalRects( position, 3, 0, 20 );
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
            var isValid = BindingEditorUtils.IsConverterValid( prevType, converter, binding.IsTwoWay );
            var infoStr = $"{typeInfo.input.Name} {direction} {typeInfo.output.Name}";
            var info = new GUIContent( infoStr, tooltip: !isValid ? isValid.ErrorMessage : null );
        
            EditorGUI.LabelField( position, new GUIContent(title), info, isValid ? Resources.DefaultLabel : Resources.ErrorLabel );
            if( isLastConverter && GUI.Button( appendBtnRect, Resources.AddButtonContent ) )
            {
                AppendConverter( binding, host, convertersProp );
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