using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AI_SUPPORT_SERVICE
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatBotController : ControllerBase
    {
        #region Fields and Constructor

        private readonly ILogger<ChatBotController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;


        public ChatBotController(ILogger<ChatBotController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        #endregion

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                var _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                var _apiUrl = "https://api.openai.com/v1/chat/completions";
                if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_apiUrl))
                {
                    _logger.LogError("API Key or URL is missing from configuration.");
                    return StatusCode(500, "Server configuration error.");
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    messages = new[]
                    {
                        new { role = "user", content = request.Message }
                    }
                };

                var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_apiUrl, jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI API call failed: {StatusCode}, Response: {Response}", response.StatusCode, errorResponse);
                    return StatusCode((int)response.StatusCode, "Error processing request.");
                }

                var resContent = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(resContent);
                var responseText = data?.choices?[0]?.message?.content?.ToString()?.Trim() ?? "No response received.";

                return Ok(new { response = responseText });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to chatbot.");
                return StatusCode(500, new { message = "Error sending message to chatbot." });
            }
        }

        // Create a request model
        public class ChatRequest
        {
            public string Message { get; set; }
        }
    }
}
