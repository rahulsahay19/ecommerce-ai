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
    //signals
    isOpen = signal(false);
    isTyping = signal(false);
    userInput = signal('');
    messages = signal<ChatMessage[]>([]);
    private chatService = inject(ChatService);

    toggleChat(){
        this.isOpen.update((open) => !open);
    }

    onInput(event: Event){
        const target = event.target as HTMLInputElement;
        this.userInput.set(target.value);
    }

    sendMessage(){
        const text = this.userInput().trim();
        if(!text) return;

        //Add user message 
        this.messages.update(msgs=> [...msgs, {role: 'user', content: text}]);
        this.userInput.set('');
        this.isTyping.set(true);

        //Call backened
        this.chatService.ask(text).subscribe({
            next:(res) =>{
                this.messages.update(msgs=> [...msgs, {role: 'bot',content: res.answer, products:res.products ?? []}]);
                this.isTyping.set(false);
            },
            error: () =>{
                this.messages.update(msgs => [...msgs, {role:'assisteant', content: '⚠️ Error contacting server'}]);
                this.isTyping.set(false);
            }
        })
    }
}