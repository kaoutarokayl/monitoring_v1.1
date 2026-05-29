using System.ComponentModel.DataAnnotations.Schema;

namespace KtcWeb.Models.Monitoring;

/// <summary>
/// Ligne dbo.Clients pour SqlTableDependency : doit refléter les colonnes réelles de la table
/// (pas le DTO avec jointure HardwareTypes).
/// </summary>
[Table("Clients", Schema = "dbo")]
public class ClientAtm
{
    [Column("client_id")]
    public int ClientId { get; set; }

    /// <summary>Type SQL typique : uniqueidentifier avec NEWID().</summary>
    [Column("ktcguid")]
    public Guid KtcGuid { get; set; }

    [Column("connectable")]
    public byte Connectable { get; set; }

    [Column("networkaddress")]
    public string NetworkAddress { get; set; } = string.Empty;

    [Column("clientname")]
    public string ClientName { get; set; } = string.Empty;

    [Column("detailsunknown")]
    public bool DetailsUnknown { get; set; }

    [Column("latitude")]
    public double Latitude { get; set; }

    [Column("longitude")]
    public double Longitude { get; set; }

    [Column("timezone")]
    public string Timezone { get; set; } = string.Empty;

    /// <summary>Colonne XML en base ; le trigger renvoie souvent une représentation texte.</summary>
    [Column("comments")]
    public string? Comments { get; set; }

    [Column("clienttype")]
    public byte ClientType { get; set; }

    [Column("gridposition")]
    public int? GridPosition { get; set; }

    [Column("business_id")]
    public short BusinessId { get; set; }

    [Column("branch_id")]
    public short BranchId { get; set; }

    [Column("hardwaretype_id")]
    public short HardwareTypeId { get; set; }

    [Column("owner_id")]
    public int OwnerId { get; set; }

    [Column("deletelater")]
    public bool DeleteLater { get; set; }

    [Column("active")]
    public bool Active { get; set; }

    [Column("subnet")]
    public string? Subnet { get; set; }

    [Column("level1_region_id")]
    public int? Level1RegionId { get; set; }

    [Column("level2_region_id")]
    public int? Level2RegionId { get; set; }

    [Column("level3_region_id")]
    public int? Level3RegionId { get; set; }

    [Column("level4_region_id")]
    public int? Level4RegionId { get; set; }

    [Column("level5_region_id")]
    public int? Level5RegionId { get; set; }

    /// <summary>Colonnes présentes sur votre INSERT (filtre suppression / purge).</summary>
    [Column("deleted_timestamp")]
    public DateTime? DeletedTimestamp { get; set; }

    [Column("salt")]
    public byte[]? Salt { get; set; }

    [Column("authhash")]
    public byte[]? AuthHash { get; set; }

    [Column("hypervisor_active")]
    public bool HypervisorActive { get; set; }

    [Column("mergeto_client_id")]
    public int? MergeToClientId { get; set; }

    [Column("feature_flags")]
    public int? FeatureFlags { get; set; }
}

/// <summary>
/// Version de ClientAtm sans la colonne XML Comments pour SqlTableDependency.
/// </summary>
[Table("Clients", Schema = "dbo")]
public class ClientAtmDependency
{
    [Column("client_id")]
    public int ClientId { get; set; }

    [Column("ktcguid")]
    public Guid KtcGuid { get; set; }

    [Column("connectable")]
    public byte Connectable { get; set; }

    [Column("networkaddress")]
    public string NetworkAddress { get; set; } = string.Empty;

    [Column("clientname")]
    public string ClientName { get; set; } = string.Empty;

    [Column("detailsunknown")]
    public bool DetailsUnknown { get; set; }

    [Column("latitude")]
    public double Latitude { get; set; }

    [Column("longitude")]
    public double Longitude { get; set; }

    [Column("timezone")]
    public string Timezone { get; set; } = string.Empty;

    // Skip Comments (XML column not supported by SqlTableDependency)

    [Column("clienttype")]
    public byte ClientType { get; set; }

    [Column("gridposition")]
    public int? GridPosition { get; set; }

    [Column("business_id")]
    public short BusinessId { get; set; }

    [Column("branch_id")]
    public short BranchId { get; set; }

    [Column("hardwaretype_id")]
    public short HardwareTypeId { get; set; }

    [Column("owner_id")]
    public int OwnerId { get; set; }

    [Column("deletelater")]
    public bool DeleteLater { get; set; }

    [Column("active")]
    public bool Active { get; set; }

    [Column("subnet")]
    public string? Subnet { get; set; }

    [Column("level1_region_id")]
    public int? Level1RegionId { get; set; }

    [Column("level2_region_id")]
    public int? Level2RegionId { get; set; }

    [Column("level3_region_id")]
    public int? Level3RegionId { get; set; }

    [Column("level4_region_id")]
    public int? Level4RegionId { get; set; }

    [Column("level5_region_id")]
    public int? Level5RegionId { get; set; }

    [Column("deleted_timestamp")]
    public DateTime? DeletedTimestamp { get; set; }

    [Column("salt")]
    public byte[]? Salt { get; set; }

    [Column("authhash")]
    public byte[]? AuthHash { get; set; }

    [Column("hypervisor_active")]
    public bool HypervisorActive { get; set; }

    [Column("mergeto_client_id")]
    public int? MergeToClientId { get; set; }

    [Column("feature_flags")]
    public int? FeatureFlags { get; set; }
}
