using Application.DTOs.Exercises;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api")]
    public class ExercisesController : ControllerBase
    {
        private readonly IExerciseService _exerciseService;

        public ExercisesController(IExerciseService exerciseService)
        {
            _exerciseService = exerciseService;
        }

        [HttpGet("lessons/{lessonId:int}/exercises")]
        public async Task<IActionResult> GetLessonExercises(int lessonId)
        {
            var exercises = await _exerciseService.GetLessonExercisesAsync(lessonId);
            return Ok(exercises);
        }

        [HttpGet("exercises/{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var exercise = await _exerciseService.GetByIdAsync(id);

            if (exercise is null)
                return NotFound();

            return Ok(exercise);
        }

        [HttpPost("lessons/{lessonId:int}/exercises")]
        public async Task<IActionResult> Create(int lessonId, [FromBody] CreateExerciseDto dto)
        {
            var createdExercise = await _exerciseService.CreateAsync(lessonId, dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = createdExercise.Id },
                createdExercise);
        }

        [HttpPut("exercises/{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateExerciseDto dto)
        {
            var updatedExercise = await _exerciseService.UpdateAsync(id, dto);

            if (updatedExercise is null)
                return NotFound();

            return Ok(updatedExercise);
        }

        [HttpDelete("exercises/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _exerciseService.DeleteAsync(id);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}