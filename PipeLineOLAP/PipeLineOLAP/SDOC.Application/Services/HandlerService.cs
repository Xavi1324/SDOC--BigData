using Microsoft.Extensions.Logging;
using SDOC.Application.Interfaces.IRepository;
using SDOC.Application.Interfaces.IServices;
using SDOC.Application.Result;

namespace SDOC.Application.Services
{
    public class HandlerService : IHandlerService
    {
        private readonly IApiSocialCommentSourceRepository _apiRepo;
        private readonly ICsvInternalSurveySourceRepository _csvRepo;
        private readonly IWebReviewSourceRepository _webRepo;
        private readonly ILogger<HandlerService> _logger;

        public HandlerService(  IApiSocialCommentSourceRepository apiRepo,
                                ICsvInternalSurveySourceRepository csvRepo,
                                IWebReviewSourceRepository webRepo,
                                ILogger<HandlerService> logger)

        {
            _apiRepo = apiRepo;
            _csvRepo = csvRepo;
            _webRepo = webRepo;
            _logger = logger;

        }

        public  Task<ServiceResult> DataHandler()
        {
            
            throw new NotImplementedException();
        }
    }
}
