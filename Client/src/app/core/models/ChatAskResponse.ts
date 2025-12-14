import { ChatResponse } from "./ChatResponse";

export interface ChatAskResponse {
  conversationId: string;
  response: ChatResponse;
}
