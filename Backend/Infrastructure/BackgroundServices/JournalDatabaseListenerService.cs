using KtcWeb.Hubs;
using KtcWeb.Models.Monitoring;
using Microsoft.AspNetCore.SignalR;
using TableDependency.SqlClient;
using TableDependency.SqlClient.Base;
using TableDependency.SqlClient.Base.Enums;
using TableDependency.SqlClient.Base.EventArgs;
using ErrorEventArgsT = TableDependency.SqlClient.Base.EventArgs.ErrorEventArgs;

namespace KtcWeb.Infrastructure.BackgroundServices;

/// <summary>
/// Écoute les modifications sur TransactionData_P (journal + transactions)
/// et ChequeMedia_P (video journal) et propage via SignalR.
/// </summary>
public sealed class JournalDatabaseListenerService : IHostedService, IDisposable
{
    private readonly IHubContext<KtcMonitoringHub> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JournalDatabaseListenerService> _logger;

    private SqlTableDependency<TransactionDataDependency>? _transactionDependency;
    private SqlTableDependency<ChequeMediaDependency>? _chequeMediaDependency;

    public JournalDatabaseListenerService(
        IHubContext<KtcMonitoringHub> hubContext,
        IConfiguration configuration,
        ILogger<JournalDatabaseListenerService> logger)
    {
        _hubContext = hubContext;
        _configuration = configuration;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("KtcDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("KtcDb connection string missing; JournalDatabaseListenerService not started.");
            return Task.CompletedTask;
        }

        try
        {
            // Explicit mapper: [Column] EF attributes not recognized by SqlTableDependency with mapper: null.
            // TransactionData_P has complex columns (xml, etc.) — only map safe columns needed for routing.
            var txMapper = new ModelToTableMapper<TransactionDataDependency>();
            txMapper.AddMapping(m => m.ClientId, "client_id");
            txMapper.AddMapping(m => m.TransactionId, "transaction_id");
            txMapper.AddMapping(m => m.TransactionTimestamp, "transaction_timestamp");

            var txUpdateOf = new UpdateOfModel<TransactionDataDependency>();
            txUpdateOf.Add(m => m.ClientId);
            txUpdateOf.Add(m => m.TransactionId);
            txUpdateOf.Add(m => m.TransactionTimestamp);

            _transactionDependency = new SqlTableDependency<TransactionDataDependency>(
                connectionString,
                tableName: "TransactionData_P",
                schemaName: null,
                mapper: txMapper,
                updateOf: txUpdateOf,
                filter: null,
                notifyOn: DmlTriggerType.All,
                executeUserPermissionCheck: true,
                includeOldValues: false);

            _transactionDependency.OnChanged += (sender, e) => OnTransactionChanged(e);
            _transactionDependency.OnError += OnError;
            _transactionDependency.Start();
            _logger.LogInformation("SqlTableDependency started on TransactionData_P table.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SqlTableDependency on TransactionData_P; journal/transaction real-time updates will not be pushed.");
        }

        try
        {
            // ChequeMedia_P has varbinary (media_data) and uniqueidentifier (tx_uuid) columns.
            // Explicit mapper + updateOf restricts SqlTableDependency to only validate the mapped columns.
            var chequeMapper = new ModelToTableMapper<ChequeMediaDependency>();
            chequeMapper.AddMapping(m => m.ClientId, "client_id");
            chequeMapper.AddMapping(m => m.MediaId, "media_id");

            var chequeUpdateOf = new UpdateOfModel<ChequeMediaDependency>();
            chequeUpdateOf.Add(m => m.ClientId);
            chequeUpdateOf.Add(m => m.MediaId);

            _chequeMediaDependency = new SqlTableDependency<ChequeMediaDependency>(
                connectionString,
                tableName: "ChequeMedia_P",
                schemaName: null,
                mapper: chequeMapper,
                updateOf: chequeUpdateOf,
                filter: null,
                notifyOn: DmlTriggerType.All,
                executeUserPermissionCheck: true,
                includeOldValues: false);

            _chequeMediaDependency.OnChanged += (sender, e) => OnChequeMediaChanged(e);
            _chequeMediaDependency.OnError += OnError;
            _chequeMediaDependency.Start();
            _logger.LogInformation("SqlTableDependency started on ChequeMedia_P table.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ChequeMedia_P table not available; video journal real-time updates will not be pushed.");
        }

        return Task.CompletedTask;
    }

    private void OnTransactionChanged(RecordChangedEventArgs<TransactionDataDependency> e)
    {
        if (e.Entity == null) return;
        if (e.ChangeType != ChangeType.Insert && e.ChangeType != ChangeType.Update) return;

        _logger.LogInformation("TransactionData_P notified: {ChangeType}, ClientId={ClientId}, TransactionId={TransactionId}",
            e.ChangeType, e.Entity.ClientId, e.Entity.TransactionId);

        _ = BroadcastTransactionAsync(e.Entity);
    }

    private void OnChequeMediaChanged(RecordChangedEventArgs<ChequeMediaDependency> e)
    {
        if (e.Entity == null) return;
        if (e.ChangeType != ChangeType.Insert && e.ChangeType != ChangeType.Update) return;

        _logger.LogInformation("ChequeMedia_P notified: {ChangeType}, ClientId={ClientId}, MediaId={MediaId}",
            e.ChangeType, e.Entity.ClientId, e.Entity.MediaId);

        _ = BroadcastVideoJournalAsync(e.Entity);
    }

    private async Task BroadcastTransactionAsync(TransactionDataDependency entity)
    {
        try
        {
            var journalGroup = $"client_{entity.ClientId}_journal";
            var transactionGroup = $"client_{entity.ClientId}_transactions";
            var payload = new { clientId = entity.ClientId, transactionId = entity.TransactionId };

            await _hubContext.Clients.Group(journalGroup).SendAsync("ReceiveJournalUpdate", payload);
            await _hubContext.Clients.Group(transactionGroup).SendAsync("ReceiveTransactionUpdate", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR push failed for TransactionData_P update.");
        }
    }

    private async Task BroadcastVideoJournalAsync(ChequeMediaDependency entity)
    {
        try
        {
            var groupName = $"client_{entity.ClientId}_videojournal";
            var payload = new { clientId = entity.ClientId, mediaId = entity.MediaId };

            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveVideoJournalUpdate", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR push failed for ChequeMedia_P update.");
        }
    }

    private void OnError(object? sender, ErrorEventArgsT e)
    {
        _logger.LogError(e.Error, "SqlTableDependency error in JournalDatabaseListenerService");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _transactionDependency?.Stop();
            _transactionDependency?.Dispose();
            _transactionDependency = null;
            _logger.LogInformation("TransactionData_P listener stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping TransactionData_P listener.");
        }

        try
        {
            _chequeMediaDependency?.Stop();
            _chequeMediaDependency?.Dispose();
            _chequeMediaDependency = null;
            _logger.LogInformation("ChequeMedia_P listener stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping ChequeMedia_P listener.");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try
        {
            _transactionDependency?.Stop();
            _transactionDependency?.Dispose();
            _chequeMediaDependency?.Stop();
            _chequeMediaDependency?.Dispose();
        }
        catch
        {
            // ignored
        }

        _transactionDependency = null;
        _chequeMediaDependency = null;
    }
}
