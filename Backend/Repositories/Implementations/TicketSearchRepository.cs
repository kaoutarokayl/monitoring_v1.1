using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using KtcWeb.Application.DTOs;
using KtcWeb.Domain.Interfaces;
using KtcWeb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KtcWeb.Infrastructure.Repositories
{
    public class TicketSearchRepository : ITicketSearchRepository
    {
        private readonly KtcDbContext _context;

        public TicketSearchRepository(KtcDbContext context)
        {
            _context = context;
        }

        public async Task<List<TicketTypeLookupDto>> GetTicketTypesAsync()
        {
            var connection = _context.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = @"SELECT tickettype_id AS TicketTypeId, typename AS TypeName FROM dbo.TroubleTicketTypes ORDER BY typename";

            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var reader = await command.ExecuteReaderAsync();
            var list = new List<TicketTypeLookupDto>();

            while (await reader.ReadAsync())
            {
                list.Add(new TicketTypeLookupDto
                {
                    TicketTypeId = reader.GetInt32(reader.GetOrdinal("TicketTypeId")),
                    TypeName = reader.GetString(reader.GetOrdinal("TypeName"))
                });
            }

            return list;
        }

        public async Task<List<ErrorCodeLookupDto>> GetErrorCodesAsync()
        {
            var connection = _context.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = @"SELECT errorcodetype_id AS ErrorCodeTypeId, errorcode AS ErrorCode, errortext AS ErrorText FROM dbo.ErrorCodeTypes ORDER BY errorcode";

            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var reader = await command.ExecuteReaderAsync();
            var list = new List<ErrorCodeLookupDto>();

            while (await reader.ReadAsync())
            {
                list.Add(new ErrorCodeLookupDto
                {
                    ErrorCodeTypeId = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("ErrorCodeTypeId"))),
                    ErrorCode = reader.IsDBNull(reader.GetOrdinal("ErrorCode")) ? string.Empty : reader.GetString(reader.GetOrdinal("ErrorCode")),
                    ErrorText = reader.IsDBNull(reader.GetOrdinal("ErrorText")) ? string.Empty : reader.GetString(reader.GetOrdinal("ErrorText"))
                });
            }

            return list;
        }

        public async Task<List<TicketSearchResultDto>> SearchTicketsAsync(TicketSearchCriteriaDto criteria)
        {
            var sql = new StringBuilder(@"
                SELECT
                    tt.TroubleTicket_id AS TicketId,
                    ISNULL(ttt.typename, 'N/A') AS TicketType,
                    CASE WHEN tt.ticketstatus_id = 1 THEN 'Open'
                         WHEN tt.ticketstatus_id = 2 THEN 'Dispatched'
                         WHEN tt.ticketstatus_id = 3 THEN 'Closed'
                         WHEN tt.ticketstatus_id = 4 THEN 'AutoClosed'
                         WHEN tt.ticketstatus_id = 5 THEN 'Suspended'
                         WHEN tt.ticketstatus_id = 6 THEN 'Planned'
                         WHEN tt.ticketstatus_id = 7 THEN 'Cancelled'
                         ELSE 'Unknown' END AS Status,
                    ISNULL(c.clientname, 'N/A') AS AtmName,
                    ISNULL(b.businessname, '') AS BusinessName,
                    ISNULL(br.branchname, '') AS BranchName,
                    ISNULL(g.groupname, '') AS GroupName,
                    ISNULL(dl.dispatchname, '') AS DispatchedTo,
                    ISNULL(u.user_name, '') AS Owner,
                    ISNULL(ect.errorcode, '') AS ErrorCode,
                    ISNULL(ect.errortext, '') AS ErrorText,
                    tt.creationtime AS Created,
                    ISNULL(tt.updatetime, tt.creationtime) AS LastChangeDate,
                    tt.closedtime AS ClosedDate,
                    CONCAT(CAST(DATEDIFF(MINUTE, tt.creationtime, ISNULL(tt.closedtime, GETUTCDATE())) AS varchar(20)), ' mins') AS Duration,
                    CASE WHEN EXISTS (SELECT 1 FROM dbo.TicketSLAs sla WHERE sla.TroubleTicket_id = tt.TroubleTicket_id AND sla.end_time IS NULL)
                         THEN 'Open SLA'
                         ELSE 'All SLAs closed' END AS SlaSummary
                FROM dbo.TroubleTickets tt
                LEFT JOIN dbo.TroubleTicketTypes ttt ON ttt.tickettype_id = tt.tickettype_id
                LEFT JOIN dbo.ErrorCodeTypes ect ON ect.errorcodetype_id = ttt.errorcodetype_id
                LEFT JOIN dbo.KTCUsers u ON u.user_id = tt.owner_id
                LEFT JOIN dbo.DispatchList dl ON dl.dispatch_id = tt.dispatch_id
                LEFT JOIN dbo.Clients c ON c.client_id = tt.client_id
                LEFT JOIN dbo.Branches br ON br.branch_id = c.branch_id
                LEFT JOIN dbo.Businesses b ON b.business_id = c.business_id
                LEFT JOIN dbo.Groups g ON g.group_id = tt.group_id
                WHERE tt.TroubleTicket_id > 0");

            var parameters = new List<object>();
            var index = 0;

            void AddClause(string clause, object value)
            {
                sql.Append(" AND ").Append(clause);
                parameters.Add(value);
                index++;
            }

            if (criteria.TicketId.HasValue)
            {
                AddClause($"tt.TroubleTicket_id = {{{index}}}", criteria.TicketId.Value);
            }
            else
            {
                if (criteria.GroupId.HasValue)
                {
                    AddClause($"tt.group_id = {{{index}}}", criteria.GroupId.Value);
                }

                if (criteria.BusinessId.HasValue)
                {
                    AddClause($"c.business_id = {{{index}}}", criteria.BusinessId.Value);
                }

                if (criteria.BranchId.HasValue)
                {
                    AddClause($"c.branch_id = {{{index}}}", criteria.BranchId.Value);
                }

                if (!string.IsNullOrWhiteSpace(criteria.AtmName))
                {
                    AddClause($"LOWER(ISNULL(c.clientname, '')) LIKE LOWER({{{index}}})", $"%{criteria.AtmName}%");
                }

                if (criteria.CreatedAfter.HasValue)
                {
                    AddClause($"tt.creationtime >= {{{index}}}", criteria.CreatedAfter.Value);
                }

                if (criteria.CreatedBefore.HasValue)
                {
                    AddClause($"tt.creationtime <= {{{index}}}", criteria.CreatedBefore.Value);
                }

                if (criteria.TicketTypeId.HasValue)
                {
                    AddClause($"tt.tickettype_id = {{{index}}}", criteria.TicketTypeId.Value);
                }

                if (criteria.ErrorCodeTypeId.HasValue)
                {
                    AddClause($"ect.errorcodetype_id = {{{index}}}", criteria.ErrorCodeTypeId.Value);
                }

                if (!string.IsNullOrWhiteSpace(criteria.Owner))
                {
                    AddClause($"LOWER(ISNULL(u.user_name, '')) LIKE LOWER({{{index}}})", $"%{criteria.Owner}%");
                }

                if (!string.IsNullOrWhiteSpace(criteria.DispatchedTo))
                {
                    AddClause($"LOWER(ISNULL(dl.dispatchname, '')) LIKE LOWER({{{index}}})", $"%{criteria.DispatchedTo}%");
                }

                if (!string.IsNullOrWhiteSpace(criteria.TicketStatus) && criteria.TicketStatus != "All")
                {
                    if (criteria.TicketStatus == "Open/Dispatched")
                    {
                        sql.Append(" AND tt.ticketstatus_id IN (1, 2)");
                    }
                    else if (criteria.TicketStatus == "Closed")
                    {
                        sql.Append(" AND tt.ticketstatus_id IN (3, 4, 5)");
                    }
                }

                if (!string.IsNullOrWhiteSpace(criteria.SlaStatus) && criteria.SlaStatus != "No Filter")
                {
                    switch (criteria.SlaStatus)
                    {
                        case "No Ticket SLAs":
                            sql.Append(" AND NOT EXISTS (SELECT 1 FROM dbo.TicketSLAs sla WHERE sla.TroubleTicket_id = tt.TroubleTicket_id)");
                            break;
                        case "Has any open SLAs":
                            sql.Append(" AND EXISTS (SELECT 1 FROM dbo.TicketSLAs sla WHERE sla.TroubleTicket_id = tt.TroubleTicket_id AND sla.end_time IS NULL)");
                            break;
                        case "Has any due in <X hours":
                            if (criteria.SlaHours.HasValue)
                            {
                                AddClause($"EXISTS (SELECT 1 FROM dbo.TicketSLAs sla WHERE sla.TroubleTicket_id = tt.TroubleTicket_id AND sla.end_time IS NULL AND sla.expected_end_time <= DATEADD(hour, {{{index}}}, GETUTCDATE()))", criteria.SlaHours.Value);
                            }
                            break;
                        case "Has open exceeded SLAs":
                            sql.Append(" AND EXISTS (SELECT 1 FROM dbo.TicketSLAs sla WHERE sla.TroubleTicket_id = tt.TroubleTicket_id AND sla.end_time IS NULL AND sla.expected_end_time < GETUTCDATE())");
                            break;
                        case "All SLAs are closed":
                            sql.Append(" AND EXISTS (SELECT 1 FROM dbo.TicketSLAs sla WHERE sla.TroubleTicket_id = tt.TroubleTicket_id) AND NOT EXISTS (SELECT 1 FROM dbo.TicketSLAs sla WHERE sla.TroubleTicket_id = tt.TroubleTicket_id AND sla.end_time IS NULL)");
                            break;
                    }
                }

                if (!string.IsNullOrWhiteSpace(criteria.ExtraDataField) && !string.IsNullOrWhiteSpace(criteria.ExtraDataValue))
                {
                    AddClause($"CONVERT(nvarchar(max), tt.extra_data) LIKE LOWER({{{index}}})", $"%<{criteria.ExtraDataField}>{criteria.ExtraDataValue}</{criteria.ExtraDataField}>%");
                }
            }

            sql.Append(" ORDER BY tt.creationtime DESC");

            return await _context.Database.SqlQueryRaw<TicketSearchResultDto>(sql.ToString(), parameters.ToArray()).ToListAsync();
        }
    }
}
