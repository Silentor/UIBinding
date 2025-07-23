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

namespace UIBindings.SourceGen;

[Generator]
public class UIBindingsGenerator : IIncrementalGenerator
{
    private const String ObservablePropertyFullyQualifiedName = "UIBindings.ObservablePropertyAttribute";
    private const String INotifyPropertyChangedFullyQualifiedName = "UIBindings.INotifyPropertyChanged";
    private const String INotifyPropertyChangingFullyQualifiedName = "UIBindings.INotifyPropertyChanging";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Add the marker attribute
        context.RegisterPostInitializationOutput(static ctx => ctx.AddSource(
                "ObservablePropertyAttribute.g.cs", SourceText.From(AttributesHelper.ObservableProperty, Encoding.UTF8)));

        //Find all fields with the ObservablePropertyAttribute
        IncrementalValuesProvider<AnnotatedField?> observableFields = context.SyntaxProvider
                                                                             .ForAttributeWithMetadataName(
                                                                                      ObservablePropertyFullyQualifiedName,
                                                                                      predicate: static (s, _) => true,
                                                                                      transform: static (ctx, _) => GetAnnotatedField( ctx.SemanticModel, ctx.TargetNode, ctx.TargetSymbol ) )
                                                                             .Where( static m => m is not null );

        IncrementalValueProvider<ImmutableArray<AnnotatedField?>> observableFieldsCollection = observableFields.Collect();

        var classesWithFields = observableFieldsCollection.SelectMany( GetClassWithObservableProperties );

        // Generate source code for each enum found
        context.RegisterSourceOutput(classesWithFields,
                static (spc, source) => GenerateObservablePropertyCode(source!, spc));
    }


    

    private static AnnotatedField? GetAnnotatedField( SemanticModel semanticModel, SyntaxNode variableDeclarator, ISymbol symbol )
    {
        //Must be when marker attribute is applied to a field
        if (variableDeclarator is not VariableDeclaratorSyntax varDeclarator )
            return null;

        //var varDeclaration = (VariableDeclarationSyntax)varDeclarator.Parent;
        //var fieldDeclaration = (FieldDeclarationSyntax)varDeclaration.Parent;
        //var classDeclaration = (ClassDeclarationSyntax)fieldDeclaration.Parent;
        //var namespaceDeclaration = (NamespaceDeclarationSyntax)classDeclaration.Parent;

        //todo just syntax queries should be faster than semantic symbol queries, so replace this with syntax queries
        //Profile!
        var fieldSymbol = (IFieldSymbol)symbol;
        var fieldName = fieldSymbol.Name;
        var fieldType = fieldSymbol.Type.ToDisplayString(  );
        var declaredType = fieldSymbol.ContainingType;
        var className = declaredType.ToDisplayString();
        var classFullName = declaredType.ToDisplayString( SymbolDisplayFormat.FullyQualifiedFormat );
        var namespaceNameOnly = declaredType.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        //var namespaceName = declaredType.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return new AnnotatedField()
               {
                       Name = fieldName,
                       Type  = fieldType,
                       ClassFullName = classFullName,
                       FieldSymbol = fieldSymbol,
               };
    }

    private static IEnumerable<ClassWithFields> GetClassWithObservableProperties(ImmutableArray<AnnotatedField?> fields, CancellationToken token )
    {
        // Perform the grouping operation on the collected ImmutableArray
        return fields.GroupBy( f => f.ClassFullName )
                     .Select( group => new ClassWithFields( group.Key, group.First().FieldSymbol.ContainingType, group.ToImmutableArray() ) );
    }

    private static void GenerateObservablePropertyCode(ClassWithFields source, SourceProductionContext spc )
    {
        var sb = new StringBuilder();

        GenerateNamespace( sb, source );
        spc.AddSource( $"{source.SourceFilePartName}.g.cs", sb.ToString() );
    }

    private static void GenerateNamespace( StringBuilder sb, ClassWithFields source )
    {
        // Generate the namespace declaration
        if ( string.IsNullOrEmpty( source.ClassNamespace ) )
            GenerateClass( sb, source );

        sb.AppendLine($"namespace {source.ClassNamespace}");
        sb.AppendLine("{");
        GenerateClass( sb, source );
        sb.AppendLine("}");
    }

    private static void GenerateClass( StringBuilder sb, ClassWithFields source )
    {
        //Generate containing types if any
        var indent = 1;
        if( source.ContainingTypes != null && source.ContainingTypes.Count > 0 )
        {
            foreach ( var containingType in source.ContainingTypes )
            {
                AppendLine( sb, $"partial class {containingType}", indent );
                AppendLine( sb, "{", indent );
                indent++;
            }
        }

        AppendLine( sb, $"""[System.CodeDom.Compiler.GeneratedCode("UIBindings.SourceGen", "{Assembly.GetExecutingAssembly().GetName().Version}")]""", indent );
        AppendLine( sb, $"partial class {source.ClassName}", indent );
        AppendLine( sb, "{", indent );
        GenerateProperties( sb, source, indent + 1 );
        AppendLine( sb, "}", indent );

        if( source.ContainingTypes != null && source.ContainingTypes.Count > 0 )
        {
            foreach ( var _ in source.ContainingTypes )
            {
                indent--;
                AppendLine( sb, "}", indent );
                
            }
        }
    }

    private static void GenerateNotiFyEventInfrastructure(StringBuilder sb, ClassWithFields source, int indent )
    {
        // Generate the event and OnPropertyChanged method
        AppendLine( sb, "public event Action<System.Object, System.String> PropertyChanged;", indent );
        AppendLine( sb, "", indent );
        AppendLine( sb, "protected virtual void OnPropertyChanged( [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null )", indent );
        AppendLine( sb, "{", indent );
        AppendLine( sb, "if ( PropertyChanged != null )", indent + 1 );
        AppendLine( sb, "{", indent + 1 );
        AppendLine( sb, "PropertyChanged.Invoke(this, propertyName );", indent + 2 );
        AppendLine( sb, "}", indent + 1 );
        AppendLine( sb, "}", indent );
    }

    private static void GenerateProperties( StringBuilder sb, ClassWithFields source, int indent )
    {
        foreach ( var annotatedField in source.Fields )
        {
            var propName = ConvertFieldNameToPropertyName( annotatedField.Name );
            AppendLine( sb, $"public {annotatedField.Type} {propName}", indent );   
            AppendLine( sb, "{", indent );   
            AppendLine( sb, $"get => {annotatedField.Name};", indent + 1 );   
            AppendLine( sb, "set", indent + 1 );   
            AppendLine( sb, "{", indent + 1 );
            AppendLine( sb, $"if (!EqualityComparer<{annotatedField.Type}>.Default.Equals({annotatedField.Name}, value))", indent + 2 );
            AppendLine( sb, "{", indent + 2 );
            AppendLine( sb, $"var oldValue = {annotatedField.Name};", indent + 3 );
            AppendLine( sb, $"On{propName}Changing( oldValue, value );", indent + 3 );
            if( source.IsImplementsNotifyChanging )
                AppendLine( sb, "OnPropertyChanging( );", indent + 3 );
            AppendLine( sb, $"{annotatedField.Name} = value;", indent + 3 );
            AppendLine( sb, $"On{propName}Changed( oldValue, value );", indent + 3 );
            AppendLine( sb, "OnPropertyChanged( );", indent + 3 );
            AppendLine( sb, "}", indent + 2 );
            AppendLine( sb, "}", indent + 1 );   
            AppendLine( sb, "}", indent );   

            AppendLine( sb, $"partial void On{propName}Changing( {annotatedField.Type} oldValue, {annotatedField.Type} newValue );", indent );
            AppendLine( sb, $"partial void On{propName}Changed( {annotatedField.Type} oldValue, {annotatedField.Type} newValue );", indent );
            AppendLine( sb, "", indent );
        }
    }

    private static void Append( StringBuilder sb, string text, int indent = 0 )
    {
        if ( indent > 0 )
            sb.Append( new string( ' ', indent * 4 ) ); 

        sb.Append( text );
    }

    private static void AppendLine( StringBuilder sb, string text, int indent = 0 )
    {
        if ( indent > 0 )
            sb.Append( new string( ' ', indent * 4 ) ); 

        sb.AppendLine( text );
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

    private class AnnotatedField : IEquatable<AnnotatedField>
    {
        public string Name;
        public string Type;
        public string ClassFullName;
        public IFieldSymbol FieldSymbol;    //Stored for future processing, exclude from equality check

        public bool Equals(AnnotatedField other)
        {
            return Name == other.Name && Type == other.Type && ClassFullName == other.ClassFullName;
        }

        public override bool Equals(object? obj)
        {
            return obj is AnnotatedField other && Equals( other );
        }

        public override int GetHashCode( )
        {
            unchecked
            {
                var hashCode = Name.GetHashCode();
                hashCode = (hashCode * 397) ^ Type.GetHashCode();
                hashCode = (hashCode * 397) ^ ClassFullName.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(AnnotatedField left, AnnotatedField right)
        {
            return left.Equals( right );
        }

        public static bool operator !=(AnnotatedField left, AnnotatedField right)
        {
            return !left.Equals( right );
        }
    }

    private class ClassWithFields : IEquatable<ClassWithFields>
    {
        public readonly string          ClassFullName;
        public readonly ImmutableArray<AnnotatedField> Fields;

        public readonly string          ClassNamespace;
        public readonly string          ClassName;
        public readonly string          SourceFilePartName;         //Namespace + type names without file name incorrect symbols
        public readonly List<String>?   ContainingTypes;

        public Boolean IsImplementsNotifyChanging; 
        public readonly INamedTypeSymbol ClassSymbol;

        public ClassWithFields(String classFullName, INamedTypeSymbol classSymbol, ImmutableArray<AnnotatedField> fields )
        {
            ClassFullName = classFullName;
            Fields        = fields;
            ClassSymbol   = classSymbol;
            ClassName     = classSymbol.Name;
            SourceFilePartName = classSymbol.ToDisplayString( new SymbolDisplayFormat( globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                    genericsOptions: SymbolDisplayGenericsOptions.None,
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces ) );
            IsImplementsNotifyChanging = classSymbol.AllInterfaces.Any( i => i.ToDisplayString( ) == INotifyPropertyChangingFullyQualifiedName );

            //Make namespace string
            var ns = classSymbol.ContainingNamespace;
            if ( ns != null && !ns.IsGlobalNamespace )
                ClassNamespace = ns.ToDisplayString( );
            else
                ClassNamespace = string.Empty;

            //make list of containing types (if any)
            var currentType = classSymbol;
            if ( currentType.ContainingType != null )
            {
                var typeStack   = new Stack<string>(); // Use a stack to get outer types first
                while ( currentType.ContainingType != null )
                {
                    typeStack.Push( currentType.ContainingType.ToDisplayString( SymbolDisplayFormat.MinimallyQualifiedFormat ) ); // Just the name of the containing type
                    currentType = currentType.ContainingType;
                }

                if ( typeStack.Count > 0 )
                {
                    ContainingTypes = new List<String>( typeStack.Count );
                    while ( typeStack.Count > 0 )
                    {
                        ContainingTypes.Add( typeStack.Pop() );
                    }
                }
            }

        }

        public bool Equals(ClassWithFields? other)
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
            return Equals( (ClassWithFields) obj );
        }

        public override int GetHashCode( )
        {
            unchecked
            {
                return (ClassFullName.GetHashCode() * 397) ^ Fields.GetHashCode();
            }
        }

        public static bool operator ==(ClassWithFields? left, ClassWithFields? right)
        {
            return Equals( left, right );
        }

        public static bool operator !=(ClassWithFields? left, ClassWithFields? right)
        {
            return !Equals( left, right );
        }
    }
}