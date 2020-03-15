namespace Ldv.Scrappy.Bll
{
    public interface IMapper
    {
        TDestination Map<TSource, TDestination>(TSource source);
    }
}