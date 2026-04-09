using Application.DTOs.Common;
using Domain.Enums;

namespace Application.DTOs.Words
{
    public class WordQueryDto : PagedQueryDto
    {
        public LanguageLevel? Level { get; set; }
        public int? CategoryId { get; set; }
        public bool? IsActive { get; set; }
    }
}
