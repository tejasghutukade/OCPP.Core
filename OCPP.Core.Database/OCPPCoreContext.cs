using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OCPP.Core.Database
{
    public partial class OCPPCoreContext : DbContext
    {
        private IConfiguration _configuration;
        private ILogger _logger;
        public OCPPCoreContext(DbContextOptions<OCPPCoreContext> options , ILoggerFactory loggerFactory, IConfiguration config)
            : base(options)
        {
            _configuration = config;
            _logger = loggerFactory.CreateLogger<OCPPCoreContext>();
        }

        public virtual DbSet<ChargePoint> ChargePoints { get; set; } = null!;
        public virtual DbSet<ChargeTag> ChargeTags { get; set; } = null!;
        public virtual DbSet<ChargingProfile> ChargingProfiles { get; set; } = null!;
        public virtual DbSet<ConnectorStatus> ConnectorStatuses { get; set; } = null!;
        public virtual DbSet<ConnectorStatusView> ConnectorStatusViews { get; set; } = null!;
        public virtual DbSet<MessageLog> MessageLogs { get; set; } = null!;
        public virtual DbSet<Transaction> Transactions { get; set; } = null!;

        

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                _logger.LogCritical("Database not configured");
                // if (!string.IsNullOrEmpty(_configuration.GetConnectionString("MySql")))
                // {
                //     optionsBuilder.UseMySql(_configuration.GetConnectionString("MySql"), ServerVersion.Parse("8.0.28-mysql"));
                // }
                // else
                // {
                //     string sqlConnString = _configuration.GetConnectionString("SqlServer");
                //     string liteConnString = _configuration.GetConnectionString("SQLite");
                //     // if (!string.IsNullOrWhiteSpace(sqlConnString))
                //     // {
                //     //     optionsBuilder.UseSqlServer(sqlConnString);
                //     // }
                //     // else if (!string.IsNullOrWhiteSpace(liteConnString))
                //     // {
                //     //     optionsBuilder.UseSqlite(liteConnString);
                //     // }
                // }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseCollation("utf8mb4_0900_ai_ci")
                .HasCharSet("utf8mb4");

            modelBuilder.Entity<ChargePoint>(entity =>
            {
                entity.ToTable("ChargePoint");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CbSerialNumber).HasMaxLength(25);

                entity.Property(e => e.ChargePointId).HasMaxLength(50);

                entity.Property(e => e.ClientCertThumb).HasMaxLength(100);

                entity.Property(e => e.Comment).HasMaxLength(200);

                entity.Property(e => e.CpSerialNumber).HasMaxLength(25);

                entity.Property(e => e.CurrentTime).HasColumnType("datetime");

                entity.Property(e => e.FirmwareVersion).HasMaxLength(50);

                entity.Property(e => e.Iccid).HasMaxLength(20);

                entity.Property(e => e.Imsi).HasMaxLength(20);

                entity.Property(e => e.Interval).HasDefaultValueSql("'5'");

                entity.Property(e => e.MeterSerialNumber).HasMaxLength(25);

                entity.Property(e => e.MeterType).HasMaxLength(25);

                entity.Property(e => e.Model).HasMaxLength(20);

                entity.Property(e => e.Name).HasMaxLength(100);

                entity.Property(e => e.Password).HasMaxLength(50);

                entity.Property(e => e.Status)
                    .HasColumnType("enum('Accepted','Pending','Rejected')")
                    .HasDefaultValueSql("'Accepted'");

                entity.Property(e => e.Username).HasMaxLength(50);

                entity.Property(e => e.Vendor).HasMaxLength(20);
            });

            modelBuilder.Entity<ChargeTag>(entity =>
            {
                entity.HasIndex(e => e.ParentTagId, "fk_parenttag_id_idx");

                entity.Property(e => e.ExpiryDate).HasColumnType("datetime");

                entity.Property(e => e.TagId).HasMaxLength(50);

                entity.Property(e => e.TagName).HasMaxLength(200);

                entity.Property(e => e.TagStatus)
                    .HasColumnType("enum('Accepted','Blocked','Expired','Invalid','ConcurentTx')")
                    .HasDefaultValueSql("'Accepted'");

                entity.HasOne(d => d.ParentTag)
                    .WithMany(p => p.InverseParentTag)
                    .HasForeignKey(d => d.ParentTagId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_parenttag_id");
            });

            modelBuilder.Entity<ChargingProfile>(entity =>
            {
                entity.ToTable("ChargingProfile");

                entity.HasIndex(e => e.ChargePointId, "fk_chargepoint_id_idx");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.ChargingProfileKind).HasColumnType("enum('Absolute','Recurring','Relative')");

                entity.Property(e => e.ChargingProfilePurpose).HasColumnType("enum('ChargePointMaxProfile','TxDefaultProfile','TxProfile')");

                entity.Property(e => e.RecurrencyKind).HasColumnType("enum('Daily','Weekly')");

                entity.Property(e => e.Status).HasColumnType("enum('Accepted','Rejected','NotSupported')");

                entity.Property(e => e.ValidFrom).HasColumnType("datetime");

                entity.Property(e => e.ValidTo).HasColumnType("datetime");

                entity.HasOne(d => d.ChargePoint)
                    .WithMany(p => p.ChargingProfiles)
                    .HasForeignKey(d => d.ChargePointId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_chargepoint_id");
            });

            modelBuilder.Entity<ConnectorStatus>(entity =>
            {
                entity.ToTable("ConnectorStatus");

                entity.HasIndex(e => new { e.ChargePointId, e.ConnectorId }, "clustered_key");

                entity.Property(e => e.ConnectorName).HasMaxLength(100);

                entity.Property(e => e.ErrorCode).HasMaxLength(45);

                entity.Property(e => e.Info).HasMaxLength(50);

                entity.Property(e => e.LastMeterTime).HasColumnType("datetime");

                entity.Property(e => e.LastStatus).HasMaxLength(100);

                entity.Property(e => e.LastStatusTime).HasColumnType("datetime");

                entity.Property(e => e.VendorErrorCode).HasMaxLength(45);

                entity.Property(e => e.VendorId).HasMaxLength(255);

                entity.HasOne(d => d.ChargePoint)
                    .WithMany(p => p.ConnectorStatuses)
                    .HasForeignKey(d => d.ChargePointId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_cp_cp_cpid");
            });

            modelBuilder.Entity<ConnectorStatusView>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("ConnectorStatusView");

                entity.Property(e => e.ConnectorName).HasMaxLength(100);

                entity.Property(e => e.LastMeterTime).HasColumnType("datetime");

                entity.Property(e => e.LastStatus).HasMaxLength(100);

                entity.Property(e => e.LastStatusTime).HasColumnType("datetime");

                entity.Property(e => e.StartResult).HasMaxLength(100);

                entity.Property(e => e.StartTagId).HasMaxLength(45);

                entity.Property(e => e.StartTime).HasColumnType("datetime");

                entity.Property(e => e.StopReason).HasMaxLength(100);

                entity.Property(e => e.StopTagId).HasMaxLength(45);

                entity.Property(e => e.StopTime).HasColumnType("datetime");

                entity.Property(e => e.TransactionId).HasDefaultValueSql("'0'");
            });

            modelBuilder.Entity<MessageLog>(entity =>
            {
                entity.HasKey(e => e.LogId)
                    .HasName("PRIMARY");

                entity.ToTable("MessageLog");

                entity.Property(e => e.Direction).HasColumnType("enum('Sent','Recieved')");

                entity.Property(e => e.ErrorCode).HasMaxLength(100);

                entity.Property(e => e.LogTime).HasColumnType("datetime");

                entity.Property(e => e.Message).HasMaxLength(100);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasIndex(e => e.ChargePointId, "fk_tx_cp_cpid_idx");

                entity.Property(e => e.StartResult).HasMaxLength(100);

                entity.Property(e => e.StartTagId).HasMaxLength(45);

                entity.Property(e => e.StartTime).HasColumnType("datetime");

                entity.Property(e => e.StopReason).HasMaxLength(100);

                entity.Property(e => e.StopTagId).HasMaxLength(45);

                entity.Property(e => e.StopTime).HasColumnType("datetime");

                entity.Property(e => e.Uid).HasMaxLength(45);

                entity.HasOne(d => d.ChargePoint)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(d => d.ChargePointId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_tx_cp_cpid");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
