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
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Add the marker attribute
        context.RegisterPostInitializationOutput(static ctx => ctx.AddSource(
                "ObservablePropertyAttribute.g.cs", SourceText.From(AttributesHelper.ObservableProperty, Encoding.UTF8)));

        //Find all fields with the ObservablePropertyAttribute
        IncrementalValuesProvider<AnnotatedField?> observableFields = context.SyntaxProvider
                                                                             .ForAttributeWithMetadataName(
                                                                                      "UIBindings.ObservablePropertyAttribute",
                                                                                      predicate: static (s, _) => true,
                                                                                      transform: static (ctx, _) => GetAnnotatedField( ctx.SemanticModel, ctx.TargetNode, ctx.TargetSymbol ) )
                                                                             .Where( static m => m is not null );

        IncrementalValueProvider<ImmutableArray<AnnotatedField?>> observableFieldsCollection = observableFields.Collect();

        var grouped = observableFieldsCollection.SelectMany( GroupFieldsByClass );

        // Generate source code for each enum found
        context.RegisterSourceOutput(grouped,
                static (spc, source) => Execute(source!, spc));
    }


    

    private static AnnotatedField? GetAnnotatedField( SemanticModel semanticModel, SyntaxNode variableDeclarator, ISymbol symbol )
    {
        //return new AnnotatedField() { Name = fieldDeclaration.Kind().ToString() };

        //Must be when marker attribute is applied to a field
        if (variableDeclarator is not VariableDeclaratorSyntax varDeclarator )
            return null;

        var varDeclaration = (VariableDeclarationSyntax)varDeclarator.Parent;
        var fieldDeclaration = (FieldDeclarationSyntax)varDeclaration.Parent;
        var classDeclaration = (ClassDeclarationSyntax)fieldDeclaration.Parent;
        var namespaceDeclaration = (NamespaceDeclarationSyntax)classDeclaration.Parent;

        var fieldSymbol = (IFieldSymbol)symbol;
        var fieldType = fieldSymbol.Type.ToDisplayString(  );
        var declaredType = (ITypeSymbol)fieldSymbol.ContainingType;
        var classFullName = declaredType.ToDisplayString();
        //var namespaceName = declaredType.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return new AnnotatedField()
               {
                       Name = varDeclarator.Identifier.Text,
                       Type  = fieldType,
                       ClassName  = classFullName,
                       //ClassNameSpace = namespaceName,
               };

        //var fieldSymbol = semanticModel.GetDeclaredSymbol(fieldSyntax.Declaration.Variables.FirstOrDefault());
        //if (fieldSymbol is null /*|| !fieldSymbol.HasAttribute("UIBindings.ObservablePropertyAttribute") */)
            //return default;

        // return new AnnotatedField
        // {
        //     Name = varDeclarator.Declaration.Variables.FirstOrDefault()?.Identifier.Text,
        //     //Type = fieldSymbol.Type.ToDisplayString()
        // };
    }

    private static IEnumerable<ClassWithFields> GroupFieldsByClass(ImmutableArray<AnnotatedField?> fields, CancellationToken token )
    {
        // Perform the grouping operation on the collected ImmutableArray
        return fields.GroupBy( f => f.ClassName )
                     .Select( group => new ClassWithFields( group.Key, group.ToImmutableArray() ) );
    }

    private static void Execute(ClassWithFields source, SourceProductionContext spc )
    {
        var sb = new StringBuilder();

        GenerateNamespace( sb, source );
        spc.AddSource( $"{source.ClassFullName}.g.cs", sb.ToString() );

        //
        //
        // sb.AppendLine( $"//namespace {source.ClassNamespace}" );
        // sb.AppendLine( $"//partial class {source.ClassName}" );
        // foreach ( var annotatedField in source.Fields )
        // {
        //     sb.Append( "//" );
        //     sb.Append( annotatedField.Type );
        //     sb.Append( " " );
        //     var propName = ConvertFieldNameToPropertyName( annotatedField.Name );
        //     sb.Append( propName );
        //     sb.AppendLine();
        //     //sb.AppendLine( annotatedField.SymbolType );
        // }
        
        
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
        AppendLine( sb, $"""[System.CodeDom.Compiler.GeneratedCode("UIBindings.SourceGen", "{Assembly.GetExecutingAssembly().GetName().Version}")]""", indent: 1 );
        AppendLine( sb, $"partial class {source.ClassName} : UIBindings.INotifyPropertyChanged", indent: 1 );
        AppendLine( sb, "{", indent: 1 );
        GenerateProperties( sb, source );

        GenerateNotiFyEvent( sb, source );

        AppendLine( sb, "}", indent: 1 );
    }

    private static void GenerateNotiFyEvent(StringBuilder sb, ClassWithFields source )
    {
        AppendLine( sb, "public event Action<System.Object, System.String> PropertyChanged;", 2 );
        AppendLine( sb, "", 2 );
        AppendLine( sb, "protected virtual void OnPropertyChanged( [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null )", 2 );
        AppendLine( sb, "{", 2 );
        AppendLine( sb, "if ( PropertyChanged != null )", 3 );
        AppendLine( sb, "{", 3 );
        AppendLine( sb, "PropertyChanged.Invoke(this, propertyName );", 4 );
        AppendLine( sb, "}", 3 );
        AppendLine( sb, "}", 2 );
    }

    private static void GenerateProperties( StringBuilder sb, ClassWithFields source )
    {
        foreach ( var annotatedField in source.Fields )
        {
            var propName = ConvertFieldNameToPropertyName( annotatedField.Name );
            AppendLine( sb, $"public {annotatedField.Type} {propName}", 2 );   
            AppendLine( sb, "{", 2 );   
            AppendLine( sb, $"get => {annotatedField.Name};", 3 );   
            AppendLine( sb, "set", 3 );   
            AppendLine( sb, "{", 3 );
            AppendLine( sb, $"if (!EqualityComparer<{annotatedField.Type}?>.Default.Equals({annotatedField.Name}, value))", 4 );
            AppendLine( sb, "{", 4 );
            AppendLine( sb, $"var oldValue = {annotatedField.Name};", 5 );
            AppendLine( sb, $"On{propName}Changing( oldValue, value );", 5 );
            AppendLine( sb, $"{annotatedField.Name} = value;", 5 );
            AppendLine( sb, $"On{propName}Changed( oldValue, value );", 5 );
            AppendLine( sb, "OnPropertyChanged( );", 5 );
            AppendLine( sb, "}", 4 );
            AppendLine( sb, "}", 3 );   
            AppendLine( sb, "}", 2 );   

            AppendLine( sb, $"partial void On{propName}Changing( {annotatedField.Type} oldValue, {annotatedField.Type} newValue );", 2 );
            AppendLine( sb, $"partial void On{propName}Changed( {annotatedField.Type} oldValue, {annotatedField.Type} newValue );", 2 );
            AppendLine( sb, "", 2 );
        }
    }

    private static void AppendLine( StringBuilder sb, string text, int indent = 0 )
    {
        if ( indent > 0 )
            sb.Append( new string( ' ', indent * 4 ) ); 

        sb.AppendLine( text );
    }

    private static string ConvertFieldNameToPropertyName( string fieldName )
    {
        // Convert field name to property name by removing the leading underscore
        if (fieldName.StartsWith( "_" ))
            return $"{fieldName[1].ToString().ToUpper()}{fieldName.Substring( 2 )}";

        return fieldName;
    }

    private record AnnotatedField
    {
        public string Name;
        public string Type;
        public string ClassNameSpace;
        public string ClassName;
        public IFieldSymbol FieldSymbol;
    }

    private record ClassWithFields
    {
        public string ClassFullName;
        public string ClassNamespace => ClassFullName.Substring( 0, ClassFullName.LastIndexOf( '.' ) );
        public string ClassName => ClassFullName.Substring( ClassFullName.LastIndexOf( '.' ) + 1 );
        public ImmutableArray<AnnotatedField> Fields;

        public ClassWithFields(String classFullName, ImmutableArray<AnnotatedField> fields )
        {
            ClassFullName = classFullName;
            Fields = fields;
        }
    }
}