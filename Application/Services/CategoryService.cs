using Application.DTOs.Category;
using Application.Interfaces;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoreRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoreRepository = categoryRepository;
        }
        public async Task<List<CategoryResponseDto>> GetAllAsync()
        {
            var categoryes = await _categoreRepository.GetAllAsync();

            return categoryes.Select(x => new CategoryResponseDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description
            }).ToList();
        }

        public async Task<CategoryResponseDto?> GetByIdAsync(int id)
        {
            var category = await _categoreRepository.GetByIdAsync(id);
            if (category is null)
                return null;
            return new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            };
        }

        public async Task<CategoryResponseDto> CreateAsync(CreateCategoryDto dto)
        {
            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description
            };
            await _categoreRepository.AddAsync(category);
            await _categoreRepository.SaveChangesAsync();
            return new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            };
        }

        public async Task<bool> UpdateAsync(int id, UpdateCategoryDto dto)
        {
            var category = await _categoreRepository.GetByIdAsync(id);
            if (category is null)
                return false;
            category.Description = dto.Description;
            category.Name = dto.Name;
            _categoreRepository.Update(category);
            await _categoreRepository.SaveChangesAsync();
            return true;
        }
        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _categoreRepository.GetByIdAsync(id);
            if (category is null)
                return false;
            _categoreRepository.Delete(category);
            await _categoreRepository.SaveChangesAsync();

            return true;
        }
    }
}
