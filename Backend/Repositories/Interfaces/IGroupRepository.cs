using KtcWeb.Application.DTOs;

namespace KtcWeb.Domain.Interfaces
{
    public interface IGroupRepository
    {
        Task<List<GroupDto>> GetAllGroupsAsync();
        Task<GroupDto?> GetGroupByIdAsync(int groupId);
        Task<List<ClientSimpleDto>> GetAllClientsAsync();
        Task<List<ClientSimpleDto>> GetClientsByGroupAsync(int groupId);
        Task<List<ClientSimpleDto>> GetClientsByQueryAsync(string groupQuery);
        Task CreateGroupAsync(CreateGroupRequest request);
        Task UpdateGroupAsync(UpdateGroupRequest request);
        Task AddClientToGroupAsync(int groupId, int clientId);
        Task RemoveClientFromGroupAsync(int groupId, int clientId);
        Task DeleteClientGroupsAsync(int groupId);
        Task DeleteGroupAsync(int groupId);
    }
}
