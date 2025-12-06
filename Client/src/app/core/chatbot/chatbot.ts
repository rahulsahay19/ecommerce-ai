import { CommonModule } from "@angular/common";
import { Component, inject, signal } from "@angular/core";
import { RouterModule } from "@angular/router";
import { ChatMessage } from "../models/ChatMessage";
import { ChatService } from "../services/chat.service";

@Component({
    selector: 'app-chatbot',
    standalone: true,
    imports: [CommonModule, RouterModule],
    templateUrl: './chatbot.html',
    styleUrls: ['./chatbot.scss']
})
export class ChatBotComponent {

    // -----------------------------------------------------
    // UI STATE
    // -----------------------------------------------------
    isOpen = signal(false);
    isTyping = signal(false);
    isRecording = signal(false);
    userInput = signal('');
    messages = signal<ChatMessage[]>([]);

    // Audio Recording
    private audioContext?: AudioContext;
    private processor?: ScriptProcessorNode;
    private inputNode?: MediaStreamAudioSourceNode;
    private wavChunks: Float32Array[] = [];
    private recordingStopped = false;

    // Prevent double TTS audio
    private currentAudio?: HTMLAudioElement;
    isPlaying = false;

    private chatService = inject(ChatService);

    // -----------------------------------------------------
    // TOGGLE CHAT WINDOW
    // -----------------------------------------------------
    toggleChat() {
        this.isOpen.update(open => !open);
    }

    // -----------------------------------------------------
    // TEXT INPUT
    // -----------------------------------------------------
    onInput(event: Event) {
        const t = event.target as HTMLInputElement;
        this.userInput.set(t.value);
    }

    // -----------------------------------------------------
    // SEND MESSAGE (TEXT)
    // -----------------------------------------------------
    sendMessage() {
        const text = this.userInput().trim();
        if (!text) return;

        this.messages.update(m => [...m, { role: "user", content: text }]);

        this.userInput.set('');
        this.isTyping.set(true);

        this.chatService.ask(text).subscribe({
            next: (res) => {
                this.addBotMessage(res.answer, res.products);
                this.isTyping.set(false);
            },
            error: () => {
                this.addBotMessage("⚠️ Error contacting server");
                this.isTyping.set(false);
            }
        });
    }

    private addBotMessage(content: string, products?: any[]) {
        this.messages.update(m => {
            const last = m[m.length - 1];

            // Avoid duplicate bot messages
            if (last && last.role === "bot" && last.content === content) {
                return m;
            }

            return [
                ...m,
                {
                    role: "bot",
                    content,
                    products: products ?? []
                }
            ];
        });
    }

    // -----------------------------------------------------
    // START RECORDING
    // -----------------------------------------------------
    startRecording() {
        this.recordingStopped = false;

        navigator.mediaDevices.getUserMedia({ audio: true }).then(stream => {
            this.isRecording.set(true);

            this.audioContext = new AudioContext();
            this.processor = this.audioContext.createScriptProcessor(4096, 1, 1);
            this.inputNode = this.audioContext.createMediaStreamSource(stream);

            this.wavChunks = [];

            this.processor.onaudioprocess = (event) => {
                if (this.recordingStopped) return;
                this.wavChunks.push(new Float32Array(event.inputBuffer.getChannelData(0)));
            };

            this.inputNode.connect(this.processor);
            this.processor.connect(this.audioContext.destination);
        });
    }

    // -----------------------------------------------------
    // STOP RECORDING → STT
    // -----------------------------------------------------
    async stopRecording() {
        if (!this.audioContext || !this.processor) return;
        if (this.recordingStopped) return;

        this.recordingStopped = true;
        this.isRecording.set(false);

        // FULL shutdown of AudioContext → prevents duplicate playback issues
        this.processor.disconnect();
        this.inputNode?.disconnect();

        await this.audioContext.close();

        const pcm = this.mergeBuffers(this.wavChunks);
        const down = this.downsample(pcm, this.audioContext.sampleRate, 16000);
        const wavBlob = this.encodeWAV(down, 16000);

        this.sendAudioToBackend(wavBlob);
    }

    // Merge PCM buffers
    private mergeBuffers(chunks: Float32Array[]) {
        const len = chunks.reduce((a, c) => a + c.length, 0);
        const merged = new Float32Array(len);

        let offset = 0;
        for (const c of chunks) {
            merged.set(c, offset);
            offset += c.length;
        }
        return merged;
    }

    // Downsample PCM data
    private downsample(samples: Float32Array, original: number, target: number) {
        if (original === target) return samples;

        const ratio = original / target;
        const newLen = Math.round(samples.length / ratio);
        const result = new Float32Array(newLen);

        let offset = 0;
        for (let i = 0; i < newLen; i++) {
            result[i] = samples[Math.floor(offset)];
            offset += ratio;
        }
        return result;
    }

    // Create WAV File
    private encodeWAV(samples: Float32Array, sampleRate: number): Blob {
        const buffer = new ArrayBuffer(44 + samples.length * 2);
        const view = new DataView(buffer);

        const write = (o: number, s: string) => {
            for (let i = 0; i < s.length; i++) view.setUint8(o + i, s.charCodeAt(i));
        };

        write(0, "RIFF");
        view.setUint32(4, 36 + samples.length * 2, true);
        write(8, "WAVE");
        write(12, "fmt ");
        view.setUint32(16, 16, true);
        view.setUint16(20, 1, true);
        view.setUint16(22, 1, true);
        view.setUint32(24, sampleRate, true);
        view.setUint32(28, sampleRate * 2, true);
        view.setUint16(32, 2, true);
        view.setUint16(34, 16, true);
        write(36, "data");
        view.setUint32(40, samples.length * 2, true);

        let offset = 44;
        for (let i = 0; i < samples.length; i++, offset += 2) {
            const s = Math.max(-1, Math.min(1, samples[i]));
            view.setInt16(offset, s < 0 ? s * 0x8000 : s * 0x7fff, true);
        }

        return new Blob([view], { type: "audio/wav" });
    }

    // -----------------------------------------------------
    // SEND AUDIO → STT → BOT MESSAGE
    // -----------------------------------------------------
    private sendAudioToBackend(wavBlob: Blob) {
        this.isTyping.set(true);

        this.chatService.transcribe(wavBlob).subscribe({
            next: (res) => {
                const text = (res?.text || "").trim();

                if (!text) {
                    this.addBotMessage("⚠️ Could not transcribe audio");
                    this.isTyping.set(false);
                    return;
                }

                this.messages.update(m => [...m, { role: "user", content: text }]);

                this.chatService.ask(text).subscribe({
                    next: (chatRes) => {
                        this.addBotMessage(chatRes.answer, chatRes.products);
                        this.isTyping.set(false);
                    }
                });
            },
            error: () => {
                this.addBotMessage("⚠️ Error transcribing audio");
                this.isTyping.set(false);
            }
        });
    }

    // -----------------------------------------------------
    // PLAY LAST BOT MESSAGE — **NO MORE DOUBLE AUDIO **
    // -----------------------------------------------------
    playLastBotMessage() {

        // Stop any previous audio completely
        if (this.currentAudio) {
            this.currentAudio.pause();
            this.currentAudio.currentTime = 0;
            this.currentAudio = undefined;
        }

        if (this.isPlaying) return;

        const lastBot = [...this.messages()]
            .reverse()
            .find(m => m.role === "bot" && m.content?.trim());

        if (!lastBot) return;

        this.isPlaying = true;

        this.chatService.synthesize(lastBot.content).subscribe({
            next: (blob) => {
                const url = URL.createObjectURL(blob);

                this.currentAudio = new Audio(url);

                this.currentAudio.onended = () => {
                    this.isPlaying = false;
                    this.currentAudio = undefined;
                    URL.revokeObjectURL(url);
                };

                this.currentAudio.play().catch(() => {
                    this.isPlaying = false;
                });
            },
            error: () => {
                this.isPlaying = false;
            }
        });
    }
}
