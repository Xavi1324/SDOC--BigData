using SDOC.Domain.Entities.DB;

namespace SDOC.Application.Interfaces.IRepository
{
    public interface IWebReviewSourceRepository
    {
        Task<IReadOnlyList<WebReviewDB>> GetAllAsync();
    }
}
