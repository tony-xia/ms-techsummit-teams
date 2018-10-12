using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MessageExt.Controllers
{
    [ApiController]
    public class MessagingExtensionController : ControllerBase
    {
        private readonly ILogger<MessagingExtensionController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public MessagingExtensionController(ILogger<MessagingExtensionController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost]
        [Route("api/extension")]
        public async Task<IActionResult> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Invoke)
            {
                if (activity.IsComposeExtensionQuery())
                {
                    // This is the response object that will get sent back to the messaging extension request.
                    var invokeResponse = new ComposeExtensionResponse();

                    // This helper method gets the query as an object.
                    var query = activity.GetComposeExtensionQueryData();

                    if (query.CommandId != null && query.Parameters != null && query.Parameters.Count > 0)
                    {
                        var keyword = string.Empty;
                        if (query.Parameters[0].Name != "initialRun")
                        {
                            keyword = query.Parameters[0].Value.ToString();
                        }

                        var stories = await SearchStories(keyword);
                        var results = new ComposeExtensionResult()
                        {
                            AttachmentLayout = "list",
                            Type = "result",
                            Attachments = BuildAttachments(stories)
                        };
                        invokeResponse.ComposeExtension = results;
                    }

                    // Return the response
                    return Ok(invokeResponse);
                }
            }

            // Failure case catch-all.
            return BadRequest("Invalid request! This API supports only messaging extension requests. Check your query and try again");
        }

        private List<ComposeExtensionAttachment> BuildAttachments(IList<StoryDocument> stories)
        {
            var attachments = new List<ComposeExtensionAttachment>();
            foreach (var story in stories)
            {
                var attachment = new ComposeExtensionAttachment
                {
                    ContentType = HeroCard.ContentType,
                    Content = CreateCard(story),
                    Preview = new Attachment()
                    {
                        ContentType = ThumbnailCard.ContentType,
                        Content = CreatePreviewCard(story),
                    }
                };
                attachments.Add(attachment);
            }
            return attachments;
        }

        private HeroCard CreateCard(StoryDocument story)
        {
            return new HeroCard()
            {
                Title = story.CustomerName.FirstOrDefault(),
                Subtitle = story.IndustryName.FirstOrDefault(),
                Text = story.Headline,
                Images = new List<CardImage>()
                {
                    new CardImage()
                    {
                        Url = story.ImageUrl
                    }
                },
                Buttons = new List<CardAction>() 
                {
                    new CardAction()
                    {
                        Type = "openUrl",
                        Title = "Detail",
                        Value = "https://customers.microsoft.com/en-us/story/" + story.Id
                    }
                }
            };
        }

        private ThumbnailCard CreatePreviewCard(StoryDocument story)
        {
            return new ThumbnailCard()
            {
                Title = story.CustomerName.FirstOrDefault(),
                Subtitle = story.IndustryName.FirstOrDefault(),
                Images = new List<CardImage>()
                {
                    new CardImage()
                    {
                        Url = story.ImageUrl
                    }
                }
            };
        }

        private async Task<List<StoryDocument>> SearchStories(string keyword)
        {
            using (var client = _httpClientFactory.CreateClient())
            {
                var payload = JsonConvert.SerializeObject(new StorySearchRequest() { Text = keyword });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var httpResponse = await client.PostAsync("https://customers.microsoft.com/en-us/api/search", content);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get stories. Http StatusCode: {0}", httpResponse.StatusCode);
                    return new List<StoryDocument>();
                }

                var responseString = await httpResponse.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<StorySearchResponse>(responseString);
                if (response.SearchResult.Results == null)
                {
                    _logger.LogInformation("No matched document is found. Keyword: {0}", keyword);
                    return new List<StoryDocument>();
                }

                return response.SearchResult.Results.Select(r => r.Document).ToList();
            }
        }
    }

    public class StoryDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("story_customer_name")]
        public List<string> CustomerName { get; set; }
        [JsonProperty("story_industry_friendlyname")]
        public List<string> IndustryName { get; set; }
        [JsonProperty("story_search_results_image")]
        public string ImageUrl { get; set; }
        [JsonProperty("story_headline")]
        public string Headline { get; set; }
    }

    public class StorySearchRequest
    {
        [JsonProperty("facet_filters")]
        public object[] FacetFilters { get; } = new object[0];
        [JsonProperty("related_documents")]
        public object[] RelatedDocuments { get; } = new object[0];
        [JsonProperty("featured_sections")]
        public object FeaturedSections { get; } = null;
        [JsonProperty("page_id")]
        public string PageId { get; set; }= "0";
        [JsonProperty("sort_mode")]
        public string SortMode { get; set; }= "cam_rank desc";
        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;
    }

    public class StorySearchResponse
    {
        public class ScoredStoryDocument
        {
            public double Score { get; set; }
            public StoryDocument Document { get; set; }
        }

        public class ResultResponse
        {
            public List<ScoredStoryDocument> Results { get; set; }
        }

        [JsonProperty("search_result")]
        public ResultResponse SearchResult { get; set; }
    }
}
