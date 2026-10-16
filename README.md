# RunWay

ASP.NET Core MVC (.NET 9) + ASP.NET Core Identity + SQL Server.

## Before you run
1. **SQL Server name** - open `appsettings.json` and check `ConnectionStrings:DefaultConnection`.
   It is set to `LabVM2049939\SQLEXPRESS` (the lab machine). On another computer use for example:
   `Server=(localdb)\\MSSQLLocalDB;Database=RunWayDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True`
2. **Fresh database** - if an older `RunWayDB` exists from the previous version of this project, delete it
   (Visual Studio > SQL Server Object Explorer) or change the database name. Old tables are missing the
   FirstName / LastName / Gender / ProfileImagePath columns and cause "Invalid column name" errors.
3. **No old migrations** - do not add the old `Migrations` folder (Identity 3.0 scaffold). This project
   creates the schema itself on first start. If you add your own migration later
   (`dotnet ef migrations add InitialCreate`), the app applies it automatically.
4. Requires the .NET 9 SDK (Visual Studio 2022 17.12+). For .NET 8 change `net9.0` to `net8.0` and the
   package versions `9.0.12` to `8.0.11` in `RunWay.csproj`.
5. Bootstrap and jQuery load from a CDN, so the machine needs internet access for styling.

## Run
Open `RunWay.sln` in Visual Studio and press F5 (or `dotnet run`).
On first start the database, the roles (Admin, Organiser, Participant) and the admin account are created.

## Local admin account (coursework only - change before deploying)
- admin@admin.com / Admin@123

## Pages
- `/` Home
- `/Identity/Account/Register` (first name, last name, gender, profile image) and `/Identity/Account/Login`
- `/Identity/Account/Manage` account settings
- `/Home/AdminDashboard` and `/UserRoles` (Admin only)
