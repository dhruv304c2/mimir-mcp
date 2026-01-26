# Mimir-MCP

A lightweight reference implementation for embedding Model Context Protocol (MCP) servers inside a Unity project. Use it to expose in-game tools over HTTP so external MCP-aware clients (LLM agents, automation suites, etc.) can interact with your running scene.

## How To Use

1. **Open the Unity Project**
   - Launch the project in the Unity Editor.
   - Pick or create a scene you want to instrument with MCP access.

3. **Register MCP Tools / Install as a Package**
   - The MCP runtime now lives underneath `Assets/MimirMCP` (display name **MimirMCP**) so it is compiled directly when this repo is opened, yet it still contains a `package.json` so the folder can be consumed as a Git package. The package declares dependencies on `com.unity.nuget.newtonsoft-json` and `com.cysharp.unitask`, so you get Newtonsoft and UniTask automatically when it is installed.
   - To consume this repository as a Git package from another project, add the package entry to that project's `Packages/manifest.json`. Example:
     ```json
     {
     "dependencies": {
       "com.mimir.mimirmcp": "https://github.com/<your-org>/MimirMCP.git?path=/Assets/MimirMCP#v0.1.0"
     }
    }
     ```
     Replace `<your-org>` with the actual GitHub org/user (and optionally point to a tag/branch). Unity will download only the package payload (no `Assets/Tests`) thanks to the `path` query parameter.
     If you install via Git or via a local folder, ensure your consuming project's `Packages/manifest.json` includes the scoped registry for UniTask:
     ```json
     {
       "scopedRegistries": [
         {
           "name": "package.openupm.com",
           "url": "https://package.openupm.com",
           "scopes": [
             "com.cysharp.unitask"
           ]
         }
       ],
       "dependencies": {
         "com.cysharp.unitask": "2.5.4",
         "com.mimir.mimirmcp": "https://github.com/dhruv304c2/mimir-mcp.git?path=/Assets/MimirMCP#v0.1.0"
       }
     }
     ```
   - Runtime code is located under `Assets/MimirMCP/Runtime/InGameMCP/**`. Testing/host harness scripts stay under `Assets/Tests` and reference the package through the `MimirMCP.Tests.asmdef`, keeping them out of the distributable package payload.
   - Call the new helper methods on `MCPHandler` to register built-in tools: `UseDefaultToolSet()` wires up logging, scene hierarchy, transforms, serialized property setters, and material tools; or compose your own by calling `UseTransformToolSet()`, `UseSerializedPropertyToolSet()`, `UseMaterialToolSet()`, etc.
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
