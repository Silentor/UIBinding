using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SourceGenSnapshotTester;

public class InitVerify( )
{
    [ModuleInitializer]
    public static void Init() =>
            VerifySourceGenerators.Initialize();
}


public class UnitTest1
{

    [Fact]
    public Task Test1( )
    {
        var source = """
            using System;
            using UIBindings;
            using UIBindings.SourceGen;

            namespace MyNamespace.NS2
            {
                public class ExternalClass<T>
                {
                    public class TestClass : ObservableObject
                    {
                        [property: Obsolete("Use Obs2 instead", false)]
                        //[NotifyPropertyChangedFor(nameof(Obs2))]
                        [NotifyPropertyChangedFor("Obs2", "Obs3")]
                        [ObservableProperty]
                        private int _observableField;

                        [ObservableProperty]
                        private System.String _obs2, _obs3;
                    }
                }
            }
            """;

        return Verify( source );
    }

    static Task  Verify( string source )
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
        IEnumerable<PortableExecutableReference> references = new[]
                                                              {
                                                                      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                                                                      MetadataReference.CreateFromFile(typeof(UIBindings.ObservableObject).Assembly.Location),
                                                                      MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                                                                      MetadataReference.CreateFromFile( Assembly.Load( "netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51" ).Location ),
                                                                      MetadataReference.CreateFromFile( Assembly.Load( "System.Runtime, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" ).Location ),
                                                              };
        CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: "Tests",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ));

        var diagnostics = compilation.GetDiagnostics();

        var generator = new UIBindings.SourceGen.UIBindingsGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the source generator!
        driver = driver.RunGenerators(compilation);

        return Verifier.Verify(driver);
    }
}