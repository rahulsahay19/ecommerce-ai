import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { ChatResponse } from "../models/ChatResponse";

@Injectable({ providedIn: 'root' })
export class ChatService {

  private http = inject(HttpClient);

  // Adjust if you deploy
  private baseUrl = 'https://localhost:7138';

  // ---------------------------------------------------
  // TEXT → CHATBOT  (LLM Response + Products)
  // ---------------------------------------------------
  ask(message: string): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(
      `${this.baseUrl}/chat/ask`,
      { message }
    );
  }

  // ---------------------------------------------------
  // AUDIO → SPEECH TO TEXT (Azure STT)
  //     Always returns: { text: string }
  // ---------------------------------------------------
  transcribe(audioBlob: Blob): Observable<{ text: string }> {
    const formData = new FormData();
    formData.append("file", audioBlob, "audio.wav");

    return this.http.post<{ text: string }>(
      `${this.baseUrl}/voice/stt`,
      formData
    );
  }

  // ---------------------------------------------------
  //  TEXT → AUDIO (Azure TTS)
  //     Returns WAV Blob for playback
  // ---------------------------------------------------
  synthesize(text: string): Observable<Blob> {
    return this.http.post(
      `${this.baseUrl}/voice/tts`,
      { text },
      { responseType: "blob" }  // MUST be blob for WAV
    );
  }
}
