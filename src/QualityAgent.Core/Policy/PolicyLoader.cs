using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace QualityAgent.Core.Policy;

public static class PolicyLoader
{
    public static PolicyModel Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Policy file not found: {path}");

        var yaml = File.ReadAllText(path);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var model = deserializer.Deserialize<PolicyModel>(yaml) ?? new PolicyModel();

        if (model.Version != 1)
            throw new InvalidOperationException($"Unsupported policy version: {model.Version}");

        return model;
    }
}
