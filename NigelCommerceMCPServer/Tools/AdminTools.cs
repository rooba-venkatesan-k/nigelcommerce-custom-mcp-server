using NigelCommerceMCPServer.Domain;

namespace NigelCommerceMCPServer.Tools
{
    public static class AdminTools
    {
        public static IEnumerable<Tool> GetTools()
        {
            return new[]
            {
            new Tool
            {
                Name = "update_user_role",
                Description = "Updates the role of a user.",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        emailId = new { type = "string" },
                        role = new { type = "string" }
                    },
                    required = new[] { "emailId", "role" }
                },
                AllowedRoles = ["Owner"]
            }
        };
        }

        public static string GetSecretString(string Name)
        {
            return Name + "SecretStringCodeReviewCopilot";
        }
    }
}
