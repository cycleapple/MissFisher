using Mono.Cecil;
using Mono.Cecil.Cil;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: MissFisherPatcher <input.dll> <output.dll> <lang.json>");
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

static void ResetBody(MethodDefinition target, params Instruction[] instructions)
{
    target.Body.ExceptionHandlers.Clear();
    target.Body.Variables.Clear();
    target.Body.Instructions.Clear();
    foreach (var instruction in instructions)
        target.Body.Instructions.Add(instruction);
    target.Body.InitLocals = false;
    target.Body.MaxStackSize = instructions.Length;
}

ResetBody(method, Instruction.Create(OpCodes.Ret));

const string licenseProviderTypeName = "sQfyASd0mxNxQpTwRRmb.PGeWdHd0c5pApLeb0V79";
const string licenseEvaluationMethodName = "OQAd0qjSfCd";
var licenseProvider = assembly.MainModule.GetType(licenseProviderTypeName)
    ?? throw new InvalidOperationException($"License provider {licenseProviderTypeName} was not found.");
var licenseEvaluationMethod = licenseProvider.Methods.SingleOrDefault(x =>
        x.Name == licenseEvaluationMethodName &&
        x.Parameters.Count == 1 &&
        x.Parameters[0].ParameterType.FullName == "System.Byte[]")
    ?? throw new InvalidOperationException(
        $"License evaluation method {licenseProviderTypeName}.{licenseEvaluationMethodName} was not found.");
var licenseType = licenseProvider.NestedTypes.SingleOrDefault(x =>
        x.BaseType?.FullName == "System.ComponentModel.License")
    ?? throw new InvalidOperationException("Nested license result type was not found.");
var licenseConstructor = licenseType.Methods.SingleOrDefault(x =>
        x.IsConstructor &&
        !x.IsStatic &&
        x.Parameters.Count == 2 &&
        x.Parameters[0].ParameterType.FullName == "System.Object" &&
        x.Parameters[1].ParameterType.FullName == "System.String")
    ?? throw new InvalidOperationException("Nested license result constructor was not found.");

ResetBody(
    licenseEvaluationMethod,
    Instruction.Create(OpCodes.Ldarg_0),
    Instruction.Create(OpCodes.Ldstr, "MissFisher API13 compatibility build"),
    Instruction.Create(OpCodes.Newobj, licenseConstructor),
    Instruction.Create(OpCodes.Ret));

const string languageResourceName = "MissFisher.Data.Resources.lang.json";
var languageResource = assembly.MainModule.Resources.SingleOrDefault(x => x.Name == languageResourceName)
    ?? throw new InvalidOperationException($"Language resource {languageResourceName} was not found.");
var languageResourceIndex = assembly.MainModule.Resources.IndexOf(languageResource);
assembly.MainModule.Resources.RemoveAt(languageResourceIndex);
assembly.MainModule.Resources.Insert(
    languageResourceIndex,
    new EmbeddedResource(
        languageResourceName,
        languageResource.Attributes,
        File.ReadAllBytes(args[2])));

assembly.Name.Version = new Version(1, 6, 5, 12);
assembly.MainModule.Assembly.Name.Version = assembly.Name.Version;
assembly.Write(args[1]);

using var verificationAssembly = AssemblyDefinition.ReadAssembly(args[1]);
var verifiedExpiryMethod = verificationAssembly.MainModule.GetType(typeName)?.Methods
    .SingleOrDefault(x => x.Name == methodName);
var verifiedLicenseMethod = verificationAssembly.MainModule.GetType(licenseProviderTypeName)?.Methods
    .SingleOrDefault(x =>
        x.Name == licenseEvaluationMethodName &&
        x.Parameters.Count == 1 &&
        x.Parameters[0].ParameterType.FullName == "System.Byte[]");
var verifiedLanguageResource = verificationAssembly.MainModule.Resources
    .OfType<EmbeddedResource>()
    .SingleOrDefault(x => x.Name == languageResourceName);
if (verificationAssembly.Name.Version != new Version(1, 6, 5, 12) ||
    verifiedExpiryMethod?.Body.Instructions.Count != 1 ||
    verifiedExpiryMethod.Body.Instructions[0].OpCode != OpCodes.Ret ||
    verifiedLicenseMethod?.Body.Instructions.Count != 4 ||
    verifiedLicenseMethod.Body.Instructions[2].OpCode != OpCodes.Newobj ||
    verifiedLicenseMethod.Body.Instructions[3].OpCode != OpCodes.Ret ||
    verifiedLanguageResource is null ||
    !verifiedLanguageResource.GetResourceData().SequenceEqual(File.ReadAllBytes(args[2])))
{
    throw new InvalidOperationException("Post-write verification failed.");
}

Console.WriteLine(
    $"Patched {typeName}.{methodName} and " +
    $"{licenseProviderTypeName}.{licenseEvaluationMethodName}; " +
    $"replaced {languageResourceName}; version={assembly.Name.Version}; verification=passed");
return 0;
