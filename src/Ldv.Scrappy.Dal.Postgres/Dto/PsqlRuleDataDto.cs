using System;

namespace Ldv.Scrappy.Dal.Postgres
{
    [Dapper.Contrib.Extensions.Table("ruledata")]
    public class PsqlRuleDataDto
    {
        public int? id { get; set; }
        public string ruleid { get; set; }
        public DateTime timestamp { get; set; }
        public string value { get; set; }
    }
}