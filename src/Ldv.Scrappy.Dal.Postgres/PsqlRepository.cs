using System;
using System.Threading.Tasks;
using AutoMapper;
using Dapper;
using Dapper.Contrib.Extensions;
using Ldv.Scrappy.Bll;
using Npgsql;

namespace Ldv.Scrappy.Dal.Postgres
{
    public class PsqlRepository : IRepository
    {
        private readonly PsqlRepositoryParameters _parameters;
        private readonly Bll.IMapper _mapper;

        public PsqlRepository(PsqlRepositoryParameters parameters, Bll.IMapper mapper)
        {
            _parameters = parameters;
            _mapper = mapper;
        }

        public async Task Save(RuleData data)
        {
            await using var conn = new NpgsqlConnection(_parameters.ConnectionString);
            await conn.OpenAsync();
            var dbDto = _mapper.Map<RuleData, PsqlRuleDataDto>(data);
            var newId = await conn.InsertAsync(dbDto);
        }

        public async Task<RuleData> GetLastByRuleId(string ruleId)
        {
            await using var conn = new NpgsqlConnection(_parameters.ConnectionString);
            await conn.OpenAsync();
            var sql = @"
select
	*
from
	ruledata rd
where
	rd.ruleid = @ruleId
order by
	rd.""timestamp"" desc
limit 1";
            return _mapper.Map<PsqlRuleDataDto, RuleData>(
                await conn.QuerySingleOrDefaultAsync<PsqlRuleDataDto>(sql, new {ruleId}));
        }
    }
}