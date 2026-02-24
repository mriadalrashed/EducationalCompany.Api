using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Application.Services;
using EducationalCompany.Api.Domain.Entities;
using EducationalCompany.Api.Domain.Interfaces;
using EducationalCompany.Api.Infrastructure.Repositories;
using EducationalCompany.Infrastructure.Repositories;
using FluentAssertions;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace EducationalCompany.Tests.Unit.Services;

public class CourseServiceTests
{
    private CourseService _service;
    private Mock<ICourseRepository> _courseRepoMock;

    public CourseServiceTests()
    {
        var (uowMock, courseRepoMock) = MockUnitOFWorkFactory.Create();
        _courseRepoMock = courseRepoMock;
        _service = new CourseService(uowMock.Object);
    }

    [Fact]
    public async Task CreateCourseAsync_ShouldCreateCourse_WhenNameIsUnique()
    {
        // Arrange
        var dto = new CreateCourseDto
        {
            Name = "Clean Architecture",
            Description = "Intro",
            DurationHours = 40,
            Price = 5000
        };

        _courseRepoMock
            .Setup(r => r.CourseNameExistsAsync(dto.Name))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CreateCourseAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(dto.Name);

        _courseRepoMock.Verify(r => r.AddAsync(It.IsAny<Course>()), Times.Once);
    }

    [Fact]
    public async Task CreateCourseAsync_ShouldThrow_WhenNameExists()
    {
        // Arrange
        var dto = new CreateCourseDto { Name = "Duplicate" };

        _courseRepoMock
            .Setup(r => r.CourseNameExistsAsync(dto.Name))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateCourseAsync(dto));
    }

    [Fact]
    public async Task GetCourseByIdAsync_ShouldThrow_WhenNotFound()
    {
        _courseRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Course?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetCourseByIdAsync(Guid.NewGuid()));
    }
}