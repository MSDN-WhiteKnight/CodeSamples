using System;

namespace MethodSignatureSnippets
{    
    class Program
    {
        static void Main(string[] args)
        {
            MethodSignatureSnippets.BuildAssembly1("TestAssembly1");
            MethodSignatureSnippets.BuildAssembly2("TestAssembly2");
            MethodSignatureSnippets.BuildAssembly3("TestAssembly3");
            MethodSignatureSnippets.BuildAssembly4("TestAssembly4");
            Console.WriteLine("Finished");
            Console.ReadLine();
        }
    }
}
