using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UIBindings.Runtime;
using Unity.Profiling;

namespace UIBindings.Adapters
{
    /// <summary>
    /// Data item of some part of binding path. Can be property adapter, field adapter, collection adapter, etc.
    /// Assume we has a binding path: "Player.GetInventory.Items[0].ItemName"
    /// This is a chain of path adapters:
    /// 1) Implicit SourceReadAdapter to read source object from binding
    /// 2) Property adapter to read Player property from source object
    /// 3) Method adapter to call GetInventory() method on Player object
    /// 4) Property adapter to read Items property from Inventory object
    /// 5) Collection adapter to read [0] (or range??) item from Items collection
    /// 6) Property adapter to read ItemName property from Item object
    /// </summary>
    public abstract class PathAdapter : DataProvider
    {
        public override bool IsTwoWay { get; }

        public abstract Type OutputType  { get; }

        public abstract string MemberName { get; }

        /// <summary>
        /// Connect this adapter to another adapter.
        /// </summary>
        /// <param name="sourceAdapter"></param>
        /// <param name="isTwoWayBinding"></param>
        /// <param name="notifyPropertyChanged"></param>
        /// <exception cref="ArgumentNullException"></exception>
        protected PathAdapter( PathAdapter sourceAdapter, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged )
        {
            NotifyPropertyChanged = notifyPropertyChanged;
            IsTwoWay              = isTwoWayBinding;
            SourceAdapter       = sourceAdapter ?? throw new ArgumentNullException( nameof(sourceAdapter) );
            DoSourcePropertyChangedDelegate = DoSourcePropertyChanged;
        }

        /// <summary>
        /// Connect this adapter to source object.
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="isTwoWayBinding"></param>
        /// <param name="notifyPropertyChanged"></param>
        protected PathAdapter( object sourceObject, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged )
        {
            NotifyPropertyChanged = notifyPropertyChanged;
            IsTwoWay              = isTwoWayBinding;
            SourceObject          = sourceObject;
            DoSourcePropertyChangedDelegate = DoSourcePropertyChanged;
        }

        /// <summary>
        /// Property adapter will be detect his property changing by subscribing to INotifyPropertyChanged of source object.
        /// If source object does not support INotifyPropertyChanged, method will do nothing.
        /// </summary>
        public virtual void Subscribe( )
        {
            if( IsSubscribed )
                return;

            IsSubscribed = true;
            IsInited = false;

            if ( SourceAdapter == null )
            {
                if( SourceObject is INotifyPropertyChanged notifySourceObject )
                {
                    notifySourceObject.PropertyChanged += DoSourcePropertyChangedDelegate;
                    IsNeedPollingSelf =  false;
                }
                else
                    IsNeedPollingSelf = true; 
            }
            else
            {
                //Do not subscribe right now, actually subscribe at TryGetValue because we need to get source object first
                SourceAdapter.Subscribe( );
                IsNeedPollingSelf = true; //Didn't know for sure, better to poll one time
            }

            OnSubscribed();
        }

        public virtual void  Unsubscribe( )
        {
            if( !IsSubscribed )
                return;

            IsSubscribed = false;

            if ( SourceAdapter == null )
            {
                if ( SourceObject is INotifyPropertyChanged notifySourceObject )
                {
                    notifySourceObject.PropertyChanged -= DoSourcePropertyChangedDelegate; 
                }
            }
            else
            {
                SourceAdapter.Unsubscribe( );
            }

            OnUnsubscribed();
        }

        /// <summary>
        /// If return true, there is no way to detect property changes.
        /// So binding system will need to poll this adapter periodically to detect changes.
        /// If any one Path Adapter in chain is need polling, whole binding will be polled.
        /// </summary>
        /// <returns></returns>
        public virtual Boolean IsNeedPolling => SourceAdapter != null ? SourceAdapter.IsNeedPolling || IsNeedPollingSelf : IsNeedPollingSelf;

        /// <summary>
        /// Change source object, applicable to first adapter in chain only.
        /// </summary>
        /// <param name="sourceObject"></param>
        public void SetSourceObject( object sourceObject )
        {
            // Only the first adapter in chain can have source object set directly
            if ( SourceAdapter != null )
            {
                SourceAdapter.SetSourceObject( sourceObject );
            }
            else
            {
                if ( SourceObject != sourceObject )
                {
                    var wasSubscribed = IsSubscribed;

                    Unsubscribe();

                    SourceObject = sourceObject;

                    if ( wasSubscribed )
                    {
                        Subscribe();
                    }
                }
            }
        }

        /// <summary>
        /// For debugging, its ignore boxing
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract EResult TryGetValue(out object value );

        /// <summary>
        /// Sometimes we need to adapt type of property to some other type, for example, if it is enum we want to use StructEnum
        /// </summary>
        /// <param name="propertyType"></param>
        /// <returns></returns>
        // public static Type GetAdaptedType( Type propertyType )
        // {
        //     if ( propertyType == null ) return null;
        //     //if ( propertyType.IsEnum ) return typeof(StructEnum); TODO go for generic adapter, no need to convert enum to StructEnum (only if Binding<StructEnum> used)
        //     return propertyType;
        // }

        public static PathAdapter GetPropertyAdapter( PathAdapter sourceAdapter, PropertyInfo propertyInfo, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged )
        {
            var  sourceType   = propertyInfo.DeclaringType;
            var  propertyType = propertyInfo.PropertyType;
            var complexAdapterType = typeof(PropertyAdapter<,>).MakeGenericType( sourceType, propertyType );
            var result = (PathAdapter)Activator.CreateInstance( complexAdapterType, propertyInfo, sourceAdapter, isTwoWayBinding, notifyPropertyChanged );
            return result;
        }

        public static PathAdapter GetPropertyAdapter( object sourceObject, PropertyInfo propertyInfo, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged )
        {
            var  sourceType   = propertyInfo.DeclaringType;
            var  propertyType = propertyInfo.PropertyType;
            var complexAdapterType = typeof(PropertyAdapter<,>).MakeGenericType( sourceType, propertyType );
            var result = (PathAdapter)Activator.CreateInstance( complexAdapterType, propertyInfo, sourceObject, isTwoWayBinding, notifyPropertyChanged );
            return result;
        }

        public static PathAdapter GetMethodAdapter( PathAdapter sourceAdapter, MethodInfo methodInfo, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged )
        {
            var sourceType = methodInfo.DeclaringType;
            var paramz     = methodInfo.GetParameters();
            if ( paramz.Length < 3 )
            {
                Type adapterType = typeof(CallMethodAdapter<>).MakeGenericType( sourceType );
                var result = (PathAdapter)Activator.CreateInstance( adapterType, methodInfo, sourceAdapter, isTwoWayBinding, notifyPropertyChanged );
                return result;
            }
            else
            {
                throw new NotSupportedException($"Method with {paramz.Length} parameters is not supported");
            }
        }

        public static PathAdapter GetMethodAdapter( object sourceObject, MethodInfo methodInfo, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged )
        {
            var  sourceType   = methodInfo.DeclaringType;
            var paramz = methodInfo.GetParameters();
            if ( paramz.Length < 3 )
            {
                Type adapterType = typeof(CallMethodAdapter<>).MakeGenericType( sourceType );
                var result = (PathAdapter)Activator.CreateInstance( adapterType, methodInfo, sourceObject, isTwoWayBinding, notifyPropertyChanged );
                return result;
            }
            else
            {
                throw new NotSupportedException($"Method with {paramz.Length} parameters is not supported");
            }
        }

        // Is value was first time readed after subscribe or source object change
        protected bool      IsInited;

        protected bool IsNeedPollingSelf = true;

        // Previous adapter in chain
        protected readonly PathAdapter SourceAdapter;

        // Source object (for the first adapter in chain only)
        protected object SourceObject;

        protected          bool                   IsSubscribed;
        private readonly Action<object, string>   NotifyPropertyChanged;
        protected readonly Action<object, string> DoSourcePropertyChangedDelegate;

        protected static readonly ProfilerMarker ReadPropertyMarker  = new ( ProfilerCategory.Scripts,  $"Binding.{nameof(PathAdapter)}.ReadValue" );
        protected static readonly ProfilerMarker WritePropertyMarker = new ( ProfilerCategory.Scripts,  $"Binding.{nameof(PathAdapter)}.WriteValue" );

        protected void DoSourcePropertyChanged( object sender, String propertyName )
        {
            if( String.IsNullOrEmpty( propertyName ) || propertyName == MemberName )
            {
                NotifyPropertyChanged?.Invoke( sender, propertyName );
            }
        }

        protected virtual void OnSubscribed( ) { }
        protected virtual void OnUnsubscribed( ) { }

    }
}