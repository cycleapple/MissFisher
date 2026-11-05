using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using Mono.Cecil;

if (args.Length == 2 && args[0] == "--list-resources")
{
    using var source = AssemblyDefinition.ReadAssembly(args[1]);
    foreach (var listedResource in source.MainModule.Resources)
        Console.WriteLine(listedResource.Name);
    return 0;
}

if (args.Length == 3 && args[0] == "--extract-lang")
{
    using var source = AssemblyDefinition.ReadAssembly(args[1]);
    VerifyApi14(source);
    var language = source.MainModule.Resources.OfType<EmbeddedResource>()
        .Single(x => x.Name == "MissFisher.Resources.Localization.lang.json");
    File.WriteAllBytes(args[2], language.GetResourceData());
    Console.WriteLine("Extracted verified API14 language resource.");
    return 0;
}

if (args.Length != 4 || !Version.TryParse(args[3], out var outputVersion) || outputVersion.Revision < 0)
{
    Console.Error.WriteLine("Usage: MissFisherPatcher <official-api14.dll> <output.dll> <lang.json> <unique-four-part-version>");
    return 2;
}

var input = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
if (input.Equals(output, StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Keep the official input assembly unchanged; choose another output path.");

var referenceDirectory = Environment.GetEnvironmentVariable("DALAMUD_HOME")
    ?? throw new InvalidOperationException("Set DALAMUD_HOME to the verified API14 reference directory.");
if (System.Reflection.AssemblyName.GetAssemblyName(Path.Combine(referenceDirectory, "Dalamud.dll")).Version?.Major != 14)
    throw new InvalidOperationException("DALAMUD_HOME must contain real API14 references.");
using var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(input)!);
resolver.AddSearchDirectory(referenceDirectory);
resolver.AddSearchDirectory(Path.GetDirectoryName(typeof(object).Assembly.Location)!);
using var assembly = AssemblyDefinition.ReadAssembly(input, new ReaderParameters { InMemory = true, AssemblyResolver = resolver });
VerifyApi14(assembly);
var originalCodeHash = CodeHash(assembly);
var originalResources = assembly.MainModule.Resources.OfType<EmbeddedResource>()
    .Where(x => x.Name != "MissFisher.Resources.Localization.lang.json")
    .ToDictionary(x => x.Name, x => Convert.ToHexString(SHA256.HashData(x.GetResourceData())));
if (outputVersion <= assembly.Name.Version)
    throw new InvalidOperationException("The localized package must have a unique version newer than the input.");

const string resourceName = "MissFisher.Resources.Localization.lang.json";
var resource = assembly.MainModule.Resources.OfType<EmbeddedResource>().SingleOrDefault(x => x.Name == resourceName)
    ?? throw new InvalidOperationException($"Missing {resourceName}; review the upstream resource format first.");
var officialLanguage = JsonNode.Parse(resource.GetResourceData()) as JsonObject
    ?? throw new InvalidOperationException("Unsupported upstream language format.");
var translations = JsonNode.Parse(File.ReadAllText(args[2])) as JsonObject
    ?? throw new InvalidOperationException("Unsupported localization format.");
var translated = 0;
foreach (var (key, value) in officialLanguage)
{
    if (value is JsonArray upstream && upstream.Count > 0 &&
        translations[key] is JsonArray localized && localized.Count > 0 && localized[0] is not null)
    {
        // Preserve new API14 keys and the upstream English/Japanese entries.
        upstream[0] = localized[0]!.DeepClone();
        translated++;
    }
}
if (translated == 0)
    throw new InvalidOperationException("No matching translation keys; review the upstream schema before patching.");

var bytes = Encoding.UTF8.GetBytes(officialLanguage.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
var index = assembly.MainModule.Resources.IndexOf(resource);
assembly.MainModule.Resources.RemoveAt(index);
assembly.MainModule.Resources.Insert(index, new EmbeddedResource(resourceName, resource.Attributes, bytes));
assembly.Name.Version = outputVersion;
foreach (var attribute in assembly.CustomAttributes.Where(a =>
             a.AttributeType.FullName is "System.Reflection.AssemblyFileVersionAttribute" or "System.Reflection.AssemblyInformationalVersionAttribute"))
    if (attribute.ConstructorArguments.Count == 1)
        attribute.ConstructorArguments[0] = new CustomAttributeArgument(assembly.MainModule.TypeSystem.String, outputVersion.ToString());
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
assembly.Write(output);

using var verification = AssemblyDefinition.ReadAssembly(output);
var verified = verification.MainModule.Resources.OfType<EmbeddedResource>().Single(x => x.Name == resourceName);
if (verification.Name.Version != outputVersion || !verified.GetResourceData().SequenceEqual(bytes))
    throw new InvalidOperationException("Post-write verification failed.");
var localizedCodeHash = CodeHash(verification);
if (originalCodeHash != localizedCodeHash)
    throw new InvalidOperationException("Executable method bodies changed; refusing the localized package.");
foreach (var (name, hash) in originalResources)
    if (verification.MainModule.Resources.OfType<EmbeddedResource>().Single(x => x.Name == name) is not { } unchanged ||
        Convert.ToHexString(SHA256.HashData(unchanged.GetResourceData())) != hash)
        throw new InvalidOperationException("Non-language resource changed: " + name);
File.WriteAllText(output + ".verification.json", JsonSerializer.Serialize(new
{
    apiLevel = 14, runtimeMajor = 10, version = outputVersion.ToString(),
    translatedKeys = translated, totalKeys = officialLanguage.Count,
    originalCodeHash, localizedCodeHash, unchangedResources = originalResources.Count,
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"API14 verified; translated {translated}/{officialLanguage.Count} keys; version={outputVersion}; verification=passed.");
return 0;

static void VerifyApi14(AssemblyDefinition assembly)
{
    var dalamud = assembly.MainModule.AssemblyReferences.SingleOrDefault(x => x.Name == "Dalamud");
    var runtime = assembly.MainModule.AssemblyReferences.SingleOrDefault(x => x.Name == "System.Runtime");
    if (dalamud?.Version.Major != 14 || runtime?.Version.Major != 10)
        throw new InvalidOperationException($"Requires a real API14/.NET10 input; found Dalamud {dalamud?.Version}, runtime {runtime?.Version}.");
}

static string CodeHash(AssemblyDefinition assembly)
{
    static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> types)
    {
        foreach (var type in types)
        {
            yield return type;
            foreach (var nested in Types(type.NestedTypes)) yield return nested;
        }
    }
    var code = string.Join("\n", Types(assembly.MainModule.Types).SelectMany(type => type.Methods)
        .Where(method => method.HasBody).Select(method => method.FullName + "\n" +
            string.Join("\n", method.Body.Instructions.Select(instruction => instruction.ToString()))));
    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
}
