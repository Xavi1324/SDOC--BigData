using Microsoft.AspNetCore.Mvc;
using SDOC.Api.Data.Interfaces;

namespace SDOC.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SocialCommentsApiController : ControllerBase
    {
        private readonly ISocialCommetsRepository _socialCommetsRepository;

        public SocialCommentsApiController(ISocialCommetsRepository socialCommetsRepository)
        {
            _socialCommetsRepository = socialCommetsRepository;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var comments = await _socialCommetsRepository.ReadAsync();
            return Ok(comments);
        }

    }
}
