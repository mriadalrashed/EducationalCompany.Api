using EducationalCompany.Application.DTOs;
using EducationalCompany.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EducationalCompany.Presentation.Endpoints;

public static class ParticipantEndpoints
{
    public static void MapParticipantEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/participants")
            .WithTags("Participants");
        // Remove: .WithOpenApi();

        // GET: /api/participants
        group.MapGet("/", async (IParticipantService service, [FromQuery] string search = "") =>
        {
            if (!string.IsNullOrEmpty(search))
                return Results.Ok(await service.SearchParticipantsAsync(search));

            return Results.Ok(await service.GetAllParticipantsAsync());
        })
        .WithName("GetAllParticipants")
        .Produces<IEnumerable<ParticipantDto>>(StatusCodes.Status200OK);

        // GET: /api/participants/{id}
        group.MapGet("/{id}", async (Guid id, IParticipantService service) =>
        {
            try
            {
                var participant = await service.GetParticipantByIdAsync(id);
                return Results.Ok(participant);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"Participant with ID {id} not found");
            }
        })
        .WithName("GetParticipantById")
        .Produces<ParticipantDto>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status404NotFound);

        // GET: /api/participants/{id}/with-registrations
        group.MapGet("/{id}/with-registrations", async (Guid id, IParticipantService service) =>
        {
            try
            {
                var participant = await service.GetParticipantWithRegistrationsAsync(id);
                return Results.Ok(participant);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"Participant with ID {id} not found");
            }
        })
        .WithName("GetParticipantWithRegistrations")
        .Produces<ParticipantDto>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status404NotFound);

        // POST: /api/participants
        group.MapPost("/", async (CreateParticipantDto dto, IParticipantService service) =>
        {
            try
            {
                var participant = await service.CreateParticipantAsync(dto);
                return Results.CreatedAtRoute("GetParticipantById", new { id = participant.Id }, participant);
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
        .WithName("CreateParticipant")
        .Produces<ParticipantDto>(StatusCodes.Status201Created)
        .Produces<string>(StatusCodes.Status400BadRequest);

        // PUT: /api/participants/{id}
        group.MapPut("/{id}", async (Guid id, UpdateParticipantDto dto, IParticipantService service) =>
        {
            try
            {
                await service.UpdateParticipantAsync(id, dto);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"Participant with ID {id} not found");
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
        .WithName("UpdateParticipant")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<string>(StatusCodes.Status404NotFound)
        .Produces<string>(StatusCodes.Status400BadRequest);

        // DELETE: /api/participants/{id}
        group.MapDelete("/{id}", async (Guid id, IParticipantService service) =>
        {
            try
            {
                await service.DeleteParticipantAsync(id);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"Participant with ID {id} not found");
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .WithName("DeleteParticipant")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<string>(StatusCodes.Status404NotFound)
        .Produces<string>(StatusCodes.Status400BadRequest);
    }
}