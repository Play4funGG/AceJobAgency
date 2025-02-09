using System;

namespace AceJobAgency.Model
{
    public class UserSession
    {
        public int Id { get; set; }
        public string UserId { get; set; } // Foreign key to ApplicationUser
        public string SessionId { get; set; } // Unique session identifier
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        public ApplicationUser User { get; set; }
    }
}