using System;

namespace SupportLine.Models
{
    public class TicketResponse
    {
        public int ResponseID { get; set; }
        public int TicketID { get; set; } // Foreign key to the Ticket
        public string UserName { get; set; } // Added to display the name of the person who sent the message
        public string UserRole { get; set; } // Added to display the role of the person who sent the message (Customer or Agent)
        public string Message { get; set; }
        public DateTime DateReplied { get; set; }
    }
}
