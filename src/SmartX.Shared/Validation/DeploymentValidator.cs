namespace SmartX.Shared.Validation;


public static class DeploymentValidator
{

    public static ValidationResult Validate(DeploymentNode root) =>
        ValidateRecursive(root, expectedDepth: 0, ancestry: new List<string>());

    private static ValidationResult ValidateRecursive(
        DeploymentNode node, int expectedDepth, List<string> ancestry)
    {
        var path = new List<string>(ancestry) { $"{node.Level}:{node.Name}" };

        if ((int)node.Level != expectedDepth)
        {
            return ValidationResult.Fail(
                $"'{node.Name}' is tier {node.Level} but appears at depth {expectedDepth} " +
                $"(expected tier {(DeploymentLevel)expectedDepth}). Path so far: {string.Join(" -> ", path)}");
        }


        if (node.Level == DeploymentLevel.Node)
        {
            return ValidationResult.Ok(string.Join(" -> ", path));
        }

        if (node.Children.Count == 0)
        {
            return ValidationResult.Fail(
                $"'{node.Name}' ({node.Level}) has no children — hierarchy is incomplete. " +
                $"Path so far: {string.Join(" -> ", path)}");
        }

        foreach (var child in node.Children)
        {
            var childResult = ValidateRecursive(child, expectedDepth + 1, path);
            if (!childResult.Success)
            {
                return childResult;
            }
        }

        return ValidationResult.Ok(string.Join(" -> ", path) + " (+children)");
    }

    public static ValidationResult ValidateFlat(
        string facility, string zone, string subZone, string nodeId)
    {
        if (string.IsNullOrWhiteSpace(facility) || string.IsNullOrWhiteSpace(zone) ||
            string.IsNullOrWhiteSpace(subZone) || string.IsNullOrWhiteSpace(nodeId))
        {
            return ValidationResult.Fail(
                "Facility, Zone, SubZone and NodeId must all be provided to build a deployment path.");
        }

        var root = new DeploymentNode(facility, facility, DeploymentLevel.Facility)
            .AddChild(new DeploymentNode($"{facility}/{zone}", zone, DeploymentLevel.Zone)
                .AddChild(new DeploymentNode($"{facility}/{zone}/{subZone}", subZone, DeploymentLevel.SubZone)
                    .AddChild(new DeploymentNode($"{facility}/{zone}/{subZone}/{nodeId}", nodeId, DeploymentLevel.Node))));

        return Validate(root);
    }
}
