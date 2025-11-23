# Flights API - MCP Server

This API exposes flight data through both REST endpoints and MCP (Model Context Protocol) tools.

## MCP Server Endpoint

The API provides an MCP server endpoint using the **HTTP Streamable** protocol:

### `/mcp` - HTTP Streamable Protocol
**POST** endpoint for bidirectional streaming communication using newline-delimited JSON over HTTP.

The HTTP Streamable protocol works as follows:
- Requests and responses are sent as newline-delimited JSON (`\n`)
- Multiple requests can be sent over a single HTTP connection
- Each request must be on a single line
- Each response is written as a single line
- The connection stays open for bidirectional streaming

**Usage:**
```bash
# Using curl with interactive mode
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  --no-buffer \
  --data-binary '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}
'
```

**Example with multiple requests:**
```bash
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}
{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' | \
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  --no-buffer \
  --data-binary @-
```

## MCP Methods

### `initialize`
Initialize the MCP server and get server information.

```json
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}
```

**Response:**
```json
{"jsonrpc":"2.0","id":1,"result":{"protocolVersion":"2024-11-05","capabilities":{"tools":{}},"serverInfo":{"name":"flights-api-mcp-server","version":"1.0.0"}}}
```

### `tools/list`
List all available tools.

```json
{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}
```

**Response:**
```json
{"jsonrpc":"2.0","id":2,"result":{"tools":[{"name":"search_flights","description":"Search for flights by origin, destination, and departure date...","inputSchema":{...}}]}}
