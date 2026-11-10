using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using RunWay.Api.Data;
var output=Path.GetFullPath(args.FirstOrDefault()??"docs");Directory.CreateDirectory(output);
using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer("Server=localhost;Database=RaceDayPlanningDb;Trusted_Connection=True;TrustServerCertificate=True").Options);
var entities=db.Model.GetEntityTypes().Select(e=>new{
 Name=e.GetTableName(),Properties=e.GetProperties().Select(p=>new{Name=p.Name,Type=p.GetColumnType(),Nullable=p.IsNullable,PrimaryKey=e.FindPrimaryKey()!.Properties.Contains(p),ForeignKey=e.GetForeignKeys().Any(f=>f.Properties.Contains(p))}).ToArray(),
 ForeignKeys=e.GetForeignKeys().Select(f=>new{Properties=f.Properties.Select(p=>p.Name).ToArray(),Parent=f.PrincipalEntityType.GetTableName(),ParentProperties=f.PrincipalKey.Properties.Select(p=>p.Name).ToArray(),Unique=f.IsUnique,Required=f.IsRequired}).ToArray()
}).OrderBy(e=>e.Name).ToArray();
File.WriteAllText(Path.Combine(output,"schema.json"),JsonSerializer.Serialize(entities,new JsonSerializerOptions{WriteIndented=true}));
var sql="-- RaceDay implementation-aligned schema. Run on a clean SQL Server instance.\n-- Never overwrite an existing database containing coursework data.\nIF DB_ID(N'RaceDayPlanningDb') IS NULL CREATE DATABASE [RaceDayPlanningDb];\nGO\nUSE [RaceDayPlanningDb];\nGO\nSET QUOTED_IDENTIFIER ON;\nSET ANSI_NULLS ON;\nSET ANSI_PADDING ON;\nSET ANSI_WARNINGS ON;\nSET ARITHABORT ON;\nSET CONCAT_NULL_YIELDS_NULL ON;\nSET NUMERIC_ROUNDABORT OFF;\nGO\nIF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AspNetUsers') THROW 50001, 'Use a fresh empty RaceDayPlanningDb database; existing data has not been changed.', 1;\nGO\n";
sql+=db.Database.GenerateCreateScript()+"\nGO\n-- Realistic sample users: two organisers and two participants. Passwords are Identity hashes.\n";
sql+="INSERT [AspNetRoles] ([Id],[Name],[NormalizedName],[ConcurrencyStamp]) VALUES (N'role-organiser',N'Organiser',N'ORGANISER',N'role-org-stamp'),(N'role-participant',N'Participant',N'PARTICIPANT',N'role-part-stamp'),(N'role-admin',N'Admin',N'ADMIN',N'role-admin-stamp');\n";
var samples=new[]{("org-thabo","Thabo","Mbeki","thabo@raceday.co.za","role-organiser"),("org-nadia","Nadia","Williams","nadia@raceday.co.za","role-organiser"),("part-sipho","Sipho","Ndlovu","sipho@example.com","role-participant"),("part-lerato","Lerato","Dlamini","lerato@example.com","role-participant")};
foreach(var (id,first,last,email,role) in samples){
 var user=new ApplicationUser{Id=id,FirstName=first,LastName=last,Email=email,UserName=email};var hash=new PasswordHasher<ApplicationUser>().HashPassword(user,"Coursework@123");
 sql+=$"INSERT [AspNetUsers] ([Id],[FirstName],[LastName],[Gender],[UserName],[NormalizedUserName],[Email],[NormalizedEmail],[EmailConfirmed],[PasswordHash],[SecurityStamp],[ConcurrencyStamp],[PhoneNumberConfirmed],[TwoFactorEnabled],[LockoutEnabled],[AccessFailedCount]) VALUES (N'{id}',N'{first}',N'{last}',N'PreferNotToSay',N'{email}',N'{email.ToUpperInvariant()}',N'{email}',N'{email.ToUpperInvariant()}',1,N'{hash}',N'{Guid.NewGuid()}',N'{Guid.NewGuid()}',0,0,1,0);\n";
 sql+=$"INSERT [AspNetUserRoles] ([UserId],[RoleId]) VALUES(N'{id}',N'{role}');\n";
}
sql+=@"
SET IDENTITY_INSERT [Events] ON;
INSERT [Events] ([Id],[Name],[Description],[Date],[Location],[DistanceKm],[EventType],[OrganiserId],[CreatedAt]) VALUES
(1,N'Soweto Community 10K',N'Community road running race',DATEADD(day,-7,DATEADD(hour,2,GETUTCDATE())),N'Soweto, Johannesburg',10,N'Run',N'org-thabo',GETUTCDATE()),
(2,N'Cape Town Charity Walk',N'A family charity walk',DATEADD(day,21,DATEADD(hour,2,GETUTCDATE())),N'Sea Point, Cape Town',5,N'Walk',N'org-nadia',GETUTCDATE()),
(3,N'Durban Coastal Cycle',N'A timed coastal cycle event',DATEADD(day,35,DATEADD(hour,2,GETUTCDATE())),N'Durban Beachfront',25,N'Cycle',N'org-thabo',GETUTCDATE());
SET IDENTITY_INSERT [Events] OFF;
SET IDENTITY_INSERT [Categories] ON;
INSERT [Categories] ([Id],[EventId],[Name],[DistanceKm]) VALUES(1,1,N'10 km Open',10),(2,1,N'5 km Social',5),(3,2,N'5 km Walk',5),(4,3,N'25 km Cycle',25);
SET IDENTITY_INSERT [Categories] OFF;
SET IDENTITY_INSERT [Enrolments] ON;
INSERT [Enrolments] ([Id],[EventId],[CategoryId],[ParticipantId],[Status],[EnrolledAt]) VALUES
(1,1,1,N'part-sipho',N'Confirmed',DATEADD(day,-14,GETUTCDATE())),
(2,1,1,N'part-lerato',N'Confirmed',DATEADD(day,-14,GETUTCDATE())),
(3,2,3,N'part-sipho',N'Pending',GETUTCDATE()),
(4,3,4,N'part-lerato',N'Confirmed',GETUTCDATE());
SET IDENTITY_INSERT [Enrolments] OFF;
SET IDENTITY_INSERT [Results] ON;
INSERT [Results] ([Id],[EventId],[EnrolmentId],[FinishTime],[Position],[PublishedAt]) VALUES
(1,1,1,'00:47:12',1,GETUTCDATE()),(2,1,2,'00:51:30',2,GETUTCDATE());
SET IDENTITY_INSERT [Results] OFF;
GO
IF (SELECT COUNT(*) FROM sys.tables) <> 11 THROW 50002, 'Unexpected schema table count.',1;
IF (SELECT COUNT(*) FROM [Events]) <> 3 THROW 50003, 'Sample events are missing.',1;
IF (SELECT COUNT(*) FROM [AspNetUsers]) <> 4 THROW 50004, 'Sample users are missing.',1;
PRINT 'RaceDay schema and sample data verified successfully.';
GO
";
File.WriteAllText(Path.Combine(output,"RaceDay_Schema.sql"),sql);
Console.WriteLine($"Exported {entities.Length} tables, schema metadata and seeded SQL to {output}");
