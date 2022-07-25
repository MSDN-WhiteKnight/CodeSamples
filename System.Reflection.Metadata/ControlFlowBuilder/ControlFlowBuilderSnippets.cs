using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ControlFlowBuilderSnippets
{
    public class ControlFlowBuilderSnippets
    {
        //<SnippetControlFlowBuilder>
        // The following code emits method body similar to this C# code:

        /* public static void ExceptionBlockTest(int x, int y)
           {
              try
              {
                 Console.WriteLine(x / y);
              }
              catch (DivideByZeroException)
              {
                 Console.WriteLine("Error: division by zero");
              }
           }
        */

        public static InstructionEncoder ControlFlowBuilderDemo(MetadataBuilder metadata, 
            AssemblyReferenceHandle corlibAssemblyRef)
        {
            var codeBuilder = new BlobBuilder();
            var flowBuilder = new ControlFlowBuilder();
            var encoder = new InstructionEncoder(codeBuilder, flowBuilder);

            // Get reference to System.Console
            AssemblyReferenceHandle systemConsoleAssemblyRef = metadata.AddAssemblyReference(
                name: metadata.GetOrAddString("System.Console"),
                version: new Version(4, 0, 0, 0),
                culture: default(StringHandle),
                publicKeyOrToken: default(BlobHandle),
                flags: default(AssemblyFlags),
                hashValue: default(BlobHandle));
            
            TypeReferenceHandle consoleTypeRefHandle = metadata.AddTypeReference(
                systemConsoleAssemblyRef,
                metadata.GetOrAddString("System"),
                metadata.GetOrAddString("Console"));

            // Get reference to void System.Console::WriteLine(int32)
            var methodSignature = new BlobBuilder();

            new BlobEncoder(methodSignature).
                MethodSignature(isInstanceMethod: false).
                Parameters(1, returnType => returnType.Void(), parameters => parameters.AddParameter().Type().Int32());

            BlobHandle sigBlobIndex1 = metadata.GetOrAddBlob(methodSignature);

            MemberReferenceHandle refWriteLineInt32 = metadata.AddMemberReference(
                consoleTypeRefHandle,
                metadata.GetOrAddString("WriteLine"),
                sigBlobIndex1);

            // Get reference to void System.Console::WriteLine(string)
            methodSignature = new BlobBuilder();

            new BlobEncoder(methodSignature).
                MethodSignature(isInstanceMethod: false).
                Parameters(1, returnType => returnType.Void(), parameters => parameters.AddParameter().Type().String());

            BlobHandle sigBlobIndex2 = metadata.GetOrAddBlob(methodSignature);

            MemberReferenceHandle refWriteLineString = metadata.AddMemberReference(
                consoleTypeRefHandle,
                metadata.GetOrAddString("WriteLine"),
                sigBlobIndex2);

            // Get reference to System.DivideByZeroException

            TypeReferenceHandle exceptionTypeRefHandle = metadata.AddTypeReference(
                corlibAssemblyRef,
                metadata.GetOrAddString("System"),
                metadata.GetOrAddString("DivideByZeroException"));
            
            LabelHandle labelTryStart = encoder.DefineLabel();
            LabelHandle labelTryEnd = encoder.DefineLabel();
            LabelHandle labelCatchStart = encoder.DefineLabel();
            LabelHandle labelCatchEnd = encoder.DefineLabel();
            LabelHandle labelExit = encoder.DefineLabel();

            flowBuilder.AddCatchRegion(labelTryStart, labelTryEnd, labelCatchStart, labelCatchEnd,
                exceptionTypeRefHandle);

            // .try {
            encoder.MarkLabel(labelTryStart);

            // TRY_START: ldarg.0
            encoder.OpCode(ILOpCode.Ldarg_0);

            // ldarg.1
            encoder.OpCode(ILOpCode.Ldarg_1);

            // div
            encoder.OpCode(ILOpCode.Div);

            // call void [System.Console]System.Console::WriteLine(int32)
            encoder.Call(refWriteLineInt32);

            // TRY_END: leave.s EXIT            
            encoder.Branch(ILOpCode.Leave_s, labelExit);
            encoder.MarkLabel(labelTryEnd);

            // } 
            // catch [System.Runtime]System.DivideByZeroException {
            encoder.MarkLabel(labelCatchStart);

            // CATCH_START: pop
            encoder.OpCode(ILOpCode.Pop);
            
            // ldstr "Error: division by zero"
            encoder.LoadString(metadata.GetOrAddUserString("Error: division by zero"));

            // call void [System.Console]System.Console::WriteLine(string)
            encoder.Call(refWriteLineString);

            // CATCH_END: leave.s EXIT            
            encoder.Branch(ILOpCode.Leave_s, labelExit);
            encoder.MarkLabel(labelCatchEnd);

            // } EXIT: ret
            encoder.MarkLabel(labelExit);
            encoder.OpCode(ILOpCode.Ret);
            
            return encoder;
        }
        //</SnippetControlFlowBuilder>

        private static readonly Guid s_guid = new Guid("5D331769-C0A7-4B85-A46B-18006808F003");
        private static readonly BlobContentId s_contentId = new BlobContentId(s_guid, 0x04030201);

        private static MethodDefinitionHandle EmitMethod(string assemblyName,
            MetadataBuilder metadata,
            BlobBuilder ilBuilder,
            string name,
            Func<MetadataBuilder, BlobBuilder> signatureCallback,
            string[] paramNames)
        {
            BlobBuilder methodSignature = signatureCallback(metadata);

            // Create module and assembly
            metadata.AddModule(
                0,
                metadata.GetOrAddString(assemblyName + ".dll"),
                metadata.GetOrAddGuid(s_guid),
                default(GuidHandle),
                default(GuidHandle));

            metadata.AddAssembly(
                metadata.GetOrAddString(assemblyName),
                version: new Version(1, 0, 0, 0),
                culture: default(StringHandle),
                publicKey: default(BlobHandle),
                flags: 0,
                hashAlgorithm: AssemblyHashAlgorithm.None);

            // Create reference to System.Object
            AssemblyReferenceHandle corlibAssemblyRef = metadata.AddAssemblyReference(
                name: metadata.GetOrAddString("System.Runtime"),
                version: new Version(4, 0, 0, 0),
                culture: default(StringHandle),
                publicKeyOrToken: default(BlobHandle),
                flags: default(AssemblyFlags),
                hashValue: default(BlobHandle));

            TypeReferenceHandle systemObjectTypeRef = metadata.AddTypeReference(
                corlibAssemblyRef,
                metadata.GetOrAddString("System"),
                metadata.GetOrAddString("Object"));

            // Get reference to Object's constructor.
            var parameterlessCtorSignature = new BlobBuilder();

            new BlobEncoder(parameterlessCtorSignature).
                MethodSignature(isInstanceMethod: true).
                Parameters(0, returnType => returnType.Void(), parameters => { });

            BlobHandle parameterlessCtorBlobIndex = metadata.GetOrAddBlob(parameterlessCtorSignature);

            MemberReferenceHandle objectCtorMemberRef = metadata.AddMemberReference(
                systemObjectTypeRef,
                metadata.GetOrAddString(".ctor"),
                parameterlessCtorBlobIndex);

            var methodBodyStream = new MethodBodyStreamEncoder(ilBuilder);

            var codeBuilder = new BlobBuilder();
            InstructionEncoder il;

            // Emit IL for Program::.ctor
            il = new InstructionEncoder(codeBuilder);

            // ldarg.0
            il.LoadArgument(0);

            // call instance void System.Object::.ctor()
            il.Call(objectCtorMemberRef);

            // ret
            il.OpCode(ILOpCode.Ret);

            int ctorBodyOffset = methodBodyStream.AddMethodBody(il);
            codeBuilder.Clear();

            // Emit IL and exception blocks for a method using InstructionEncoder and ControlFlowBuilder
            il = ControlFlowBuilderDemo(metadata, corlibAssemblyRef);

            int methodBodyOffset = methodBodyStream.AddMethodBody(instructionEncoder: il);
            
            codeBuilder.Clear();

            // Create parameters for a method definition
            int nextParameterIndex = 1;
            ParameterHandle pFirst = default(ParameterHandle);

            for (int i = 0; i < paramNames.Length; i++)
            {
                ParameterHandle p = metadata.AddParameter(
                    ParameterAttributes.None,
                    metadata.GetOrAddString(paramNames[i]),
                    i + 1);
                nextParameterIndex++;

                if (i == 0) pFirst = p;
            }

            // Create method definition
            MethodDefinitionHandle methodDef = metadata.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                MethodImplAttributes.IL,
                metadata.GetOrAddString(name),
                metadata.GetOrAddBlob(methodSignature),
                methodBodyOffset,
                parameterList: pFirst);

            // Create method definition for Program::.ctor
            MethodDefinitionHandle ctorDef = metadata.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                MethodImplAttributes.IL,
                metadata.GetOrAddString(".ctor"),
                parameterlessCtorBlobIndex,
                ctorBodyOffset,
                parameterList: MetadataTokens.ParameterHandle(nextParameterIndex));

            // Create type definition for the special <Module> type that holds global functions
            metadata.AddTypeDefinition(
                default(TypeAttributes),
                default(StringHandle),
                metadata.GetOrAddString("<Module>"),
                baseType: default(EntityHandle),
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: MetadataTokens.MethodDefinitionHandle(1));

            // Create type definition for ConsoleApplication.Program
            metadata.AddTypeDefinition(
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit,
                metadata.GetOrAddString(assemblyName),
                metadata.GetOrAddString("Program"),
                baseType: systemObjectTypeRef,
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: methodDef);

            return methodDef;
        }

        private static void WritePEImage(
            Stream peStream,
            MetadataBuilder metadataBuilder,
            BlobBuilder ilBuilder,
            MethodDefinitionHandle entryPointHandle
            )
        {
            // Create executable with the managed metadata from the specified MetadataBuilder.
            var peHeaderBuilder = new PEHeaderBuilder(
                imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll
                );

            var peBuilder = new ManagedPEBuilder(
                peHeaderBuilder,
                new MetadataRootBuilder(metadataBuilder),
                ilBuilder,
                entryPoint: entryPointHandle,
                flags: CorFlags.ILOnly,
                deterministicIdProvider: content => s_contentId);

            // Write executable into the specified stream.
            var peBlob = new BlobBuilder();
            BlobContentId contentId = peBuilder.Serialize(peBlob);
            peBlob.WriteContentTo(peStream);
        }

        public static BlobBuilder ProduceMethodSignature(MetadataBuilder metadataBuilder)
        {
            //void Method(int, int)
            var methodSignature = new BlobBuilder();

            new BlobEncoder(methodSignature).
                MethodSignature().
                Parameters(2, returnType => returnType.Void(),
                parameters => {
                    parameters.AddParameter().Type().Int32();
                    parameters.AddParameter().Type().Int32();
                });

            return methodSignature;
        }

        public static void BuildAssembly(string name)
        {
            using (var peStream = new FileStream(name + ".dll", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                var ilBuilder = new BlobBuilder();
                var metadataBuilder = new MetadataBuilder();

                MethodDefinitionHandle entryPoint = EmitMethod(name, metadataBuilder, ilBuilder,
                    "TestMethod", ProduceMethodSignature, new string[] { "x", "y" });

                WritePEImage(peStream, metadataBuilder, ilBuilder, default(MethodDefinitionHandle));
            }

            Console.WriteLine(name);
            Assembly ass = Assembly.LoadFrom(name + ".dll");
            Type t = ass.GetType(name + ".Program");
            MethodInfo mi = t.GetMethod("TestMethod");

            try
            {
                object ret = mi.Invoke(null, new object[] { 4, 2 });

                if (ret == null) Console.WriteLine("(null)");
                else Console.WriteLine(ret);
            }
            catch (TargetInvocationException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static void Run()
        {
            BuildAssembly("Test");
        }
    }
}
