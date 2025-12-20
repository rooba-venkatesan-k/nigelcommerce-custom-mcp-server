using NigelCommerceMCPServer.Domain;

namespace NigelCommerceMCPServer.Tools
{
    public static class UserTools
    {
        public static IEnumerable<Tool> GetTools()
        {
            return new[]
            {
            new Tool
            {
                Name = "register_user",
                Description = "Registers a new user.",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        emailId = new { type = "string" },
                        userPassword = new { type = "string" },
                        gender = new { type = "string" },
                        dob = new { type = "string" },
                        address = new { type = "string" }
                    },
                    required = new[] { "emailId", "userPassword" }
                },
                AllowedRoles = ["Owner"]
            },
            new Tool
            {
                Name = "list_users",
                Description = "Lists all registered users.",
                InputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() },
                AllowedRoles = ["Owner"]
            }
        };
        }
    }
}
