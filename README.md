# HIT339 Library Management System

A library management web application built with **ASP.NET Core MVC (.NET 10)**, **Entity Framework Core**, **ASP.NET Core Identity** and **SQL Server**.

This guide explains how to download the source code, open the Visual Studio solution and run the library locally.

## 1. Requirements

Install the following on a Windows computer:

- **Visual Studio 2026** with the **ASP.NET and web development** workload
- **.NET 10 SDK** (normally installed with the appropriate Visual Studio workload)
- **SQL Server Express LocalDB** for the local development database

You do **not** need an Azure account to run the project locally.

## 2. Download the project

1. Open the GitHub repository: **https://github.com/J330/HIT339-Library**
2. Click **Code → Download ZIP**.
3. Extract the ZIP to a folder on your computer. Do not try to open the solution while it is still inside the ZIP.

Alternatively, clone the repository with Git:

```powershell
git clone https://github.com/J330/HIT339-Library.git
```

## 3. Open the solution in Visual Studio

1. Open **Visual Studio 2026**.
2. Select **Open a project or solution**.
3. Open this file from the extracted repository folder:

   `S379658-HIT339-Assignment2-Library.slnx`

4. Wait for Visual Studio to restore the required NuGet packages.
5. In **Solution Explorer**, right-click the **Library** web project and select **Set as Startup Project** if it is not already selected.

The `.slnx` solution points to `Library/S379658-HIT339-Assignment2-Library.csproj`.

## 4. Run the library

1. Make sure **SQL Server Express LocalDB** is installed and working.
2. Select the project's **HTTPS** launch profile in Visual Studio.
3. Press **F5** (or click the green **Run** button).
4. Accept the local HTTPS development certificate prompt if one appears.
5. Wait for the browser to open the local library website.

On the first run in **Development** mode, the application automatically applies its Entity Framework Core database migrations and creates local demonstration accounts.

The default development database is **`LibraryManagementDB`** on **`(localdb)\MSSQLLocalDB`**. It is configured in `Library/appsettings.json`.

## 5. Local demo accounts

These credentials are for the **local Development environment only**. They are **not** the passwords for the Azure-hosted library.

| Role | Username | Password |
|---|---|---|
| Administrator | `admin` | `Admin123!` |
| Manager | `manager` | `Manager123!` |
| Receptionist | `reception` | `Reception123!` |
| Borrower | `borrower1` | `Borrower123!` |
| Borrower | `borrower2` | `Borrower123!` |

**Never use these demonstration passwords on a public production deployment.**

## 6. Troubleshooting

- **Solution will not open:** Use a Visual Studio version that supports .NET 10 and `.slnx` solutions, and confirm you extracted the complete repository.
- **Missing packages or build errors:** In Visual Studio, select **Build → Rebuild Solution** and allow NuGet restore to complete.
- **SQL Server / LocalDB error:** Install SQL Server Express LocalDB and check that the connection string in `Library/appsettings.json` points to a working LocalDB instance.
- **Browser does not open:** Check Visual Studio's Output window for errors and confirm the HTTPS launch profile is selected.

## Local project vs. Azure website

Opening the `.slnx` runs the **development version** of the library on your own computer. By default, its database is separate from the Azure SQL database used by the live website.

The **Library Desktop Client** is a separate Windows application intended for staff. It connects to the hosted library website; you do not need that client to run the `.slnx` project in Visual Studio.
