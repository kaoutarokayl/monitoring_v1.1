using System.ComponentModel.DataAnnotations.Schema;

namespace KtcWeb.Models.Monitoring;

/// <summary>
/// Model for SqlTableDependency on CurrentStatus table.
/// Excludes problematic columns (XML, etc.) that don't serialize well.
/// </summary>
[Table("CurrentStatus")]
public class CurrentStatusDependency
{
    [Column("client_id")]
    public int ClientId { get; set; }

    [Column("component_id")]
    public short ComponentId { get; set; }

    [Column("property_id")]
    public short PropertyId { get; set; }

    [Column("value_id")]
    public int ValueId { get; set; }

    [Column("numeric_value")]
    public decimal? NumericValue { get; set; }
}

/// <summary>
/// Model for SqlTableDependency on AssetHistory table.
/// </summary>
[Table("AssetHistory")]
public class AssetHistoryDependency
{
    [Column("client_id")]
    public int ClientId { get; set; }

    [Column("timestmp")]
    public DateTime Date { get; set; }

    [Column("component_id")]
    public short ComponentId { get; set; }

    [Column("property_id")]
    public short PropertyId { get; set; }

    [Column("old_value_id")]
    public int OldValueId { get; set; }

    [Column("new_value_id")]
    public int NewValueId { get; set; }

    [Column("old_numeric_value")]
    public decimal? OldNumericValue { get; set; }

    [Column("new_numeric_value")]
    public decimal? NewNumericValue { get; set; }

    [Column("comments")]
    public string Comments { get; set; } = string.Empty;
}

/// <summary>
/// Model for SqlTableDependency on PhysicalCassette table.
/// </summary>
[Table("PhysicalCassette")]
public class PhysicalCassetteDependency
{
    [Column("client_id")]
    public int ClientId { get; set; }

    [Column("cassette_id")]
    public short CassetteId { get; set; }

    [Column("cassette_number")]
    public byte CassetteNumber { get; set; }

    [Column("cash_unit_id")]
    public byte CashUnitId { get; set; }
}

/// <summary>
/// Model for SqlTableDependency on CurrentCashUnitStatus table.
/// </summary>
[Table("CurrentCashUnitStatus")]
public class CurrentCashUnitStatusDependency
{
    [Column("client_id")]
    public int ClientId { get; set; }

    [Column("cash_unit_id")]
    public byte CashUnitId { get; set; }

    [Column("logical_status_id")]
    public byte LogicalStatusId { get; set; }

    [Column("physical_status_id")]
    public byte PhysicalStatusId { get; set; }
}

/// <summary>
/// Minimal model for SqlTableDependency on TransactionData_P.
/// Only includes columns needed for group routing (client_id) and change identification.
/// Excludes complex/unsupported column types.
/// </summary>
[Table("TransactionData_P")]
public class TransactionDataDependency
{
    [Column("client_id")]
    public int ClientId { get; set; }

    [Column("transaction_id")]
    public long TransactionId { get; set; }

    [Column("transaction_timestamp")]
    public DateTime TransactionTimestamp { get; set; }
}

/// <summary>
/// Minimal model for SqlTableDependency on ChequeMedia_P.
/// Only includes columns needed for group routing.
/// </summary>
[Table("ChequeMedia_P")]
public class ChequeMediaDependency
{
    [Column("client_id")]
    public int ClientId { get; set; }

    [Column("media_id")]
    public long MediaId { get; set; }
}
