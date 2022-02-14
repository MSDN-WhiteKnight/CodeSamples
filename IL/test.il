.assembly TestAssembly {}

.assembly extern System.Runtime {}
.assembly extern System.Console {}

.class private auto ansi beforefieldinit Program extends [System.Runtime]System.Object
{
    .method public static void Main() cil managed
    {
         .entrypoint
         .maxstack 1
        ldstr "Hello, world!"
        call void [System.Console]System.Console::WriteLine(string)
        call int32 [System.Console]System.Console::Read()
        pop
        ret
    }
}
