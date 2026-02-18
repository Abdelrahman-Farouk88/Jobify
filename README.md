# Jobify - DEPI Graduation Project

A robust recruitment platform designed to connect talent with opportunity. This project is the final graduation requirement for the **Digital Egypt Pioneers Initiative (DEPI)** under the **Full-Stack .NET track**.

---

## 👥 Team Members

- **Abdulrahman Hany Farouk**
- **Ahmed Hany**
- **Abdulrahman Ashraf**
- **Dalal Farghaly**
- **Youssif Othman**

---

## 🌟 Key Features

### For Job Seekers
- **Account Management:** Secure registration and login using JWT-based authentication.
- **Job Discovery:** Advanced search and filtering to find roles by title, category, or location.
- **Application System:** One-click application process with profile management.
- **Status Tracking:** Real-time visibility into application progress (Pending, Reviewed, Accepted/Rejected).

### For Employers
- **Recruitment Dashboard:** A centralized hub to manage job postings and track candidate pipelines.
- **Job Management:** Full CRUD operations for creating, updating, and closing job openings.
- **Applicant Screening:** Review candidate profiles and manage their status within the hiring workflow.

---

## 🛠️ Technology Stack

| Component | Technology |
| :--- | :--- |
| **Backend** | ASP.NET Core Web API (.NET 8/9) |
| **Frontend** | React.js / Angular |
| **Database** | Microsoft SQL Server |
| **ORM** | Entity Framework Core |
| **Security** | JWT (JSON Web Tokens) & ASP.NET Identity |

---

## 🏗️ Project Architecture

To ensure scalability and maintainability, the project follows standard Software Engineering principles:

```text
/
├── Backend/          # .NET Web API Core Logic
│   ├── Controllers/  # API Endpoints
│   ├── Data/         # DB Context & Migrations
│   ├── Models/       # Entities & DTOs
│   └── Services/     # Business Logic
├── Frontend/         # Client-side SPA
├── Docs/             # ERD Diagrams & API Documentation
└── README.md         # Project Overview
