import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { ChatResponse } from "../models/ChatResponse";

@Injectable({providedIn: 'root'})
export class ChatService {
    private http = inject(HttpClient);
    private apiUrl = 'https://localhost:7138/chat/ask';

    ask(message: string): Observable<ChatResponse>{
        return this.http.post<ChatResponse>(this.apiUrl, {message});
    }
}