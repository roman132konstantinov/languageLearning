using API.Security;
using Application.Common.Exceptions;
using Application.DTOs.Progress;
using Application.DTOs.Users;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserWordProgressService _userWordProgressService;
        private readonly IUserLessonProgressService _userLessonProgressService;

        public UsersController(
            IUserService userService,
            IUserWordProgressService userWordProgressService,
            IUserLessonProgressService userLessonProgressService)
        {
            _userService = userService;
            _userWordProgressService = userWordProgressService;
            _userLessonProgressService = userLessonProgressService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            EnsureCurrentUserOrAdmin(id);
            var user = await _userService.GetByIdAsync(id);
            return user is null ? NotFound() : Ok(user);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            var createdUser = await _userService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = createdUser.Id }, createdUser);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
        {
            var updatedUser = await _userService.UpdateAsync(id, dto);
            return updatedUser is null ? NotFound() : Ok(updatedUser);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _userService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }

        [HttpGet("{userId:int}/word-progress")]
        public async Task<ActionResult<List<UserWordProgressResponseDto>>> GetWordProgress(int userId)
        {
            EnsureCurrentUserOrAdmin(userId);
            var progress = await _userWordProgressService.GetByUserAsync(userId);
            return Ok(progress);
        }

        [HttpGet("{userId:int}/word-progress/{wordId:int}")]
        public async Task<ActionResult<UserWordProgressResponseDto>> GetWordProgressByWord(int userId, int wordId)
        {
            EnsureCurrentUserOrAdmin(userId);
            var progress = await _userWordProgressService.GetByUserAndWordAsync(userId, wordId);
            return progress is null ? NotFound() : Ok(progress);
        }

        [HttpGet("{userId:int}/lesson-progress")]
        public async Task<ActionResult<List<UserLessonProgressResponseDto>>> GetLessonProgress(int userId)
        {
            EnsureCurrentUserOrAdmin(userId);
            var progress = await _userLessonProgressService.GetByUserAsync(userId);
            return Ok(progress);
        }

        [HttpGet("{userId:int}/lesson-progress/{lessonId:int}")]
        public async Task<ActionResult<UserLessonProgressResponseDto>> GetLessonProgressByLesson(int userId, int lessonId)
        {
            EnsureCurrentUserOrAdmin(userId);
            var progress = await _userLessonProgressService.GetByUserAndLessonAsync(userId, lessonId);
            return progress is null ? NotFound() : Ok(progress);
        }

        private void EnsureCurrentUserOrAdmin(int targetUserId)
        {
            if (!User.IsAdmin() && User.GetRequiredUserId() != targetUserId)
            {
                throw new ForbiddenException("You can only access your own profile and progress.");
            }
        }
    }
}
