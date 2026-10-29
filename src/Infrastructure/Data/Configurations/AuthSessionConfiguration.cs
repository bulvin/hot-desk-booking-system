using Domain.Users;
using Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class AuthSessionConfiguration : IEntityTypeConfiguration<AuthSession>
{
    public void Configure(EntityTypeBuilder<AuthSession> builder)
    {
        builder.HasKey(session => session.Id);
        builder.HasOne<User>().WithMany().HasForeignKey(session => session.UserId);
        builder.HasIndex(session => session.UserId);
    }
}
