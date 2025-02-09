﻿using System;

namespace AceJobAgency.Model
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Activity { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}