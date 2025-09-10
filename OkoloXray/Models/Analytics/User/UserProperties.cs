using Newtonsoft.Json;

namespace OkoloXray.Models.Analytics.User
{
    public class UserProperties
    {
        [JsonProperty("customer_tier")]
        public UserTier CustomerTier;

        public UserProperties(UserTier customerTier)
        {
            this.CustomerTier = customerTier;
        }
    }
}