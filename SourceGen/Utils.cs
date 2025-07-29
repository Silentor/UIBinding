using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGen;

public static class Utils
{
    public static string GetSafeHintNameForType( INamedTypeSymbol symbol )
    {
        var result = symbol.ToDisplayString( );
        result = result.Replace( "<", "" ).Replace( ">", "" );
        return result;
    }

    public static string GetNamespaceString( ISymbol symbol )
    {
        var ns = symbol.ContainingNamespace;
        if ( ns != null && !ns.IsGlobalNamespace )
            return ns.ToDisplayString( );
        else
            return string.Empty;
    }

    public static IReadOnlyList<string> GetContainingTypes( INamedTypeSymbol symbol )
    {
        var result   = new List<string>(  );
        var currentType = symbol;

        do
        {
            result.Add( currentType.ToDisplayString( SymbolDisplayFormat.MinimallyQualifiedFormat )  );
            currentType = currentType.ContainingType;
        } while ( currentType != null );

        result.Reverse();
        return result;
    }

    /// <summary>
    /// Get syntax based full hierarchical type name for detect any changes in type hierarchy.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public static string GetFullTypeName( BaseTypeDeclarationSyntax node )
    {
        var syntaxNode = (SyntaxNode)node;

        //Get full hierarchical name
        var classFullName = GetTypeName( node );

        while ( syntaxNode.Parent != null )
        {
            syntaxNode = syntaxNode.Parent;
            var name = syntaxNode switch
                       {
                               BaseTypeDeclarationSyntax bt      => GetTypeName( bt ),
                               BaseNamespaceDeclarationSyntax ns => ns.Name.ToString(),
                               _                                 => string.Empty
                       };
            if( name != string.Empty )
                classFullName = $"{name}.{classFullName}";
        }

        return classFullName;

        string GetTypeName( BaseTypeDeclarationSyntax baseTypeSyntax )
        {
            var name = baseTypeSyntax.Identifier.Text;

            if ( baseTypeSyntax is TypeDeclarationSyntax  typeSyntax )
            {
                //Get also generic type names
                if ( typeSyntax.TypeParameterList != null )
                {
                    name += $"<{string.Join( ", ", typeSyntax.TypeParameterList.Parameters.Select( tp => tp.Identifier.Text ) )}>";
                    //Also get generic constraints if any
                    if ( typeSyntax.ConstraintClauses.Count > 0 )
                    {
                        name += $"({string.Join( ", ", typeSyntax.ConstraintClauses.Select( cc => $"{cc.Name.Identifier.Text} : {string.Join( ", ", cc.Constraints.Select( GetConstraintIdentifier ) )}" ) )})";
                    }
                }

                return name;
            }
            else return name;

            string GetConstraintIdentifier( TypeParameterConstraintSyntax constraintSyntax )
            {
                return constraintSyntax switch
                       {
                               ClassOrStructConstraintSyntax  cos  => cos.ClassOrStructKeyword.Text,
                               ConstructorConstraintSyntax cons    => cons.NewKeyword.Text,
                               //AllowsConstraintClauseSyntax allow  => allow.AllowsKeyword.Text,       not supported by Unity Roslyn version
                               DefaultConstraintSyntax def         => def.DefaultKeyword.Text,
                               TypeConstraintSyntax typeConstraint => typeConstraint.Type.ToString(),
                               _                                   => string.Empty
                       };
            }

        }

    }

    /// <summary>
    /// Get syntax based attributes as string for detect any changes in attributes.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public static string GetAttributesAsString( MemberDeclarationSyntax node )
    {
        return String.Join( "\r\n", node.AttributeLists.Select( al => al.ToString() ) );
    }
}