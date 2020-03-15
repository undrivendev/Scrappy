using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Html.Parser;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace Ldv.Scrappy.Bll
{
    public class ScrappyService
    {
        private readonly ScrappyServiceParameters _parameters;
        private readonly HttpClient _httpClient;
        private readonly IRepository _repository;
        private readonly ILogger _logger;
        private readonly INotifier _notifier;

        public ScrappyService(
            ScrappyServiceParameters parameters, 
            ILogger logger, 
            HttpClient httpClient, 
            IRepository repository,
            INotifier notifier)
        {
            _parameters = parameters;
            _logger = logger;
            _httpClient = httpClient;
            _repository = repository;
            _notifier = notifier;
        }

        public async Task DownloadAndNotify()
        {
            await Task.WhenAll(_parameters.Rules.Select(HandleRule));
        }

        private async Task HandleRule(Rule rule)
        {
            _logger.Log(new LogEntry(LoggingEventType.Debug, $"Handling rule: {JsonSerializer.Serialize(rule)}"));
            try
            {
                var response = await _httpClient.GetAsync(rule.Url);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var newValue = await ParseData(responseContent, rule.Selectors, rule.PreSaveScript);
                
                var existing = await _repository.GetLastByRuleId(rule.Id);
                if (existing is null || existing.Value != newValue)
                {
                    await _repository.Save(new RuleData() { Value =  newValue, Timestamp = DateTime.UtcNow, RuleId = rule.Id});
                    await _notifier.Send(rule, existing?.Value, newValue);
                }
            }
            catch (Exception e)
            {
                _logger.Log(new LogEntry(LoggingEventType.Error, "Error while scrapping data", e));
            }
            
        }

        /// <summary>
        /// https://github.com/AngleSharp/AngleSharp/blob/master/doc/Basics.md
        /// </summary>
        /// <returns></returns>
        private async Task<string> ParseData(string html, IList<string> selectors, string preSaveScript)
        {
            var context = BrowsingContext.New(AngleSharp.Configuration.Default);
            var document = context.GetService<IHtmlParser>().ParseDocument(html);
            var elementsHtml = selectors.SelectMany((s) => document.QuerySelectorAll(s).Select((e) => e.OuterHtml)).ToList();
            if (!string.IsNullOrWhiteSpace(preSaveScript))
            {
                elementsHtml = (await Task.WhenAll(elementsHtml.Select((e) => CSharpScript.EvaluateAsync<string>(string.Format(preSaveScript, e.Replace(@"""", @"""""")))))).ToList();
            }
            return JsonSerializer.Serialize(elementsHtml);
        }
    }

    
}