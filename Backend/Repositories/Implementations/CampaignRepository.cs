using KtcWeb.Application.DTOs;
using KtcWeb.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KtcWeb.Infrastructure.Repositories
{
    public class CampaignRepository : ICampaignRepository
    {
        private const string SelectCampaignColumns = @"
            SELECT campaign_id AS CampaignId, name AS Name, package_name AS PackageName,
                   start_date AS StartDate, end_date AS EndDate, purge_date AS PurgeDate,
                   priority AS Priority, campaign_type AS CampaignType,
                   campaign_status AS CampaignStatus, campaign_in_testmode AS CampaignInTestmode,
                   download_id AS DownloadId, CampaignData AS CampaignData,
                   DynamicCampaignData AS DynamicCampaignData, external_id AS ExternalId,
                   max_shows AS MaxShows, rest_hours AS RestHours, interactive AS Interactive,
                   max_show_me_later_shows AS MaxShowMeLaterShows,
                   show_me_later_rest_hours AS ShowMeLaterRestHours
            FROM [KALKTCCustomer].[dbo].[Campaigns]";

        private readonly KtcDbContext _context;

        public CampaignRepository(KtcDbContext context)
        {
            _context = context;
        }

        public Task<List<CampaignDto>> GetAllCampaignsAsync() =>
            _context.Database
                .SqlQueryRaw<CampaignDto>(SelectCampaignColumns + " ORDER BY name")
                .ToListAsync();

        public Task<CampaignDto?> GetCampaignByIdAsync(int campaignId) =>
            _context.Database
                .SqlQueryRaw<CampaignDto>(SelectCampaignColumns + " WHERE campaign_id = {0}", campaignId)
                .FirstOrDefaultAsync();

        public Task<CampaignDto?> GetCampaignByNameLatestAsync(string name) =>
            _context.Database
                .SqlQueryRaw<CampaignDto>(SelectCampaignColumns + " WHERE name = {0} ORDER BY campaign_id DESC", name)
                .FirstOrDefaultAsync();

        public Task CreateCampaignAsync(CreateCampaignRequest r) =>
            _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO [KALKTCCustomer].[dbo].[Campaigns]
                (name, package_name, start_date, end_date, purge_date, priority, campaign_type,
                 campaign_status, campaign_in_testmode, download_id, CampaignData, DynamicCampaignData,
                 external_id, max_shows, rest_hours, interactive, max_show_me_later_shows, show_me_later_rest_hours)
                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17})",
                r.Name!,
                r.PackageName          ?? "",
                r.StartDate            ?? DateTime.Now,
                r.EndDate              ?? DateTime.Now.AddMonths(1),
                r.PurgeDate            ?? DateTime.Now.AddMonths(2),
                r.Priority             ?? 5,
                r.CampaignType         ?? 0,
                r.CampaignStatus       ?? 0,
                r.CampaignInTestmode   ?? false,
                r.DownloadId           ?? 0,
                r.CampaignData         ?? "",
                r.DynamicCampaignData  ?? "",
                r.ExternalId           ?? "",
                r.MaxShows             ?? 0,
                r.RestHours            ?? 0,
                r.Interactive          ?? false,
                r.MaxShowMeLaterShows  ?? 0,
                r.ShowMeLaterRestHours ?? 0);

        public Task UpdateCampaignAsync(int campaignId, CreateCampaignRequest r) =>
            _context.Database.ExecuteSqlRawAsync(@"
                UPDATE [KALKTCCustomer].[dbo].[Campaigns]
                SET name = {0}, package_name = {1}, start_date = {2}, end_date = {3},
                    purge_date = {4}, priority = {5}, campaign_type = {6}, campaign_status = {7},
                    campaign_in_testmode = {8}, download_id = {9}, CampaignData = {10},
                    DynamicCampaignData = {11}, external_id = {12}, max_shows = {13},
                    rest_hours = {14}, interactive = {15}, max_show_me_later_shows = {16},
                    show_me_later_rest_hours = {17}
                WHERE campaign_id = {18}",
                r.Name             ?? "",
                r.PackageName      ?? "",
                r.StartDate        ?? DateTime.Now,
                r.EndDate          ?? DateTime.Now.AddMonths(1),
                r.PurgeDate        ?? DateTime.Now.AddMonths(2),
                r.Priority         ?? 5,
                r.CampaignType     ?? 0,
                r.CampaignStatus   ?? 0,
                r.CampaignInTestmode   ?? false,
                r.DownloadId       ?? 0,
                r.CampaignData     ?? "",
                r.DynamicCampaignData  ?? "",
                r.ExternalId       ?? "",
                r.MaxShows         ?? 0,
                r.RestHours        ?? 0,
                r.Interactive      ?? false,
                r.MaxShowMeLaterShows  ?? 0,
                r.ShowMeLaterRestHours ?? 0,
                campaignId);

        public Task<int> DeleteCampaignAsync(int campaignId) =>
            _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM [KALKTCCustomer].[dbo].[Campaigns] WHERE campaign_id = {0}", campaignId);

        public Task<List<CampaignBusinessDto>> GetCampaignBusinessesAsync(int campaignId) =>
            _context.Database.SqlQueryRaw<CampaignBusinessDto>(@"
                SELECT CAST(cb.campaign_id AS INT) AS CampaignId,
                       CAST(cb.business_id AS INT) AS BusinessId,
                       COALESCE(b.businessname, 
                                CASE WHEN cb.business_id = 0 THEN 'All Businesses'
                                     WHEN cb.business_id = -1 THEN 'Default business'
                                     ELSE 'Business #' + CAST(cb.business_id AS VARCHAR(10))
                                END) AS BusinessName
                FROM [KALKTCCustomer].[dbo].[CampaignBusinesses] cb
                LEFT JOIN [KALKTCDB].[dbo].[Businesses] b ON CAST(cb.business_id AS INT) = CAST(b.business_id AS INT)
                WHERE cb.campaign_id = {0}", campaignId).ToListAsync();

        public Task<List<CampaignGroupDto>> GetCampaignGroupsAsync(int campaignId) =>
            _context.Database.SqlQueryRaw<CampaignGroupDto>(@"
                SELECT cg.campaign_id AS CampaignId, cg.group_id AS GroupId,
                       g.groupname AS GroupName, cg.group_included AS GroupIncluded
                FROM [KALKTCCustomer].[dbo].[CampaignGroups] cg
                LEFT JOIN [KALKTCDB].[dbo].[Groups] g ON cg.group_id = g.group_id
                WHERE cg.campaign_id = {0}", campaignId).ToListAsync();

        public Task<List<CampaignBINRangeDto>> GetCampaignBINRangesAsync(int campaignId) =>
            _context.Database.SqlQueryRaw<CampaignBINRangeDto>(@"
                SELECT campaign_id AS CampaignId, bin_min AS BinMin, bin_max AS BinMax
                FROM [KALKTCCustomer].[dbo].[CampaignBINRanges]
                WHERE campaign_id = {0}", campaignId).ToListAsync();

        public Task<List<CampaignShownCountDto>> GetCampaignShownCountsAsync(int campaignId) =>
            _context.Database.SqlQueryRaw<CampaignShownCountDto>(@"
                SELECT csc.campaign_id AS CampaignId, csc.business_id AS BusinessId,
                       b.businessname AS BusinessName, csc.count AS Count
                FROM [KALKTCCustomer].[dbo].[CampaignShownCounts] csc
                LEFT JOIN [KALKTCDB].[dbo].[Businesses] b ON csc.business_id = b.business_id
                WHERE csc.campaign_id = {0}", campaignId).ToListAsync();

        /// <summary>
        /// Remplace tous les liens CampaignBusinesses pour une campagne.
        /// Supprime les anciens puis insère les nouveaux.
        /// </summary>
        public async Task SetCampaignBusinessesAsync(int campaignId, List<int> businessIds)
        {
            // 1. Supprimer tous les liens existants pour cette campagne
            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM [KALKTCCustomer].[dbo].[CampaignBusinesses] WHERE campaign_id = {0}",
                campaignId);

            // 2. Insérer les nouveaux liens
            foreach (var businessId in businessIds)
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO [KALKTCCustomer].[dbo].[CampaignBusinesses] (campaign_id, business_id) VALUES ({0}, {1})",
                    campaignId, businessId);
            }
        }

    }
}