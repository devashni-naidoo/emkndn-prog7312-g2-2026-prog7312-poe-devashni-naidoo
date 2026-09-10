using SmartX.Shared.Validation;

namespace SmartX.Tests;

internal static class DeploymentValidatorTests
{
    public static void Run()
    {
        Console.WriteLine("DeploymentValidator (recursion)");

        var valid = DeploymentValidator.ValidateFlat("Facility A", "Zone 1", "Sub-Zone B", "Node-07");
        Check.That(valid.Success, "Facility A -> Zone 1 -> Sub-Zone B -> Node-07 validates successfully");

        var missingField = DeploymentValidator.ValidateFlat("Facility A", "Zone 1", "", "Node-07");
        Check.That(!missingField.Success, "an empty Sub-Zone is rejected before recursion even starts");

        // Build a tree with a deliberately wrong tier ordering to exercise the
        // recursive depth check directly (not just via the flat convenience path).
        var badTree = new DeploymentNode("f1", "Facility A", DeploymentLevel.Facility)
            .AddChild(new DeploymentNode("z1", "Zone 1", DeploymentLevel.Zone)
                // Node placed directly under Zone, skipping Sub-Zone.
                .AddChild(new DeploymentNode("n1", "Node-99", DeploymentLevel.Node)));

        var badResult = DeploymentValidator.Validate(badTree);
        Check.That(!badResult.Success, "a Node nested directly under a Zone (skipping Sub-Zone) is rejected");

        // A Zone with no children at all should also fail (incomplete hierarchy).
        var incompleteTree = new DeploymentNode("f2", "Facility B", DeploymentLevel.Facility)
            .AddChild(new DeploymentNode("z2", "Zone 9", DeploymentLevel.Zone));

        var incompleteResult = DeploymentValidator.Validate(incompleteTree);
        Check.That(!incompleteResult.Success, "a Zone with no children is rejected as an incomplete hierarchy");

        // A wider tree: Facility with two valid Zone branches should still pass.
        var wideTree = new DeploymentNode("f3", "Facility C", DeploymentLevel.Facility)
            .AddChild(new DeploymentNode("z3a", "Zone 1", DeploymentLevel.Zone)
                .AddChild(new DeploymentNode("sz3a", "Sub-Zone A", DeploymentLevel.SubZone)
                    .AddChild(new DeploymentNode("n3a", "Node-01", DeploymentLevel.Node))))
            .AddChild(new DeploymentNode("z3b", "Zone 2", DeploymentLevel.Zone)
                .AddChild(new DeploymentNode("sz3b", "Sub-Zone B", DeploymentLevel.SubZone)
                    .AddChild(new DeploymentNode("n3b", "Node-02", DeploymentLevel.Node))));

        var wideResult = DeploymentValidator.Validate(wideTree);
        Check.That(wideResult.Success, "a facility with multiple valid zone branches validates via recursion into each branch");
    }
}
