namespace InventoryManagementSystem.Models
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Staff = "Staff";
        public const string InventoryManager = "Inventory Manager";
        public const string PurchaseManager = "Purchase Manager";
        public const string SalesManager = "Sales Manager";

        // Comma-separated role lists for [Authorize(Roles = "...")] - Authorize treats
        // a comma-separated list as "OR", so Admin always retains access alongside
        // whichever specific manager role owns that area.
        public const string AdminOrInventory = Admin + "," + InventoryManager;
        public const string AdminOrPurchase = Admin + "," + PurchaseManager;
        public const string AdminOrSales = Admin + "," + SalesManager;
    }
}
