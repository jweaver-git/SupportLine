using System;
using System.ComponentModel.DataAnnotations;

namespace SupportLine.Models
{
    public class Ticket
    {
        public int TicketID { get; set; }
        public int UserID { get; set; } // Foreign key to the User who created the ticket
        public int StatusID { get; set; } // Foreign key to the Status of the ticket

        [Required(ErrorMessage = "Please enter a subject for your ticket.")]
        [StringLength(150)]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Please provide a description of your issue.")]
        public string Description { get; set; }
        public DateTime DateCreated { get; set; }
        public string StatusName { get; set; } // e.g., "Open", "In Progress", "Resolved", "Closed"       

        // Added to display Customer name on the Agent's view of the ticket list
        public string CustomerName { get; set; }

        // Holds conversation history for the ticket, which can be displayed in the ticket details view
        public List<TicketResponse> Responses { get; set; } = new List<TicketResponse>();
    }
}
