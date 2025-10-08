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
            var timingProp = property.FindPropertyRelative( nameof(DataBinding.UpdateMode.Timing) );
            var delayProp = property.FindPropertyRelative( nameof(DataBinding.UpdateMode.Delay) );
            var scaledTimeProp = property.FindPropertyRelative( nameof(DataBinding.UpdateMode.ScaledTime) );

            if(delayProp.floatValue < 0) delayProp.floatValue = 0;

            using ( EditorGUIUtils.ZeroIndent() )
            {
                var showPollingSettings = !((DataBinding.EMode)modeProp.intValue     == DataBinding.EMode.OneTime ||
                                            (DataBinding.ETiming)timingProp.intValue == DataBinding.ETiming.Manual);
                var showScaledTimeCheckbox = delayProp.floatValue > 0;
                
                if ( showPollingSettings )
                {
                    var (modeRect, timingRect, pollingRect) = EditorGUIUtils.GetHorizontalRects( position, 1, 0, 0, position.width > 250 ? showScaledTimeCheckbox ? 120 : 60 : 1 );
                    EditorGUI.PropertyField( modeRect, modeProp, GUIContent.none );
                    EditorGUI.PropertyField( timingRect, timingProp, GUIContent.none );
                    if ( showScaledTimeCheckbox )
                    {
                        var (labelRect, delayRect, unscaledRect) = EditorGUIUtils.GetHorizontalRects( pollingRect, 1, 50, 0, 15 );
                        var labelContent = Resources.DelayAndScaledLabel;
                        labelContent.text = scaledTimeProp.boolValue ? "dT scl" : "dT unscl";
                        GUI.Label( labelRect, labelContent, Resources.RightAlignedLabel );
                        EditorGUI.PropertyField( delayRect, delayProp, GUIContent.none );
                        EditorGUI.PropertyField( unscaledRect, scaledTimeProp, GUIContent.none );
                    }
                    else
                    {
                        var (labelRect, delayRect) = EditorGUIUtils.GetHorizontalRects( pollingRect, 1, 20, 0 );
                        GUI.Label( labelRect, Resources.DelayLabel, Resources.RightAlignedLabel );
                        EditorGUI.PropertyField( delayRect, delayProp, GUIContent.none );
                    }
                }
                else
                {
                    var (modeRect, timingRect) = EditorGUIUtils.GetHorizontalRects( position, 1, 0, 0 );
                    EditorGUI.PropertyField( modeRect, modeProp, GUIContent.none );
                    EditorGUI.PropertyField( timingRect, timingProp, GUIContent.none );
                }
            }

            EditorGUI.EndProperty();
        }

        private static class Resources
        {
            public static readonly GUIStyle   RightAlignedLabel = new ( GUI.skin.label ) { alignment = TextAnchor.MiddleRight, margin = new RectOffset(2, 1, 1, 1), padding = new RectOffset( 0, 0, 0, 0 ) };
            public static readonly GUIContent DelayLabel        = new ( "dT", "The delay in seconds between updates." );
            public static readonly GUIContent DelayAndScaledLabel = new ( "dT unscl", "The delay in seconds between updates. Checkbox: if true, the binding will use scaled time for the delay." );
        }
    }
}