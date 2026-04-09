using Application.DTOs.Words;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class WordsController : ControllerBase
    {
        private readonly IWordService _wordService;

        public WordsController(IWordService wordService)
        {
            _wordService = wordService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] WordQueryDto query)
        {
            var words = await _wordService.GetAllAsync(query);
            return Ok(words);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var word = await _wordService.GetByIdAsync(id);
            return word is null ? NotFound() : Ok(word);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Create([FromBody] CreateWordDto dto)
        {
            var id = await _wordService.CreateAsync(dto);
            var createdWord = await _wordService.GetByIdAsync(id);
            return CreatedAtAction(nameof(GetById), new { id }, createdWord);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateWordDto dto)
        {
            var updated = await _wordService.UpdateAsync(id, dto);
            return updated ? NoContent() : NotFound();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _wordService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}
