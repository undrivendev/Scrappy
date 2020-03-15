using AutoMapper;
using Ldv.Scrappy.Bll;

namespace Ldv.Scrappy.Dal.Postgres
{
    public static class AutoMapperConfiguration
    {
        public static void Configure(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<RuleData, PsqlRuleDataDto>()
                .ForMember(obj => obj.id, expression => expression.Ignore());
            cfg.CreateMap<PsqlRuleDataDto, RuleData>();
        }
    }
}