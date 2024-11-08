using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ProyectoANF.Models;

public partial class PlanillaPagoDbContext : DbContext
{
    public PlanillaPagoDbContext()
    {
    }

    public PlanillaPagoDbContext(DbContextOptions<PlanillaPagoDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Planilla> Planillas { get; set; }

    public virtual DbSet<Trabajadore> Trabajadores { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Planilla>(entity =>
        {
            entity.HasKey(e => e.PlanillaId).HasName("PK__Planilla__D6603A4A2CC74637");

            entity.Property(e => e.PlanillaId).HasColumnName("PlanillaID");
            entity.Property(e => e.Afp)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("AFP");
            entity.Property(e => e.FechaGeneracion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Isss)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("ISSS");
            entity.Property(e => e.Renta).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.SalarioBruto).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.SalarioNeto).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TrabajadorId).HasColumnName("TrabajadorID");

            entity.HasOne(d => d.Trabajador).WithMany(p => p.Planillas)
                .HasForeignKey(d => d.TrabajadorId)
                .HasConstraintName("FK__Planillas__Traba__412EB0B6");
        });

        modelBuilder.Entity<Trabajadore>(entity =>
        {
            entity.HasKey(e => e.TrabajadorId).HasName("PK__Trabajad__C5EF3BC1847BB7F3");

            entity.HasIndex(e => e.Dui, "UQ__Trabajad__C03671B967B2AFED").IsUnique();

            entity.HasIndex(e => e.Nit, "UQ__Trabajad__C7DEC3C29EDB14DE").IsUnique();

            entity.Property(e => e.TrabajadorId).HasColumnName("TrabajadorID");
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Afp)
                .HasMaxLength(20)
                .HasColumnName("AFP");
            entity.Property(e => e.Cargo).HasMaxLength(50);
            entity.Property(e => e.Dui)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("DUI");
            entity.Property(e => e.Isss)
                .HasMaxLength(15)
                .HasColumnName("ISSS");
            entity.Property(e => e.Nit)
                .HasMaxLength(17)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("NIT");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.SalarioBase).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.UsuarioId).HasName("PK__Usuarios__2B3DE7984BBF026B");

            entity.HasIndex(e => e.NombreUsuario, "UQ__Usuarios__6B0F5AE08A5BB03D").IsUnique();

            entity.Property(e => e.UsuarioId).HasColumnName("UsuarioID");
            entity.Property(e => e.Contraseña).HasMaxLength(255);
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NombreUsuario).HasMaxLength(50);
            entity.Property(e => e.Rol).HasMaxLength(20);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
