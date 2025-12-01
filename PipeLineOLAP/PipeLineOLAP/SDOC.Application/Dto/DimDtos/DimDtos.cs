using SDOC.Application.Dto.Sources;

namespace SDOC.Application.Dto.DimDtos
{
    public class DimDtos
    {
        public List<WebReviewDbDto> WebReviews { get; set; } = new ();
        public List<SurveyCsvDto> Surveys { get; set; } = new ();
        public List<SocialCommentApiDto> SocialComments { get; set; } = new();
    }

}
