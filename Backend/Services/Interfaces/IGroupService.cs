using KtcWeb.Application.DTOs;

namespace KtcWeb.Application.Interfaces
{
    public interface IGroupService
    {
        Task<List<GroupDto>> GetAllGroupsAsync();
        Task<List<ClientSimpleDto>> GetGroupClientsAsync(int groupId);
        Task<GroupDetailsDto?> GetGroupDetailsAsync(int groupId);
        Task CreateGroupAsync(CreateGroupRequest request);
        Task UpdateGroupAsync(UpdateGroupRequest request);
        Task AddClientToGroupAsync(int groupId, int clientId);
        Task RemoveClientFromGroupAsync(int groupId, int clientId);
        Task DeleteGroupAsync(int groupId);
    }
}
