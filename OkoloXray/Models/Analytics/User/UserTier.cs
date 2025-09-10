using Newtonsoft.Json;

namespace OkoloXray.Models.Analytics.User
{
    public class UserTier
    {
        [JsonProperty("value")]
        public string Value;

        public UserTier(string value)
        {
            this.Value = value;
        }
    }
}