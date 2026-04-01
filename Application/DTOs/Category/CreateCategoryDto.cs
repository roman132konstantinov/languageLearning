using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Category
{
    public class CreateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
