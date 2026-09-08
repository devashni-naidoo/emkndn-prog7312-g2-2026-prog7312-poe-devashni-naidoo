namespace SmartX.Shared.Validation;

/// <summary>
/// One node in a nested device deployment tree / multi-tier configuration
/// profile, e.g. Facility A -> Zone 1 -> Sub-Zone B -> Node 07.
/// </summary>
public sealed class DeploymentNode
{
    public string Id { get; }
    public string Name { get; }
    public DeploymentLevel Level { get; }
    public List<DeploymentNode> Children { get; } = new();

    public DeploymentNode(string id, string name, DeploymentLevel level)
    {
        Id = id;
        Name = name;
        Level = level;
    }

    public DeploymentNode AddChild(DeploymentNode child)
    {
        Children.Add(child);
        return this;
    }
}
