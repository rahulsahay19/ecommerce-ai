
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace AiService.Services
{
    public class VoiceService : IVoiceService
    {
        private readonly string _speechKey;
        private readonly string _speechRegion;

        public VoiceService(IConfiguration config)
        {
            _speechKey = config["AzureSpeech:ApiKey"] ?? throw new ArgumentNullException("Azure Speech Key is missing");
            _speechRegion = config["AzureSpeech:Region"] ?? throw new ArgumentNullException("Azure Speech Region is missing");
        }
        public async Task<string> TranscribeAsync(IFormFile audioFile, CancellationToken ct)
        {
            //integrate azure speech to text
            var speechConfig = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
            speechConfig.SpeechRecognitionLanguage = "en-US";

            using var audioStream = audioFile.OpenReadStream();
            using var audioFormat = AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1);
            using var reader = new BinaryAudioStreamReader(audioStream);
            using var audioInput = AudioConfig.FromStreamInput(reader, audioFormat);

            using var recognizer = new SpeechRecognizer(speechConfig, audioInput);

            var result = await recognizer.RecognizeOnceAsync().WaitAsync(ct);

            switch (result.Reason)
            {
                case ResultReason.RecognizedSpeech:
                    return result.Text;
                case ResultReason.NoMatch:
                    return "No speech recognized";
                case ResultReason.Canceled:
                    var cancellation = CancellationDetails.FromResult(result);
                    return $"Canceled: {cancellation.ErrorDetails}";
                default:
                    return string.Empty;
            }            
        }
        public async Task<byte[]> SynthesizeAsync(string text, CancellationToken ct)
        {
            var speechConfig = SpeechConfig.FromSubscription(_speechKey, _speechRegion);

            speechConfig.SpeechSynthesisVoiceName = "en-US-AnaNeural";
            speechConfig.SetSpeechSynthesisOutputFormat(
                SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm);

            // DO NOT use BinaryAudioOutputStream here.
            using var synthesizer = new SpeechSynthesizer(speechConfig);

            var result = await synthesizer.SpeakTextAsync(text).WaitAsync(ct);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                // This ALWAYS contains a valid WAV file with a proper RIFF header.
                return result.AudioData;
            }

            if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                throw new Exception($"TTS Error: {cancellation.ErrorDetails}");
            }

            return Array.Empty<byte>();
        }

    }

    // -----------------------------------------------------
    // HELPER: BinaryAudioStreamReader (for wav/webm streams)
    // -----------------------------------------------------
    public class BinaryAudioStreamReader : PullAudioInputStreamCallback
    {
        private readonly Stream _stream;

        public BinaryAudioStreamReader(Stream stream)
        {
            _stream = stream;
        }

        public override int Read(byte[] buffer, uint size)
        {
            return _stream.Read(buffer, 0, (int)size);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _stream.Dispose();

            base.Dispose(disposing);
        }
    }

    // -----------------------------------------------------
    // HELPER: BinaryAudioOutputStream (TTS to MemoryStream)
    // -----------------------------------------------------
    public class BinaryAudioOutputStream : PushAudioOutputStreamCallback
    {
        private readonly Stream _output;

        public BinaryAudioOutputStream(Stream output)
        {
            _output = output;
        }

        public override uint Write(byte[] buffer)
        {
            _output.Write(buffer, 0, buffer.Length); 
            return (uint)buffer.Length;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _output.Dispose();

            base.Dispose(disposing);
        }
    }
}

