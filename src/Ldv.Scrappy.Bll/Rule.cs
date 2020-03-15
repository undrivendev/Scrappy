using System.Collections.Generic;

namespace Ldv.Scrappy.Bll
{
    public class Rule
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public IList<string> Selectors { get; set; }
        public string PreSaveScript { get; set; }
    }
}