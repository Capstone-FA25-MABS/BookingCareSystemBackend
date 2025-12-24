using BookingCare.Services.Payment.Enums;
using BookingCare.Services.Payment.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Payment.Data;

/// <summary>
/// Database context for the Payment Service
/// </summary>
public class PaymentDbContext : DbContext
{
    private const string SqlGetDate = "GETDATE()";

    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options) { }

    /// <summary>
    /// DbSet for the payments table
    /// </summary>
    public DbSet<PaymentEntity> Payments { get; set; }

    /// <summary>
    /// DbSet for the payment_methods table
    /// </summary>
    public DbSet<PaymentMethodEntity> PaymentMethods { get; set; }

    /// <summary>
    /// DbSet for the payos_payment_mappings table
    /// </summary>
    public DbSet<PayOSPaymentMappingEntity> PayOSPaymentMappings { get; set; }

    /// <summary>
    /// DbSet for the bank_accounts table
    /// </summary>
    public DbSet<BankAccountEntity> BankAccounts { get; set; }

    /// <summary>
    /// DbSet for the refund_histories table
    /// </summary>
    public DbSet<RefundHistoryEntity> RefundHistories { get; set; }

    /// <summary>
    /// DbSet for the hospital_payouts table
    /// </summary>
    public DbSet<HospitalPayoutEntity> HospitalPayouts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure PaymentEntity
        modelBuilder.Entity<PaymentEntity>(entity =>
        {
            // Enum conversion
            entity
                .Property(e => e.TransactionType)
                .HasConversion(
                    v => v.ToString(),
                    v => (TransactionType)Enum.Parse(typeof(TransactionType), v)
                )
                .HasMaxLength(20);

            entity
                .Property(e => e.Status)
                .HasConversion(
                    v => v.ToString(),
                    v => (PaymentStatus)Enum.Parse(typeof(PaymentStatus), v)
                )
                .HasMaxLength(10);

            // Relationship with PaymentMethod
            entity
                .HasOne(d => d.PaymentMethod)
                .WithMany(p => p.Payments)
                .HasForeignKey(d => d.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_payments_payment_method_id");

            // Check constraints - Updated for EF Core 8
            entity.ToTable(
                "payments",
                t =>
                {
                    t.HasCheckConstraint(
                        "CK_payments_transaction_type",
                        "[transaction_type] IN ('APPOINTMENT', 'SUBSCRIPTION')"
                    );
                    t.HasCheckConstraint(
                        "CK_payments_status",
                        "[status] IN ('PENDING', 'COMPLETED', 'FAILED', 'REFUNDED')"
                    );
                }
            );
        });

        // Configure PaymentMethodEntity
        modelBuilder.Entity<PaymentMethodEntity>(entity =>
        {
            // Enum conversion for Status
            entity
                .Property(e => e.Status)
                .HasConversion(
                    v => v.ToString(),
                    v => (PaymentMethodStatus)Enum.Parse(typeof(PaymentMethodStatus), v)
                )
                .HasMaxLength(10);

            // Check constraint for status - Updated for EF Core 8
            entity.ToTable(
                "payment_methods",
                t =>
                {
                    t.HasCheckConstraint(
                        "CK_payment_methods_status",
                        "[status] IN ('ACTIVE', 'INACTIVE')"
                    );
                }
            );
        });

        // Configure PayOSPaymentMappingEntity
        modelBuilder.Entity<PayOSPaymentMappingEntity>(entity =>
        {
            // Relationship with Payment entity
            entity
                .HasOne<PaymentEntity>()
                .WithMany()
                .HasForeignKey(e => e.PaymentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_payos_payment_mappings_payment_id");
        });

        // Configure BankAccountEntity
        modelBuilder.Entity<BankAccountEntity>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Check constraints - Updated for EF Core 8
            entity.ToTable(
                "bank_accounts",
                t =>
                {
                    t.HasCheckConstraint(
                        "CK_bank_accounts_bank_code_length",
                        "LEN([bank_code]) >= 2 AND LEN([bank_code]) <= 10"
                    );
                    t.HasCheckConstraint(
                        "CK_bank_accounts_account_number_length",
                        "LEN([account_number]) >= 6 AND LEN([account_number]) <= 20"
                    );
                    t.HasCheckConstraint(
                        "CK_bank_accounts_account_number_numeric",
                        "[account_number] NOT LIKE '%[^0-9]%'"
                    );
                }
            );

            // Default values
            entity.Property(e => e.IsDefault).HasDefaultValue(false);

            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql(SqlGetDate);

            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(SqlGetDate);
        });

        // Configure RefundHistoryEntity
        modelBuilder.Entity<RefundHistoryEntity>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Enum conversion for Status
            entity
                .Property(e => e.Status)
                .HasConversion(
                    v => v.ToString(),
                    v => (RefundStatus)Enum.Parse(typeof(RefundStatus), v)
                )
                .HasMaxLength(20);

            // Relationship with PaymentEntity (1:N - one payment can have multiple refund histories)
            // Example scenarios:
            // 1. Patient changes to cheaper doctor -> creates 1st refund for price difference
            // 2. Patient later cancels appointment -> creates 2nd refund for remaining amount
            entity
                .HasOne(r => r.Payment)
                .WithMany()
                .HasForeignKey(r => r.PaymentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_refund_histories_payment_id");

            // Relationship with BankAccountEntity (optional - can be null)
            entity
                .HasOne(r => r.BankAccount)
                .WithMany()
                .HasForeignKey(r => r.BankAccountId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_refund_histories_bank_account_id");

            // Check constraints and business rules
            entity.ToTable(
                "refund_histories",
                t =>
                {
                    t.HasCheckConstraint(
                        "CK_refund_histories_status",
                        "[status] IN ('WAITING', 'PENDING', 'COMPLETED')"
                    );
                    t.HasCheckConstraint(
                        "CK_refund_histories_refund_amount_positive",
                        "[refund_amount] > 0"
                    );
                    // Transfer date only exists when status = COMPLETED
                    t.HasCheckConstraint(
                        "CK_refund_histories_transfer_date_completed",
                        "([status] = 'COMPLETED' AND [transfer_date] IS NOT NULL) OR ([status] != 'COMPLETED')"
                    );
                }
            );

            // Default values
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(SqlGetDate);

            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(SqlGetDate);
        });

        // Configure HospitalPayoutEntity
        modelBuilder.Entity<HospitalPayoutEntity>(entity =>
        {
            // Enum conversion
            entity.Property(e => e.Status).HasConversion<string>();

            // Foreign key relationship with BankAccountEntity
            entity
                .HasOne(e => e.BankAccount)
                .WithMany()
                .HasForeignKey(e => e.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_hospital_payouts_bank_account_id");

            // Check constraints
            entity.ToTable(
                "hospital_payouts",
                t =>
                {
                    t.HasCheckConstraint(
                        "CK_hospital_payouts_status",
                        "[status] IN ('PENDING', 'COMPLETED')"
                    );
                    t.HasCheckConstraint(
                        "CK_hospital_payouts_total_amount_positive",
                        "[total_amount] > 0"
                    );
                    t.HasCheckConstraint(
                        "CK_hospital_payouts_appointment_count_positive",
                        "[appointment_count] > 0"
                    );
                    t.HasCheckConstraint(
                        "CK_hospital_payouts_period_valid",
                        "[period_start] <= [period_end]"
                    );
                }
            );

            // Default values
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(SqlGetDate);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(SqlGetDate);

            // Indexes for efficient querying
            entity.HasIndex(e => e.HospitalId).HasDatabaseName("IX_hospital_payouts_hospital_id");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_hospital_payouts_status");
            entity
                .HasIndex(e => new
                {
                    e.HospitalId,
                    e.PeriodStart,
                    e.PeriodEnd,
                })
                .HasDatabaseName("IX_hospital_payouts_hospital_period");
        });

        // Seed data for payment methods
        SeedData(modelBuilder);
    }

    /// <summary>
    /// Seed data for common payment methods
    /// </summary>
    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<PaymentMethodEntity>()
            .HasData(
                new PaymentMethodEntity
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "CASH",
                    Description = "Thanh toán bằng tiền mặt",
                    Status = PaymentMethodStatus.INACTIVE,
                },
                new PaymentMethodEntity
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "CREDIT_CARD",
                    Description = "Thanh toán bằng thẻ tín dụng",
                    Status = PaymentMethodStatus.INACTIVE,
                },
                new PaymentMethodEntity
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "BANK_TRANSFER",
                    Description = "Chuyển khoản ngân hàng",
                    Status = PaymentMethodStatus.INACTIVE,
                },
                new PaymentMethodEntity
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = "MOMO",
                    Description = "Thanh toán qua MoMo",
                    Status = PaymentMethodStatus.INACTIVE,
                },
                new PaymentMethodEntity
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Name = "ZALOPAY",
                    Description = "Thanh toán qua ZaloPay",
                    Status = PaymentMethodStatus.INACTIVE,
                },
                new PaymentMethodEntity
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    Name = "VNPAY",
                    Description = "Thanh toán qua VNPay",
                    ImageUrl =
                        "https://vnpay.vn/s1/statics.vnpay.vn/2023/6/0oxhzjmxbksr1686814746087.png",
                    Status = PaymentMethodStatus.ACTIVE,
                },
                new PaymentMethodEntity
                {
                    Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    Name = "PAYOS",
                    Description = "Thanh toán qua PayOS",
                    ImageUrl = "https://payos.vn/docs/img/logo.svg",
                    Status = PaymentMethodStatus.ACTIVE,
                },
                new PaymentMethodEntity
                {
                    Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    Name = "STRIPE",
                    Description = "Thanh toán qua Stripe",
                    ImageUrl =
                        "https://images.ctfassets.net/fzn2n1nzq965/HTTOloNPhisV9P4hlMPNA/cacf1bb88b9fc492dfad34378d844280/Stripe_icon_-_square.svg",
                    Status = PaymentMethodStatus.ACTIVE,
                }
            );
    }
}
