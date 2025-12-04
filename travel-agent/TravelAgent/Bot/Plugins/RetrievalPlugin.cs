using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder;

namespace TravelAgent.Bot.Plugins
{
    public class RetrievalPlugin
    {
        private static readonly HttpClient _httpClient = new();
        private readonly AgentApplication _app;
        private readonly ITurnContext _turnContext;

        public RetrievalPlugin(AgentApplication app, ITurnContext turnContext)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _turnContext = turnContext ?? throw new ArgumentNullException(nameof(turnContext));
        }

        /// <summary>
        /// Retrieve travel policies about expenses use graph API.
        /// </summary>
        /// <param name="userquery">The user query as a string</param>
        /// <returns></returns>
        [Description("This function talks to Microsoft 365 Copilot Retrieval API and gets travel policies about expenses and reimbursements, flight booking, ground transportation, hotel accommodations which are nicely formatted. " +
            "It accepts user query as input and send out a chunk of relevant text and a link to the file in the results.")]
        public async Task<string> BuildRetrievalAsync([Description("The query of the user.")]string userquery)
        {
            string accessToken = await _app.UserAuthorization.GetTurnTokenAsync(_turnContext, "graph");

            try
            {
                var requestBody = new
                {
                    queryString = userquery,
                    dataSource = "sharePoint",
                    filterExpression = "path:\"https://m365cpi95634428.sharepoint.com/sites/TravelPolicies\"",
                    resourceMetadata = new[] { "title" },
                    maximumNumberOfResults = 10
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/beta/copilot/retrieval");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"Error: {response.StatusCode} - {responseContent}";
                }

                return responseContent;
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.GetType().Name} - {ex.Message}\nStackTrace: {ex.StackTrace}";
            }
        }
    }
}