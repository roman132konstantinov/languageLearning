using Application.DTOs.Lessons;
using Application.DTOs.LessonWords;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class LessonsController : ControllerBase
    {
        private readonly ILessonService _lessonService;
        private readonly ILessonWordService _lessonWordService;

        public LessonsController(
            ILessonService lessonService,
            ILessonWordService lessonWordService)
        {
            _lessonService = lessonService;
            _lessonWordService = lessonWordService;
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
            return lesson is null ? NotFound() : Ok(lesson);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Create([FromBody] CreateLessonDto dto)
        {
            var createdLesson = await _lessonService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = createdLesson.Id }, createdLesson);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateLessonDto dto)
        {
            var updatedLesson = await _lessonService.UpdateAsync(id, dto);
            return updatedLesson is null ? NotFound() : Ok(updatedLesson);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _lessonService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }

        [HttpGet("{lessonId:int}/words")]
        public async Task<IActionResult> GetLessonWords(int lessonId)
        {
            var words = await _lessonWordService.GetLessonWordsAsync(lessonId);
            return Ok(words);
        }

        [HttpPost("{lessonId:int}/words")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> AddWordToLesson(int lessonId, [FromBody] AddWordToLessonDto dto)
        {
            await _lessonWordService.AddWordToLessonAsync(lessonId, dto);
            return NoContent();
        }

        [HttpDelete("{lessonId:int}/words/{wordId:int}")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> RemoveWordFromLesson(int lessonId, int wordId)
        {
            var deleted = await _lessonWordService.RemoveWordFromLessonAsync(lessonId, wordId);
            return deleted ? NoContent() : NotFound();
        }
    }
}
