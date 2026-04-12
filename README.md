\# SupportLine - Library Ticket Management System



\## Project Context

> SupportLine was originally developed as a localized academic project to architect a library and IT support ticket management system. It is being uploaded here in its finalized state as a comprehensive demonstration of ASP.NET Core MVC architecture, relational database management, and interactive UI design.



\## Features \& Architecture

\- \*\*Self-Contained Database:\*\* Refactored from MySQL to an embedded SQLite database (`SupportLine.db`) for zero-friction portability. Recruiters can clone and run the application instantly with pre-populated demo data.

\- \*\*Role-Based Access Control:\*\* Custom session-based authentication routes users to separate Customer or Support Agent dashboards.

\- \*\*Secure Data Handling:\*\* Complete utilization of parameterized queries to prevent SQL injection.

