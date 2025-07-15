using System;
using System.Linq;
using System.Reflection;
using UIBindings.Runtime;
using UIBindings.Runtime.Utils;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Search;
using Object = UnityEngine.Object;

namespace UIBindings.Editor.Utils
{
    /// <summary>
    /// Draw base binding controls
    /// </summary>
    [CustomPropertyDrawer(typeof(BindingBase))]
    public abstract class BindingBaseEditor : PropertyDrawer
    {
        /// <summary>
        /// Sometimes embedded property drawers want to know what is the type of source property.
        /// </summary>
        //public static Type SourcePropertyType { get; private set; } 

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {
            using ( new EditorGUI.PropertyScope( position, label, property ) ) ;

            position.height = EditorGUIUtility.singleLineHeight;
            var labelRect = position;

            //Draw property label with foldout
            labelRect.width     = EditorGUIUtility.labelWidth;
            property.isExpanded = EditorGUI.Foldout( labelRect, property.isExpanded, label, true );

            //Draw main content line = enabled toggle + main binding info label
            var mainLineContentPosition = position;
            mainLineContentPosition.xMin += EditorGUIUtility.labelWidth;
            var rects = GUIUtils.GetHorizontalRects( mainLineContentPosition, 2, 0, 20 );

            //Draw Enabled toggle
            var enabledProp = property.FindPropertyRelative( nameof(BindingBase.Enabled) );
            var isEnabled   = enabledProp.boolValue;
            EditorGUI.PropertyField( rects.Item2, enabledProp, GUIContent.none );

            using ( new EditorGUI.DisabledGroupScope( !isEnabled ) )
            {
                //Draw main binding info label itself
                var (bindingObject, bindingHost) = BindingEditorUtils.GetBindingObject<BindingBase>( property );
                var mainStr = GetMainString( property );
                var isValid = isEnabled ? BindingEditorUtils.IsBindingValid( bindingObject, bindingHost ) : ValidationResult.Valid;
                GUI.Label( rects.Item1, new GUIContent( mainStr, tooltip: isValid.ErrorMessage ),
                        isValid ? Resources.DefaultLabel : Resources.ErrorLabel );

                //Draw binding fields and additional controls
                if ( property.isExpanded )
                {
                    using ( new EditorGUI.IndentLevelScope() )
                    {
                        //SourcePropertyType = sourcePropType; //Hack, set static property for embedded drawers
                        position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) ); 
                        DrawSourceField( position, property, bindingObject, bindingHost );
                        position = position.Translate( new Vector2( 0, Resources.LineHeightWithMargin ) );
                        DrawPathField( position, property, bindingObject, bindingHost );
                        DrawAdditionalFields( position, property, bindingObject, bindingHost );
                    }
                }
            }
        }

        private void DrawSourceField(  Rect position, SerializedProperty bindingProp, BindingBase binding, Object host )
        {
            var sourceObjectProp = bindingProp.FindPropertyRelative( nameof(BindingBase.Source) );
            var sourceTypeStrProp = bindingProp.FindPropertyRelative( nameof(BindingBase.SourceType) );
            var bindTypeProp = bindingProp.FindPropertyRelative( nameof(BindingBase.BindToType) );

            position = EditorGUI.PrefixLabel( position, new GUIContent( bindTypeProp.boolValue ? "Source type" : "Source reference" ));

            using var zeroIndentScope = new EditorGUIUtils.ZeroLevelScope(  ); //Indent mess with some fields without labels

            var (sourceFieldRect, sourceTypeBtnRect) = GUIUtils.GetHorizontalRects( position, 2, 0, 50 );
            var bindType = bindTypeProp.boolValue;
            if ( bindType )         //Type source, use sourceTypeStrProp
            {
                var currentType = sourceTypeStrProp.stringValue;
                var (content, style) = GetSourceTypeFieldContent( currentType );
                if ( GUI.Button( sourceFieldRect, content, style ) )
                {
                    //Show search service window to select type
                    var provider2 = new TypeSearchProvider2( typeof(System.Object) );
                    var context = SearchService.CreateContext(provider2, "type:");
                    var state = new SearchViewState(context)
                                {
                                        title               = "Type",
                                        queryBuilderEnabled = true,
                                        hideTabs            = true,
                                        selectHandler       = (si, isCancelled) =>
                                        {
                                            if( isCancelled ) return;
                                            sourceTypeStrProp.stringValue = ((Type)si.data).AssemblyQualifiedName;
                                            sourceTypeStrProp.serializedObject.ApplyModifiedProperties();
                                        },
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
            else                    //Reference source, use sourceObjectProp
            {
                var oldSource = sourceObjectProp.objectReferenceValue;
                var parentSource = BindingUtils.GetEffectiveSource( binding, host );
                UnityEngine.Object newSource = null;
                if ( !parentSource )    //No parent mode, only raw local source
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField( sourceFieldRect, sourceObjectProp, GUIContent.none );
                    if ( EditorGUI.EndChangeCheck() )
                        newSource = sourceObjectProp.objectReferenceValue;
                }
                else if ( !oldSource && parentSource )       //Source not overriden, use parent source
                {
                    var (inheritedSourceRect, sourceFieldRect2) = GUIUtils.GetHorizontalRects( sourceFieldRect, 2, 15, 0 );
                    //Draw inherited source mark
                    GUI.Label( inheritedSourceRect, new GUIContent( "↑", tooltip: $"Used parent object {parentSource.name} as a Source. Select another source object to override." ) );  

                    //Draw object field with parent source 
                    EditorGUI.BeginChangeCheck();
                    var @override = EditorGUI.ObjectField( sourceFieldRect2, parentSource, typeof(UnityEngine.Object), true );
                    if ( EditorGUI.EndChangeCheck() )       //If changed, override parent source
                    {
                        newSource = @override;
                    }
                }
                else                //Source is overriden, draw object field with override mark, allow to clear override
                {
                    var (overridenSourceRect, sourceFieldRect2) = GUIUtils.GetHorizontalRects( sourceFieldRect, 2, 15, 0 );
                    //Draw override source mark
                    if ( GUI.Button( overridenSourceRect, new GUIContent("*", tooltip: $"Parent source overrided. Click to ping parent source object {parentSource.name}. Select None to remove source override and use parent source.") ) )  
                        EditorGUIUtility.PingObject( parentSource );

                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField( sourceFieldRect2, sourceObjectProp, GUIContent.none );
                    if ( EditorGUI.EndChangeCheck() )
                        newSource = sourceObjectProp.objectReferenceValue;
                }

                //Some automation to select proper component from GO reference
                if ( newSource is GameObject sourceGO )
                {
                    if ( sourceGO.TryGetComponent( out ViewModel vmComp ) )
                        sourceObjectProp.objectReferenceValue = vmComp;
                    else if ( sourceGO.TryGetComponent( out INotifyPropertyChanged inotifyPropertyChanged ) )
                        sourceObjectProp.objectReferenceValue = (Component)inotifyPropertyChanged;
                    else if ( sourceGO.TryGetComponent( out MonoBehaviour monoBehaviour ) )
                        sourceObjectProp.objectReferenceValue = monoBehaviour;
                    else if ( sourceGO.TryGetComponent( out Component justAnyComponent ) )
                        sourceObjectProp.objectReferenceValue = justAnyComponent;
                    else
                        sourceObjectProp.objectReferenceValue = sourceGO;       //If GO without any component?? use it

                    sourceObjectProp.serializedObject.ApplyModifiedProperties(); 
                }
            }

            //Button "object reference" or "type" for source
            var nextModeLabel = bindType ? new GUIContent("Ref", "Click to switch to Unity object source mode") : new GUIContent("Type", "Click to switch to C# any type source mode");
            if ( GUI.Button( sourceTypeBtnRect, nextModeLabel ) )
            {
                bindType               = !bindType;
                bindTypeProp.boolValue = bindType;
            }

            return;

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

        protected abstract string GetMainString( SerializedProperty property );

        protected abstract void DrawPathField(   Rect position, SerializedProperty property, BindingBase binding, Object host );

        protected virtual void DrawAdditionalFields(   Rect position, SerializedProperty property, BindingBase binding, Object host ) { }

        protected static class Resources
        {
            public static readonly GUIStyle DefaultLabel = new GUIStyle( GUI.skin.label );
            public static readonly GUIStyle ErrorLabel = new GUIStyle( DefaultLabel )
                                                         {
                                                                 normal  = { textColor  = Color.red },
                                                                 hover   = { textColor  = Color.red },
                                                                 focused =  { textColor = Color.red }
                                                         };
            public static GUIStyle TextField => new GUIStyle( GUI.skin.textField );

            public static GUIStyle DisabledTextField => new GUIStyle( TextField )
                                                        {
                                                                normal  = { textColor = Color.gray },
                                                                hover   = { textColor = Color.gray },
                                                                focused = { textColor = Color.gray },
                                                        };
            public static GUIStyle ErrorTextField => new GUIStyle( TextField )
                                                     {
                                                             normal  = { textColor = Color.red },
                                                             hover   = { textColor = Color.red },
                                                             focused = { textColor = Color.red }
                                                     };

            public static GUIStyle PlaceholderTextField => new GUIStyle( TextField )
                                                           { 
                                                                   fontStyle = FontStyle.Italic,
                                                                   normal    = { textColor = Color.gray },
                                                                   hover     = { textColor = Color.gray },
                                                                   focused   = { textColor = Color.gray }
                                                           };

            public static readonly float LineHeightWithMargin = EditorGUIUtility.singleLineHeight + 2;

            public static readonly GUIContent AddButtonContent = new GUIContent( "+", "Add compatible converter" );
            public static readonly GUIContent RemoveBtnContent = new GUIContent( "-", "Remove converter" );
        }


        
    }
}