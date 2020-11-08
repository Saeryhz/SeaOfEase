using System.Collections.Generic;
using System.Net.Http;

namespace SeaOfEase.SeaOfThieves.Util
{
    public class Webhook
    {
        private static readonly HttpClient client = new HttpClient();
        public static async void SendMessage(string player, string destination)
        {
            var values = new Dictionary<string, string>
            {
                { "content", $"{player} is connecting to {destination}" }
            };
            var content = new FormUrlEncodedContent(values);
            await client.PostAsync(Constants.DiscordWebHook, content);
        }
    }
}
