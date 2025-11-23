# WPC 2025 - Building Pro-Code Agents with Microsoft 365

This repository contains demo projects for the **WPC 2025 Conference in Milan**, showcasing how to build professional, production-ready agents within the Microsoft 365 ecosystem. The session demonstrates practical implementations of intelligent agents using the Microsoft 365 Agents SDK and Toolkit.

## Overview

This repository includes two comprehensive demonstrations that illustrate different approaches to building Microsoft 365 agents:

1. **EchoAgent** - A foundational custom engine agent template
2. **Travel Agent** - An advanced intelligent travel assistant with document retrieval capabilities

Both projects leverage the [Microsoft 365 Agents SDK](https://github.com/Microsoft/Agents) and demonstrate best practices for building, debugging, and deploying agents in Microsoft Teams and Microsoft 365 environments.

## Projects

### 1. EchoAgent

A basic custom engine agent template built with TypeScript/Node.js that demonstrates the fundamentals of agent development. This agent responds to user questions similar to ChatGPT, enabling natural conversations using a custom engine.

**Key Features:**
- Built on Microsoft 365 Agents SDK
- Integration with Azure OpenAI for natural language processing
- Support for Microsoft 365 Agents Playground debugging
- Clean, extensible architecture for customization

**Technology Stack:**
- Node.js (18, 20, or 22)
- TypeScript
- Azure OpenAI
- Microsoft 365 Agents Toolkit

**Quick Start:**
```bash
cd EchoAgent
# Configure Azure OpenAI in env/.env.playground.user
# Press F5 to debug in Microsoft 365 Agents Playground
```

[View detailed EchoAgent documentation →](./EchoAgent/README.md)

### 2. Travel Agent

An intelligent travel assistant that provides comprehensive travel support, including policy compliance checking, flight searches, and hotel recommendations. This advanced sample demonstrates integration with Microsoft 365 Retrieval API to access company documents and policies stored in SharePoint or OneDrive for Business.

**Key Features:**
- Azure OpenAI integration for conversational AI
- Microsoft 365 Retrieval API for document search and grounding
- Custom plugins for specialized travel assistance
- Policy-compliant travel recommendations
- Real-time flight and hotel search capabilities
- Authentication and authorization with Microsoft 365 services

**Technology Stack:**
- .NET 9.0
- C#
- Azure OpenAI
- Microsoft 365 Agents Toolkit for Visual Studio
- Microsoft 365 Retrieval API
- .NET Aspire (AppHost)

**Quick Start:**
```bash
cd travel-agent
# Configure Azure OpenAI in env/.env.local.user
# Upload sample documents to OneDrive for Business
# Configure retrieval plugin settings
# Press F5 to debug in Microsoft Teams
```

[View detailed Travel Agent documentation →](./travel-agent/README.md)

## Prerequisites

### Common Requirements
- **Microsoft 365 tenant** with administrative permissions
- **Azure subscription** with appropriate permissions
- **Azure OpenAI resource**: [Request access](https://aka.ms/oai/access)

### For EchoAgent
- [Node.js](https://nodejs.org/) (versions 18, 20, or 22)
- [Microsoft 365 Agents Toolkit VS Code Extension](https://aka.ms/teams-toolkit)

### For Travel Agent
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Visual Studio 2026](https://visualstudio.microsoft.com/)
- [Microsoft 365 Agents Toolkit for Visual Studio](https://aka.ms/teams-toolkit-vs)

## What You'll Learn

This session and these demos illustrate:

- **Agent Architecture**: How to structure and build intelligent agents from scratch
- **Natural Language Processing**: Integration with Azure OpenAI for conversational experiences
- **Document Retrieval**: Using Microsoft 365 Retrieval API to access organizational knowledge
- **Custom Plugins**: Extending agent capabilities with specialized functionality
- **Authentication**: Implementing secure authentication flows with Microsoft 365
- **Debugging & Testing**: Local development and testing with Microsoft 365 Agents Playground
- **Deployment**: Publishing agents to Microsoft Teams and Azure
- **Best Practices**: Production-ready patterns and architectural considerations

## Repository Structure

```
WPC2025-CopilotAgent/
├── EchoAgent/                    # Node.js TypeScript agent template
│   ├── src/                      # Source code
│   ├── appPackage/               # Application manifest templates
│   ├── env/                      # Environment configurations
│   └── infra/                    # Azure provisioning templates
│
├── travel-agent/                 # .NET travel assistant agent
│   ├── TravelAgent/              # Main agent application
│   ├── M365Agent/                # Microsoft 365 agent project
│   ├── Flights.Api/              # Flight search API (MCP)
│   └── TravelAgent.AppHost/      # .NET Aspire host
│
├── README.md                     # This file
├── LICENSE
├── CODE_OF_CONDUCT.md
└── SECURITY.md
```

## Getting Started

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-org/WPC2025-CopilotAgent.git
   cd WPC2025-CopilotAgent
   ```

2. **Choose your project**
   - For a foundational understanding: Start with **EchoAgent**
   - For advanced scenarios: Explore **Travel Agent**

3. **Follow project-specific setup**
   - Each project contains detailed setup instructions in its README
   - Configure Azure OpenAI credentials
   - Set up Microsoft 365 accounts and permissions

4. **Debug locally**
   - Use Microsoft 365 Agents Playground (EchoAgent)
   - Use Microsoft Teams browser debugging (Travel Agent)

## Additional Resources

- [Microsoft 365 Agents SDK Documentation](https://github.com/Microsoft/Agents)
- [Microsoft 365 Agents Toolkit Documentation](https://docs.microsoft.com/microsoftteams/platform/toolkit/teams-toolkit-fundamentals)
- [Microsoft 365 Agents Toolkit CLI](https://aka.ms/teamsfx-toolkit-cli)
- [Microsoft 365 Agents Samples](https://github.com/OfficeDev/TeamsFx-Samples)
- [Azure OpenAI Documentation](https://learn.microsoft.com/azure/ai-services/openai/)
- [Microsoft 365 Retrieval API](https://learn.microsoft.com/microsoft-365-copilot/extensibility/api/ai-services/retrieval/copilotroot-retrieval)

## Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit the [CLA page](https://cla.opensource.microsoft.com/).

Please review our [Code of Conduct](./CODE_OF_CONDUCT.md) before contributing.

## Support

For questions and support, please refer to [SUPPORT.md](./SUPPORT.md).

## Security

For security concerns, please review our [Security Policy](./SECURITY.md).

## License

This project is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.

---

**WPC 2025 Conference - Milan**  
*Building Pro-Code Agents with Microsoft 365*
