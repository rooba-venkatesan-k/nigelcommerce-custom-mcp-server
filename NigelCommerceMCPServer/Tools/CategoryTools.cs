using NigelCommerceMCPServer.Domain;

namespace NigelCommerceMCPServer.Tools
{
    public static class CategoryTools
    {
        public static IEnumerable<Tool> GetTools()
        {
            return new[]
            {
            new Tool
            {
                Name = "list_categories",
                Description = "Retrieves all categories.",
                InputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() },
                AllowedRoles = ["Owner", "Manager", "Customer"]
            },
            new Tool
            {
                Name = "get_category",
                Description = "Retrieves a category by its ID.",
                InputSchema = new
                {
                    type = "object",
                    properties = new { categoryId = new { type = "integer" } },
                    required = new[] { "categoryId" }
                },
                AllowedRoles = ["Owner", "Manager", "Customer"]
            },
            new Tool
            {
                Name = "add_category",
                Description = "Adds a new category.",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        categoryName = new { type = "string" }
                    },
                    required = new[] { "categoryName" }
                },
                AllowedRoles = ["Owner", "Manager"]
            }
        };
        }
    }
}
