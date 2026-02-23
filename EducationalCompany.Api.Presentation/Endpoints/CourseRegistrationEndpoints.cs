using EducationalCompany.Api.Application.DTOs;
using EducationalCompany.Api.Application.Services;
using EducationalCompany.Api.Application.Interfaces;

namespace EducationalCompany.Presentation.Endpoints;

public static class CourseRegistrationEndpoints
{
    public static void MapCourseRegistrationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/registrations")
            .WithTags("Course Registrations");

        // GET: /api/registrations
        group.MapGet("/", async (ICourseRegistrationService service) =>
        {
            return Results.Ok(await service.GetAllRegistrationsAsync());
        })
        .WithName("GetAllRegistrations")
        .Produces<IEnumerable<CourseRegistrationDto>>(StatusCodes.Status200OK);

        // GET: /api/registrations/{id}
        group.MapGet("/{id}", async (Guid id, ICourseRegistrationService service) =>
        {
            try
            {
                var registration = await service.GetRegistrationByIdAsync(id);
                return Results.Ok(registration);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"Registration with ID {id} not found");
            }
        })
        .WithName("GetRegistrationById")
        .Produces<CourseRegistrationDto>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status404NotFound);

        // GET: /api/registrations/{id}/details
        group.MapGet("/{id}/details", async (Guid id, ICourseRegistrationService service) =>
        {
            try
            {
                var registration = await service.GetRegistrationDetailsAsync(id);
                return Results.Ok(registration);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"Registration with ID {id} not found");
            }
        })
        .WithName("GetRegistrationDetails")
        .Produces<CourseRegistrationDto>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status404NotFound);

        // GET: /api/registrations/participant/{participantId}
        group.MapGet("/participant/{participantId}", async (Guid participantId, ICourseRegistrationService service) =>
        {
            try
            {
                var registrations = await service.GetRegistrationsByParticipantAsync(participantId);
                return Results.Ok(registrations);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"Participant with ID {participantId} not found");
            }
        })
        .WithName("GetRegistrationsByParticipant")
        .Produces<IEnumerable<CourseRegistrationDto>>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status404NotFound);

        // GET: /api/registrations/occasion/{occasionId}
        group.MapGet("/occasion/{occasionId}", async (Guid occasionId, ICourseRegistrationService service) =>
        {
            try
            {
                var registrations = await service.GetRegistrationsByOccasionAsync(occasionId);
                return Results.Ok(registrations);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"Course occasion with ID {occasionId} not found");
            }
        })
        .WithName("GetRegistrationsByOccasion")
        .Produces<IEnumerable<CourseRegistrationDto>>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status404NotFound);

        // POST: /api/registrations
        group.MapPost("/", async (CreateCourseRegistrationDto dto, ICourseRegistrationService service) =>
        {
            try
            {
                var registration = await service.CreateRegistrationAsync(dto);
                return Results.CreatedAtRoute("GetRegistrationById", new { id = registration.Id }, registration);
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
        .WithName("CreateRegistration")
        .Produces<CourseRegistrationDto>(StatusCodes.Status201Created)
        .Produces<string>(StatusCodes.Status404NotFound)
        .Produces<string>(StatusCodes.Status400BadRequest);

        // PUT: /api/registrations/{id}/status
        group.MapPut("/{id}/status", async (Guid id, UpdateRegistrationStatusDto dto, ICourseRegistrationService service) =>
        {
            try
            {
                await service.UpdateRegistrationsStatusAsync(id, dto);
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
        .WithName("UpdateRegistrationStatus")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<string>(StatusCodes.Status404NotFound)
        .Produces<string>(StatusCodes.Status400BadRequest);

        // PUT: /api/registrations/{id}/confirm
        group.MapPut("/{id}/confirm", async (Guid id, ICourseRegistrationService service) =>
        {
            try
            {
                await service.ConfirmRegistrationAsync(id);
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
        .WithName("ConfirmRegistration")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<string>(StatusCodes.Status404NotFound)
        .Produces<string>(StatusCodes.Status400BadRequest);

        // PUT: /api/registrations/{id}/cancel
        group.MapPut("/{id}/cancel", async (Guid id, ICourseRegistrationService service) =>
        {
            try
            {
                await service.CancelRegistrationAsync(id);
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
        .WithName("CancelRegistration")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<string>(StatusCodes.Status404NotFound)
        .Produces<string>(StatusCodes.Status400BadRequest);

        // DELETE: /api/registrations/{id}
        group.MapDelete("/{id}", async (Guid id, ICourseRegistrationService service) =>
        {
            try
            {
                await service.DeleteRegistrationAsync(id);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"Registration with ID {id} not found");
            }
        })
        .WithName("DeleteRegistration")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<string>(StatusCodes.Status404NotFound);
    }
}