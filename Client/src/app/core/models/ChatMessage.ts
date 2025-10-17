import { Product } from "../../store/models/Product";

export interface ChatMessage {
    role: 'user' | 'assisteant' | 'bot';
    content: string;
    products?: Product[];
}