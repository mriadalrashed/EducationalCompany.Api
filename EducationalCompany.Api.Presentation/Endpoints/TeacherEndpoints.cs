using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCompany.Presentation.Endpoints;

public static class TeacherEndpoints
{
    public static void MapTeacherEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/teachers")
            .WithTags("Teachers");
        

        // GET: /api/teachers
        group.MapGet("/", async (ITeacherService service, [FromQuery] string search = "") =>
        {
            if (!string.IsNullOrEmpty(search))
                return Results.Ok(await service.SearchTeachersAsync(search));

            return Results.Ok(await service.GetAllTeachersAsync());
        })
        .WithName("GetAllTeachers")
        .Produces<IEnumerable<TeacherDto>>(StatusCodes.Status200OK);

        // GET: /api/teachers/{id}
        group.MapGet("/{id}", async (Guid id, ITeacherService service) =>
        {
            try
            {
                var teacher = await service.GetTeacherByIdAsync(id);
                return Results.Ok(teacher);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"Teacher with ID {id} not found");
            }
        })
        .WithName("GetTeacherById")
        .Produces<TeacherDto>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status404NotFound);

        // GET: /api/teachers/{id}/with-occasions
        group.MapGet("/{id}/with-occasions", async (Guid id, ITeacherService service) =>
        {
            try
            {
                var teacher = await service.GetTeacherWithOccasionsAsync(id);
                return Results.Ok(teacher);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"Teacher with ID {id} not found");
            }
        })
        .WithName("GetTeacherWithOccasions")
        .Produces<TeacherDto>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status404NotFound);

        // POST: /api/teachers
        group.MapPost("/", async (CreateTeacherDto dto, ITeacherService service) =>
        {
            try
            {
                var teacher = await service.CreateTeacherAsync(dto);
                return Results.CreatedAtRoute("GetTeacherById", new { id = teacher.Id }, teacher);
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
        .WithName("CreateTeacher")
        .Produces<TeacherDto>(StatusCodes.Status201Created)
        .Produces<string>(StatusCodes.Status400BadRequest);

        // PUT: /api/teachers/{id}
        group.MapPut("/{id}", async (Guid id, UpdateTeacherDto dto, ITeacherService service) =>
        {
            try
            {
                await service.UpdateTeacherAsync(id, dto);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"Teacher with ID {id} not found");
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
        .WithName("UpdateTeacher")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<string>(StatusCodes.Status404NotFound)
        .Produces<string>(StatusCodes.Status400BadRequest);

        // DELETE: /api/teachers/{id}
        group.MapDelete("/{id}", async (Guid id, ITeacherService service) =>
        {
            try
            {
                await service.DeleteTeacherAsync(id);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"Teacher with ID {id} not found");
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .WithName("DeleteTeacher")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<string>(StatusCodes.Status404NotFound)
        .Produces<string>(StatusCodes.Status400BadRequest);
    }
}