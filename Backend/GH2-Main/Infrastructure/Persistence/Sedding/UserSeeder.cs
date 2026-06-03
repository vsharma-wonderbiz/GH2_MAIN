using AuthMicroservice.Domain.Entities;

namespace Infrastructure.Persistence.Seeding
{
   public static class UserSeeder
    {
         public static void Seed(ApplicationDbContext context)
        {


            context.Database.EnsureCreated(); // Create DB if not exists

            if (!context.Users.Any(u => u.Role == "Admin"))
            {
                Console.WriteLine("No admin found. Creating default admin...");

                var admin = new User
                {
                    Username = "Admin",
                    Email = "admin@example.com",
                    Role = "Admin",
                    PasswordHash = HashPassword("Admin@123") // Use same hash as normal users
                };

                context.Users.Add(admin);
                context.SaveChanges();

                Console.WriteLine("Default admin created!");
            }
            else
            {
                Console.WriteLine("Admin already exists. Skipping seed.");
            }
        }


        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}