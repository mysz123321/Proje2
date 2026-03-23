namespace Staj2.Domain.Entities
{
    public class EndpointPermission
    {
        public int Id { get; set; }
        public string ControllerName { get; set; } // Örn: "Admin"
        public string ActionName { get; set; }     // Örn: "GetAllUsers"
        public string RequiredPermission { get; set; } // Örn: "User.Read"
    }
}