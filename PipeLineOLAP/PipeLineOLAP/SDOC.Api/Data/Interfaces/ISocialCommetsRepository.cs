using SDOC.Api.Data.entites;

namespace SDOC.Api.Data.Interfaces
{
    public interface ISocialCommetsRepository 
    {
       Task<IEnumerable<SocialCommetsApi>> ReadAsync();
    }
}
