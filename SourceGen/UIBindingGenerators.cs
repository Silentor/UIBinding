using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SourceGen;

namespace UIBindings.SourceGen;

/// <summary>
/// Generates code for observable properties from marked fields and for implementing INotifyPropertyChanged/INotifyPropertyChanging interfaces.
/// </summary>
[Generator]
public class UIBindingsGenerator : IIncrementalGenerator
{
    private const String ObservablePropertyAttributeFullyQualifiedName = "UIBindings.SourceGen.ObservablePropertyAttribute";
    private const String NotifyPropertyChangedForAttributeFullyQualifiedName = "UIBindings.SourceGen.NotifyPropertyChangedForAttribute";
    private const String INotifyPropertyChangedAttributeFullyQualifiedName = "UIBindings.SourceGen.INotifyPropertyChangedAttribute";
    //private const String INotifyPropertyChangingAttributeFullyQualifiedName = "UIBindings.SourceGen.INotifyPropertyChangingAttribute";
    private const String INotifyPropertyChangingFullyQualifiedName = "UIBindings.INotifyPropertyChanging";
    private const String INotifyPropertyChangedFullyQualifiedName = "UIBindings.INotifyPropertyChanged";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //Find all fields with the ObservablePropertyAttribute
        IncrementalValuesProvider<ObservableField> observableFields = context.SyntaxProvider
                                                                             .ForAttributeWithMetadataName(
                                                                                      ObservablePropertyAttributeFullyQualifiedName,
                                                                                      predicate: static (s, _) => s is VariableDeclaratorSyntax,
                                                                                      transform: static (ctx, cancel) => GetAnnotatedField( ctx.SemanticModel, ctx.TargetNode, ctx.TargetSymbol, ctx.Attributes, cancel ) )
                                                                             .Where( static m => m is not null )!;

        var observableFieldsProcessed = observableFields.Select( static (of, ct) => ProcessObservableField( of, ct ) );
        var observableFieldsCollection = observableFieldsProcessed.Collect();
        var classesWithFields = observableFieldsCollection.SelectMany( GetClassWithObservableProperties );

        // Generate source code for each enum found
        context.RegisterSourceOutput(classesWithFields, static (spc, source) => EmitObservablePropertyCode(source!, spc));

        //Find all classes with the ObservablePropertyAttribute
        IncrementalValuesProvider<INotifyPropertyChangedClass> notifyChangedClasses = context.SyntaxProvider
                                                                                             .ForAttributeWithMetadataName(
                                                                                                      INotifyPropertyChangedAttributeFullyQualifiedName,
                                                                                                      predicate: static (s, _) => s is ClassDeclarationSyntax,
                                                                                                      transform: static (ctx, cancel) => GetINotifyChangedClasses( ctx.SemanticModel, ctx.TargetNode, ctx.TargetSymbol, ctx.Attributes, cancel ) )
                                                                                             .Where( static m => m is not null )!;

        var processedNotifyChangedClasses = notifyChangedClasses.Select( static (c, cancel) => ProcessNotifyChangedClass( c, cancel ) ).Where( n => n is not null )!;

        context.RegisterSourceOutput( processedNotifyChangedClasses, static (spc, source ) => EmitINotifyPropertyChangedCode( source!, spc ) );
    }


    //Get fast syntax info about marked field
    private static ObservableField? GetAnnotatedField(   SemanticModel semanticModel, SyntaxNode syntaxNode, ISymbol symbol,
                                                         ImmutableArray<AttributeData> attributes, CancellationToken cancel )
    {
        //Must be when marker attribute is applied to a field
        if (syntaxNode is not VariableDeclaratorSyntax varDeclarator )
            return null;

        var varDeclaration = (VariableDeclarationSyntax)varDeclarator.Parent;
        var fieldDeclaration = (FieldDeclarationSyntax)varDeclaration.Parent;
        var classDeclaration = (ClassDeclarationSyntax)fieldDeclaration.Parent;
        var classFullHierarchicalName = Utils.GetFullTypeName( classDeclaration );
        var fullName = $"{classFullHierarchicalName}.{varDeclaration.Type.GetText(  )} {varDeclarator.Identifier.Text}";

        var attributesAsString = Utils.GetAttributesAsString( fieldDeclaration );

        return new ObservableField()
               {
                       FieldFullName    = fullName,
                       AttributesString = attributesAsString,
                       FieldSymbol      = (IFieldSymbol)symbol,
                       SyntaxNode       = varDeclarator,
                       SemanticModel = semanticModel,
               };
    }

    //Make heavy semantic operations
    private static ObservableField ProcessObservableField(ObservableField of, CancellationToken cancel )
    {
        var varDeclarator = of.SyntaxNode;
        var varDeclaration   = (VariableDeclarationSyntax)varDeclarator.Parent;
        var fieldDeclaration = (FieldDeclarationSyntax)varDeclaration.Parent;
        var classDeclaration = (ClassDeclarationSyntax)fieldDeclaration.Parent;
        var semanticModel = of.SemanticModel;

        var attributeList = fieldDeclaration.AttributeLists;
        var allAttributesSyntaxStr = fieldDeclaration.AttributeLists.Select( al => al.ToString() ).ToImmutableArray();
        var attributesToTransfer = new List<String>();
        foreach ( var attributeListSyntax in attributeList )
        {
            if ( attributeListSyntax.Target?.Identifier.Text == "property" ) //This attribute should be transferred to the property
            {
                foreach ( var attributeSyntax in attributeListSyntax.Attributes )
                {
                    var attributeConstructor = semanticModel.GetSymbolInfo( attributeSyntax, cancellationToken: cancel ).Symbol as IMethodSymbol;
                    if( attributeConstructor == null )
                        continue;
                    var attributeFulName = attributeConstructor.ContainingType.ToDisplayString( SymbolDisplayFormat.FullyQualifiedFormat );
                    var attributeArgs = attributeSyntax.ArgumentList?.ToString() ?? string.Empty;
                    attributesToTransfer.Add( $"[{attributeFulName}{attributeArgs}]" );
                }
                
            }
        }
        //var classDeclaration = (ClassDeclarationSyntax)fieldDeclaration.Parent;
        //var namespaceDeclaration = (NamespaceDeclarationSyntax)classDeclaration.Parent;

        var fieldSymbol = of.FieldSymbol;
        var fieldName = fieldSymbol.Name;
        var fieldType = fieldSymbol.Type.ToDisplayString(  );
        var declaredType = fieldSymbol.ContainingType;
        var classFullName = declaredType.ToDisplayString( SymbolDisplayFormat.FullyQualifiedFormat );
        //var namespaceName = declaredType.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
 
        var allAttributes = fieldSymbol.GetAttributes();
        var alsoNotifyProperties = allAttributes
                                  .Where( a => a.AttributeClass?.ToDisplayString() == NotifyPropertyChangedForAttributeFullyQualifiedName )
                                  .SelectMany( a => a.ConstructorArguments )
                                  .SelectMany( arg => arg.Kind == TypedConstantKind.Primitive ? new [] { arg.Value.ToString() } : arg.Values.Select( v => v.Value.ToString() ) )
                                  .Where( v => !string.IsNullOrEmpty( v ) )
                                  .ToArray();

        return new ObservableField()
               {
                       Name = fieldName,
                       Type  = fieldType,
                       ClassFullName = classFullName,
                       Attributes = allAttributesSyntaxStr,
                       AlsoNotifyProperties = alsoNotifyProperties,
                       AttributesForProperty = attributesToTransfer,
                       FieldSymbol = fieldSymbol,
               };
    }

    //Group fields of one class to special holder object to process each class only once
    private static IEnumerable<ClassWithObservableFields> GetClassWithObservableProperties(ImmutableArray<ObservableField> fields, CancellationToken token )
    {
        // Perform the grouping operation on the collected ImmutableArray
        return fields.GroupBy( f => f.ClassFullName )
                     .Select( group => new ClassWithObservableFields( group.Key, group.First().FieldSymbol.ContainingType, group.ToImmutableArray() ) );
    }

    private static void EmitObservablePropertyCode(ClassWithObservableFields source, SourceProductionContext spc )
    {
        var sb = new IndentedStringBuilder();

        GenerateEnclosingWithCode( sb, source.ClassNamespace, source.ContainingTypes, sb => GenerateProperties( sb, source ) );
        spc.AddSource( $"{source.SourceFilePartName}.ObservableProperties.g.cs", sb.ToString() );
    }

    //Generate enclosing namespace and classes and some code
    private static void GenerateEnclosingWithCode( IndentedStringBuilder sb, string namespaces, IEnumerable<string> containingTypes, Action<IndentedStringBuilder> generateCode )
    {
        // Generate the namespace declaration
        if ( string.IsNullOrEmpty( namespaces ) )
            GenerateClass( sb, containingTypes, generateCode );

        sb.AppendLine($"namespace {namespaces}");
        sb.AppendLine("{").AddIndent();
        sb.AppendLine( "using System;" );
        sb.AppendLine( "using System.Collections.Generic;" );
        GenerateClass( sb, containingTypes, generateCode );
        sb.RemoveIndent().AppendLine("}");
    }

    private static void GenerateClass( IndentedStringBuilder sb, IEnumerable<string> containingTypes, Action<IndentedStringBuilder> generateCode )
    {
        //Generate containing types if any
        foreach ( var containingType in containingTypes )
        {
            sb.AppendLine(  $"partial class {containingType}" );
            sb.AppendLine( "{" );
            sb.AddIndent();
        }

        generateCode( sb );

        foreach ( var _ in containingTypes )
        {
            sb.RemoveIndent();
            sb.AppendLine( "}" );
        }
    }

    private static void GenerateProperties( IndentedStringBuilder sb, ClassWithObservableFields source )
    {
        var generatedCodeAttribute = GetGeneratedCodeAttributeString();
        foreach ( var annotatedField in source.Fields )
        {
            var propName = ConvertFieldNameToPropertyName( annotatedField.Name );
            foreach ( var attrForGeneratedProp in annotatedField.AttributesForProperty )                
                sb.AppendLine( attrForGeneratedProp );
            sb.AppendLine( generatedCodeAttribute );
            sb.AppendLine( $"public {annotatedField.Type} {propName}" );   
            sb.AppendLine( "{");
            sb.AddIndent();
            sb.AppendLine( $"get => {annotatedField.Name};" );   
            sb.AppendLine( "set" );   
            sb.AppendLine( "{" );
            sb.AddIndent();
            sb.AppendLine( $"if (!EqualityComparer<{annotatedField.Type}>.Default.Equals({annotatedField.Name}, value))" );
            sb.AppendLine( "{" );
            sb.AddIndent();
            sb.AppendLine( $"var oldValue = {annotatedField.Name};" );
            sb.AppendLine( $"On{propName}Changing( oldValue, value );" );
            //if ( source.IsImplementsNotifyChanging )
            {
                sb.AppendLine( "OnPropertyChanging( );"  );
                foreach ( var alsoNotify in annotatedField.AlsoNotifyProperties )
                {
                    sb.AppendLine( $"OnPropertyChanging( \"{alsoNotify}\" );" );    
                }
            }
            sb.AppendLine( $"{annotatedField.Name} = value;" );
            sb.AppendLine( $"On{propName}Changed( oldValue, value );" );
            sb.AppendLine( "OnPropertyChanged( );" );

            foreach ( var alsoNotify in annotatedField.AlsoNotifyProperties )
            {
                sb.AppendLine( $"OnPropertyChanged( \"{alsoNotify}\" );" );
            }

            sb.RemoveIndent();
            sb.AppendLine( "}" );
            sb.RemoveIndent();
            sb.AppendLine( "}" );
            sb.RemoveIndent();
            sb.AppendLine( "}" );   

            sb.AppendLine( generatedCodeAttribute );
            sb.AppendLine( $"partial void On{propName}Changing( {annotatedField.Type} oldValue, {annotatedField.Type} newValue );" );
            sb.AppendLine( generatedCodeAttribute );
            sb.AppendLine( $"partial void On{propName}Changed( {annotatedField.Type} oldValue, {annotatedField.Type} newValue );" );
            sb.AppendLine( "" );
        }
    }

    private static INotifyPropertyChangedClass? GetINotifyChangedClasses(   SemanticModel semanticModel, SyntaxNode syntaxNode, ISymbol symbol,
                                                                            ImmutableArray<AttributeData> attributes, CancellationToken cancel )
    {
        if ( syntaxNode is not ClassDeclarationSyntax typeNode )
            return null;

        var classFullName = Utils.GetFullTypeName( typeNode );
        var attributesStr = Utils.GetAttributesAsString( typeNode );

        return new INotifyPropertyChangedClass()
               {
                       ClassFullName = classFullName,
                       Attributes    = attributesStr,
                       ClassNode     = typeNode,
                       ClassSymbol = (INamedTypeSymbol)symbol,
               };
    }

    private static INotifyPropertyChangedClass? ProcessNotifyChangedClass(INotifyPropertyChangedClass notifClass, CancellationToken cancel )
    {
        var classSymbol = notifClass.ClassSymbol;
        if ( classSymbol.BaseType != null )
        {
            var baseInterfaces = classSymbol.BaseType.AllInterfaces;
            if(baseInterfaces.Any( i => i.ToDisplayString( ) == INotifyPropertyChangedFullyQualifiedName )) //Notify property changed infrastructure should be already here 
            {
                return null;
            }

            var baseType = classSymbol.BaseType;
            while( baseType != null && baseType.ToDisplayString(  ) != "object" )
            {
                if ( baseType.GetAttributes().Any( a => a.AttributeClass?.ToDisplayString( ) == INotifyPropertyChangedAttributeFullyQualifiedName ) )//Notify property changed infrastructure will be generated 
                    return null;
                baseType = baseType.BaseType;
            }
        }

        notifClass.SourceFilePartName = Utils.GetSafeHintNameForType( classSymbol );
        notifClass.Namespace = Utils.GetNamespaceString( classSymbol );
        notifClass.ContainingTypes = Utils.GetContainingTypes( classSymbol );
        //notifClass.IsImplementsNotifyChanging = classSymbol.AllInterfaces.Any( i => i.ToDisplayString( ) == INotifyPropertyChangingAttributeFullyQualifiedName );

        return notifClass;
    }

    private static void EmitINotifyPropertyChangedCode(INotifyPropertyChangedClass source, SourceProductionContext spc )
    {
        var sb = new IndentedStringBuilder();
        GenerateEnclosingWithCode( sb, source.Namespace, source.ContainingTypes.Take( source.ContainingTypes.Count - 1 ), sb => GenerateINotifyPropertyChangedInfrastructure(sb, source) );
        
        spc.AddSource( $"{source.SourceFilePartName}.INotifyPropertyChanged.g.cs", sb.ToString() );
    }

    private static void GenerateINotifyPropertyChangedInfrastructure(IndentedStringBuilder sb, INotifyPropertyChangedClass source )
    {
        //var isNotifyChangingSupported = source.IsImplementsNotifyChanging;
        var className = source.ContainingTypes.Last();
        sb.AppendLine( $"partial class {className} : {INotifyPropertyChangedFullyQualifiedName}, {INotifyPropertyChangingFullyQualifiedName}"  );
        sb.AppendLine("{").AddIndent();

        // Generate the event and OnPropertyChanged method
        var generatedCodeAttribute = GetGeneratedCodeAttributeString();
        sb.AppendLine( generatedCodeAttribute );
        sb.AppendLine( "public event Action<System.Object, System.String> PropertyChanged;" );
        sb.AppendLine(  );
        sb.AppendLine( generatedCodeAttribute );
        sb.AppendLine( "protected virtual void OnPropertyChanged( [global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null )");
        sb.AppendLine( "{" ).AddIndent();
        sb.AppendLine( "PropertyChanged?.Invoke(this, propertyName );"  );
        sb.RemoveIndent().AppendLine( "}" );
        sb.AppendLine( generatedCodeAttribute );
        sb.AppendLine( "protected virtual void OnPropertyChangedAll( )");
        sb.AppendLine( "{" ).AddIndent();
        sb.AppendLine( "PropertyChanged?.Invoke(this, null );"  );
        sb.RemoveIndent().AppendLine( "}" );
        //if(  isNotifyChangingSupported ) 
        {
            sb.AppendLine( generatedCodeAttribute );
            sb.AppendLine( "public event Action<System.Object, System.String> PropertyChanging;" );
            sb.AppendLine(  );
            sb.AppendLine( generatedCodeAttribute );
            sb.AppendLine( "protected virtual void OnPropertyChanging( [global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null )");
            sb.AppendLine( "{" ).AddIndent();
            sb.AppendLine( "PropertyChanging?.Invoke(this, propertyName );"  );
            sb.RemoveIndent().AppendLine( "}" );
        }
        sb.AppendLine();

        sb.AppendLine( generatedCodeAttribute );
        sb.AppendLine( "protected bool SetProperty<T>(/*[NotNullIfNotNull( nameof(newValue) )]*/ ref T field, T newValue, [global::System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)" );
        sb.AppendLine("{" ).AddIndent();
        sb.AppendLine( "if (EqualityComparer<T>.Default.Equals(field, newValue))" );
        sb.AddIndent().AppendLine( "return false;" ).RemoveIndent();
        //if(isNotifyChangingSupported)
            sb.AppendLine( "OnPropertyChanging( propertyName );" );
        sb.AppendLine( "field = newValue;" );
        sb.AppendLine( "OnPropertyChanged( propertyName );" );
        sb.AppendLine( "return true;" );
        sb.RemoveIndent().AppendLine( "}" );
        sb.AppendLine();

        sb.AppendLine( generatedCodeAttribute );
        sb.AppendLine( "protected bool SetProperty<T>(/*[NotNullIfNotNull(nameof(newValue))]*/ ref T field, T newValue, IEqualityComparer<T> comparer, [global::System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)" );
        sb.AppendLine( "{" ).AddIndent();
        sb.AppendLine( "global::UnityEngine.Assertions.Assert.IsNotNull( comparer );" );
        sb.AppendLine( "if (comparer.Equals(field, newValue))" );
        sb.AddIndent().AppendLine( "return false;" ).RemoveIndent();
        //if(isNotifyChangingSupported)
            sb.AppendLine( "OnPropertyChanging( propertyName );" );
        sb.AppendLine( "field = newValue;" );
        sb.AppendLine( "OnPropertyChanged( propertyName );" );
        sb.AppendLine( "return true;" );
        sb.RemoveIndent().AppendLine( "}" );
        sb.AppendLine();

        sb.AppendLine( generatedCodeAttribute );
        sb.AppendLine( "protected bool SetProperty<TModel, T>(T oldValue, T newValue, TModel model, Action<TModel, T> callback, [global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null ) where TModel : class" );
        sb.AppendLine( "{" ).AddIndent();
        sb.AppendLine( "global::UnityEngine.Assertions.Assert.IsNotNull( model );" );
        sb.AppendLine( "global::UnityEngine.Assertions.Assert.IsNotNull( callback );" );
        sb.AppendLine( "if (EqualityComparer<T>.Default.Equals(oldValue, newValue))" );
        sb.AddIndent().AppendLine( "return false;" ).RemoveIndent();
        //if(isNotifyChangingSupported)
            sb.AppendLine( "OnPropertyChanging( propertyName );" );
        sb.AppendLine( "callback(model, newValue);" );
        sb.AppendLine( "OnPropertyChanged( propertyName );" );
        sb.AppendLine( "return true;" );
        sb.RemoveIndent().AppendLine( "}" );
        sb.AppendLine();

        sb.AppendLine( generatedCodeAttribute );
        sb.AppendLine( "protected bool SetProperty<TModel, T>(T oldValue, T newValue, IEqualityComparer<T> comparer, TModel model, Action<TModel, T> callback, [global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null ) where TModel : class" );
        sb.AppendLine( "{" ).AddIndent();
        sb.AppendLine( "global::UnityEngine.Assertions.Assert.IsNotNull( model );" );
        sb.AppendLine( "global::UnityEngine.Assertions.Assert.IsNotNull( callback );" );
        sb.AppendLine( "global::UnityEngine.Assertions.Assert.IsNotNull( comparer );" );
        sb.AppendLine( "if (comparer.Equals(oldValue, newValue))" );
        sb.AddIndent().AppendLine( "return false;" ).RemoveIndent();
        //if(isNotifyChangingSupported)
            sb.AppendLine( "OnPropertyChanging( propertyName );" );
        sb.AppendLine( "callback(model, newValue);" );
        sb.AppendLine( "OnPropertyChanged( propertyName );" );
        sb.AppendLine( "return true;" );
        sb.RemoveIndent().AppendLine( "}" );

        sb.RemoveIndent().AppendLine("}");
    }

    private static string ConvertFieldNameToPropertyName( string fieldName )
    {
        // Process prefix "m_" field name
        if( fieldName.StartsWith( "m_" ) )
            return $"{fieldName[2].ToString().ToUpper()}{fieldName.Substring( 3 )}";

        // Process prefix underscore field name
        if (fieldName.StartsWith( "_" ))
            return $"{fieldName[1].ToString().ToUpper()}{fieldName.Substring( 2 )}";

        //Process begins with lowercase letter field name
        if( fieldName[0].ToString().ToLower() == fieldName[0].ToString() )
            return $"{fieldName[0].ToString().ToUpper()}{fieldName.Substring( 1 )}";

        return fieldName;
    }

    private static string GetGeneratedCodeAttributeString( )
    {
        //Get version from assembly
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return $"""[global::System.CodeDom.Compiler.GeneratedCode("UIBindings.SourceGen.UIBindingsGenerator", "{version}")]""";
    }

    private class ObservableField : IEquatable<ObservableField>
    {
        //Main data, used for equality check, fast and syntax-only
        public string FieldFullName;
        public string AttributesString;

        //Saved from syntax phase
        public IFieldSymbol             FieldSymbol;    //Stored for future processing, exclude from equality check
        public VariableDeclaratorSyntax SyntaxNode;
        public SemanticModel            SemanticModel;



        public string Name;
        public string Type;
        public string ClassFullName;
        
        
        public ImmutableArray<string> Attributes = ImmutableArray<String>.Empty;//All attributes as a strings, just to detect any changes in attributes

        //Not for equality check
        public string[] AlsoNotifyProperties;
        public IReadOnlyList<string> AttributesForProperty = Array.Empty<String>();

        public bool Equals(ObservableField other)
        {
            return FieldFullName == other.FieldFullName && AttributesString == other.AttributesString;
        }

        public override bool Equals(object? obj)
        {
            return obj is ObservableField other && Equals( other );
        }

        public override int GetHashCode( )
        {
            unchecked
            {
                var hashCode = GetSimpleDeterministicHash( FieldFullName );
                hashCode = (hashCode * 397) ^ GetSimpleDeterministicHash( AttributesString );
                return hashCode;
            }
        }

        private static int GetSimpleDeterministicHash(string s)
        {
            unchecked // Allow overflow, which is fine for hashing
            {
                int hash = 23; // Start with a prime number
                foreach (char c in s)
                {
                    hash = hash * 31 + c; // Multiply by another prime, add character value
                }
                return hash;
            }
        }

        public static bool operator ==(ObservableField left, ObservableField right)
        {
            return left.Equals( right );
        }

        public static bool operator !=(ObservableField left, ObservableField right)
        {
            return !left.Equals( right );
        }
    }

    private class ClassWithObservableFields : IEquatable<ClassWithObservableFields>
    {
        public readonly string          ClassFullName;
        public readonly ImmutableArray<ObservableField> Fields;

        public readonly string          ClassNamespace;
        public readonly string          SourceFilePartName;         //Namespace + type names without file name incorrect symbols
        public readonly IReadOnlyList<String>   ContainingTypes;

        //public Boolean IsImplementsNotifyChanging; 
        public readonly INamedTypeSymbol ClassSymbol;

        public ClassWithObservableFields(String classFullName, INamedTypeSymbol classSymbol, ImmutableArray<ObservableField> fields )
        {
            ClassFullName = classFullName;
            Fields        = fields;
            ClassSymbol   = classSymbol;
            SourceFilePartName = Utils.GetSafeHintNameForType( classSymbol );
            //IsImplementsNotifyChanging = classSymbol.AllInterfaces.Any( i => i.ToDisplayString( ) == INotifyPropertyChangingFullyQualifiedName );

            //Make namespace string
            ClassNamespace = Utils.GetNamespaceString( classSymbol );

            //make list of containing types (if any)
            ContainingTypes = Utils.GetContainingTypes( classSymbol );
        }

        public bool Equals(ClassWithObservableFields? other)
        {
            if ( other is null ) return false;
            if ( ReferenceEquals( this, other ) ) return true;
            return ClassFullName == other.ClassFullName && Fields.Equals( other.Fields );
        }

        public override bool Equals(object? obj)
        {
            if ( obj is null ) return false;
            if ( ReferenceEquals( this, obj ) ) return true;
            if ( obj.GetType() != GetType() ) return false;
            return Equals( (ClassWithObservableFields) obj );
        }

        public override int GetHashCode( )
        {
            unchecked
            {
                return (ClassFullName.GetHashCode() * 397) ^ Fields.GetHashCode();
            }
        }

        public static bool operator ==(ClassWithObservableFields? left, ClassWithObservableFields? right)
        {
            return Equals( left, right );
        }

        public static bool operator !=(ClassWithObservableFields? left, ClassWithObservableFields? right)
        {
            return !Equals( left, right );
        }
    }

    private class INotifyPropertyChangedClass: IEquatable<INotifyPropertyChangedClass>
    {
        //To equality check (syntax only)
        public string ClassFullName;
        public string Attributes;

        //Additional data (semantic)
        public ClassDeclarationSyntax ClassNode;
        public INamedTypeSymbol ClassSymbol;
        public string SourceFilePartName;
        public string Namespace; 
        public IReadOnlyList<string> ContainingTypes = new List<string>( );
        //public bool IsImplementsNotifyChanging; 

        public bool Equals(INotifyPropertyChangedClass? other)
        {
            if ( other is null ) return false;
            if ( ReferenceEquals( this, other ) ) return true;
            return ClassFullName == other.ClassFullName && Attributes == other.Attributes;
        }

        public override bool Equals(object? obj)
        {
            if ( obj is null ) return false;
            if ( ReferenceEquals( this, obj ) ) return true;
            if ( obj.GetType() != GetType() ) return false;
            return Equals( (INotifyPropertyChangedClass) obj );
        }

        public override int GetHashCode( )
        {
            unchecked
            {
                return (ClassFullName.GetHashCode() * 397) ^ Attributes.GetHashCode();
            }
        }

        public static bool operator ==(INotifyPropertyChangedClass? left, INotifyPropertyChangedClass? right)
        {
            return Equals( left, right );
        }

        public static bool operator !=(INotifyPropertyChangedClass? left, INotifyPropertyChangedClass? right)
        {
            return !Equals( left, right );
        }
    }
}