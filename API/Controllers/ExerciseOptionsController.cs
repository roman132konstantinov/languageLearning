using Application.DTOs.ExerciseOption;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/exercises/{exerciseId:int}/options")]
    public class ExerciseOptionsController : ControllerBase
    {
        private readonly IExerciseOptionService _exerciseOptionService;

        public ExerciseOptionsController(IExerciseOptionService exerciseOptionService)
        {
            _exerciseOptionService = exerciseOptionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetByExerciseId(int exerciseId)
        {
            var options = await _exerciseOptionService.GetByExerciseIdAsync(exerciseId);
            return Ok(options);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int exerciseId, int id)
        {
            var option = await _exerciseOptionService.GetByIdAsync(id);
            if (option == null || option.ExerciseId != exerciseId)
                return NotFound();

            return Ok(option);
        }

        [HttpPost]
        public async Task<IActionResult> Create(int exerciseId, [FromBody] CreateExerciseOptionDto dto)
        {
            var created = await _exerciseOptionService.CreateAsync(exerciseId, dto);
            return CreatedAtAction(nameof(GetById), new { exerciseId, id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int exerciseId, int id, [FromBody] UpdateExerciseOptionDto dto)
        {
            var existingOption = await _exerciseOptionService.GetByIdAsync(id);
            if (existingOption == null || existingOption.ExerciseId != exerciseId)
                return NotFound();

            var updated = await _exerciseOptionService.UpdateAsync(id, dto);
            if (!updated)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int exerciseId, int id)
        {
            var existingOption = await _exerciseOptionService.GetByIdAsync(id);
            if (existingOption == null || existingOption.ExerciseId != exerciseId)
                return NotFound();

            var deleted = await _exerciseOptionService.DeleteAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
