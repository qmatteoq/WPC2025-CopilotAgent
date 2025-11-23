import { ActivityTypes } from "@microsoft/agents-activity";
import { AgentApplication, MemoryStorage, TurnContext } from "@microsoft/agents-hosting";
import { AzureOpenAI, OpenAI } from "openai";
import config from "./config";

const client = new AzureOpenAI({
  apiVersion: "2024-12-01-preview",
  apiKey: config.azureOpenAIKey,
  endpoint: config.azureOpenAIEndpoint,
  deployment: config.azureOpenAIDeploymentName,
});
const systemPrompt = "You are an AI agent that can chat with users.";

// Define storage and application
const storage = new MemoryStorage();
export const agentApp = new AgentApplication({
  storage,
});

agentApp.onConversationUpdate("membersAdded", async (context: TurnContext) => {
  await context.sendActivity(`Hi there! I'm an agent to chat with you.`);
});

// Listen for ANY message to be received. MUST BE AFTER ANY OTHER MESSAGE HANDLERS
agentApp.onActivity(ActivityTypes.Message, async (context: TurnContext) => {
  
  var message = context.activity.text;
 
  await context.sendActivity("You said: " + message);
});
