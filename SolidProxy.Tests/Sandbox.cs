using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SolidProxy.Tests
{
    public class Sandbox
    {
        public interface A
        {

        }

        public interface B : A
        {

        }

        public class C : A
        {

        }

        [Test]
        public void TestSandbox()
        {
            var options = ScriptOptions.Default.WithReferences(typeof(C).Assembly);
            var code = $@"
return typeof(SolidProxy.Tests.Sandbox.C);
";
            var res = CSharpScript.EvaluateAsync(code, options).Result;
            Assert.AreEqual(typeof(C), res);

            code = @"
namespace SolidProxy.Tests {
    public class D : SolidProxy.Tests.Sandbox.C {
    }
}
";
            var parseOptions = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse, SourceCodeKind.Regular);
            var syntaxTree = CSharpSyntaxTree.ParseText(code, parseOptions);

            var assemblyName = Guid.NewGuid().ToString();
            var compilationOptions = new CSharpCompilationOptions(
               OutputKind.DynamicallyLinkedLibrary,
               optimizationLevel: OptimizationLevel.Debug,
               allowUnsafe: true);

            var coredir = Directory.GetParent(typeof(object).Assembly.Location).FullName;
            var compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                new[] {
                    MetadataReference.CreateFromFile(typeof(Object).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(Path.Combine(coredir, "mscorlib.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(coredir, "System.Runtime.dll")),
                    MetadataReference.CreateFromFile(typeof(C).Assembly.Location)
                },
                compilationOptions);

            var stream = new MemoryStream();
            var emitResult = compilation.Emit(stream);
            Assert.IsTrue(emitResult.Success);
            var a = Assembly.Load(stream.ToArray());
            var t = a.GetTypes().Single(o => o.FullName == "SolidProxy.Tests.D");

        }
    }
}