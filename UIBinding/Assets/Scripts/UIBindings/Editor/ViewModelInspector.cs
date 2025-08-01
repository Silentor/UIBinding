using System;
using System.Collections.Generic;
using System.Reflection;
using UIBindings.Editor.Utils;
using UIBindings.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Object = System.Object;

namespace UIBindings.Editor
{
    /// <summary>
    /// Display hierarchy of child ViewModels and Bindings
    /// </summary>
    [CustomEditor( typeof(ViewModel), true )]
    public class ViewModelInspector : UnityEditor.Editor
    {
        private ViewModel _target;

        private void OnEnable( )
        {
            _target = target as ViewModel;
        }

        public override void OnInspectorGUI( )
        {
            base.OnInspectorGUI();

            var vms = _target.GetComponentsInChildren<ViewModel>();
            if ( vms.Length > 0 )
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField( "ViewModels hierarchy:", EditorStyles.boldLabel );

                var vmTree = CreateTree( _target, vms );
                DrawTreeItem( vmTree, 0 );
                
            }
        }

        private void  DrawTreeItem( ViewModelTreeItem item, int depth )
        {
            if ( item == null )
                return;

            GUILayout.BeginHorizontal();
            GUILayout.Space( depth * 10 );
            if( GUILayout.Button( item.VM.name, GUI.skin.label ) )
                EditorGUIUtility.PingObject( item.VM );
            GUILayout.EndHorizontal();

            foreach ( var (host, binding, field) in item.Bindings )
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space( depth * 10 + 100);
                GUIContent bindingStr;
                GUIStyle bindingStyle = Resources.DefaultLabel;
                if ( Application.isPlaying )
                {
                    bindingStr = new GUIContent( binding.GetFullRuntimeInfo() );
                }
                else
                {
                    var bindingSourceInfo = BindingEditorUtils.GetBindingSourceInfo( binding, host );
                    var bindingDirection = BindingEditorUtils.GetBindingDirection( binding );
                    var bindingTargetInfo = BindingEditorUtils.GetBindingTargetInfo( binding, field, host );
                    var totalStr = $"{bindingSourceInfo} {bindingDirection} {bindingTargetInfo}";
                    var isBindingValid = BindingEditorUtils.IsBindingValid( binding, host );
                    bindingStr = new GUIContent(totalStr, tooltip: isBindingValid ? string.Empty : isBindingValid.ErrorMessage );
                    bindingStyle = isBindingValid ? Resources.DefaultLabel : Resources.ErrorLabel;
                }
                if ( GUILayout.Button( bindingStr, bindingStyle ) )
                {
                    EditorGUIUtility.PingObject( host );
                }
                GUILayout.EndHorizontal();
            }

            foreach ( var child in item.Childs )
            {
                DrawTreeItem( child, depth + 1 );
            }
        }

        /// <summary>
        /// Convert list of ViewModels to a tree structure of ViewModelTreeItem.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        private ViewModelTreeItem CreateTree( ViewModel root, IReadOnlyList<ViewModel> list )
        {
            var rootItem = new ViewModelTreeItem
                           {
                                   VM     = root,
                                   Depth  = 0,
                           };
            rootItem.Parent = rootItem;

            var itemMap = new Dictionary<ViewModel, ViewModelTreeItem>();
            itemMap[ root ] = rootItem;

            //Make parentr/childs items 
            foreach ( var vm in list )
            {
                if ( vm == root )
                    continue;

                //Make tree item for current item
                if( !itemMap.TryGetValue( vm, out var existingItem ) )
                {
                    existingItem = new ViewModelTreeItem
                                   {
                                           VM     = vm,
                                           Depth  = 0
                                   };
                    itemMap[ vm ] = existingItem;
                }

                if ( existingItem.Parent == null )
                {
                    var parent = vm.transform.parent?.GetComponentInParent<ViewModel>();
                    if ( parent != null )
                    {
                        if( !itemMap.TryGetValue( parent, out var parentItem ) )
                        {
                            parentItem = new ViewModelTreeItem
                                         {
                                                 Parent = null,
                                                 VM     = parent,
                                                 Childs = new List<ViewModelTreeItem>(),
                                         };
                            itemMap[ parent ] = parentItem;
                        }

                        existingItem.Parent = parentItem;
                        parentItem.Childs.Add( existingItem );
                    }
                }
            }

            //Calculate depth
            foreach ( var (key, value) in itemMap )
            {
                if( key == root )
                    continue;

                value.Depth = GetDepth( value, root, 0 );

                Int32 GetDepth(ViewModelTreeItem value, ViewModel viewModel, int depth )
                {
                    Assert.IsNotNull( value.Parent );   //Hanging item
                    Assert.IsTrue( depth < 100, "ViewModel depth is too deep, possible infinite loop" );

                    if (value.Parent == value || value.Parent.VM == viewModel)
                        return depth;

                    return GetDepth(value.Parent, viewModel, depth + 1);
                }
            }

            //Fill bindings for each VM
            var allBinders = root.transform.GetComponentsInChildren<BinderBase>();
            foreach ( var binder in allBinders )
            {
                var parentVm = binder.transform.GetComponentInParent<ViewModel>( true );
                if ( parentVm && itemMap.TryGetValue( parentVm, out var parentTreeItem ) )
                {
                    var bindings = GetBindings( binder );
                    parentTreeItem.Bindings.AddRange( bindings );
                }

                IReadOnlyList<(UnityEngine.Object, BindingBase, FieldInfo)> GetBindings(BinderBase binderBase )
                {
                    var bindingList = new List<(UnityEngine.Object, BindingBase, FieldInfo)>();
                    var fields = binderBase.GetType().GetFields( BindingFlags.Public | BindingFlags.Instance );
                    foreach ( var field in fields )
                    {
                        if ( field.FieldType.IsSubclassOf( typeof( BindingBase ) ) )
                        {
                            if ( field.GetValue( binderBase ) is BindingBase binding && binding.Enabled )
                            {
                                bindingList.Add( (binderBase, binding, field) );
                            }
                        }
                    }
                    return bindingList;
                }
            }

            return rootItem;
        }


        private class ViewModelTreeItem
        {
            public ViewModel VM;
            public ViewModelTreeItem Parent;
            public int Depth;
            public List<ViewModelTreeItem> Childs = new ();
            public List<(UnityEngine.Object, BindingBase, FieldInfo)> Bindings = new ();
        }

        protected static class Resources
        {
            public static readonly GUIStyle DefaultLabel = new GUIStyle( GUI.skin.label );

            public static readonly GUIStyle ErrorLabel = new GUIStyle( DefaultLabel )
                                                         {
                                                                 normal  = { textColor  = Color.red },
                                                                 hover   = { textColor  = Color.red },
                                                                 focused =  { textColor = Color.red }
                                                         };
        }
    }
}