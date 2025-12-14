import { Product } from "../../store/models/Product";

export interface ChatMessage {
    role: 'user' | 'assistant' | 'bot';
    content: string;
    products?: Product[];
}