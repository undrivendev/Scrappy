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
SELECT
    *
FROM
    ruledata rd
WHERE
    rd.ruleid = @ruleId
ORDER BY
    rd. "" timestamp "" DESC
LIMIT 1;
";
            return _mapper.Map<PsqlRuleDataDto, RuleData>(
                await conn.QuerySingleOrDefaultAsync<PsqlRuleDataDto>(sql, new {ruleId}));
        }

        public async Task EnsureInitialized()
        {
            await using var conn = new NpgsqlConnection(_parameters.ConnectionString);
            await conn.OpenAsync();
            
            var sqlCheck = @"
SELECT
    EXISTS (
        SELECT
        FROM
            pg_tables
        WHERE
            schemaname = 'public'
            AND tablename = 'ruledata');
";
            
            if (!await conn.ExecuteScalarAsync<bool>(sqlCheck))
            {
                var sqlCreate = @"CREATE TABLE public.ruledata (
    id serial NOT NULL,
    ruleid text NOT NULL,
    "" timestamp "" timestamp NOT NULL,
    value text NOT NULL,
    CONSTRAINT ruledata_pkey PRIMARY KEY (id),
    CONSTRAINT ruledata_un UNIQUE (ruleid, "" timestamp "")
);
";
                await conn.ExecuteAsync(sqlCreate);
            }
        }
    }
}