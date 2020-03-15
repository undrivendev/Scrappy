using System.Collections.Generic;
using System.Security;

namespace Ldv.Scrappy.Bll
{
    public class SendGridNotifierParameters
    {
        public string ApiKey { get; set; }
        public IList<string> Recipients { get; set; }
    }
}