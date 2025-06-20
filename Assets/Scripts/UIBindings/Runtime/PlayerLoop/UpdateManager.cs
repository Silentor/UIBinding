using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace UIBindings.Runtime.PlayerLoop
{
    /// <summary>
    /// I dont bother unsubscribing from Player Loop on exit Play mode, just make sure to not subscribe twice on next Play mode start.
    /// But i'm clear all subscriptions on exiting Play mode, so no hanging references.
    /// </summary>
    public static class UpdateManager
    {
        public static void Register([NotNull] IUpdate update)
        {
            RegisterUpdate( update.DoUpdate);
        }

        public static void Register([NotNull] IBeforeLateUpdate update)
        {
            RegisterUpdate( update.DoBeforeLateUpdate);
        }

        public static void Register([NotNull] IAfterLateUpdate update)
        {
            RegisterUpdate( update.DoAfterLateUpdate);
        }

        public static void RegisterUpdate([NotNull] Action update, int order = 0 )
        {
            Register( ref _afterUpdateList, update, order );
        }

        public static void RegisterBeforeLateUpdate([NotNull] Action update, int order = 0 )
        {
            Register( ref _beforeLateUpdateList, update, order );
        }

        public static void RegisterAfterLateUpdate([NotNull] Action update, int order = 0 )
        {
            Register( ref _afterLateUpdateList, update, order );
        }

        public static void Unregister([NotNull] IUpdate update)
        {
            UnregisterUpdate( update.DoUpdate );
        }

        public static void Unregister([NotNull] IBeforeLateUpdate update)
        {
            UnregisterBeforeLateUpdate( update.DoBeforeLateUpdate );
        }

        public static void Unregister([NotNull] IAfterLateUpdate update)
        {
            UnregisterAfterLateUpdate( update.DoAfterLateUpdate );
        }

        public static void UnregisterUpdate([NotNull] Action update)
        {
            Unregister( ref _afterUpdateList, update );
        }

        public static void UnregisterBeforeLateUpdate([NotNull] Action update)
        {
            Unregister( ref _beforeLateUpdateList, update );
        }

        public static void UnregisterAfterLateUpdate([NotNull] Action update)
        {
            Unregister( ref _beforeLateUpdateList, update );
        }

        private static UpdateList _afterUpdateList      = new ( new List<UpdateItem>() );
        private static UpdateList _beforeLateUpdateList = new ( new List<UpdateItem>() );
        private static UpdateList _afterLateUpdateList  = new ( new List<UpdateItem>() );

        private static List<UpdateItem> _tempListOfAddedItems = new ();

        private static readonly ProfilerMarker AfterUpdateMarker      = new ( "UpdateManager.AfterUpdate" );
        private static readonly ProfilerMarker BeforeLateUpdateMarker = new ( "UpdateManager.BeforeLateUpdate" );
        private static readonly ProfilerMarker AfterLateUpdateMarker  = new ( "UpdateManager.AfterLateUpdate" );

        [RuntimeInitializeOnLoadMethod]
        private static void Init( )
        {
            var defaultSystems = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();

            var myBeforeLateUpdate = new PlayerLoopSystem
                                     {
                                             subSystemList  = null,
                                             updateDelegate = OnBeforeLateUpdate,
                                             type           = typeof(UpdateManagerBeforeLateUpdate)
                                     };

            var myAfterLateUpdate = new PlayerLoopSystem
                                    {
                                            subSystemList  = null,
                                            updateDelegate = OnAfterLateUpdate,
                                            type           = typeof(UpdateManagerAfterLateUpdate)
                                    };

            var myUpdateSystem = new PlayerLoopSystem
                                 {
                                         subSystemList  = null,
                                         updateDelegate = OnUpdate,
                                         type           = typeof(UpdateManagerUpdate)
                                 };

            AddInSystem( ref defaultSystems, typeof(Update.ScriptRunBehaviourUpdate), myUpdateSystem, 0 );
            AddBeforeSystem( ref defaultSystems, typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate), myBeforeLateUpdate );
            AddAfterSystem( ref defaultSystems, typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate), myAfterLateUpdate );
            UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop( defaultSystems );

            static bool AddInSystem( ref PlayerLoopSystem loopSystem, Type parentSystem, PlayerLoopSystem systemToAdd, int index )
            {
                //Insert here
                if ( loopSystem.type == parentSystem )
                {
                    //Do not duplicate add
                    if ( loopSystem.subSystemList != null )
                    {
                        foreach ( var subs in loopSystem.subSystemList )
                        {
                            if ( subs.type == systemToAdd.type )
                                return true; //Already added
                        }
                    }

                    var newSubSystemList = loopSystem.subSystemList != null
                            ? new List<PlayerLoopSystem>( loopSystem.subSystemList )
                            : new List<PlayerLoopSystem>();
                    newSubSystemList.Insert( Math.Clamp( index, 0, newSubSystemList.Count ), systemToAdd );

                    loopSystem.subSystemList = newSubSystemList.ToArray();
                    return true;
                }
                else
                {
                    //Search in sub systems
                    if ( loopSystem.subSystemList != null )
                        for ( int i = 0; i < loopSystem.subSystemList.Length; i++ )
                        {
                            if ( AddInSystem( ref loopSystem.subSystemList[ i ], parentSystem, systemToAdd, index ) )
                                return true;
                        }
                }

                return false;
            }


            static bool AddBeforeSystem( ref PlayerLoopSystem loopSystem, Type siblingSystem, PlayerLoopSystem systemToAdd )
            {
                if ( loopSystem.subSystemList != null )
                {
                    var siblingSystemIndex = FindSystem( loopSystem.subSystemList, siblingSystem );
                    if ( siblingSystemIndex >= 0 )
                    {
                        if( FindSystem( loopSystem.subSystemList, systemToAdd.type ) >= 0 )
                            return true; //Already added

                        var newSubSystemList = new List<PlayerLoopSystem>( loopSystem.subSystemList );
                        newSubSystemList.Insert( siblingSystemIndex, systemToAdd );
                        loopSystem.subSystemList = newSubSystemList.ToArray();
                        return true;
                    }

                    for ( int i = 0; i < loopSystem.subSystemList.Length; i++ )
                    {
                        if ( AddBeforeSystem( ref loopSystem.subSystemList[ i ], siblingSystem, systemToAdd ) )
                            return true;
                    }
                }

                return false;
            }

            static bool AddAfterSystem( ref PlayerLoopSystem loopSystem, Type siblingSystem, PlayerLoopSystem systemToAdd )
            {
                if ( loopSystem.subSystemList != null )
                {
                    var siblingSystemIndex = FindSystem( loopSystem.subSystemList, siblingSystem );
                    if ( siblingSystemIndex >= 0 )
                    {
                        if( FindSystem( loopSystem.subSystemList, systemToAdd.type ) >= 0 )
                            return true; //Already added

                        var newSubSystemList = new List<PlayerLoopSystem>( loopSystem.subSystemList );
                        var newSystemIndex   = Math.Clamp( siblingSystemIndex + 1, 0, newSubSystemList.Count );
                        newSubSystemList.Insert( newSystemIndex, systemToAdd );
                        loopSystem.subSystemList = newSubSystemList.ToArray();
                        return true;
                    }

                    for ( int i = 0; i < loopSystem.subSystemList.Length; i++ )
                    {
                        if ( AddAfterSystem( ref loopSystem.subSystemList[ i ], siblingSystem, systemToAdd ) )
                            return true;
                    }
                }

                return false;
            }

            static int FindSystem( PlayerLoopSystem[] systems, Type typeToFind )
            {
                for ( int i = 0; i < systems.Length; i++ )
                {
                    if ( systems[ i ].type == typeToFind )
                        return i;
                }

                return -1;
            }
        }

        private static void Register( ref UpdateList updateList, [NotNull] Action action, int order = 0 )
        {
            if ( action == null ) throw new ArgumentNullException( nameof(action) );

            var newItem = new UpdateItem( action, order );
            if( updateList.Index >= 0 )
            {
                _tempListOfAddedItems.Add( newItem );//Main list is processing now, do not interfere with it
            }
            else
                AddSortedUpdatedItem( ref updateList, newItem );
        }

        private static void Unregister( ref UpdateList updateList, [NotNull] Action action )
        {
            if ( action == null ) throw new ArgumentNullException( nameof(action) );

            var indexOfExistingItem = updateList.Actions.FindIndex( ui => ui.Action == action );
            if( indexOfExistingItem < 0 )
                return; 

            //If update list processing right now and index of processing item needs to be fixed
            if( updateList.Index >= 0 && indexOfExistingItem <= updateList.Index )
            {
                updateList.Index--;
            }

            updateList.Actions.RemoveAt( indexOfExistingItem );
        } 

        private static void OnUpdate( )
        {
            AfterUpdateMarker.Begin( _afterUpdateList.Actions.Count );
            DoUpdate( ref _afterLateUpdateList );
            AfterUpdateMarker.End();
            PostprocessAddedItems( ref _afterLateUpdateList );
        }

        private static void OnBeforeLateUpdate( )
        {
            BeforeLateUpdateMarker.Begin( _beforeLateUpdateList.Actions.Count );
            DoUpdate( ref _beforeLateUpdateList );
            BeforeLateUpdateMarker.End();
            PostprocessAddedItems( ref _beforeLateUpdateList );
        }

        private static void OnAfterLateUpdate( )
        {
            AfterLateUpdateMarker.Begin( _afterLateUpdateList.Actions.Count );
            DoUpdate( ref _afterLateUpdateList );
            AfterLateUpdateMarker.End();
            PostprocessAddedItems( ref _afterLateUpdateList );
        }

        private static void DoUpdate( ref UpdateList updates )
        {
            updates.Index = 0;
            while ( updates.Index < updates.Actions.Count )
            {
                updates.Actions[ updates.Index ].Action();
                updates.Index++;
            }

            updates.Index = -1;
        }

        private static void PostprocessAddedItems( ref UpdateList updates )
        {
            if ( _tempListOfAddedItems.Count == 0 )
                return;

            //Updated should be executed in order (but after all already registered items)
            _tempListOfAddedItems.Sort();
            foreach ( var newlyAddedItem in _tempListOfAddedItems )                
                newlyAddedItem.Action();

            //If count of newly added items are small, add one to one, instead add range and resort list
            if ( _tempListOfAddedItems.Count < updates.Actions.Count / 2 )
            {
                foreach ( var newlyAddedItem in _tempListOfAddedItems )
                {
                    AddSortedUpdatedItem( ref updates, newlyAddedItem );
                }
            }
            else
            {
                updates.Actions.AddRange( _tempListOfAddedItems );
                updates.Actions.Sort();
            }

            _tempListOfAddedItems.Clear();
        }

        private static void AddSortedUpdatedItem( ref UpdateList updateList, UpdateItem newItem )
        {
            var indexToInsert = updateList.Actions.BinarySearch( newItem );
            if( indexToInsert < 0 )                
                indexToInsert = ~indexToInsert;
            updateList.Actions.Insert( indexToInsert, newItem );
        }


        // private class OrderComparer : Comparer<UpdateItem>
        // {
        //     public override Int32 Compare(UpdateItem x, UpdateItem y )
        //     {
        //         return x.Order.CompareTo( y.Order );
        //     }
        // }

        private struct UpdateList
        {
            public readonly List<UpdateItem> Actions;
            public int          Index;

            public UpdateList( List<UpdateItem> actions )
            {
                Actions = actions;
                Index   = -1;
            }
        }

        private struct UpdateItem: IComparable<UpdateItem>
        {
            public readonly Action  Action;
            public readonly int     Order;

            public UpdateItem( Action action, int order )
            {
                Action = action;
                Order = order;
            }

            public int CompareTo(UpdateItem other)
            {
                return Order.CompareTo( other.Order );
            }
        }

#region Editor
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void CleanUp( )
        {
            UnityEditor.EditorApplication.playModeStateChanged += state =>
            {
                if ( state == UnityEditor.PlayModeStateChange.ExitingPlayMode )
                {
                    //Unregister all updates
                    _afterUpdateList.Actions.Clear();
                    _beforeLateUpdateList.Actions.Clear();
                    _afterLateUpdateList.Actions.Clear();
                }
            };
        }
#endif
#endregion
    }

    internal sealed class UpdateManagerBeforeLateUpdate
    {
    }

    internal sealed class UpdateManagerAfterLateUpdate
    {
    }


    internal sealed class UpdateManagerUpdate
    {
    }

    /// <summary>
    /// Actually after scripting Update
    /// </summary>
    public interface IUpdate
    {
        void DoUpdate( );
    }

    public interface IBeforeLateUpdate
    {
        void DoBeforeLateUpdate( );
    }

    public interface IAfterLateUpdate
    {
        void DoAfterLateUpdate( );
    }

}