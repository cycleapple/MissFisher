using Mono.Cecil;
using Mono.Cecil.Cil;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: MissFisherPatcher <input.dll> <output.dll>");
    return 2;
}

var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0]))!);
resolver.AddSearchDirectory(@"E:\FFXIV\XIVLauncher\api13-work\dalamud");

using var assembly = AssemblyDefinition.ReadAssembly(args[0], new ReaderParameters
{
    InMemory = true,
    ReadWrite = false,
    AssemblyResolver = resolver,
});

const string typeName = "LU6xQkdhMoRovnEAwiMI.tmZyNDdhGONAZuroAyGn";
const string methodName = "m07denkVnu1";
var type = assembly.MainModule.GetType(typeName)
    ?? throw new InvalidOperationException($"Protection type {typeName} was not found.");
var method = type.Methods.SingleOrDefault(x => x.Name == methodName)
    ?? throw new InvalidOperationException($"Protection method {typeName}.{methodName} was not found.");

method.Body.ExceptionHandlers.Clear();
method.Body.Variables.Clear();
method.Body.Instructions.Clear();
method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
method.Body.InitLocals = false;
method.Body.MaxStackSize = 0;

assembly.Name.Version = new Version(1, 6, 5, 10);
assembly.MainModule.Assembly.Name.Version = assembly.Name.Version;
assembly.Write(args[1]);

Console.WriteLine($"Patched {typeName}.{methodName}; version={assembly.Name.Version}");
return 0;
