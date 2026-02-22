using EducationalCompany.Application.DTOs;
using EducationalCompany.Application.Interfaces;

namespace EducationalCompany.Presentation.Endpoints;

public static class CourseOccasionEndpoints
{
    public static void MapCourseOccasionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/course-occasions")
            .WithTags("Course Occasions");

        // GET: /api/course-occasions
        group.MapGet("/", async (ICourseOccasionService service) =>
        {
            return Results.Ok(await service.GetAllOccasionsAsync());
        })
        .WithName("GetAllCourseOccasions")
        .Produces<IEnumerable<CourseOccasionDto>>(StatusCodes.Status200OK);

        // GET: /api/course-occasions/upcoming
        group.MapGet("/upcoming", async (ICourseOccasionService service) =>
        {
            return Results.Ok(await service.GetUpcomingOccasionsAsync());
        })
        .WithName("GetUpcomingCourseOccasions")
        .Produces<IEnumerable<CourseOccasionDto>>(StatusCodes.Status200OK);

        // GET: /api/course-occasions/{id}
        group.MapGet("/{id}", async (Guid id, ICourseOccasionService service) =>
        {
            try
            {
                var occasion = await service.GetOccasionByIdAsync(id);
                return Results.Ok(occasion);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"Course occasion with ID {id} not found");
            }
        })
        .WithName("GetCourseOccasionById")
        .Produces<CourseOccasionDto>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status404NotFound);

        // GET: /api/course-occasions/{id}/with-registrations
        group.MapGet("/{id}/with-registrations", async (Guid id, ICourseOccasionService service) =>
        {
            try
            {
                var occasion = await service.GetOccasionWithRegistrationsAsync(id);
                return Results.Ok(occasion);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"Course occasion with ID {id} not found");
            }
        })
        .WithName("GetCourseOccasionWithRegistrations")
        .Produces<CourseOccasionDto>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status404NotFound);

        // GET: /api/course-occasions/course/{courseId}
        group.MapGet("/course/{courseId}", async (Guid courseId, ICourseOccasionService service) =>
        {
            try
            {
                var occasions = await service.GetByCourseIdAsync(courseId);
                return Results.Ok(occasions);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"Course with ID {courseId} not found");
            }
        })
        .WithName("GetCourseOccasionsByCourseId")
        .Produces<IEnumerable<CourseOccasionDto>>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status404NotFound);

        // GET: /api/course-occasions/{id}/is-full
        group.MapGet("/{id}/is-full", async (Guid id, ICourseOccasionService service) =>
        {
            try
            {
                var isFull = await service.IsOccasionFullAsync(id);
                return Results.Ok(new { isFull });
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"Course occasion with ID {id} not found");
            }
        })
        .WithName("IsCourseOccasionFull")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status404NotFound);

        // POST: /api/course-occasions
        group.MapPost("/", async (CreateCourseOccasionDto dto, ICourseOccasionService service) =>
        {
            try
            {
                var occasion = await service.CreateOccasionAsync(dto);
                return Results.CreatedAtRoute("GetCourseOccasionById", new { id = occasion.Id }, occasion);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
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
        .WithName("CreateCourseOccasion")
        .Produces<CourseOccasionDto>(StatusCodes.Status201Created)
        .Produces<string>(StatusCodes.Status404NotFound)
        .Produces<string>(StatusCodes.Status400BadRequest);

        // PUT: /api/course-occasions/{id}
        group.MapPut("/{id}", async (Guid id, CreateCourseOccasionDto dto, ICourseOccasionService service) =>
        {
            try
            {
                await service.UpdateOccasionAsync(id, dto);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
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
        .WithName("UpdateCourseOccasion")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<string>(StatusCodes.Status404NotFound)
        .Produces<string>(StatusCodes.Status400BadRequest);

        // PUT: /api/course-occasions/{id}/assign-teacher
        group.MapPut("/{id}/assign-teacher", async (Guid id, AssignTeacherDto dto, ICourseOccasionService service) =>
        {
            try
            {
                await service.AssignTeacherAsync(id, dto);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .WithName("AssignTeacherToOccasion")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<string>(StatusCodes.Status404NotFound)
        .Produces<string>(StatusCodes.Status400BadRequest);

        // DELETE: /api/course-occasions/{id}
        group.MapDelete("/{id}", async (Guid id, ICourseOccasionService service) =>
        {
            try
            {
                await service.DeleteOccasionAsync(id);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"Course occasion with ID {id} not found");
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .WithName("DeleteCourseOccasion")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<string>(StatusCodes.Status404NotFound)
        .Produces<string>(StatusCodes.Status400BadRequest);
    }
}