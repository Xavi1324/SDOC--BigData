namespace SDOC.Application.Dto.Sources
{
    public class SocialCommentApiDto
    {
        public long OpinionId { get; set; }
        public int? ClientId { get; set; }
        public int ProductId { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
    }
}
