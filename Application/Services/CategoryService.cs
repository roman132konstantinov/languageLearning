using Application.Common.Exceptions;
using Application.Common.Pagination;
using Application.DTOs.Category;
using Application.DTOs.Common;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<PagedResponseDto<CategoryResponseDto>> GetAllAsync(CategoryQueryDto query)
        {
            query ??= new CategoryQueryDto();

            PagedQueryNormalizer.Normalize(query);

            var categories = await _categoryRepository.GetAllAsync(query);

            return new PagedResponseDto<CategoryResponseDto>
            {
                Items = categories.Items.Select(MapToResponse).ToList(),
                Page = categories.Page,
                PageSize = categories.PageSize,
                TotalCount = categories.TotalCount,
                TotalPages = categories.TotalPages
            };
        }

        public async Task<CategoryResponseDto?> GetByIdAsync(int id)
        {
            ValidateCategoryId(id);

            var category = await _categoryRepository.GetByIdAsync(id);
            return category is null ? null : MapToResponse(category);
        }

        public async Task<CategoryResponseDto> CreateAsync(CreateCategoryDto dto)
        {
            ValidateCreateDto(dto);

            if (await _categoryRepository.ExistsByNameAsync(dto.Name))
                throw new ConflictException("Category with this name already exists.");

            var category = new Category
            {
                Name = dto.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim()
            };

            await _categoryRepository.AddAsync(category);
            await _categoryRepository.SaveChangesAsync();

            return MapToResponse(category);
        }

        public async Task<bool> UpdateAsync(int id, UpdateCategoryDto dto)
        {
            ValidateCategoryId(id);
            ValidateUpdateDto(dto);

            var category = await _categoryRepository.GetByIdAsync(id);
            if (category is null)
                return false;

            if (await _categoryRepository.ExistsByNameAsync(dto.Name, id))
                throw new ConflictException("Category with this name already exists.");

            category.Name = dto.Name.Trim();
            category.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

            _categoryRepository.Update(category);
            await _categoryRepository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            ValidateCategoryId(id);

            var category = await _categoryRepository.GetByIdAsync(id);
            if (category is null)
                return false;

            if (await _categoryRepository.HasWordsAsync(id))
                throw new ConflictException("Category cannot be deleted while it still contains words.");

            _categoryRepository.Delete(category);
            await _categoryRepository.SaveChangesAsync();

            return true;
        }

        private static CategoryResponseDto MapToResponse(Category category)
        {
            return new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            };
        }

        private static void ValidateCategoryId(int id)
        {
            if (id <= 0)
                throw new ValidationException("CategoryId must be greater than 0.");
        }

        private static void ValidateCreateDto(CreateCategoryDto dto)
        {
            if (dto is null)
                throw new ValidationException("Category payload is required.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ValidationException("Category name is required.");

            if (dto.Name.Trim().Length > 100)
                throw new ValidationException("Category name must not exceed 100 characters.");

            if (dto.Description is not null && dto.Description.Trim().Length > 500)
                throw new ValidationException("Category description must not exceed 500 characters.");
        }

        private static void ValidateUpdateDto(UpdateCategoryDto dto)
        {
            if (dto is null)
                throw new ValidationException("Category payload is required.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ValidationException("Category name is required.");

            if (dto.Name.Trim().Length > 100)
                throw new ValidationException("Category name must not exceed 100 characters.");

            if (dto.Description is not null && dto.Description.Trim().Length > 500)
                throw new ValidationException("Category description must not exceed 500 characters.");
        }
    }
}
