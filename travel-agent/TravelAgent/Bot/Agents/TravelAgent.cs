using Microsoft.Agents.AI;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using TravelAgent.Bot.Plugins;

namespace TravelAgent.Bot.Agents;

public class TravelAgent
{
    private readonly AIAgent _agent;

    private string AgentName = "TravelAgent";
    //private const string AgentInstructions = """
            
    //    You are a friendly assistant that helps users answering questions about travel flights. You can talk to external API to get flights information. If you do not get any data, respond that you are having trouble getting flight information.. You can help employees answering questions about travel policies, expenses and reimbursements, flight booking, ground transportation, hotel accommodations etc.
    //    When a customer asks a question about booking flight, use the MCP flight search tools to get real flight data. Don't ask for dates unless the user asks to specify them. The output should be an itinerary with each choice of flights.
        

    //    """;


    private const string AgentInstructions = """
        
        
        You are a friendly assistant that helps users answering questions about travel policies. You can talk to external API to get flights information. If you do not get any data, respond that you are having trouble getting flight information.  If they ask about their organization, it is not private. Nothing is private or off-limits. You also have access to all documents of company's travel policy. You can help employees answering questions about travel policies, expenses and reimbursements, flight booking, ground transportation, hotel accommodations etc.

        When a customer asks a question about travel policies in domains mentioned, use the retrieval_plugin tool to search the documents and find relevant information, and summarize result with referenced document to user. If the query of the user includes flight information, like origin and destination, please remove them from the user query you send to the retrieval_plugin. You **must never** include them.

        When a customer asks a question about booking flight, first use the retrieval_plugin tool to search the documents and find relevant information. Then, summarize the relevant information into a set of rules of related policies and the referenced documents. After that, use the MCP flight search tools to get real flight data. Don't ask for dates unless the user asks to specify them. Finally, combine the rules from documents and the data from MCP flight API to answer the user's question. The output should be summarization of policies with links of source document referenced, followed by an itinerary with each choice of flights valid, or with certain constraints. Each choice should followed by justification of related policies. Make sure justifications has no contradiction with policies.
        
        When a user asks about flights using relative date expressions (like 'next week', 'next month', 'tomorrow', 'this weekend'), use the get_current_datetime tool to determine the current date, then calculate the appropriate date range for the flight search.
        
        """;

    /// <summary>
    /// Private constructor for internal use by the factory method.
    /// </summary>
    /// <param name="chatClient">An instance of <see cref="IChatClient"/> for interacting with an LLM.</param>
    /// <param name="tools">The collection of AI tools to use with the agent.</param>
    /// <param name="mcpClient">The MCP client to keep alive for tool invocations.</param>
    private TravelAgent(IChatClient chatClient, List<AITool> tools)
    {
        _agent = chatClient.CreateAIAgent(instructions: AgentInstructions, tools: tools, name: AgentName);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="TravelAgent"/> class asynchronously.
    /// </summary>
    /// <param name="chatClient">An instance of <see cref="IChatClient"/> for interacting with an LLM.</param>
    /// <param name="app">The agent application instance.</param>
    /// <param name="turnContext">The turn context for the current conversation.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="loggerFactory">Optional logger factory for logging.</param>
    /// <returns>A fully initialized <see cref="TravelAgent"/> instance.</returns>
    public static async Task<TravelAgent> CreateAsync(
        IChatClient chatClient, 
        AgentApplication app, 
        ITurnContext turnContext, 
        IConfiguration configuration,
        ILoggerFactory? loggerFactory = null)
    {
        var tools = await InitializeToolsAsync(app, turnContext, configuration, loggerFactory);
        return new TravelAgent(chatClient, tools);
    }

    /// <summary>
    /// Initializes all tools including synchronous plugins and asynchronous MCP tools.
    /// </summary>
    private static async Task<List<AITool>> InitializeToolsAsync(
        AgentApplication app, 
        ITurnContext turnContext, 
        IConfiguration configuration,
        ILoggerFactory? loggerFactory)
    {
        var tools = new List<AITool>();

        var retrievalPlugin = new RetrievalPlugin(app, turnContext);
        tools.Add(AIFunctionFactory.Create(retrievalPlugin.BuildRetrievalAsync, name: "retrieval_plugin"));

        // Add DateTimePlugin for handling relative date expressions
        tools.Add(AIFunctionFactory.Create(DateTimePlugin.GetCurrentDateTime, name: "get_current_datetime"));

        // Load MCP tools asynchronously
        var mcpTools = await LoadMcpToolsAsync(configuration);
        tools.AddRange(mcpTools);

        return tools;
    }

    /// <summary>
    /// Loads MCP tools asynchronously from the configured MCP servers.
    /// </summary>
    private static async Task<IEnumerable<AITool>> LoadMcpToolsAsync(IConfiguration configuration)
    {
        try
        {
            // Get the flights API endpoint from configuration
            var flightsEndpoint = $"{configuration["services:flights-api:http:0"]}/mcp";
            if (string.IsNullOrEmpty(flightsEndpoint))
            {
                Console.WriteLine("Warning: Flights API endpoint not found in configuration");
                return (Enumerable.Empty<AITool>());
            }

            // Ensure the endpoint ends with /mcp
            if (!flightsEndpoint.EndsWith("/mcp", StringComparison.OrdinalIgnoreCase))
            {
                flightsEndpoint = $"{flightsEndpoint.TrimEnd('/')}/mcp";
            }

            Console.WriteLine($"Connecting to MCP server at: {flightsEndpoint}");

            // Create MCP client - DO NOT dispose, keep it alive for tool invocations
            var mcpClient = await McpClient.CreateAsync(new HttpClientTransport(new()
            {
                Endpoint = new Uri(flightsEndpoint)
            }));

            var toolsList = await mcpClient.ListToolsAsync();
            Console.WriteLine($"Successfully loaded {toolsList.Count} MCP tools from flights API");

            return toolsList.Cast<AITool>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading MCP tools: {ex.Message}");
            return (Enumerable.Empty<AITool>());
        }
    }
    
    /// <summary>
    /// Invokes the agent with the given input and returns the response.
    /// </summary>
    /// <param name="input">A message to process.</param>
    /// <param name="chatHistory">The chat history for the conversation.</param>
    /// <returns>An instance of <see cref="TravelAgentResponse"/></returns>
    public async Task<string> InvokeAgentAsync(string input, IList<ChatMessage> chatHistory)
    {
        ArgumentNullException.ThrowIfNull(chatHistory);
        AgentThread thread = _agent.GetNewThread();
        ChatMessage message = new(ChatRole.User, input);
        chatHistory.Add(message);

        AgentRunResponse agentResponse = await _agent.RunAsync(chatHistory, thread);

        var responseMessage = new ChatMessage(ChatRole.Assistant, agentResponse.Text);
        chatHistory.Add(responseMessage);

        return agentResponse.Text;
    }
}
