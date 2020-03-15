using System.Threading.Tasks;

namespace Ldv.Scrappy.Bll
{
    public interface IRepository
    {
        Task Save(RuleData data);
        Task<RuleData> GetLastByRuleId(string ruleId);
    }
}