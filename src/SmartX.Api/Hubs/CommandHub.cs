using Microsoft.AspNetCore.SignalR;

namespace SmartX.Api.Hubs;

/// <summary>
/// Pushes live command results and alerts to every connected dashboard —
/// this is what makes the Real-Time Command Stream page actually real-time
/// rather than poll-based. Server -> client only: clients don't call
/// methods on this hub, they just listen for
/// <c>ReceiveCommandResult</c> and <c>ReceiveAlert</c> broadcasts sent via
/// <see cref="IHubContext{CommandHub}"/> from <see cref="Services.CommandProcessorWorker"/>
/// and <see cref="Services.AlertCoordinator"/>.
/// </summary>
public sealed class CommandHub : Hub
{
}
