using Application.DTOs.Lessons;
using Application.DTOs.LessonWords;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
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

        [HttpGet("{lessonId:int}/words")]
        public async Task<IActionResult> GetLessonWords(int lessonId)
        {
            var words = await _lessonWordService.GetLessonWordsAsync(lessonId);
            return Ok(words);
        }

        [HttpPost("{lessonId:int}/words")]
        public async Task<IActionResult> AddWordToLesson(int lessonId, [FromBody] AddWordToLessonDto dto)
        {
            await _lessonWordService.AddWordToLessonAsync(lessonId, dto);
            return NoContent();
        }

        [HttpDelete("{lessonId:int}/words/{wordId:int}")]
        public async Task<IActionResult> RemoveWordFromLesson(int lessonId, int wordId)
        {
            var deleted = await _lessonWordService.RemoveWordFromLessonAsync(lessonId, wordId);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}