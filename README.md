# BookNest - A Book E-Commerce Platform

BookNest is a feature-rich e-commerce platform built using ASP.NET MVC, designed for seamless book management and transactions. The application leverages Entity Framework, SQL Server, and Microsoft Azure for robust performance and scalability.

## Features

- **CRUD Operations**: Efficient book management using Entity Framework and SQL Server.
- **Authentication & Authorization**: Secure login system with role-based access control.
- **Responsive UI**: Developed with Razor Views, JavaScript, and Bootstrap for an optimal user experience.
- **Azure Deployment**: Hosted on Microsoft Azure for scalability and high availability.

## Technologies Used

- **Frontend**: Razor Views, JavaScript, Bootstrap
- **Backend**: ASP.NET MVC, C#, Entity Framework
- **Database**: SQL Server
- **Deployment**: Microsoft Azure

## Installation Guide

### Prerequisites
- .NET SDK (latest version)
- SQL Server & SQL Server Management Studio (SSMS)
- Visual Studio
- Microsoft Azure Account (for deployment)

### Steps to Run Locally

1. Clone the repository:
   ```sh
   git clone https://github.com/yourusername/BookNest.git
   cd BookNest
   ```
2. Open the project in **Visual Studio**.
3. Restore NuGet packages:
   ```sh
   dotnet restore
   ```
4. Configure the database in `appsettings.json`:
   ```json
   "ConnectionStrings": {
       "DefaultConnection": "Server=your_server;Database=BookNestDB;User Id=your_user;Password=your_password;"
   }
   ```
5. Apply migrations and update the database:
   ```sh
   dotnet ef database update
   ```
6. Run the application:
   ```sh
   dotnet run
   ```
7. Open **http://localhost:5000** in your browser.

## Deploying to Azure

1. Publish the project to **Azure App Services** from Visual Studio.
2. Set up an **Azure SQL Database** and update the connection string.
3. Configure **App Service Settings** and deploy.

## Future Enhancements

- Implement a **shopping cart and checkout system**.
- Add **payment gateway integration**.
- Improve search and filtering functionalities.
- Enhance UI/UX with modern design elements.

---
Author: Kaustubh Desale  
GitHub: https://github.com/kaustubh5716/Bookly  
Deployed On: Microsoft Azure
