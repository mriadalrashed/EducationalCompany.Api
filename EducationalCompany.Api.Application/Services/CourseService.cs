using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Infrastructure;
using EducationalCompany.Api.Application.Interfaces;
using System.Linq;

namespace EducationalCompany.Api.Application.Services
{

    // Service responsible for course business logic
    public class CourseService : ICourseService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CourseService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // Get all courses
        public async Task<IEnumerable<CourseDto>> GetAllCoursesAsync()
        {
            var courses = await _unitOfWork.Courses.GetAllAsync();
            return courses.Select(MapToDto);
        }

        // Get course by ID
        public async Task<CourseDto> GetCourseByIdAsync(Guid id)
        {
           var course = await _unitOfWork.Courses.GetByIdAsync(id);

           if (course == null)
               throw new KeyNotFoundException($"Course with id {id} not found.");

           return MapToDto(course);
        }

        // Create new course
        public async Task<CourseDto> CreateCourseAsync(CreateCourseDto dto)
        {
            if (await _unitOfWork.Courses.CourseNameExistsAsync(dto.Name))

                throw new InvalidOperationException($"Course With Name '{dto.Name}' Already Exists");

            var course = new Course(
                dto.Name,
                dto.Description,
                dto.DurationHours,
                dto.Price
            );

            await _unitOfWork.Courses.AddAsync(course);  
            return MapToDto(course);

        }

        // Update existing course
        public async Task UpdateCourseAsync(Guid id, UpdateCourseDto dto)
        {
           var course = await _unitOfWork.Courses.GetByIdAsync(id);
            
            if (course ==null)
                throw new KeyNotFoundException($"Course with id {id} not found.");

            if (dto.Name != course.Name && await _unitOfWork.Courses.CourseNameExistsAsync(dto.Name))
                throw new InvalidOperationException($"Course With Name '{dto.Name}' Already Exists");

            course.Update(dto.Name, dto.Description, dto.DurationHours, dto.Price);

            await _unitOfWork.Courses.UpdateAsync(course);
        }

        // Delete course
        public async Task DeleteCourseAsync(Guid id)
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(id);
            if (course == null)
                throw new KeyNotFoundException($"Course with id {id} not found.");
            await _unitOfWork.Courses.DeleteAsync(course);
        }


        // Search courses
        public async Task<IEnumerable<CourseDto>> SearchCoursesAsync(string searchTerm)
        {
            var courses = await _unitOfWork.Courses.SearchAsync(searchTerm);
            return courses.Select(MapToDto);
        }

        // Map entity to DTO
        private CourseDto MapToDto(Course course)
        {
            return new CourseDto
            {
                Name = course.Name,
                Description = course.Description,
                DurationHours = course.DurationHours,
                Price = course.Price
            };
        }

    }
}
