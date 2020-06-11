// .NET JScript execution example
// Author: MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight)
using System;
using System.Collections.Generic;
using Microsoft.JScript;
using System.CodeDom.Compiler;
using System.Text;
using System.Reflection;

namespace ConsoleApp1
{
    class Program
    {
        static object JsExecute(
            string script, 
            string[] refs,
            string type,
            string func, 
            params object[] args)
        {
            JScriptCodeProvider jsc = new JScriptCodeProvider();
            CompilerParameters parameters = new CompilerParameters( refs, "test.dll", true);
            parameters.GenerateExecutable = false;

            CompilerResults results = jsc.CompileAssemblyFromFile(parameters, new string[] { script });

            if (results.Errors.Count > 0)
            {
                Console.WriteLine("Errors: ");

                foreach (CompilerError err in results.Errors)
                {
                    Console.WriteLine(err.ToString());
                }
                
                return null;
            }

            Assembly ass = results.CompiledAssembly;
            Type c = ass.GetType(type);
            MethodInfo f = c.GetMethod(func);
            return f.Invoke(null, args);
        }

        static void Main(string[] args)
        {
            object res = JsExecute(
                "test.js", 
                new[] { "mscorlib.dll", "System.Core.dll" }, 
                "C",
                "calc_sum",
                new object[] { 1, 2 }
                );

            Console.WriteLine(res);
            Console.Read();
        }

    }

}


