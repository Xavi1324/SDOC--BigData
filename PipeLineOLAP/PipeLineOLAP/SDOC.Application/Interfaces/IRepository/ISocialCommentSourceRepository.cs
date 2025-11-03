using SDOC.Domain.Entities.Api;

namespace SDOC.Application.Interfaces.IRepository
{
    public interface ISocialCommentSourceRepository
    {
        Task<IReadOnlyList<SocialCommetsApi>> GetAllAsync();
    }
}
