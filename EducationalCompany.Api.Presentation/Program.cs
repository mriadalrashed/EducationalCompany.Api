using EducationalCompany.Api.Application.Extensions;
using EducationalCompany.Api.Infrastructure.Data;
using EducationalCompany.Api.Infrastructure.Extensions;
using EducationalCompany.Api.Presentation.Endpoints;
using EducationalCompany.Presentation.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddCors(options => 
options.AddPolicy("AllowReactApp",policy => { policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials(); }));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.UseAuthorization();

CourseEndpoints.MapCourseEndpoints(app);
TeacherEndpoints.MapTeacherEndpoints(app);
CourseOccasionEndpoints.MapCourseOccasionEndpoints(app);
CourseRegistrationEndpoints.MapCourseRegistrationEndpoints(app);
ParticipantEndpoints.MapParticipantEndpoints(app);

app.Run();
