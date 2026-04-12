using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using SupportLine.Models;
using System;
using System.Collections.Generic;

namespace SupportLine.Controllers
{
    public class TicketsController : Controller
    {
        private readonly IConfiguration _configuration;

        public TicketsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }



        // =======================
        //    CUSTOMER DASHBOARD
        // =======================

        // GET: /Tickets/CustomerDashboard
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)] // Prevents caching to ensure customers see the most up-to-date ticket information
        public IActionResult CustomerDashboard()
        {
            // Authentication Check: Ensures that only logged-in users can access the dashboard.
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                //  If not logged in, redirects to the login page.
                return RedirectToAction("Login", "Account");
            }

            List<Ticket> myTickets = new List<Ticket>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (SqliteConnection connection = new SqliteConnection(connectionString))
                {
                    connection.Open();

                    // Name Fetch: Solution to many attempted fixes for personalized Customer Dashboard
                    string nameQuery = "SELECT Name FROM USERS WHERE UserID = @UserID";
                    using (SqliteCommand nameCmd = new SqliteCommand(nameQuery, connection))
                    {
                        nameCmd.Parameters.AddWithValue("@UserID", userId);
                        var dbName = nameCmd.ExecuteScalar();
                        ViewBag.UserName = (dbName != null && dbName != DBNull.Value) ? dbName.ToString() : "Customer";
                    }

                    // Data Retrieval: Fetches the tickets created by the logged-in user from the database.
                    string query = "SELECT t.*, s.StatusName " +
                                   "FROM TICKETS t " +
                                   "LEFT JOIN TICKET_STATUS s ON t.StatusID = s.StatusID " +
                                   "WHERE t.UserID = @UserID " +
                                   "ORDER BY t.StatusID ASC, t.TicketID DESC";

                    using (SqliteCommand cmd = new SqliteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserID", userId.Value);

                        using (SqliteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Populates a list of Ticket objects with the retrieved data to display on the dashboard.
                                myTickets.Add(new Ticket
                                {
                                    TicketID = Convert.ToInt32(reader["TicketID"]),
                                    Subject = reader["Subject"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    DateCreated = Convert.ToDateTime(reader["DateCreated"]),
                                    StatusName = reader["StatusName"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Error Handling: Catches any exceptions that occur during database operations and displays an error message.
                ViewBag.ErrorMessage = "An error occurred while loading your tickets. Please try again later.";
                Console.WriteLine("Database Error:" + ex.Message); // Log the exception for debugging purposes
            }

            // Returns the view with the list of tickets to be displayed on the customer's dashboard.
            return View(myTickets);
        }



        // =======================
        // SUPPORT AGENT DASHBOARD
        // =======================

        // GET: /Tickets/AgentDashboard
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)] // Prevents caching to ensure agents see the most up-to-date ticket information
        public IActionResult AgentDashboard()
        {
            // Security: Ensures user is logged in
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                // If not logged in, redirects to the login page.
                return RedirectToAction("Login", "Account");
            }

            List<Ticket> allTickets = new List<Ticket>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (SqliteConnection connection = new SqliteConnection(connectionString))
                {
                    connection.Open();

                    // Ticket Clean-up: Automatically closes tickets that have been in "Resolved" status for more than 7 days to keep the dashboard organized and ensure that old tickets are not left open indefinitely.
                    string cleanupQuery = @"UPDATE TICKETS SET StatusID = 4
                                            WHERE StatusID = 3 AND TicketID IN (
                                                SELECT TicketID FROM (
                                                    SELECT TicketID, MAX(DateReplied) as LastReply
                                                    FROM RESPONSES GROUP BY TicketID
                                                ) as temp
                                                WHERE LastReply <= datetime('now', '-7 days')
                                            )"; // Set Interval to 1 MINUTE for testing purposes, change back to 7 DAY for production

                    using (SqliteCommand cleanupCmd = new SqliteCommand(cleanupQuery, connection))
                    {
                        cleanupCmd.ExecuteNonQuery();
                    }

                    // Role Verification: Checks if the logged-in user has the "Support Agent" role to restrict access to the agent dashboard.
                    string roleQuery = "SELECT Role FROM USERS WHERE UserID = @UserID";
                    using (SqliteCommand roleCmd = new SqliteCommand(roleQuery, connection))
                    {
                        roleCmd.Parameters.AddWithValue("@UserID", userId);
                        var dbRole = roleCmd.ExecuteScalar();
                        if (dbRole == null || dbRole.ToString() != "Support Agent")
                        {
                            // If the user is not a Support Agent, redirects to the login page or an unauthorized access page.
                            return RedirectToAction("Login", "Account");
                        }
                    }

                    // Data Retrieval: Fetches all tickets from the database, including the name of the user who created each ticket and the current status.
                    string query = @"SELECT t.*, s.StatusName, u.Name AS UserName
                                     FROM TICKETS t
                                     LEFT JOIN TICKET_STATUS s ON t.StatusID = s.StatusID
                                     LEFT JOIN USERS u ON t.UserID = u.UserID
                                     ORDER BY t.StatusID ASC, t.TicketID DESC";

                    using (SqliteCommand cmd = new SqliteCommand(query, connection))
                    {
                        using (SqliteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Populates a list of Ticket objects with the retrieved data to display on the agent dashboard.
                                allTickets.Add(new Ticket
                                {
                                    TicketID = Convert.ToInt32(reader["TicketID"]),
                                    Subject = reader["Subject"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    DateCreated = Convert.ToDateTime(reader["DateCreated"]),
                                    StatusName = reader["StatusName"] == DBNull.Value ? "Open" : reader["StatusName"].ToString(),

                                    // Added to display the name of the customer who created the ticket
                                    CustomerName = reader["UserName"] == DBNull.Value ? "Unknown Customer" : reader["UserName"].ToString()
                                });
                            }
                        }
                    }

                    // Name Fetch: Gets the Agent's for personalized Agent Dashboard
                    string nameQuery = "SELECT Name FROM USERS WHERE UserID = @UserID";
                    using (SqliteCommand nameCmd = new SqliteCommand(nameQuery, connection))
                    {
                        nameCmd.Parameters.AddWithValue("@UserID", userId);
                        var dbName = nameCmd.ExecuteScalar();
                        ViewBag.UserName = (dbName != null && dbName != DBNull.Value) ? dbName.ToString() : "Agent";
                    }
                }
            }
            catch (Exception ex)
            {
                // Error Handling: Catches any exceptions that occur during database operations and displays an error message.
                ViewBag.ErrorMessage = "Database Error: " + ex.Message;
                Console.WriteLine("Database Error: " + ex.Message); // Log the exception for debugging purposes
            }

            return View(allTickets);
        }



        // =======================
        //    TICKET SUBMISSION
        // =======================

        // GET: /Tickets/Create
        [HttpGet]
        public IActionResult Create()
        {
            // Authentication Check: Ensures that only logged-in users can access the ticket submission form.
            if (HttpContext.Session.GetInt32("UserID") == null)
            {
                // If not logged in, redirects to the login page.
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        // POST: /Tickets/Create
        [HttpPost]
        public IActionResult Create(string Subject, string Description)
        {
            // Authentication Check: Ensures that only logged-in users can submit a ticket.
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                // If not logged in, redirects to the login page.
                return RedirectToAction("Login", "Account");
            }

            // Ensuring Customer is not submitting an empty ticket
            if (string.IsNullOrWhiteSpace(Subject) || string.IsNullOrWhiteSpace(Description))
            {
                ViewBag.ErrorMessage = "Subject and Description are required.";
                return View();
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (SqliteConnection connection = new SqliteConnection(connectionString))
                {
                    connection.Open();

                    // StatusID 1 corresponds to "Open"
                    string query = @"INSERT INTO TICKETS (UserID, StatusID, Subject, Description, DateCreated) " +
                                            "VALUES (@UserID, 1, @Subject, @Description, @DateCreated)";
                    using (SqliteCommand cmd = new SqliteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserID", userId.Value);
                        cmd.Parameters.AddWithValue("@Subject", Subject);
                        cmd.Parameters.AddWithValue("@Description", Description);

                        // Stamps with current date and time
                        cmd.Parameters.AddWithValue("@DateCreated", DateTime.Now);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Error Handling: Catches any exceptions that occur during database operations and displays an error message.
                ViewBag.ErrorMessage = "An error occurred while submitting your ticket. Please try again later.";
                Console.WriteLine(ex.Message); // Log the exception for debugging purposes
                return View();
            }
            // Simulated Email Notification: In a real application, this is where you would trigger an email notification to the support team about the new ticket submission. For this prototype, we will simply log a message to the console.
            TempData["EmailNotification"] = "Ticket submitted successfully! A confirmation email has been sent to your registered email address, and the support team has been notified of your new ticket. Thank you for reaching out to us!";

            // After successful submission, redirects the user back to their dashboard to see the new ticket listed.
            return RedirectToAction("CustomerDashboard");
        }



        /// =======================
        /// TICKET DETAILS & RESPONSES
        /// =======================

        // GET: /Tickets/Details/{id}
        [HttpGet]
        public IActionResult Details(int id)
        {
            // Authentication Check: Ensures that only logged-in users can access the ticket details.
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                // If not logged in, redirects to the login page.
                return RedirectToAction("Login", "Account");
            }

            Ticket ticket = null;
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                // Adding a role check from the database to bypass session issues in prototype phase
                string roleQuery = "SELECT Role FROM USERS WHERE UserID = @UserID";
                using (SqliteCommand roleCmd = new SqliteCommand(roleQuery, connection))
                {
                    roleCmd.Parameters.AddWithValue("@UserID", userId);
                    var dbRole = roleCmd.ExecuteScalar();
                    ViewBag.IsAgent = (dbRole != null && dbRole.ToString() == "Support Agent");
                }

                // Data Retrieval: Fetches the ticket details from the database, including the conversation history (responses) for that ticket.
                string query = @"SELECT t.*, s.StatusName, u.Name AS CustomerName
                                 FROM TICKETS t
                                 LEFT JOIN TICKET_STATUS s ON t.StatusID = s.StatusID
                                 LEFT JOIN USERS u ON t.UserID = u.UserID
                                 WHERE t.TicketID = @TicketID";

                using (SqliteCommand cmd = new SqliteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@TicketID", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ticket = new Ticket
                            {
                                TicketID = Convert.ToInt32(reader["TicketID"]),
                                Subject = reader["Subject"].ToString(),
                                Description = reader["Description"].ToString(),
                                DateCreated = Convert.ToDateTime(reader["DateCreated"]),
                                StatusName = reader["StatusName"].ToString(),
                                CustomerName = reader["CustomerName"].ToString()
                            };
                        }
                    }
                }

                if (ticket == null) return NotFound();

                // Fetches the conversation history (responses) for that ticket.
                string responseQuery = @"SELECT r.*, u.Name, u.Role
                                         FROM RESPONSES r
                                         JOIN USERS u ON r.UserID = u.UserID
                                         WHERE r.TicketID = @TicketID
                                         ORDER BY r.DateReplied ASC";

                using (SqliteCommand cmd = new SqliteCommand(responseQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@TicketID", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ticket.Responses.Add(new TicketResponse
                            {
                                UserName = reader["Name"].ToString(),
                                UserRole = reader["Role"].ToString(),
                                Message = reader["Message"].ToString(),
                                DateReplied = Convert.ToDateTime(reader["DateReplied"])
                            });
                        }
                    }
                }

                return View(ticket);

            }
        }

        // POST: /Tickets/Details/{id}
        [HttpPost]
        public IActionResult AddResponse(int TicketID, string Message, string ActionType)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToAction("Login", "Account");

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                // Ticket Closure: If the "Close" button is clicked, updates the ticket's status to "Closed" in the database.
                if (ActionType == "Close")
                {
                    string closeQuery = "UPDATE TICKETS SET StatusID = 4 WHERE TicketID = @TicketID"; // StatusID 4 corresponds to "Closed"
                    using (SqliteCommand cmd = new SqliteCommand(closeQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@TicketID", TicketID);
                        cmd.ExecuteNonQuery();
                    }

                    if (string.IsNullOrWhiteSpace(Message)) Message = "--- Customer has closed this ticket. ---";
                }
                // Ticket Resolution: If the "Resolve" button is clicked, updates the ticket's status to "Resolved" in the database.
                else if (ActionType == "Resolve")
                {
                    string resolveQuery = "UPDATE TICKETS SET StatusID = 3 WHERE TicketID = @TicketID"; // StatusID 3 corresponds to "Resolved"
                    using (SqliteCommand cmd = new SqliteCommand(resolveQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@TicketID", TicketID);
                        cmd.ExecuteNonQuery();
                    }

                    if (string.IsNullOrWhiteSpace(Message)) Message = "--- Support Agent has marked this ticket as resolved. ---";
                }

                // If the message is empty and the action is "Reply", it simply redirects back to the details page without adding a new response, allowing users to change the ticket status without needing to add a message.
                if (string.IsNullOrWhiteSpace(Message) && ActionType == "Reply")
                {
                    return RedirectToAction("Details", new { id = TicketID });
                }

                // Saves the new response to the database, associating it with the correct ticket and user, and timestamps it with the current date and time.
                string responseQuery = @"INSERT INTO RESPONSES (TicketID, UserID, Message, DateReplied) 
                                        VALUES (@TicketID, @UserID, @Message, @DateReplied)";

                using (SqliteCommand cmd = new SqliteCommand(responseQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@TicketID", TicketID);
                    cmd.Parameters.AddWithValue("@UserID", userId.Value);
                    cmd.Parameters.AddWithValue("@Message", Message);
                    cmd.Parameters.AddWithValue("@DateReplied", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }

                // Sets the ticket status to "In Progress" when a Support Agent responds to an open ticket, while allowing customers to add messages without changing the status.
                string roleQuery = "SELECT Role FROM USERS WHERE UserID = @UserID";
                string userRole = "";

                using (SqliteCommand roleCmd = new SqliteCommand(roleQuery, connection))
                {
                    roleCmd.Parameters.AddWithValue("@UserID", userId);
                    var dbRole = roleCmd.ExecuteScalar();
                    if (dbRole != null) userRole = dbRole.ToString();
                }

                if (userRole == "Support Agent" && ActionType == "Reply")
                {
                    string updateStatusQuery = "UPDATE TICKETS SET StatusID = 2 WHERE TicketID = @TicketID AND StatusID = 1"; // StatusID 2 corresponds to "In Progress", StatusID 1 corresponds to "Open"
                    using (SqliteCommand cmd = new SqliteCommand(updateStatusQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@TicketID", TicketID);
                        cmd.ExecuteNonQuery();
                    }
                }

                // If a Customer replies to a resolved ticket, it reopens the ticket by changing the status back to "Open", allowing for continued communication if the issue was not fully resolved.
                if (userRole == "Customer" && ActionType == "Reply")
                {
                    string reopenQuery = "UPDATE TICKETS SET StatusID = 1 WHERE TicketID = @TicketID AND StatusID = 3"; // StatusID 1 corresponds to "Open", StatusID 3 corresponds to "Resolved"
                    using (SqliteCommand cmd = new SqliteCommand(reopenQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@TicketID", TicketID);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Pop-up Notification: After adding a response, sets a TempData message to display a pop-up notification confirming that the response has been added successfully.
                // string userRole = "";
                // string roleQuery = "SELECT Role FROM USERS WHERE UserID = @UserID";

                using (SqliteCommand roleCmd = new SqliteCommand(roleQuery, connection))
                {
                    roleCmd.Parameters.AddWithValue("@UserID", HttpContext.Session.GetInt32("UserID"));
                    var dbRole = roleCmd.ExecuteScalar()?.ToString();
                }

                if (userRole == "Support Agent")
                {
                    TempData["ResponseNotification"] = "Your response has been added successfully! The customer will be notified of your reply.";
                }
                else
                {
                    TempData["ResponseNotification"] = "Your message has been added successfully! The support team will be notified of your reply.";
                }

                return RedirectToAction("Details", new { id = TicketID });
            }
        }
    }
}
