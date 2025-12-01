using SDOC.Application.Dto.DimDtos;
using SDOC.Application.Result;

namespace SDOC.Application.Interfaces.IRepository
{
    public interface IDwhRepository
    {
        Task<ServiceResult> DimensionsLoader(DimDtos dimDtos);
    }
}
