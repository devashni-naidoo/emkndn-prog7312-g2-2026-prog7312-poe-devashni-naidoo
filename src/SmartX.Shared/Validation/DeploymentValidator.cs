namespace SmartX.Shared.Validation;

/// <summary>
/// Recursively parses and validates a nested device deployment tree /
/// multi-tier configuration profile, confirming that every node is correctly
/// nested within its expected parent chain (Facility -> Zone -> Sub-Zone ->
/// Node) with no missing tiers and no empty branches.
/// </summary>
public static class DeploymentValidator
{
    /// <summary>Entry point: validates an entire tree starting at its root.</summary>
    public static ValidationResult Validate(DeploymentNode root) =>
        ValidateRecursive(root, expectedDepth: 0, ancestry: new List<string>());

    /// <summary>
    /// Recursive case: checks the current node sits at the depth its
    /// <see cref="DeploymentLevel"/> demands, then recurses into each child
    /// one level deeper. Base case: a Node-level tier with a valid ancestry
    /// chain terminates the recursion successfully; a level with no children
    /// before reaching Node terminates it with a failure.
    /// </summary>
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

        // Base case: reached a leaf Node tier — the whole ancestry chain
        // above it has already been confirmed correct by the recursive calls
        // that got us here, so this branch of the tree is valid.
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

        // Recursive case: descend into every child one tier deeper.
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

    /// <summary>
    /// Convenience builder used by the API to turn a flat registration
    /// request (Facility, Zone, SubZone, NodeId strings) into the nested
    /// <see cref="DeploymentNode"/> tree the recursive validator expects,
    /// then validates it in one call.
    /// </summary>
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
