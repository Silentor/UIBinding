using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UIBindings.Adapters;
using UIBindings.Converters;
using UIBindings.Runtime.Utils;
using UnityEditor;

namespace UIBindings.Editor.Utils
{
    /// <summary>
    /// Get misc editor info for bindings. For debug. Consists of binding source info, binding direction info and binding target info.
    /// </summary>
    public static class BindingEditorUtils
    {
        public static string GetBindingSourceInfo( BindingBase binding, UnityEngine.Object host )
        {
            if ( binding is DataBinding dataBinding )
                return GetBindingSourceInfo( dataBinding, host );
            else if( binding is CallBinding callBinding )
                return GetBindingSourceInfo( callBinding, host );

            return "not implemented";
        }

        public static string GetBindingSourceInfo( DataBinding binding, UnityEngine.Object host )
        {
            var (sourceObjectType, sourceObject) = BindingUtils.GetSourceTypeAndObject( binding, host );
            if( sourceObjectType == null ) return "?";

            var sourceObjInfo = sourceObject ? $"'{sourceObject.name}'({sourceObject.GetType().Name})" : $"({sourceObjectType.Name})";

            var pathParser = new PathParser( sourceObjectType, binding.Path );
            var propInfo = pathParser.LastProperty;
            if( propInfo == null )
            {
                return $"{sourceObjInfo}.?";
            }
            else
            {
                var propType = propInfo.PropertyType;
                return $"{propType.GetPrettyName()} {sourceObjInfo}.{binding.Path}";
            }
        }

        public static string GetBindingSourceInfo( CallBinding binding, UnityEngine.Object host )
        {
            var (sourceObjectType, sourceObject) = BindingUtils.GetSourceTypeAndObject( binding, host );
            if( sourceObjectType == null ) return "?";

            var sourceObjInfo = sourceObject ? $"'{sourceObject.name}'({sourceObject.GetType().Name})" : $"({sourceObjectType.Name})";

            var pathParser = new PathParser( sourceObjectType, binding.Path );
            var methodInfo = pathParser.LastMethod;
            if( methodInfo == null )
            {
                return $"{sourceObjInfo}.?()";
            }
            else
            {
                var returnType = methodInfo.ReturnType.GetPrettyName();
                var paramsString = methodInfo.GetParameters().Length > 0
                        ? String.Concat("(", String.Join( ", ", methodInfo.GetParameters().Select( p => p.ParameterType.Name ) ), ")")
                        : "()";
                return $"{returnType} {sourceObjInfo}.{binding.Path}{paramsString}" ;
            }
        }

        public static string GetBindingDirection( BindingBase binding )
        {
            return binding.GetBindingDirection();
        }

        public static string GetBindingTargetInfo( BindingBase binding, FieldInfo bindingFieldInfo, UnityEngine.Object host, bool includeTargetObject = true )
        {
            var bindingTypeStr  = binding.GetType().GetPrettyName();
            var bindingPropName = bindingFieldInfo.Name;

            if( includeTargetObject )
                return $"{bindingTypeStr} '{host.name}'({host.GetType().GetPrettyName()}).{bindingPropName}";
            else
                return $"{bindingTypeStr} {bindingPropName}";
        }

        public static ValidationResult IsBindingValid( BindingBase binding, UnityEngine.Object host )
        {
            if ( binding is DataBinding dataBinding )
            {
                var checkResult = IsSourceValid( dataBinding, host );
                if( !checkResult.IsValid )
                    return checkResult;
                checkResult = IsConvertersValid( dataBinding, host );
                if( !checkResult.IsValid )
                    return checkResult;

                return ValidationResult.Valid;
            }
            else if ( binding is CallBinding callBinding )
            {
                var checkResult = IsSourceValid( callBinding, host );
                if( !checkResult.IsValid )
                    return checkResult;

                // CallBinding does not have converters, so we skip that check
                return ValidationResult.Valid;
            }
            else
            {
                return ValidationResult.Invalid( "not implemented" );
            }
        }

        public static ValidationResult IsSourceValid( BindingBase binding, UnityEngine.Object host )
        {
            if ( binding is DataBinding dataBinding )
                return IsSourceValid( dataBinding, host );
            else if( binding is CallBinding callBinding )
                return IsSourceValid( callBinding, host );

            return ValidationResult.Invalid( "not implemented" );
        }

        public static ValidationResult IsSourceValid( DataBinding binding, UnityEngine.Object host )
        {
            var (sourceObjectType, _) = BindingUtils.GetSourceTypeAndObject( binding, host );
            if( sourceObjectType == null ) return ValidationResult.Invalid( "Source not found" );

            if( string.IsNullOrEmpty( binding.Path ) )
                return ValidationResult.Invalid( "Path is empty" );

            var pathParser = new PathParser( sourceObjectType, binding.Path );
            var propInfo = pathParser.LastProperty;
            if( propInfo == null )
                return ValidationResult.Invalid( $"Property '{binding.Path}' not found in {sourceObjectType.Name}" );

            if( !propInfo.CanRead )
                return ValidationResult.Invalid( $"Property '{binding.Path}' is not readable" );

            return ValidationResult.Valid;
        }

        public static ValidationResult IsSourceValid( CallBinding binding, UnityEngine.Object host )
        {
            var (sourceObjectType, _) = BindingUtils.GetSourceTypeAndObject( binding, host );
            if( sourceObjectType == null ) return ValidationResult.Invalid( "Source not defined" );

            if( string.IsNullOrEmpty( binding.Path ) )
                return ValidationResult.Invalid( "Path is empty" );

            var pathParser = new PathParser( sourceObjectType, binding.Path );
            var methodInfo  = pathParser.LastMethod;
            if( methodInfo == null )
                return ValidationResult.Invalid( $"Method '{binding.Path}()' not found in {sourceObjectType.Name}" );

            if ( !CallBindingEditor.IsProperMethod( methodInfo ) )
                return ValidationResult.Invalid( $"Method '{binding.Path}()' is not supported" );

            return ValidationResult.Valid;
        }

        public static ValidationResult IsConvertersValid( DataBinding binding, UnityEngine.Object host )
        {
            var sourceProperty = GetSourceProperty( binding, host );
            if( sourceProperty == null )
                return ValidationResult.Invalid( "Source property not found" );

            var sourcePropertyType = PathAdapter.GetAdaptedType( sourceProperty.PropertyType );
            Predicate<Type> isTargetCompatible = binding.IsCompatibleWith;
            var converters = binding.Converters;
            var isTwoWayBinding = binding.IsTwoWay;

            // If no converters, check direct assignability
            if ( converters.Count == 0 )
            {
                if ( isTargetCompatible( sourcePropertyType ) || ImplicitConversion.IsConversionSupported( sourcePropertyType, isTargetCompatible ) )
                    return ValidationResult.Valid;
                else
                {
                    return ValidationResult.Invalid( $"Source type {sourcePropertyType.Name} is not compatible with binding target type." );
                }
            }

            // Check the chain: sourceType -> [converter1] -> ... -> [converterN] -> targetType
            for (int i = 0; i < converters.Count; i++)
            {
                var converter     = converters[i];
                if ( converter == null )              //Something wrong with converter
                    return ValidationResult.Invalid( $"Converter at index {i} is null." );

                var converterResult = IsConverterValid( sourcePropertyType, converter, isTwoWayBinding );
                if( !converterResult.IsValid )
                    return converterResult;

                sourcePropertyType = converter.OutputType;
            }

            // After all converters, the result type must be assignable to the target type
            if ( isTargetCompatible(sourcePropertyType) || ImplicitConversion.IsConversionSupported( sourcePropertyType, isTargetCompatible ) )
                return ValidationResult.Valid;
            else
            {
                return ValidationResult.Invalid( $"Final last converter's type {sourcePropertyType.Name} is not compatible with binding target type." );
            }
        }

        public static ValidationResult IsConverterValid(Type prevType, ConverterBase converter, bool isBindingTwoWay )
        {
            if( prevType == null )
            {
                return ValidationResult.Invalid( "Previous type is null" );
            }

            if( converter == null )
            {
                return ValidationResult.Invalid( "Converter is null" );
            }

            if ( isBindingTwoWay && !converter.IsTwoWay )
            {
                return ValidationResult.Invalid( $"Converter {converter.GetType().Name} is one-way, but binding is two-way." );
            }

            var isAssignable = prevType == converter.InputType || ImplicitConversion.IsConversionSupported( prevType, converter.InputType );
            if ( !isAssignable )
            {
                return ValidationResult.Invalid( $"Converter {converter.GetType().Name} input type {converter.InputType.Name} is not compatible with previous type {prevType.Name}." );
            }

            return ValidationResult.Valid;
        }

        public static PropertyInfo GetSourceProperty( DataBinding binding, UnityEngine.Object host )
        {
            var (sourceType, _) = BindingUtils.GetSourceTypeAndObject( binding, host );
            if ( sourceType == null )
                return null;

            var pathParser = new PathParser( sourceType, binding.Path );
            return pathParser.LastProperty;
        }

        public static (T binding, UnityEngine.Object host) GetBindingObject<T>( SerializedProperty bindingProp ) where T : BindingBase
        {
            var hostObject = bindingProp.serializedObject.targetObject;
            var propertyPath = bindingProp.propertyPath;
            var propInfo = hostObject.GetType().GetField( propertyPath, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
            var result = propInfo?.GetValue( hostObject );
            return ((T)result, bindingProp.serializedObject.targetObject);
        }
    }

    public struct ValidationResult
    {
        public bool   IsValid;
        public string ErrorMessage;

        public static ValidationResult Valid => new () { IsValid = true, ErrorMessage = string.Empty };
        public static ValidationResult Invalid( string errorMessage )
        {
            return new ValidationResult() { IsValid = false, ErrorMessage = errorMessage };
        }

        public static ValidationResult operator +(ValidationResult left, ValidationResult right)
        {
            return new ValidationResult()
                   {
                           IsValid      = left.IsValid && right.IsValid,
                           ErrorMessage = string.Concat( left.ErrorMessage, right.ErrorMessage )
                   };
        }

        public static implicit operator bool(ValidationResult result)
        {
            return result.IsValid;
        }
        
    }

}