namespace Ldv.Scrappy.Bll
{
    public class AutoMapperWrapper : IMapper
    {
        private readonly AutoMapper.IMapper _mapper;

        public AutoMapperWrapper(AutoMapper.IMapper mapper)
        {
            _mapper = mapper;
        }

        public TDestination Map<TSource, TDestination>(TSource source) => _mapper.Map<TSource, TDestination>(source);

    }
}