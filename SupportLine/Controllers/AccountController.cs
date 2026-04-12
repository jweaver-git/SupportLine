using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using SupportLine.Models;
using System;

namespace SupportLine.Controllers
{
    public class AccountController : Controller
    {
        // Allows us to access the configuration settings from appsettings.json
        private readonly IConfiguration _configuration;

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
        }



        // ======================
        //      REGISTRATION
        // ======================

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public IActionResult Register(User model)
        {
            // We remove the Role and UserID from ModelState because they are not part of the registration form input and will be set programmatically in the controller
            ModelState.Remove("Role");
            ModelState.Remove("UserID");

            // Input Validation: Checks if the [Required] and [EmailAddress] attributes in the User model are satisfied
            if (ModelState.IsValid)
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                try
                {
                    using (SqliteConnection connection = new SqliteConnection(connectionString))
                    {
                        connection.Open();

                        // Logic: Check if the email is already registered to prevent duplicate accounts
                        string checkQuery = "SELECT COUNT(*) FROM USERS WHERE Email = @Email";
                        using (SqliteCommand checkCmd = new SqliteCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@Email", model.Email);
                            int userCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                            if (userCount > 0)
                            {
                                ModelState.AddModelError("Email", "This email is already registered.");
                                return View(model);
                            }
                        }

                        // Security: We use parameterized queries to prevent SQL injection attacks when inserting user data into the database
                        string insertQuery = "INSERT INTO USERS (Name, Email, Password, Role) " +
                            "VALUES (@Name, @Email, @Password, @Role)";
                        using (SqliteCommand cmd = new SqliteCommand(insertQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@Name", model.Name);
                            cmd.Parameters.AddWithValue("@Email", model.Email);

                            // Note: In a production application, you should hash the password before storing it in the database for security reasons.
                            // To keep the scope manageable for this example, we are storing the password as plain text
                            cmd.Parameters.AddWithValue("@Password", model.Password);
                            cmd.Parameters.AddWithValue("@Role", "Customer");
                            cmd.ExecuteNonQuery();
                        }
                    }

                    return RedirectToAction("Login", "Account");
                }
                catch (Exception ex)
                {
                    // Error Handling: Catches any exceptions that occur during database operations and adds a model error to display a user-friendly message
                    ModelState.AddModelError("", "An error occurred while registering: " + ex.Message);
                }
            }

            return View(model);
        }



        // ======================
        //         LOGIN
        // ======================

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                try
                {
                    using (SqliteConnection connection = new SqliteConnection(connectionString))
                    {
                        connection.Open();

                        // Security: Queries the database to see if the email and password combination exists
                        string query = "SELECT UserID, Name, Role FROM USERS WHERE Email = @Email AND Password = @Password";
                        using (SqliteCommand cmd = new SqliteCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@Email", model.Email);
                            cmd.Parameters.AddWithValue("@Password", model.Password);

                            using (SqliteDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    // Session Management: Stores the user's ID, name, and role in the session to keep them logged in across pages
                                    HttpContext.Session.SetInt32("UserID", Convert.ToInt32(reader["UserID"]));
                                    HttpContext.Session.SetString("Name", reader["Name"].ToString());
                                    HttpContext.Session.SetString("Role", reader["Role"].ToString());

                                    // Directs users to different dashboards based on their role (Support Agent or Customer)
                                    if (reader["Role"].ToString() == "Support Agent")
                                    {
                                        return RedirectToAction("AgentDashboard", "Tickets");
                                    }
                                    else
                                    {
                                        return RedirectToAction("CustomerDashboard", "Tickets");
                                    }
                                }
                                else
                                {
                                    // Authentication Failure: If no matching user is found, displays an "Invalid email or password" message
                                    ModelState.AddModelError("", "Invalid email or password.");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while logging in: " + ex.Message);
                }
            }
            return View(model);
        }



        // ======================
        //         LOGOUT
        // ======================

        // GET: /Account/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            // Clears the user's session to log them out
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
