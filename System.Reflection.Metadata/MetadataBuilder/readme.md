# Generate .NET Core assembly using MetadataBuilder

This example generates .NET Core 3.1 targeting console app assembly using [System.Reflection.Metadata.Ecma335.MetadataBuilder](https://docs.microsoft.com/en-us/dotnet/api/system.reflection.metadata.ecma335.metadatabuilder). "ConsoleApplication.runtimeconfig.json" file should be copied into the output directory to run the generated file using shared host (`dotnet ConsoleApplication.dll` command).
