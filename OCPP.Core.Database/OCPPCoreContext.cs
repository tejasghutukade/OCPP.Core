using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace OCPP.Core.Database
{
    public class OcppCoreContext : DbContext
    {
        

        public virtual DbSet<ChargePoint> ChargePoints { get; set; } = null!;
        public virtual DbSet<ChargeTag> ChargeTags { get; set; } = null!;
        public virtual DbSet<ChargingProfile> ChargingProfiles { get; set; } = null!;
        public virtual DbSet<ConnectorStatus> ConnectorStatuses { get; set; } = null!;
        public virtual DbSet<ConnectorStatusView> ConnectorStatusViews { get; set; } = null!;
        public virtual DbSet<CpTagAccess> CpTagAccesses { get; set; } = null!;
        public virtual DbSet<MessageLog> MessageLogs { get; set; } = null!;
        public virtual DbSet<SendRequest> SendRequests { get; set; } = null!;
        public virtual DbSet<Transaction> Transactions { get; set; } = null!;

        private IConfiguration _configuration;
        private ILogger _logger;
        public OcppCoreContext(DbContextOptions<OcppCoreContext> options , ILogger logger, IConfiguration config)
            : base(options)
        {
            _configuration = config;
            _logger = logger;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                _logger.Fatal("Database not configured");
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

                entity.HasIndex(e => e.ChargePointId, "fk_cp_cp_id_idx");

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
                    .HasConstraintName("fk_cp_cp_id");
            });

            modelBuilder.Entity<ConnectorStatus>(entity =>
            {
                entity.ToTable("ConnectorStatus");

                entity.HasIndex(e => e.ChargePointId, "fk_cs_cp_id_idx");

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
                    .HasConstraintName("fk_cs_cp_id");
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

                entity.Property(e => e.StartTime).HasColumnType("datetime");

                entity.Property(e => e.StopReason).HasMaxLength(100);

                entity.Property(e => e.StopTime).HasColumnType("datetime");

                entity.Property(e => e.TransactionId).HasDefaultValueSql("'0'");
            });

            modelBuilder.Entity<CpTagAccess>(entity =>
            {
                entity.ToTable("CpTagAccess");

                entity.HasIndex(e => e.ChargePointId, "fk_cta_cp_id_idx");

                entity.HasIndex(e => e.TagId, "fk_tag_cptag_idx");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.CpTagStatus).HasMaxLength(45);

                entity.Property(e => e.Expiry).HasColumnType("datetime");

                entity.Property(e => e.Timestamp).HasColumnType("datetime");

                entity.HasOne(d => d.ChargePoint)
                    .WithMany(p => p.CpTagAccesses)
                    .HasForeignKey(d => d.ChargePointId)
                    .HasConstraintName("fk_cta_cp_id");

                entity.HasOne(d => d.Tag)
                    .WithMany(p => p.CpTagAccesses)
                    .HasForeignKey(d => d.TagId)
                    .HasConstraintName("fk_tag_cptag");
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

            modelBuilder.Entity<SendRequest>(entity =>
            {
                entity.ToTable("SendRequest");

                entity.HasIndex(e => e.ChargeTagId, "fk_ct_sq_id_idx");

                entity.HasIndex(e => e.ChargePointId, "fk_sr_cp_id_idx");

                entity.Property(e => e.CreatedDatetime).HasColumnType("datetime");

                entity.Property(e => e.RequestType).HasMaxLength(45);

                entity.Property(e => e.Status)
                    .HasColumnType("enum('Queued','Sent','Completed','Failed','Cancelled')")
                    .HasDefaultValueSql("'Queued'");

                entity.Property(e => e.Uid).HasMaxLength(45);

                entity.Property(e => e.UpdatedTimestamp)
                    .HasColumnType("timestamp")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(d => d.ChargePoint)
                    .WithMany(p => p.SendRequests)
                    .HasForeignKey(d => d.ChargePointId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_sr_cp_id");

                entity.HasOne(d => d.ChargeTag)
                    .WithMany(p => p.SendRequests)
                    .HasForeignKey(d => d.ChargeTagId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_ct_sq_id");
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasIndex(e => e.StartTagId, "fk_starttagid_idx");

                entity.HasIndex(e => e.ChargePointId, "fk_tx_cp_id_idx");

                entity.HasIndex(e => e.StopTagId, "sk_stoptagid_ct_idx");

                entity.Property(e => e.StartResult).HasMaxLength(100);

                entity.Property(e => e.StartTime).HasColumnType("datetime");

                entity.Property(e => e.StopReason).HasMaxLength(100);

                entity.Property(e => e.StopTime).HasColumnType("datetime");

                entity.Property(e => e.TransactionStatus).HasColumnType("enum('Started','Terminated','Completed')");

                entity.Property(e => e.Uid).HasMaxLength(45);

                entity.HasOne(d => d.ChargePoint)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(d => d.ChargePointId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_tx_cp_id");

                entity.HasOne(d => d.StartTag)
                    .WithMany(p => p.TransactionStartTags)
                    .HasForeignKey(d => d.StartTagId)
                    .HasConstraintName("fk_starttagid_ct");

                entity.HasOne(d => d.StopTag)
                    .WithMany(p => p.TransactionStopTags)
                    .HasForeignKey(d => d.StopTagId)
                    .HasConstraintName("sk_stoptagid_ct");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        private void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            throw new NotImplementedException();
        }
    }
}
