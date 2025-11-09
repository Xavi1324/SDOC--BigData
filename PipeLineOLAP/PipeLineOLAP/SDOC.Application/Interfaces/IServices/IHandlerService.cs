using SDOC.Application.Result;

namespace SDOC.Application.Interfaces.IServices
{
    public interface IHandlerService
    {
        Task<ServiceResult> DataHandler();
        
    }
}
