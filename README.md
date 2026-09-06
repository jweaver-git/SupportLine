# SupportLine - Library Ticket Management System

## Project Context
SupportLine is a full-stack IT ticket management system originally built as an academic project. It is archived here to demonstrate a complete ASP.NET Core MVC architecture, including secure backend routing and relational database management.

## Features & Architecture
* **Self-Contained Database:** Refactored from MySQL to an embedded SQLite database (`SupportLine.db`). This makes the repository entirely portable; reviewers can clone and run the application instantly without configuring a local database server.
* **Role-Based Access Control:** Custom session-based authentication routes users to separate Customer or Support Agent dashboards.
* **Secure Data Handling:** Uses parameterized ADO.NET queries to prevent SQL injection.

## Demo Access
To explore the custom role-based routing and ticket management features, you can log in using the following pre-configured test accounts. The embedded SQLite database contains a history of open and resolved tickets to demonstrate the UI.

**Support Agent Account**
* **Email:** support@support.com
* **Password:** support1

**Customer Account**
* **Email:** test3@test.org
* **Password:** pass2