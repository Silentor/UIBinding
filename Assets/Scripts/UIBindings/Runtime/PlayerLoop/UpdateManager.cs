using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace UIBindings.Runtime.PlayerLoop
{
    /// <summary>
    /// I dont bother unsubscribing from Player Loop on exit Play mode, just make sure to not subscribe twice on next Play mode start.
    /// </summary>
    public static class UpdateManager
    {
        private static          Boolean                 _processUpdate;
        private static          Boolean                 _processBeforeLateUpdate;
        private static          Boolean                 _processAfterLateUpdate;
        private static readonly List<IUpdate>           _delayedRemoveUpdates     = new();
        private static readonly List<IBeforeLateUpdate>  _delayedRemoveBeforeLateUpdates = new();
        private static readonly List<IAfterLateUpdate>  _delayedRemoveAfterLateUpdates = new();
        private static readonly List<IUpdate>           _updates                  = new();
        private static readonly List<IBeforeLateUpdate>  _beforeLateUpdates              = new();
        private static readonly List<IAfterLateUpdate>   _afterLateUpdates              = new();

        public static void RegisterUpdate([NotNull] IUpdate update)
        {
            if ( update == null ) throw new ArgumentNullException( nameof(update) );
            foreach ( var earlyUpdate in _updates )
            {
                if ( earlyUpdate == update )
                    return;
            }

            _updates.Add( update );
        }

        public static void RegisterBeforeLateUpdate([NotNull] IBeforeLateUpdate lateUpdate)
        {
            if ( lateUpdate == null ) throw new ArgumentNullException( nameof(lateUpdate) );

            foreach ( var upd in _beforeLateUpdates )
            {
                if ( upd == lateUpdate )
                    return;
            }

            _beforeLateUpdates.Add( lateUpdate );
        }


        public static void RegisterAfterLateUpdate([NotNull] IAfterLateUpdate lateUpdate)
        {
            if ( lateUpdate == null ) throw new ArgumentNullException( nameof(lateUpdate) );

            foreach ( var upd in _afterLateUpdates )
            {
                if ( upd == lateUpdate )
                    return;
            }

            _afterLateUpdates.Add( lateUpdate );
        }

        public static void UnregisterUpdate([NotNull] IUpdate update)
        {
            if ( update == null ) throw new ArgumentNullException( nameof(update) );
            if ( _processUpdate )
                _delayedRemoveUpdates.Add( update );
            else
                _updates.Remove( update );
        }

        public static void UnregisterBeforeLateUpdate([NotNull] IBeforeLateUpdate beforeLateUpdate)
        {
            if ( beforeLateUpdate == null ) throw new ArgumentNullException( nameof(beforeLateUpdate) );
            if ( _processBeforeLateUpdate )
                _delayedRemoveBeforeLateUpdates.Add( beforeLateUpdate );
            else
                _beforeLateUpdates.Remove( beforeLateUpdate );
        }

        public static void UnregisterAfterLateUpdate([NotNull] IAfterLateUpdate afterLateUpdate)
        {
            if ( afterLateUpdate == null ) throw new ArgumentNullException( nameof(afterLateUpdate) );
            if ( _processAfterLateUpdate )
                _delayedRemoveAfterLateUpdates.Add( afterLateUpdate );
            else
                _afterLateUpdates.Remove( afterLateUpdate );
        }

        [RuntimeInitializeOnLoadMethod]
        private static void Init( )
        {
            var defaultSystems = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();

            var myBeforeLateUpdate = new PlayerLoopSystem
                                    {
                                            subSystemList  = null,
                                            updateDelegate = OnBeforeLateUpdate,
                                            type           = typeof(UIBindingBeforeLateUpdate)
                                    };

            var myAfterLateUpdate = new PlayerLoopSystem
                                    {
                                            subSystemList  = null,
                                            updateDelegate = OnAfterLateUpdate,
                                            type           = typeof(UIBindingAfterLateUpdate)
                                    };

            var myUpdateSystem = new PlayerLoopSystem
                                 {
                                         subSystemList  = null,
                                         updateDelegate = OnUpdate,
                                         type           = typeof(UIBindingUpdate)
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

        private static int FindSystem( PlayerLoopSystem[] systems, Type typeToFind )
        {
            for ( int i = 0; i < systems.Length; i++ )
            {
                if ( systems[ i ].type == typeToFind )
                    return i;
            }

            return -1;
        }

        private static void OnUpdate( )
        {
            _processUpdate = true;
            var i = 0;
            while ( i < _updates.Count )
            {
                _updates[ i ].DoUpdate();
                i++;
            }

            _processUpdate = false;

            if ( _delayedRemoveUpdates.Count > 0 )
            {
                foreach ( var removeAfterLoop in _delayedRemoveUpdates )
                    _updates.Remove( removeAfterLoop );
                _delayedRemoveUpdates.Clear();
            }
        }

        private static void OnBeforeLateUpdate( )
        {
            _processBeforeLateUpdate = true;
            var i = 0;
            while ( i < _beforeLateUpdates.Count )
            {
                _beforeLateUpdates[ i ].DoBeforeLateUpdate();
                i++;
            }

            _processBeforeLateUpdate = false;

            if ( _delayedRemoveBeforeLateUpdates.Count > 0 )
            {
                foreach ( var removeAfterLoop in _delayedRemoveBeforeLateUpdates )
                    _beforeLateUpdates.Remove( removeAfterLoop );
                _delayedRemoveBeforeLateUpdates.Clear();
            }
        }

        private static void OnAfterLateUpdate( )
        {
            _processAfterLateUpdate = true;
            var i = 0;
            while ( i < _afterLateUpdates.Count )
            {
                _afterLateUpdates[ i ].DoAfterLateUpdate();
                i++;
            }

            _processAfterLateUpdate = false;

            if ( _delayedRemoveAfterLateUpdates.Count > 0 )
            {
                foreach ( var removeAfterLoop in _delayedRemoveAfterLateUpdates )
                    _afterLateUpdates.Remove( removeAfterLoop );
                _delayedRemoveAfterLateUpdates.Clear();
            }
        }
    }

    internal sealed class UIBindingBeforeLateUpdate
    {
    }

    internal sealed class UIBindingAfterLateUpdate
    {
    }


    internal sealed class UIBindingUpdate
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