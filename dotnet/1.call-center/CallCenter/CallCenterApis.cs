using Azure.AI.OpenAI;
using Azure.AI.TextAnalytics;
using Azure.Core;
using CallCenter.Options;
using CallCenter.Text;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CallCenter;

static class CallCenterApis
{
    private const string FollowupPrompt = """"
            
            """
            上記の会話内容を、次の4項目について回答しなさい:
            Q1>. 簡潔な文章に要約:
            Q2>. センチメントスコアを0～100の間で評価し、数値で回答:
            Q3>. キーエンティティを抽出:
            Q4>. 該当するカテゴリはどれか [情報,質問,技術,クレーム,依頼,その他]
            """
            """";

    public static WebApplication MapCallCenterApis(this WebApplication app)
    {
        app.MapPost("/token", GetTokenAsync);
        app.MapPost("/sentiment", AnalyzeSentimentAsync);
        app.MapPost("/gpt", CallGptAsync);
        return app;
    }

    public static async Task<TokenResponseBody> GetTokenAsync(
        IOptions<SpeechToTextOptions> speechToTextOptions, 
        TokenCredential tokenCredential,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(CallCenterApis));
        try
        {
            var tokenResult = await tokenCredential.GetTokenAsync(
                new TokenRequestContext(new[] { "https://cognitiveservices.azure.com/.default" }), 
                cancellationToken);
            return new TokenResponseBody(
                Status.OK,
                AzureToken: tokenResult.Token,
                ResourceId: speechToTextOptions.Value.ResourceId,
                SpeechRegion: speechToTextOptions.Value.Region);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "トークンの取得中にエラーが発生しました。");
            return new TokenResponseBody(
                Status.Error,
                "",
                "",
                "",
                "トークンの取得中にエラーが起きました。");
        }
    }

    public static async Task<GptResponseBody> CallGptAsync(GptRequestBody body,
        OpenAIClient openAIClient,
        IOptions<AOAIOptions> aoaiOptions,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        static string ExtractCategory(Regex pattern, string text)
        {
            var match = pattern.Matches(text).FirstOrDefault();
            if (match is null)
            {
                return "";
            }

            return match.Groups[2].Value
                .Replace(".", "")
                .Replace(">", "")
                .Trim();
        }

        var logger = loggerFactory.CreateLogger(nameof(CallCenterApis));
        try
        {
            var mainPrompt = body.Transcription.Length > 2600 ? 
                body.Transcription.Substring(0, 2600) :
                body.Transcription;

            var result = await openAIClient.GetCompletionsAsync(
                aoaiOptions.Value.Model,
                new CompletionsOptions
                {
                    Prompts =
                    {
                        $"{mainPrompt}{FollowupPrompt}",
                    },
                    Temperature = 0,
                    MaxTokens = 400,
                },
                cancellationToken);
            var text = result.Value.Choices[0].Text.Replace("\n", "").Replace(" .", ".").Trim();
            return new GptResponseBody(
                Status.OK,
                ExtractCategory(AnswerExtractPatterns.PatternQ1(), text).Trim('.'),
                ExtractCategory(AnswerExtractPatterns.PatternQ2(), text).Replace(".", ""),
                ExtractCategory(AnswerExtractPatterns.PatternQ3(), text).Trim('.').Split(","),
                ExtractCategory(AnswerExtractPatterns.PatternQ4(), text).Trim('.'));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OpenAI の呼び出し中にエラーが発生しました。");
            return new GptResponseBody(Status.Error,
                "", "", Array.Empty<string>(), "",
                new[] { "OpenAI の呼び出し中にエラーが発生しました。" });
        }
    }

    public static async Task<SentimentResponseBody> AnalyzeSentimentAsync(SentimentRequestBody body,
        TextAnalyticsClient textAnalyticsClient,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(CallCenterApis));
        try
        {
            var document = body.Documents.FirstOrDefault();
            if (document is null)
            {
                return new SentimentResponseBody(Status.Error, Array.Empty<SentimentConfidenceScore>(), new[] { "データが空です。" });
            }

            var result = await textAnalyticsClient.AnalyzeSentimentAsync(
                document.Text,
                document.Language,
                cancellationToken: cancellationToken);

            var scores = result.Value.ConfidenceScores;
            return new SentimentResponseBody(
                Status.OK,
                new[]
                {
                    new SentimentConfidenceScore(scores.Negative, scores.Neutral, scores.Positive),
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "感情分析中にエラーが発生しました。");
            return new SentimentResponseBody(Status.Error, Array.Empty<SentimentConfidenceScore>(), new[] { "感情分析中にエラーが発生しました。" });
        }
    }
}

static class Status
{
    public const string OK = nameof(OK);
    public const string Error = nameof(Error);
}

record TokenResponseBody(
    string Status,
    [property:JsonPropertyName("azure_token")]
    string AzureToken,
    [property:JsonPropertyName("resource_id")]
    string ResourceId,
    [property:JsonPropertyName("speech_region")]
    string SpeechRegion,
    string? Error = null);

record GptRequestBody(string Transcription);
record GptResponseBody(string Status, 
    string Q1,
    string Q2,
    string[] Q3,
    string Q4,
    string[]? Error = null);
record SentimentRequestBody(SentimentDocument[] Documents);
record SentimentDocument(string Id, string Language, string Text);
record SentimentResponseBody(
    string Status,
    [property:JsonPropertyName("confidence_scores")]
    IEnumerable<SentimentConfidenceScore> ConfidenceScores,
    string[]? Error = null);
record SentimentConfidenceScore(double Negative, double Neutral, double Positive);
