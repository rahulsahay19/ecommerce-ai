using AiService.Models;
using AiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiService.Endpoints
{
    public static class VoiceEndpoints
    {
        public static IEndpointRouteBuilder MapVoice(this IEndpointRouteBuilder app) 
        {
            app.MapPost("/voice/stt", async (
                HttpRequest request,
                [FromServices] IVoiceService voiceService,
                CancellationToken ct) =>
            {
                //Expecting audio file as multipart/form-data
                if (!request.HasFormContentType)
                    return Results.BadRequest("Content type must be multipart/form-data.");

                var form = await request.ReadFormAsync(ct);
                var file = form.Files.FirstOrDefault();

                if (file == null)
                    return Results.BadRequest("Audio file is missing.");

                //convert speech to text using Azure SDK (implemented inside Voice Service)
                var text = await voiceService.TranscribeAsync(file, ct);
                return Results.Ok(new { text = text });
            });

            app.MapPost("/voice/tts", async (
                [FromBody] VoiceRequest requestModel,
                [FromServices] IVoiceService voiceService,
                CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(requestModel.Text))
                    return Results.BadRequest("Text input is required.");

                // Convert text to speech using Azure SDK
                var audioBytes = await voiceService.SynthesizeAsync(requestModel.Text, ct);
                return Results.File(audioBytes, "audio/wav");
            });
            return app;
        }
    }
}
