using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata.Ecma335;
using System.Reflection;

namespace MethodSignatureSnippets
{
    static class MethodSignatureSnippets
    {
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
            AssemblyReferenceHandle mscorlibAssemblyRef = metadata.AddAssemblyReference(
                name: metadata.GetOrAddString("System.Runtime"),
                version: new Version(4,0,0,0),
                culture: default(StringHandle),
                publicKeyOrToken: default(BlobHandle),
                flags: default(AssemblyFlags),
                hashValue: default(BlobHandle));

            TypeReferenceHandle systemObjectTypeRef = metadata.AddTypeReference(
                mscorlibAssemblyRef,
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
            il = new InstructionEncoder(codeBuilder);                        
            il.OpCode(ILOpCode.Ldnull);
            il.OpCode(ILOpCode.Throw);

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
                    i+1);
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

        public static BlobBuilder ProduceMethodSignature1(MetadataBuilder metadataBuilder)
        {
            var methodSignature = new BlobBuilder();

            new BlobEncoder(methodSignature).
                MethodSignature().
                Parameters(0, returnType => returnType.Void(), parameters => { });
            
            return methodSignature;
        }

        public static BlobBuilder ProduceMethodSignature2(MetadataBuilder metadataBuilder)
        {
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

        public static BlobBuilder ProduceMethodSignature3(MetadataBuilder metadataBuilder)
        {
            var methodSignature = new BlobBuilder();
            
            AssemblyReferenceHandle mscorlibAssemblyRef = metadataBuilder.AddAssemblyReference(
                name: metadataBuilder.GetOrAddString("System.Threading.Thread"),
                version: new Version(4, 0, 0, 0),
                culture: default(StringHandle),
                publicKeyOrToken: default(BlobHandle),
                flags: default(AssemblyFlags),
                hashValue: default(BlobHandle));

            TypeReferenceHandle typeRef = metadataBuilder.AddTypeReference(
                mscorlibAssemblyRef,
                metadataBuilder.GetOrAddString("System.Threading"),
                metadataBuilder.GetOrAddString("Thread"));

            new BlobEncoder(methodSignature).
                MethodSignature().
                Parameters(1, returnType => returnType.Void(),
                parameters => {
                    parameters.AddParameter().Type().Type(typeRef, false);
                });

            return methodSignature;
        }

        public static BlobBuilder ProduceMethodSignature4(MetadataBuilder metadataBuilder)
        {
            var methodSignature = new BlobBuilder();

            new BlobEncoder(methodSignature).
                MethodSignature().
                Parameters(2, returnType => returnType.Void(),
                parameters => {
                    parameters.AddParameter().Type(isByRef: true).Int32();
                    parameters.AddParameter().Type().SZArray().Int32();
                });

            return methodSignature;
        }

        public static void BuildAssembly1(string name)
        {
            using (var peStream = new FileStream(name+".dll", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                var ilBuilder = new BlobBuilder();
                var metadataBuilder = new MetadataBuilder();
                
                MethodDefinitionHandle entryPoint = EmitMethod(name, metadataBuilder, ilBuilder, 
                    "TestMethod", ProduceMethodSignature1,  new string[] { });

                WritePEImage(peStream, metadataBuilder, ilBuilder, default(MethodDefinitionHandle));
            }

            Console.WriteLine(name);
            Assembly ass = Assembly.LoadFrom(name + ".dll");
            Type t = ass.GetType(name+".Program");
            MethodInfo mi = t.GetMethod("TestMethod");

            try
            {
                object ret = mi.Invoke(null, new object[] { });

                if (ret == null) Console.WriteLine("(null)");
                else Console.WriteLine(ret);
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException is NullReferenceException) Console.WriteLine("(NullReferenceException)");
                else Console.WriteLine(ex.ToString());
            }
        }

        public static void BuildAssembly2(string name)
        {
            using (var peStream = new FileStream(name + ".dll", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                var ilBuilder = new BlobBuilder();
                var metadataBuilder = new MetadataBuilder();

                MethodDefinitionHandle entryPoint = EmitMethod(name, metadataBuilder, ilBuilder,
                    "TestMethod", ProduceMethodSignature2, new string[] { "x", "y" });

                WritePEImage(peStream, metadataBuilder, ilBuilder, default(MethodDefinitionHandle));
            }

            Console.WriteLine(name);
            Assembly ass = Assembly.LoadFrom(name + ".dll");
            Type t = ass.GetType(name + ".Program");
            MethodInfo mi = t.GetMethod("TestMethod");

            try
            {
                object ret = mi.Invoke(null, new object[] { 1.0, 2.0 });

                if (ret == null) Console.WriteLine("(null)");
                else Console.WriteLine(ret);
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException is NullReferenceException) Console.WriteLine("(NullReferenceException)");
                else Console.WriteLine(ex.ToString());
            }
        }

        public static void BuildAssembly3(string name)
        {
            using (var peStream = new FileStream(name + ".dll", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                var ilBuilder = new BlobBuilder();
                var metadataBuilder = new MetadataBuilder();

                MethodDefinitionHandle entryPoint = EmitMethod(name, metadataBuilder, ilBuilder,
                    "TestMethod", ProduceMethodSignature3, new string[] { "thread" });

                WritePEImage(peStream, metadataBuilder, ilBuilder, default(MethodDefinitionHandle));
            }

            Console.WriteLine(name);
            Assembly ass = Assembly.LoadFrom(name + ".dll");
            Type t = ass.GetType(name + ".Program");
            MethodInfo mi = t.GetMethod("TestMethod");

            try
            {
                object ret = mi.Invoke(null, new object[] { null });

                if (ret == null) Console.WriteLine("(null)");
                else Console.WriteLine(ret);
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException is NullReferenceException) Console.WriteLine("(NullReferenceException)");
                else Console.WriteLine(ex.ToString());
            }
        }

        public static void BuildAssembly4(string name)
        {
            using (var peStream = new FileStream(name + ".dll", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                var ilBuilder = new BlobBuilder();
                var metadataBuilder = new MetadataBuilder();

                MethodDefinitionHandle entryPoint = EmitMethod(name, metadataBuilder, ilBuilder,
                    "TestMethod", ProduceMethodSignature4, new string[] { "refParameter", "array" });

                WritePEImage(peStream, metadataBuilder, ilBuilder, default(MethodDefinitionHandle));
            }

            Console.WriteLine(name);
            Assembly ass = Assembly.LoadFrom(name + ".dll");
            Type t = ass.GetType(name + ".Program");
            MethodInfo mi = t.GetMethod("TestMethod");

            try
            {
                object ret = mi.Invoke(null, new object[] { 0 , new int[0] });

                if (ret == null) Console.WriteLine("(null)");
                else Console.WriteLine(ret);
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException is NullReferenceException) Console.WriteLine("(NullReferenceException)");
                else Console.WriteLine(ex.ToString());
            }
        }
    }
}
