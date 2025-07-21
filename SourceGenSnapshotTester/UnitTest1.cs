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
            using UIBindings;

            namespace MyNamespace
            {
                public class TestClass
                {
                    [ObservableProperty]
                    private int _observableField;

                    [ObservableProperty]
                    private System.String _obs2, _obs3;
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
                                                                      MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
                                                              };
        CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: "Tests",
                syntaxTrees: new[] { syntaxTree },
                references: references );
        var generator = new UIBindings.SourceGen.UIBindingsGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the source generator!
        driver = driver.RunGenerators(compilation);

        return Verifier.Verify(driver);
    }
}