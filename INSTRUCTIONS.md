# Usage Instructions

This guide explains how to run, test, and configure the NigelCommerce MCP Server with your favorite AI clients.

## 🚀 How to Run

### 1. Start the NigelCommerce Backend
The MCP server is a bridge, so it requires the actual ecommerce backend to be running.
- Follow the instructions at [NigelCommerce](https://github.com/rooba-venkatesan-k/NigelCommerce).
- Ensure it is accessible (usually at `https://localhost:5141` or as configured in your `appsettings.json`).

### 2. Start this MCP Server
Open a terminal in the root of this project and run:
```powershell
dotnet run --project NigelCommerceMCPServer
```
Keep this terminal open. You should see logs indicating the server is running on `http://localhost:5016`.

---

## 🤖 Configuration in AI Clients

This server uses the **SSE (Server-Sent Events)** transport protocol.

### 1. VS Code (Modern MCP Support)

1.  In your VS Code workspace (or any folder), create a folder named `.vscode`.
2.  Create a file named `mcp.json` inside that folder.
3.  Paste the following configuration:
    ```json
    {
        "servers": {
            "NigelCommerceMcp": {
                "url": "http://localhost:5016/mcp/",
                "headers": {
                    "Authorization": "Bearer <YOUR-TOKEN-WITHOUT-ANGULAR-BRACKETS>"
                }
            }
        }
    }
    ```
4.  Replace `<YOUR-TOKEN-WITHOUT-ANGULAR-BRACKETS>` with your actual JWT token.
5.  Save the file.
6.  Look for the **"Start"** link appearing directly above the server configuration in the `mcp.json` editor (CodeLens) and click it to activate the connection.

<img width="700" height="400" alt="image" src="https://github.com/user-attachments/assets/79f9ef68-f977-447b-a490-c9b767451665" />

### 2. Claude Desktop
To use this server with the Claude Desktop app:

1.  Open your Claude Desktop configuration file:
    - **Windows**: `%AppData%\Roaming\Claude\claude_desktop_config.json`
    - **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
2.  Add the server to the `mcpServers` section using `mcp-remote` (ensure this is installed/available):
    ```json
    {
      "mcpServers": {
        "NigelCommerceMcp": {
          "command": "mcp-remote",
          "args": [
            "http://localhost:5016/mcp/",
            "--header",
            "Authorization: Bearer <YOUR-TOKEN-WITHOUT-ANGULAR-BRACKETS>"
          ]
        }
      }
    }
    ```
3.  Replace `<YOUR-TOKEN-WITHOUT-ANGULAR-BRACKETS>` with your actual JWT token.
4.  Restart Claude Desktop (Files -> Exit, then open claude desktop again).

> [!TIP]
> This method allows Claude Desktop to connect to the running SSE server locally. Ensure the MCP server is running (`dotnet run`) before starting Claude.

---

## 🎮 How to Play

Once the server is connected and the tools are visible, you can start "playing" by chatting with the AI:

### Try these prompts:
- **Inventory Check**: *"What products do we have in the inventory?"*
- **Product Details**: *"Give me the details of the product with ID 1."*
- **Management**: *"Add a new product called 'Mechanical Keyboard' in the Electronics category for $99.99 with 50 units in stock."*
- **Admin**: *"List all users and change the role of user 'JohnDoe' to Manager."*

### What happens behind the scenes?
1. The AI identifies a relevant tool (e.g., `list_products`).
2. It sends a request to this MCP Server.
3. MCP server further calls the **NigelCommerce API**.
4. The data is piped back to the AI, which presents it to you in plain English!
