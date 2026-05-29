using KtcWeb.Application.DTOs;
using KtcWeb.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KtcWeb.API.Controllers
{
    [ApiController]
    [Route("api/group")]
    public class GroupController(IGroupService groupService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<GroupDto>>> GetAllGroups()
        {
            var groups = await groupService.GetAllGroupsAsync();
            return Ok(groups);
        }

        [HttpGet("{groupId}/clients")]
        public async Task<ActionResult<List<ClientSimpleDto>>> GetGroupClients(int groupId)
        {
            var clients = await groupService.GetGroupClientsAsync(groupId);
            return Ok(clients);
        }

        [HttpGet("{groupId}")]
        public async Task<ActionResult<GroupDetailsDto>> GetGroupDetails(int groupId)
        {
            var details = await groupService.GetGroupDetailsAsync(groupId);
            if (details == null)
                return NotFound(new { error = $"Groupe {groupId} non trouvé" });

            return Ok(details);
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
        {
            await groupService.CreateGroupAsync(request);
            return Ok(new { status = "Groupe créé avec succès" });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateGroup([FromBody] UpdateGroupRequest request)
        {
            await groupService.UpdateGroupAsync(request);
            return Ok(new { status = "Groupe mis à jour avec succès" });
        }

        [HttpPost("{groupId}/add-client/{clientId}")]
        public async Task<IActionResult> AddClientToGroup(int groupId, int clientId)
        {
            await groupService.AddClientToGroupAsync(groupId, clientId);
            return Ok(new { status = "Client ajouté au groupe" });
        }

        [HttpDelete("{groupId}/remove-client/{clientId}")]
        public async Task<IActionResult> RemoveClientFromGroup(int groupId, int clientId)
        {
            await groupService.RemoveClientFromGroupAsync(groupId, clientId);
            return Ok(new { status = "Client retiré du groupe" });
        }

        [HttpDelete("{groupId}")]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            await groupService.DeleteGroupAsync(groupId);
            return Ok(new { status = "Groupe supprimé" });
        }
    }
}
