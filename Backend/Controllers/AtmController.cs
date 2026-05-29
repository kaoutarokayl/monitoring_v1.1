using KtcWeb.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace KtcWeb.API.Controllers
{
    [ApiController]
    [Route("api/atm")]
    [Authorize(Policy = "RequireReadOnly")]
    public class AtmController(IAtmApplicationService service, ILogger<AtmController> logger) : ControllerBase
    {
        // ── Video Journal ──────────────────────────────────────────────────────

        [HttpGet("clients/{id}/videojournal/search")]
        public async Task<ActionResult<List<VideoJournalEventDto>>> SearchVideoJournal(
            int id,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? search)
        {
            var fromDate = from ?? DateTime.UtcNow.AddDays(-7);
            var toDate   = to   ?? DateTime.UtcNow.AddDays(1);
            var rows = await service.SearchVideoJournalAsync(id, fromDate, toDate, search);
            return Ok(rows);
        }

        [HttpGet("clients/{id}/videojournal/media/{mediaId:long}")]
        public async Task<IActionResult> GetVideoJournalMedia(int id, long mediaId)
        {
            var media = await service.GetVideoJournalMediaAsync(id, mediaId);
            if (media == null) return NotFound(new { message = "Media introuvable" });
            return File(media.Stream, media.ContentType, media.FileName);
        }

        // ── Availability ───────────────────────────────────────────────────────

        [HttpGet("clients/{id}/availability")]
        public async Task<ActionResult<AtmAvailabilityReportDto>> GetAvailability(
            int id,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var fromDate = from ?? DateTime.UtcNow.AddDays(-7);
            var toDate   = to   ?? DateTime.UtcNow.AddDays(1);
            return Ok(await service.GetAtmAvailabilityAsync(id, fromDate, toDate));
        }

        // ── Diagnostics ────────────────────────────────────────────────────────

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
            => Ok(await service.TestConnectionAsync());

        // ── Regions ────────────────────────────────────────────────────────────

        [HttpGet("regions")]
        public async Task<ActionResult<List<RegionListDto>>> GetAllRegions()
            => Ok(await service.GetAllRegionsAsync());

        [HttpGet("regions/{id}")]
        public async Task<ActionResult<RegionDetailsDto>> GetRegionById(short id)
        {
            var region = await service.GetRegionByIdAsync(id);
            if (region == null) return NotFound(new { message = "Région introuvable" });
            return Ok(region);
        }

        [HttpPost("regions")]
        [Authorize(Policy = "RequireWrite")]
        public async Task<IActionResult> CreateRegion([FromBody] CreateRegionRequest req)
        {
            await service.CreateRegionAsync(req);
            return Ok(new { message = "Région créée avec succès" });
        }

        [HttpPut("regions/{id}")]
        [Authorize(Policy = "RequireWrite")]
        public async Task<IActionResult> UpdateRegion(short id, [FromBody] UpdateRegionRequest req)
        {
            if (!await service.UpdateRegionAsync(id, req))
                return NotFound(new { message = "Région introuvable" });
            return Ok(new { message = "Région modifiée avec succès" });
        }

        [HttpDelete("regions/{id}")]
        [Authorize(Policy = "RequireWrite")]
        public async Task<IActionResult> DeleteRegion(short id)
        {
            if (!await service.DeleteRegionAsync(id))
                return NotFound(new { message = "Région introuvable" });
            return Ok(new { message = "Région supprimée avec succès" });
        }

        // ── Businesses ─────────────────────────────────────────────────────────

        [HttpGet("businesses")]
        public async Task<ActionResult<List<BusinessDto>>> GetAllBusinesses()
            => Ok(await service.GetAllBusinessesAsync());

        [HttpGet("businesses/{id}")]
        public async Task<ActionResult<BusinessDetailsDto>> GetBusinessById(short id)
        {
            var business = await service.GetBusinessByIdAsync(id);
            if (business == null) return NotFound(new { message = "Business introuvable" });
            return Ok(business);
        }

        [HttpPost("businesses")]
        [Authorize(Policy = "RequireWrite")]
        public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessRequest req)
        {
            await service.CreateBusinessAsync(req);
            return Ok(new { message = "Business créée avec succès" });
        }

        [HttpPut("businesses/{id}")]
        [Authorize(Policy = "RequireWrite")]
        public async Task<IActionResult> UpdateBusiness(short id, [FromBody] UpdateBusinessRequest req)
        {
            if (!await service.UpdateBusinessAsync(id, req))
                return NotFound(new { message = "Business introuvable" });
            return Ok(new { message = "Business modifiée avec succès" });
        }

        [HttpDelete("businesses/{id}")]
        [Authorize(Policy = "RequireWrite")]
        public async Task<IActionResult> DeleteBusiness(short id)
        {
            if (!await service.DeleteBusinessAsync(id))
                return NotFound(new { message = "Business introuvable" });
            return Ok(new { message = "Business supprimée avec succès" });
        }

        // ── Branches ───────────────────────────────────────────────────────────

        [HttpGet("branches")]
        public async Task<ActionResult<List<BranchDto>>> GetAllBranches()
            => Ok(await service.GetAllBranchesAsync());

        [HttpGet("branches/{id}")]
        public async Task<ActionResult<BranchDto>> GetBranchById(short id)
        {
            var branch = await service.GetBranchByIdAsync(id);
            if (branch == null) return NotFound(new { message = "Branche introuvable" });
            return Ok(branch);
        }

        [HttpPost("branches")]
        [Authorize(Policy = "RequireWrite")]
        public async Task<IActionResult> CreateBranch([FromBody] CreateBranchRequest req)
        {
            await service.CreateBranchAsync(req);
            return Ok(new { message = "Branche créée avec succès" });
        }

        [HttpPut("branches/{id}")]
        [Authorize(Policy = "RequireWrite")]
        public async Task<IActionResult> UpdateBranch(short id, [FromBody] UpdateBranchRequest req)
        {
            if (!await service.UpdateBranchAsync(id, req))
                return NotFound(new { message = "Branche introuvable" });
            return Ok(new { message = "Branche modifiée avec succès" });
        }

        [HttpDelete("branches/{id}")]
        [Authorize(Policy = "RequireWrite")]
        public async Task<IActionResult> DeleteBranch(short id)
        {
            if (!await service.DeleteBranchAsync(id))
                return NotFound(new { message = "Branche introuvable" });
            return Ok(new { message = "Branche supprimée avec succès" });
        }

        // ── Clients / ATMs ─────────────────────────────────────────────────────

        [HttpGet("clients")]
        public async Task<ActionResult<List<ClientAtmDto>>> GetAllClients()
            => Ok(await service.GetAllClientsAsync());

        [HttpGet("clients/{id}")]
        public async Task<ActionResult<ClientAtmDto>> GetClientById(int id)
        {
            var client = await service.GetClientByIdAsync(id);
            if (client == null) return NotFound(new { message = "ATM introuvable" });
            return Ok(client);
        }

        [HttpPost("clients")]
        [Authorize(Policy = "RequireWrite")]
        public async Task<IActionResult> CreateClient([FromBody] CreateOrUpdateAtmRequest req)
        {
            await service.CreateClientAsync(req);
            return Ok(new { message = "ATM créé avec succès" });
        }

        [HttpPut("clients/{id}")]
        [Authorize(Policy = "RequireWrite")]
        public async Task<IActionResult> UpdateClient(int id, [FromBody] CreateOrUpdateAtmRequest req)
        {
            if (!await service.UpdateClientAsync(id, req))
                return NotFound(new { message = "ATM introuvable" });
            return Ok(new { message = "ATM modifié avec succès" });
        }

        [HttpDelete("clients/{id}")]
        [Authorize(Policy = "RequireWrite")]
        public async Task<IActionResult> DeleteClient(int id)
        {
            if (!await service.DeleteClientAsync(id))
                return NotFound(new { message = "ATM introuvable" });
            return Ok(new { message = "ATM supprimé avec succès" });
        }

        // ── ATM Detail ─────────────────────────────────────────────────────────

        [HttpGet("clients/{id}/status")]
        public async Task<ActionResult<List<AtmComponentStatusDto>>> GetAtmStatus(int id)
            => Ok(await service.GetAtmStatusAsync(id));

        [HttpGet("clients/{id}/assethistory")]
        public async Task<ActionResult<List<AtmAssetHistoryDto>>> GetAtmAssetHistory(int id)
            => Ok(await service.GetAtmAssetHistoryAsync(id));

        [HttpGet("clients/{id}/lastcontact")]
        public async Task<ActionResult<LastClientContactDto>> GetLastClientContact(int id)
        {
            var lastContact = await service.GetLastClientContactAsync(id);
            if (lastContact == null)
                return NotFound(new { message = "Aucun dernier contact trouvé pour cet ATM." });
            return Ok(lastContact);
        }

        [HttpGet("clients/{id}/softwareinfo")]
        public async Task<ActionResult<List<AtmSoftwareInfoDto>>> GetAtmSoftwareInfo(int id)
            => Ok(await service.GetAtmSoftwareInfoAsync(id));

        [HttpGet("clients/{id}/certificates")]
        public async Task<ActionResult<List<AtmCertificateDto>>> GetAtmCertificates(int id)
            => Ok(await service.GetAtmCertificatesAsync(id));

        [HttpGet("clients/{id}/tickets")]
        public async Task<ActionResult<List<AtmTicketDto>>> GetAtmTickets(
            int id,
            [FromQuery] int days          = 14,
            [FromQuery] string statusFilter = "All")
            => Ok(await service.GetAtmTicketsAsync(id, days, statusFilter));

        [HttpGet("clients/{id}/tickets-debug")]
        public async Task<ActionResult> GetAtmTicketsDebug(int id)
            => Ok(await service.GetAtmTicketsDebugAsync(id));

        // ── Counters ───────────────────────────────────────────────────────────

        [HttpGet("clients/{clientId}/components/{componentId}/application-counters")]
        public async Task<ActionResult<List<AppCounterDto>>> GetApplicationCounters(int clientId, short componentId)
            => Ok(await service.GetApplicationCountersAsync(clientId, componentId));

        [HttpGet("clients/{clientId}/components/{componentId}/replenishments")]
        public async Task<ActionResult<List<ReplenishmentDto>>> GetReplenishments(int clientId, short componentId)
            => Ok(await service.GetReplenishmentsAsync(clientId, componentId));

        [HttpGet("clients/{clientId}/components/{componentId}/xfs-counters")]
        public async Task<ActionResult<XfsCountersResponseDto>> GetXfsCounters(int clientId, short componentId)
            => Ok(await service.GetXfsCountersAsync(clientId, componentId));

        // ── Actions & Schedules ────────────────────────────────────────────────

        [HttpGet("clients/{id}/actions")]
        public async Task<ActionResult<AtmActionsResponseDto>> GetClientActions(
            int id,
            [FromQuery] DateTime? from       = null,
            [FromQuery] DateTime? to         = null,
            [FromQuery] int? days            = null,
            [FromQuery] string? addedByUser  = null)
            => Ok(await service.GetClientActionsAsync(id, from, to, days, addedByUser));

        [HttpGet("clients/{id}/schedules")]
        public async Task<ActionResult<List<AtmScheduleDto>>> GetClientSchedules(int id)
            => Ok(await service.GetClientSchedulesAsync(id));

        [HttpPost("schedules")]
        [Authorize(Policy = "RequireWrite")]
        public async Task<IActionResult> CreateSchedule([FromBody] CreateScheduleRequest request)
        {
            await service.CreateScheduleAsync(request);
            return Ok(new { message = "Schedule créé avec succès." });
        }

        [HttpGet("clients/{id}/uploads")]
        public async Task<ActionResult<List<AtmUploadDto>>> GetClientUploads(int id)
            => Ok(await service.GetClientUploadsAsync(id));

        [HttpPost("clients/{id}/uploads")]
        [Authorize(Policy = "RequireWrite")]
        public async Task<ActionResult<UploadFileResultDto>> UploadClientFile(
            int id,
            IFormFile file,
            [FromForm] byte fileType = 5,
            [FromForm] string? comments = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Aucun fichier n’a été envoyé." });

            try
            {
                var result = await service.UploadClientFileAsync(id, file, fileType, comments);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "UploadClientFile failed for clientId={ClientId} fileType={FileType}", id, fileType);
                return StatusCode(500, new
                {
                    message = ex.Message,
                    inner   = ex.InnerException?.Message,
                    type    = ex.GetType().Name,
                    trace   = ex.StackTrace
                });
            }
        }

        [HttpGet("clients/{id}/uploads/{actionId}/download")]
        public async Task<IActionResult> DownloadClientUpload(int id, long actionId)
        {
            var upload = await service.GetClientUploadFileAsync(id, actionId);
            if (upload == null)
                return NotFound(new { message = "Fichier introuvable pour cet ATM." });

            return File(upload.Value.Data, "application/octet-stream", upload.Value.FileName);
        }

        [HttpGet("command-types")]
        public async Task<ActionResult<List<RemoteCommandTypeDto>>> GetRemoteCommandTypes()
            => Ok(await service.GetRemoteCommandTypesAsync());

        [HttpPost("clients/dispatch-command")]
        [Authorize(Policy = "RequireWrite")]
        public async Task<ActionResult<DispatchRemoteActionsResponse>> DispatchRemoteCommand(
            [FromBody] DispatchRemoteActionsRequest request)
        {
            if (request.ClientIds == null || request.ClientIds.Count == 0)
                return BadRequest(new { message = "Sélectionnez au moins un ATM (clientIds)." });

            var result = await service.DispatchRemoteActionsAsync(request);
            return Ok(result);
        }

        // ── Transactions / Journal ─────────────────────────────────────────────

        [HttpGet("clients/{id}/electronic-journal")]
        public async Task<ActionResult<List<ElectronicJournalEntryDto>>> GetElectronicJournal(
            int id,
            [FromQuery] DateTime from,
            [FromQuery] DateTime to)
            => Ok(await service.GetElectronicJournalAsync(id, from, to));

        [HttpGet("transactions/lookups/type-codes")]
        public async Task<ActionResult<List<LookupItemDto>>> GetTransactionTypeCodes()
            => Ok(await service.GetTransactionTypeLookupsAsync());

        [HttpGet("transactions/lookups/reason-codes")]
        public async Task<ActionResult<List<LookupItemDto>>> GetTransactionReasonCodes()
            => Ok(await service.GetTransactionReasonLookupsAsync());

        [HttpGet("transactions/lookups/completion-codes")]
        public async Task<ActionResult<List<LookupItemDto>>> GetTransactionCompletionCodes()
            => Ok(await service.GetTransactionCompletionLookupsAsync());

        [HttpPost("clients/{id}/transactions/search")]
        public async Task<ActionResult<List<TransactionAuditDto>>> SearchAtmTransactions(
            int id,
            [FromBody] TransactionSearchCriteria criteria)
            => Ok(await service.SearchAtmTransactionsAsync(id, criteria));

        // ── Hardware Types ─────────────────────────────────────────────────────

        [HttpGet("hardwaretypes")]
        public async Task<ActionResult<List<HardwareTypeDto>>> GetHardwareTypes()
            => Ok(await service.GetHardwareTypesAsync());

        [HttpGet("businesses/{businessId}/hardwaretypes")]
        public async Task<ActionResult<List<HardwareTypeDto>>> GetHardwareTypesByBusiness(short businessId)
            => Ok(await service.GetHardwareTypesByBusinessAsync(businessId));
    }
}