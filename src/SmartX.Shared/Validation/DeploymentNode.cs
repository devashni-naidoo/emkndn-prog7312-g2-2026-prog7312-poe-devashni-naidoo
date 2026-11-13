namespace SmartX.Shared.Validation;

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
