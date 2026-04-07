using Application.DTOs.Lessons;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LessonsController : ControllerBase
    {
        private readonly ILessonService _lessonService;

        public LessonsController(ILessonService lessonService)
        {
            _lessonService = lessonService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var lessons = await _lessonService.GetAllAsync();
            return Ok(lessons);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var lesson = await _lessonService.GetByIdAsync(id);

            if (lesson is null)
                return NotFound();

            return Ok(lesson);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateLessonDto dto)
        {
            var createdLesson = await _lessonService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = createdLesson.Id },
                createdLesson);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateLessonDto dto)
        {
            var updatedLesson = await _lessonService.UpdateAsync(id, dto);

            if (updatedLesson is null)
                return NotFound();

            return Ok(updatedLesson);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _lessonService.DeleteAsync(id);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}