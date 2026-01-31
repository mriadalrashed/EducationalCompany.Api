namespace EducationalCompany.Api.Domain.Interfaces
{
    public interface IMapperService
    {
        TDestination Map<TDestination>(object source);
        TDestination Map<TSource, TDestination>(TSource source);
        TDestination Map<TSource, TDestination>(object source, TDestination destination);
    }
}
