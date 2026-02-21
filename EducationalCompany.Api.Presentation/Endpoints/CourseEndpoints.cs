using EducationalCompany.Application.DTOs;
using EducationalCompany.Application.Interfaces;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;


namespace EducationalCompany.Api.Presentation.Endpoints
{
    public static class CourseEndpoints
    {
        public static void MapCourseEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/courses")
                           .WithTags("courses");

            group.MapGet("/", async (ICourseService service, [FromQuery] string search = "") =>
            {
                if (!string.IsNullOrEmpty(search))
                    return Results.Ok(await service.SearchCoursesAsync(search));

                return Results.Ok(await service.GetAllCoursesAsync());

            })
            .WithName("GetAllCourses")
            .Produces<IEnumerable<CourseDto>>(StatusCodes.Status200OK);

            group.MapGet("/{id}", async(ICourseService service, Guid id) =>
            {
                try
                {
                    var course = await service.GetCourseByIdAsync(id);
                    return Results.Ok(course);
                }
                catch (KeyNotFoundException)
                {
                    return Results.NotFound($"course whith id :{id} not found");
                }
            })
            .WithName("GetCourseById")
            .Produces<IEnumerable<CourseDto>>(StatusCodes.Status200OK)
            .Produces<IEnumerable<CourseDto>>(StatusCodes.Status404NotFound);

            group.MapPost("/", async (ICourseService service, CreateCourseDto dto) =>
            {
                try
                {
                    var course = await service.CreateCourseAsync(dto);
                    return Results.CreatedAtRoute("GetCourseById", new { id = course.Id }, course);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            })
            .WithName("CreateCourse")
            .Produces<IEnumerable<CourseDto>>(StatusCodes.Status201Created)
            .Produces<IEnumerable<CourseDto>>(StatusCodes.Status400BadRequest);

            group.MapPut("/{id}", async (ICourseService service, UpdateCourseDto dto , Guid id) =>
            {

                try
                {
                    await service.UpdateCourseAsync(id,dto);
                    return  Results.NoContent();
                }
                catch (KeyNotFoundException)
                {
                    return Results.NotFound($"course whith id :{id} not found");
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            })
            .WithName("UpdateCourse")
            .Produces<IEnumerable<CourseDto>>(StatusCodes.Status204NoContent)
            .Produces<IEnumerable<CourseDto>>(StatusCodes.Status404NotFound)
            .Produces<IEnumerable<CourseDto>>(StatusCodes.Status400BadRequest);

            group.MapDelete("/{id}", async (ICourseService service, Guid id) =>
            {

                try
                {
                    await service.DeleteCourseAsync(id);
                    return Results.NoContent();
                }
                catch (KeyNotFoundException)
                {
                    return Results.NotFound($"course whith id :{id} not found");
                }
                
            })
            .WithName("DeleteCourse")
            .Produces<IEnumerable<CourseDto>>(StatusCodes.Status204NoContent)
            .Produces<IEnumerable<CourseDto>>(StatusCodes.Status404NotFound);
        }   
    }
}
