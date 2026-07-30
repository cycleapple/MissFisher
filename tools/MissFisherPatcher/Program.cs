using Mono.Cecil;

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

assembly.Name.Version = new Version(1, 6, 5, 13);
assembly.MainModule.Assembly.Name.Version = assembly.Name.Version;
assembly.Write(args[1]);

using var verificationAssembly = AssemblyDefinition.ReadAssembly(args[1]);
var verifiedLanguageResource = verificationAssembly.MainModule.Resources
    .OfType<EmbeddedResource>()
    .SingleOrDefault(x => x.Name == languageResourceName);
if (verificationAssembly.Name.Version != new Version(1, 6, 5, 13) ||
    verifiedLanguageResource is null ||
    !verifiedLanguageResource.GetResourceData().SequenceEqual(File.ReadAllBytes(args[2])))
{
    throw new InvalidOperationException("Post-write verification failed.");
}

Console.WriteLine(
    $"Replaced {languageResourceName}; " +
    $"version={assembly.Name.Version}; verification=passed");
return 0;
