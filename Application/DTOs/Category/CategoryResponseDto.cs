using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Category
{
    public class CategoryResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
