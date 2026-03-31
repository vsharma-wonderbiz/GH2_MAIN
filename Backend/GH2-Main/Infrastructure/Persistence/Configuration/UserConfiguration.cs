using AuthMicroservice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthMicroservice.Infrastructure.Persistence.Configuration
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> entity)
        {
       
            entity.HasKey(u => u.UserId);

            entity.Property(u => u.UserId)
                  .ValueGeneratedOnAdd();


            entity.Property(u => u.Username)
                  .IsRequired()
                  .HasMaxLength(50);

      
            entity.Property(u => u.Email)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(u => u.PasswordHash)
                  .IsRequired()
                  .HasMaxLength(255);

         
            entity.Property(u => u.Role)
                  .IsRequired()
                  .HasMaxLength(20)
                  .HasDefaultValue("User");

            entity.Property(u => u.RefreshToken)
                  .HasMaxLength(255)
                  .IsRequired(false);

            entity.Property(u => u.RefreshTokenExpiry)
                  .IsRequired(false);

        }
    }
}