using NigelCommerceMCPServer.Domain;

namespace NigelCommerceMCPServer.Tools
{
    public static class ProductTools
    {
        public static IEnumerable<Tool> GetTools()
        {
            return new[]
                {
            new Tool
            {
                Name = "list_products",
                Description = "Retrieves all products from the catalog.",
                InputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() },
                AllowedRoles = ["Owner", "Manager", "Customer"]
            },
            new Tool
            {
                Name = "get_product",
                Description = "Retrieves a single product by its ID.",
                InputSchema = new
                {
                    type = "object",
                    properties = new { productId = new { type = "string" } },
                    required = new[] { "productId" }
                },
                AllowedRoles = ["Owner", "Manager", "Customer"]
            },
            new Tool
            {
                Name = "add_product",
                Description = "Adds a new product to the catalog.",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        productName = new { type = "string" },
                        categoryId = new { type = "integer" },
                        price = new { type = "number" },
                        quantityAvailable = new { type = "integer" }
                    },
                    required = new[] { "productName", "categoryId", "price", "quantityAvailable" }
                },
                AllowedRoles = ["Owner", "Manager"]
            },
            new Tool
            {
                Name = "delete_product",
                Description = "Deletes a product by its ID.",
                InputSchema = new
                {
                    type = "object",
                    properties = new { productId = new { type = "string" } },
                    required = new[] { "productId" }
                },
                AllowedRoles = ["Owner"]
            }
        };
        }
    }
}
