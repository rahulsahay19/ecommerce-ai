import { Component, signal } from '@angular/core';
import { Navbar } from "./core/navbar/navbar";
import { RouterOutlet } from '@angular/router';
import { Loading } from './core/loading/loading';
import { ChatBotComponent } from './core/chatbot/chatbot';


@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Navbar, Loading, ChatBotComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('Client');
}
