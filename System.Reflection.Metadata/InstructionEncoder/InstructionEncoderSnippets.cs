using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace InstructionEncoderSnippets
{
    class InstructionEncoderSnippets
    {
        //<SnippetEmitMethodBody>
        // The following code emits method body similar to this C# code:

        /*public static double CalcRectangleArea(double length, double width)
        {
            if (length < 0.0)
            {
                throw new ArgumentOutOfRangeException("length");
            }

            if (width < 0.0)
            {
                throw new ArgumentOutOfRangeException("width");
            }

            return length * width;
        }*/
        
        public static InstructionEncoder EmitMethodBody(MetadataBuilder metadata, AssemblyReferenceHandle corlibAssemblyRef)
        {
            var codeBuilder = new BlobBuilder();
            var encoder = new InstructionEncoder(codeBuilder, new ControlFlowBuilder());

            // Get reference to System.ArgumentOutOfRangeException constructor
            TypeReferenceHandle typeRefHandle = metadata.AddTypeReference(
            corlibAssemblyRef,
            metadata.GetOrAddString("System"),
            metadata.GetOrAddString("ArgumentOutOfRangeException"));

            // Signature: .ctor(string)
            var ctorSignature = new BlobBuilder();

            new BlobEncoder(ctorSignature).
                MethodSignature(isInstanceMethod: true).
                Parameters(1, returnType => returnType.Void(), parameters => parameters.AddParameter().Type().String());

            BlobHandle ctorBlobIndex = metadata.GetOrAddBlob(ctorSignature);

            MemberReferenceHandle ctorMemberRef = metadata.AddMemberReference(
                typeRefHandle,
                metadata.GetOrAddString(".ctor"),
                ctorBlobIndex);

            LabelHandle label1 = encoder.DefineLabel();
            LabelHandle label2 = encoder.DefineLabel();

            // ldarg.0
            encoder.OpCode(ILOpCode.Ldarg_0);

            // ldc.r8 0
            encoder.LoadConstantR8(0);

            // bge.un.s LABEL1
            encoder.Branch(ILOpCode.Bge_un_s, label1);

            // ldstr "length"
            encoder.LoadString(metadata.GetOrAddUserString("length"));

            // newobj instance void [System.Runtime]System.ArgumentOutOfRangeException::.ctor(string)
            encoder.OpCode(ILOpCode.Newobj);
            encoder.Token(ctorMemberRef);

            // throw
            encoder.OpCode(ILOpCode.Throw);

            // LABEL1: ldarg.1
            encoder.MarkLabel(label1);
            encoder.OpCode(ILOpCode.Ldarg_1);

            // ldc.r8 0
            encoder.LoadConstantR8(0);

            // bge.un.s LABEL2
            encoder.Branch(ILOpCode.Bge_un_s, label2);

            // ldstr "width"
            encoder.LoadString(metadata.GetOrAddUserString("width"));

            // newobj instance void [System.Runtime]System.ArgumentOutOfRangeException::.ctor(string)
            encoder.OpCode(ILOpCode.Newobj);
            encoder.Token(ctorMemberRef);

            // throw
            encoder.OpCode(ILOpCode.Throw);

            // LABEL2: ldarg.0
            encoder.MarkLabel(label2);
            encoder.OpCode(ILOpCode.Ldarg_0);

            // ldarg.1
            encoder.OpCode(ILOpCode.Ldarg_1);

            // mul
            encoder.OpCode(ILOpCode.Mul);

            // ret
            encoder.OpCode(ILOpCode.Ret);

            return encoder;
        }
        //</SnippetEmitMethodBody>

        private static readonly Guid s_guid = new Guid("17E5BDE2-2243-5EAD-ABB3-1002F92068A7");
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

            // Create references to System.Object and System.Console types.
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

            // call instance void [mscorlib]System.Object::.ctor()
            il.Call(objectCtorMemberRef);

            // ret
            il.OpCode(ILOpCode.Ret);

            int ctorBodyOffset = methodBodyStream.AddMethodBody(il);
            codeBuilder.Clear();

            // Emit IL for a method
            il = EmitMethodBody(metadata, corlibAssemblyRef);

            int methodBodyOffset = methodBodyStream.AddMethodBody(il);
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
            //double Method(double, double)
            var methodSignature = new BlobBuilder();

            new BlobEncoder(methodSignature).
                MethodSignature().
                Parameters(2, returnType => returnType.Type().Double(),
                parameters => {
                    parameters.AddParameter().Type().Double();
                    parameters.AddParameter().Type().Double();
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
                    "TestMethod", ProduceMethodSignature, new string[] { "length", "width" });

                WritePEImage(peStream, metadataBuilder, ilBuilder, default(MethodDefinitionHandle));
            }

            Console.WriteLine(name);
            Assembly ass = Assembly.LoadFrom(name + ".dll");
            Type t = ass.GetType(name + ".Program");
            MethodInfo mi = t.GetMethod("TestMethod");

            try
            {
                object ret = mi.Invoke(null, new object[] { 2.0, 4.0 });

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
