using Microsoft.EntityFrameworkCore;
using ProjectApprovalSystem.Core.Entities;
using ProjectApprovalSystem.Data.Context;

namespace ProjectApprovalSystem.Web.Infrastructure;

internal static class DemoDataSeeder
{
    public static async Task SeedAsync(PasDbContext context)
    {
        if (!await context.ResearchAreas.AnyAsync())
        {
            var areas = new[]
            {
                new ResearchArea { Name = "Artificial Intelligence", Description = "Machine learning, deep learning, and intelligent systems" },
                new ResearchArea { Name = "Web Development", Description = "Full-stack, frontend, and backend web systems" },
                new ResearchArea { Name = "Cybersecurity", Description = "Secure systems, threat detection, and privacy engineering" }
            };

            await context.ResearchAreas.AddRangeAsync(areas);
            await context.SaveChangesAsync();
        }

        if (!await context.Users.AnyAsync())
        {
            var users = new[]
            {
                CreateUser("leader@pas.local", "Module Leader", UserRole.ModuleLeader),
                CreateUser("student1@pas.local", "Nethmi Perera", UserRole.Student),
                CreateUser("student2@pas.local", "Kavindu Silva", UserRole.Student),
                CreateUser("supervisor1@pas.local", "Dr. Fernando", UserRole.Supervisor),
                CreateUser("supervisor2@pas.local", "Dr. De Silva", UserRole.Supervisor)
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();
        }

        if (!await context.SupervisorExpertises.AnyAsync())
        {
            var supervisors = await context.Users.Where(user => user.Role == UserRole.Supervisor).OrderBy(user => user.Id).ToListAsync();
            var areas = await context.ResearchAreas.OrderBy(area => area.Id).ToListAsync();

            if (supervisors.Count >= 2 && areas.Count >= 3)
            {
                await context.SupervisorExpertises.AddRangeAsync(
                    new SupervisorExpertise { SupervisorId = supervisors[0].Id, ResearchAreaId = areas[0].Id },
                    new SupervisorExpertise { SupervisorId = supervisors[0].Id, ResearchAreaId = areas[1].Id },
                    new SupervisorExpertise { SupervisorId = supervisors[1].Id, ResearchAreaId = areas[2].Id },
                    new SupervisorExpertise { SupervisorId = supervisors[1].Id, ResearchAreaId = areas[1].Id }
                );

                await context.SaveChangesAsync();
            }
        }

        if (!await context.Proposals.AnyAsync())
        {
            var student = await context.Users.FirstAsync(user => user.Role == UserRole.Student);
            var webArea = await context.ResearchAreas.FirstAsync(area => area.Name == "Web Development");

            await context.Proposals.AddAsync(new Proposal
            {
                ProposalId = $"{DateTime.UtcNow.Year}-0001",
                StudentId = student.Id,
                Title = "Inclusive Capstone Collaboration Portal",
                Abstract = "A portal that improves fair matching between students and supervisors using blind review.",
                Description = "Build a web-based approval workflow that keeps identities hidden until a match is confirmed.",
                TechStack = ".NET 8, SQL Server, Razor Views",
                ResearchAreaId = webArea.Id,
                Status = ProposalStatus.Pending,
                SubmittedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
        }
    }

    private static User CreateUser(string email, string fullName, UserRole role)
    {
        return new User
        {
            Email = email,
            FullName = fullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}