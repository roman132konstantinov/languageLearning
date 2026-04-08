using Application.DTOs.Category;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _categoryService.GetAllAsync();
            return Ok(categories);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            return category is null ? NotFound() : Ok(category);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        {
            var createdCategory = await _categoryService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = createdCategory.Id }, createdCategory);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
        {
            var updated = await _categoryService.UpdateAsync(id, dto);
            return updated ? NoContent() : NotFound();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _categoryService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}
