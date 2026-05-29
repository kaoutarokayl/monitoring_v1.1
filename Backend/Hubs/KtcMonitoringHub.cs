using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace KtcWeb.Hubs;

/// <summary>Hub SignalR pour la supervision temps réel (notifications serveur → clients).</summary>
[Authorize]
public class KtcMonitoringHub : Hub
{
    /// <summary>
    /// Le client rejoigne un groupe pour recevoir les updates d'un ATM spécifique.
    /// Appelé depuis le frontend quand on accède à la page de détail d'un ATM.
    /// </summary>
    public async Task JoinAtmGroup(int clientId)
    {
        var statusGroup = $"client_{clientId}_status";
        var assetGroup = $"client_{clientId}_assets";
        var cassetteGroup = $"client_{clientId}_cassettes";
        var cashStatusGroup = $"client_{clientId}_cashstatus";
        var journalGroup = $"client_{clientId}_journal";
        var transactionGroup = $"client_{clientId}_transactions";
        var videoJournalGroup = $"client_{clientId}_videojournal";

        await Groups.AddToGroupAsync(Context.ConnectionId, statusGroup);
        await Groups.AddToGroupAsync(Context.ConnectionId, assetGroup);
        await Groups.AddToGroupAsync(Context.ConnectionId, cassetteGroup);
        await Groups.AddToGroupAsync(Context.ConnectionId, cashStatusGroup);
        await Groups.AddToGroupAsync(Context.ConnectionId, journalGroup);
        await Groups.AddToGroupAsync(Context.ConnectionId, transactionGroup);
        await Groups.AddToGroupAsync(Context.ConnectionId, videoJournalGroup);
    }

    /// <summary>
    /// Le client quitte les groupes d'un ATM quand il navigue ailleurs.
    /// </summary>
    public async Task LeaveAtmGroup(int clientId)
    {
        var statusGroup = $"client_{clientId}_status";
        var assetGroup = $"client_{clientId}_assets";
        var cassetteGroup = $"client_{clientId}_cassettes";
        var cashStatusGroup = $"client_{clientId}_cashstatus";
        var journalGroup = $"client_{clientId}_journal";
        var transactionGroup = $"client_{clientId}_transactions";
        var videoJournalGroup = $"client_{clientId}_videojournal";

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, statusGroup);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, assetGroup);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, cassetteGroup);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, cashStatusGroup);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, journalGroup);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, transactionGroup);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, videoJournalGroup);
    }
}

