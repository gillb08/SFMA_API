using AutoMapper;
using SFMA_API.Models.Dtos.Response;

namespace SFMA_API.Models.Configuration.MappingConfiguration
{
    public class PagedResponseConfiguration : Profile
    {
        public PagedResponseConfiguration()
        {
            CreateMap(typeof(PagedList<>), typeof(PagedResponse<>))
                .ConvertUsing(typeof(PagedListToPagedResponseConverter<,>));
        }

        public class PagedListToPagedResponseConverter<TSource, TDestination> : ITypeConverter<PagedList<TSource>, PagedResponse<TDestination>> where TDestination : class
        {
            public PagedResponse<TDestination> Convert(PagedList<TSource> source, PagedResponse<TDestination> destination, ResolutionContext context)
            {
                var list = source.ToList();
                var items = context.Mapper.Map<IEnumerable<TSource>, IEnumerable<TDestination>>(list);

                var pagedResponse = new PagedResponse<TDestination>
                {
                    MetaData = source.MetaData,
                    Items = items
                };
                return pagedResponse;
            }
        }
    }
}
