using Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class RefreshCredentialConfiguration : IEntityTypeConfiguration<RefreshCredential>
{
    public void Configure(EntityTypeBuilder<RefreshCredential> builder)
    {
        builder.HasKey(credential => credential.Hash);
        builder.Property(credential => credential.Hash).HasMaxLength(64);
        builder.HasOne<AuthSession>().WithMany().HasForeignKey(credential => credential.SessionId);
    }
}
