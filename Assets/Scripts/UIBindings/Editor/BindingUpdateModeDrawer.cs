using UIBindings.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIBindings.Editor
{
    [CustomPropertyDrawer( typeof(DataBinding.UpdateMode) )]
    public class BindingUpdateModeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {
            // Draw the popup
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel( position, label );

            var modeProp = property.FindPropertyRelative( nameof(DataBinding.UpdateMode.Mode) );
            var delayProp = property.FindPropertyRelative( nameof(DataBinding.UpdateMode.Delay) );
            var scaledTimeProp = property.FindPropertyRelative( nameof(DataBinding.UpdateMode.ScaledTime) );

            using ( GUIUtils.ZeroIndent() )
            {
                if ( ((DataBinding.EUpdateMode)modeProp.intValue) != DataBinding.EUpdateMode.Manual )
                {
                    var rects = GUIUtils.GetHorizontalRects( position, 1, 0, 60, 40 );
                    EditorGUI.PropertyField( rects.Item1, modeProp, GUIContent.none );
                     var delayRects = GUIUtils.GetHorizontalRects( rects.Item2, 1, 20, 40 );
                     GUI.Label( delayRects.Item1, Resources.DelayLabel, Resources.RightAlignedLabel );
                    EditorGUI.PropertyField( delayRects.Item2, delayProp, GUIContent.none );
                     var unscaledRects = GUIUtils.GetHorizontalRects( rects.Item3,  1, 20, 20 );
                     GUI.Label( unscaledRects.Item1, Resources.UnscaledTimeLabel, Resources.RightAlignedLabel );
                     EditorGUI.PropertyField( unscaledRects.Item2, scaledTimeProp, GUIContent.none );
                }
                else
                {
                    EditorGUI.PropertyField( position, modeProp, GUIContent.none );
                }
            }

            EditorGUI.EndProperty();
        }

        private static class Resources
        {
            public static readonly GUIStyle   RightAlignedLabel = new ( GUI.skin.label ) { alignment = TextAnchor.MiddleRight, margin = new RectOffset(2, 1, 1, 1), padding = new RectOffset( 0, 0, 0, 0 ) };
            public static readonly GUIContent DelayLabel        = new ( "dT", "The delay in seconds between updates." );
            public static readonly GUIContent UnscaledTimeLabel = new ( "scl", "If true, the binding will use scaled time for the delay." );
        }
    }
}