using Newtonsoft.Json;

namespace Ldv.Scrappy.Bll
{
    public static class JsonSerializer
    {
        public static string Serialize(object input) 
            => JsonConvert.SerializeObject(input, Formatting.None);
    }
}