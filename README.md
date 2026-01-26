# Mimir-MCP

A lightweight reference implementation for embedding Model Context Protocol (MCP) servers inside a Unity project. Use it to expose in-game tools over HTTP so external MCP-aware clients (LLM agents, automation suites, etc.) can interact with your running scene.

## How To Use

1. **Open the Unity Project**
   - Launch the project in the Unity Editor.
   - Pick or create a scene you want to instrument with MCP access.

3. **Register MCP Tools**
   - The sample host registers `LogMCPTool` plus several utility tools (scene hierarchy dump, transform editing, component inspector read/write for strings/bools/numbers/vectors/colors, mesh material inspection/updating).
   - Transform edits can be targeted per-property: use `transform_position_update`, `transform_rotation_update`, or `transform_scale_update` when you only need to tweak one aspect, or `transform_change` if you truly need to adjust multiple in a single call. Use `transform_inspect` first when you need to grab the current position/rotation/scale values as a starting point, and include `*_transition_time` if you want the change animated over that duration.
   - To add your own functionality, create another class that derives from `MCPToolBase`, describe its parameters via the `[MCPToolParam]` attributes, and override `ExecuteTool` (which returns a `UniTask<ContentBase[]>`) to perform the action.
   - Register new tools by calling `_mcpHandler.RegisterTool(new YourTool())` before `StartHTTPServer`.

4. **Play the Scene**
   - Enter Play mode; the Unity console will print `TestLogMCPHost listening at localhost:<port>` when the HTTP server is live.
   - Keep the Game view running while issuing MCP requests.

5. **Call The MCP Endpoints**
   - **List tools**
     ```bash
     curl -X POST http://localhost:3000/mcp \
       -H "Content-Type: application/json" \
       -d '{
             "jsonrpc": "2.0",
             "id": "list-1",
             "method": "tools/list",
             "params": {}
           }'
     ```
   - **Invoke a tool**
     ```bash
     curl -X POST http://localhost:3000/mcp \
       -H "Content-Type: application/json" \
       -d '{
             "jsonrpc": "2.0",
             "id": "call-1",
             "method": "tools/call",
             "params": {
               "tool_name": "test_log",
               "arguments": {
                 "message": "Hello from MCP!",
                 "level": "warning"
               }
             }
           }'
     ```
   - Replace `test_log` with any other registered tool name and pass the arguments defined in its schema.

6. **Shut Down Gracefully**
   - Exiting Play mode calls `StopHTTPServer` via `OnDestroy`, ensuring the listener releases the port. If you create custom hosts, mirror that cleanup to avoid dangling listeners.

## Customizing Further

- Swap in a different `ILogger` implementation to redirect logs elsewhere.
- Mount multiple MCP tools to expose game-specific debugging or control hooks.
- Deploy outside the Editor by attaching the host behaviour in build scenes—just be mindful of firewall rules for the chosen port.

With these steps the framework stays focused on practical operation: start the host, register tools, and drive it via JSON-RPC MCP calls.
