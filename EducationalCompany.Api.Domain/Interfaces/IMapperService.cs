namespace EducationalCompany.Api.Domain.Interfaces
{
    // Defines a contract for object mapping (e.g., Entity to DTO)
    public interface IMapperService
    {
        // Maps a source object to a new destination type
        TDestination Map<TDestination>(object source);

        // Maps a source type to a destination type
        TDestination Map<TSource, TDestination>(TSource source);

        // Maps values from source object into an existing destination object
        TDestination Map<TSource, TDestination>(object source, TDestination destination);
    }
}
