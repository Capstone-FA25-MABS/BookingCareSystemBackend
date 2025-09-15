using Microsoft.EntityFrameworkCore;
using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Data;

/// <summary>
/// Database context cho Payment Service
/// </summary>
public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// DbSet cho bảng payments
    /// </summary>
    public DbSet<PaymentEntity> Payments { get; set; }

    /// <summary>
    /// DbSet cho bảng payment_methods
    /// </summary>
    public DbSet<PaymentMethodEntity> PaymentMethods { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cấu hình PaymentEntity
        modelBuilder.Entity<PaymentEntity>(entity =>
        {
            // Enum conversion
            entity.Property(e => e.TransactionType)
                .HasConversion(
                    v => v.ToString(),
                    v => (TransactionType)Enum.Parse(typeof(TransactionType), v))
                .HasMaxLength(20);

            entity.Property(e => e.Status)
                .HasConversion(
                    v => v.ToString(),
                    v => (PaymentStatus)Enum.Parse(typeof(PaymentStatus), v))
                .HasMaxLength(10);

            // Relationship với PaymentMethod - FIX: Sử dụng Restrict thay vì SetNull
            entity.HasOne(d => d.PaymentMethod)
                .WithMany(p => p.Payments)
                .HasForeignKey(d => d.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_payments_payment_method_id");

            // Check constraints (tương đương SQL CHECK)
            entity.HasCheckConstraint("CK_payments_transaction_type",
                "[transaction_type] IN ('APPOINTMENT', 'SUBSCRIPTION')");

            entity.HasCheckConstraint("CK_payments_status",
                "[status] IN ('PENDING', 'COMPLETED', 'FAILED', 'REFUNDED')");
        });

        // Cấu hình PaymentMethodEntity
        modelBuilder.Entity<PaymentMethodEntity>(entity =>
        {
            // Enum conversion cho Status
            entity.Property(e => e.Status)
                .HasConversion(
                    v => v.ToString(),
                    v => (PaymentMethodStatus)Enum.Parse(typeof(PaymentMethodStatus), v))
                .HasMaxLength(10);

            // Unique constraint cho name
            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("UQ_payment_methods_name");

            // Check constraint cho status
            entity.HasCheckConstraint("CK_payment_methods_status",
                "[status] IN ('ACTIVE', 'INACTIVE')");
        });

        // Seed data cho payment methods
        SeedData(modelBuilder);
    }

    /// <summary>
    /// Seed data cho các phương thức thanh toán phổ biến
    /// </summary>
    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentMethodEntity>().HasData(
            new PaymentMethodEntity
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "CASH",
                Description = "Thanh toán bằng tiền mặt",
                Status = PaymentMethodStatus.ACTIVE
            },
            new PaymentMethodEntity
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "CREDIT_CARD",
                Description = "Thanh toán bằng thẻ tín dụng",
                Status = PaymentMethodStatus.ACTIVE
            },
            new PaymentMethodEntity
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "BANK_TRANSFER",
                Description = "Chuyển khoản ngân hàng",
                Status = PaymentMethodStatus.ACTIVE
            },
            new PaymentMethodEntity
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "MOMO",
                Description = "Thanh toán qua MoMo",
                Status = PaymentMethodStatus.ACTIVE
            },
            new PaymentMethodEntity
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Name = "ZALOPAY",
                Description = "Thanh toán qua ZaloPay",
                Status = PaymentMethodStatus.ACTIVE
            },
            new PaymentMethodEntity
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Name = "VNPAY",
                Description = "Thanh toán qua VNPay",
                Status = PaymentMethodStatus.ACTIVE
            }
        );
    }
}