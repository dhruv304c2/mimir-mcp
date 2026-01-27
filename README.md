<p align="center">
  <img src="docs/logo.jpeg" alt="Mimir MCP logo" width="320" />
</p>

Mimir MCP is an open-source Unity framework for crafting bespoke Model Context Protocol surfaces that describe your *gameplay* APIs, not your editor UI. It gives you the primitives to define how an agent perceives and manipulates your live world—tools, schemas, threading model, networking—so you can expose just enough control for safe automation inside a running build. Drop it into a scene, wire up toolsets that mirror your gameplay verbs, and your MCP-aware agent can pilot the experience through the same systems players touch.

## About The Framework

- **Embedded MCP host** – the `MCPHost` component spins up a lightweight `HttpListener` directly inside your scene (Editor or Player builds) and routes requests to registered tool handlers.
- **Unity-first execution** – incoming calls are marshalled back to the Unity main thread before they touch your objects, so you can safely mutate scene data without writing extra plumbing.
- **Composable tooling** – every capability is a small `MCPToolBase` implementation. You can enable a prebuilt toolset, mix and match sets, or add your own tools with a couple of attributes and one async override.

## Installation (Git Package)

1. Open your target Unity project.
2. Go to **Window ▸ Package Manager ▸ + ▸ Add package from Git URL…**
3. Paste `https://github.com/dhruv304c2/mimir-mcp.git?path=/Assets/MimirMCP` and confirm.
   - Unity clones the repository, but only imports the `Assets/MimirMCP` payload as a package named `com.mimir.mimirmcp`.
4. Ensure your `Packages/manifest.json` also references UniTask if it is not already present:
   ```json
   {
     "dependencies": {
       "com.cysharp.unitask": "2.5.4",
       "com.mimir.mimirmcp": "https://github.com/dhruv304c2/mimir-mcp.git?path=/Assets/MimirMCP"
     }
   }
   ```
5. Enter Play Mode once to let Unity generate asmdef caches. You now have the runtime in your project and can add the host behaviour to any scene.

## Getting Started In Unity

1. **Create a Host GameObject** – add the `MCPHostBehaviour` prefab/script (or a small MonoBehaviour that holds an `MCPHost`) to your scene.
2. **Configure networking** – set the host and port fields (defaults are `localhost:3000`).
3. **Register toolsets** – in `Start`, grab the handler and opt into the helpers you need:
   ```csharp
   void Start()
   {
       _mcpHandler = new MCPHandler(logger: new UnityLogger())
           .UseDefaultToolSet(); // or chain specific Use*ToolSet calls

       _mcpHost = new MCPHost(port: 3000);
       _mcpHost.SetLogger(new UnityLogger());
       _mcpHost.RegisterDefaultHandlers();
       _mcpHost.UseMCPHandler(_mcpHandler);
       _mcpHost.StartHTTPServer();
   }
   ```
4. **Run the scene** – when you hit Play you should see `Hosting server at localhost:3000` in the console. The host automatically stops when the behaviour is destroyed, but you can call `StopHTTPServer` manually if needed.
5. **Add custom tools (optional)** – derive from `MCPToolBase`, decorate fields/properties with `[MCPToolParam]`, implement `ExecuteTool(IReadOnlyDictionary<string, object>)`, then register the tool before calling `StartHTTPServer`.

## Built-In Toolsets & Tools

| Toolset helper | Included tools (tool name) | Typical use |
| --- | --- | --- |
| `UseLogTool()` | `log` | Send MCP-triggered logs to Unity or a custom `ILogger` implementation. |
| `UseSceneHierarchyToolSet()` | `scene_hierarchy_inspect` | Enumerate root objects and their children for quick discovery. |
| `UseTransformToolSet()` | `transform_change`, `transform_inspect`, `transform_position_update`, `transform_rotation_update`, `transform_scale_update` | Read/write positions, rotations, scales, and tween transitions for any GameObject you can address by hierarchy path. |
| `UseSerializedPropertyToolSet()` | `inspector_component_list`, `inspector_property_inspect`, plus setters for `string`, `bool`, `float`, `Vector2`, `Vector3`, and `Color` | Discover which components live on an object and change serialized fields live. |
| `UseMaterialToolSet()` | `mesh_material_inspect`, `mesh_material_set_color`, `mesh_material_set_float`, `mesh_material_set_boolean` | Inspect Renderers’ materials and update shader parameters remotely. |
| `UseDefaultToolSet()` | All of the above | Fastest way to get an end-to-end sandbox without thinking about individual registrations. |

Pick the helper that matches the control surface you want to expose. You can call multiple helpers or register additional bespoke tools to build a curated API surface for your team or automated agents.

## Calling The Hosted MCP API

Once the host is running you interact with it over HTTP. All paths are rooted at the host/port you configured.

- `GET /` – simple health probe. Example response:
  ```json
  { "message": "Service is healthy" }
  ```
- `GET /mcp` – discovery ping that confirms the MCP endpoint exists:
  ```json
  { "message": "Mimir-MCP endpoint ready at /mcp (POST for MCP RPC)." }
  ```
- `POST /mcp` – JSON-RPC style MCP endpoint. The body follows the MCP spec and supports the standard methods shown below:
  - `initialize` – negotiate protocol version.
  - `ping` – keep-alive.
  - `tools/list` – enumerate every registered tool and the schema built from your `MCPToolParam` attributes.
  - `tools/call` – invoke a tool by name with arguments.

Example `tools/list` request/response:
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
Response (truncated):
```json
{
  "jsonrpc": "2.0",
  "id": "list-1",
  "result": {
    "tools": [
      {
        "name": "log",
        "description": "A tool to log messages to the Unity console via MCP.",
        "inputSchema": {
          "properties": {
            "message": { "type": "string" },
            "level": { "type": "string" }
          },
          "required": ["message"]
        }
      }
    ]
  }
}
```

Example `tools/call` to run the log tool:
```bash
curl -X POST http://localhost:3000/mcp \
  -H "Content-Type: application/json" \
  -d '{
        "jsonrpc": "2.0",
        "id": "call-1",
        "method": "tools/call",
        "params": {
          "tool_name": "log",
          "arguments": {
            "message": "Hello from MCP!",
            "level": "warning"
          }
        }
      }'
```
Typical success payload:
```json
{
  "jsonrpc": "2.0",
  "id": "call-1",
  "result": {
    "content": [
      { "type": "text", "text": "Logged message as Warning level." }
    ]
  }
}
```

Swap `tool_name` and `arguments` to target any other built-in or custom tool. Errors from parameter validation or tool execution are surfaced as standard MCP error objects so your client can react appropriately.

## Inspiration

<p align="center">
  <img src="docs/GOW-Ragnarok-Mimir.avif" alt="Mimir guiding Kratos in God of War" width="560" />
</p>

Mimir takes its name from the disembodied sage who accompanies Kratos in *God of War*: a sharp, non-intrusive guide whose counsel empowers the hero without seizing the controller. This framework aims for the same balance. It gives your agent the knowledge and entry points it needs to advise—or even act—inside your game, while you stay in charge of the narrative surface you expose.

## License

Mimir MCP is available under the MIT License. See `LICENSE` for details.
