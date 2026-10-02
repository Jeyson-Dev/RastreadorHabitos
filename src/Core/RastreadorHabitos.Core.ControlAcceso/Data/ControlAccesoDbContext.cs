using Microsoft.EntityFrameworkCore;
using RastreadorHabitos.Core.ControlAcceso.Entities;

namespace RastreadorHabitos.Core.ControlAcceso.Data;

public class ControlAccesoDbContext : DbContext
{
    // Cada pieza es dueña de sus tablas dentro de su propio esquema SQL.
    public const string Esquema = "ControlAcceso";

    public ControlAccesoDbContext(DbContextOptions<ControlAccesoDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<TokenUsuario> TokensUsuario => Set<TokenUsuario>();
    public DbSet<SesionUsuario> SesionesUsuario => Set<SesionUsuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Esquema);

        modelBuilder.Entity<Rol>(builder =>
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Nombre).IsRequired().HasMaxLength(50);
            builder.HasIndex(r => r.Nombre).IsUnique();
            builder.Property(r => r.Descripcion).IsRequired().HasMaxLength(200);

            // Dos roles [RF-CA-04]. El nombre va sin tilde porque se usa como identificador
            // en la credencial de sesión y en las políticas de autorización.
            builder.HasData(
                new Rol { Id = Rol.IdAdministrador, Nombre = "Administrador", Descripcion = "Administrador del sistema" },
                new Rol { Id = Rol.IdEstandar, Nombre = "Estandar", Descripcion = "Usuario estándar" });
        });

        modelBuilder.Entity<Usuario>(builder =>
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
            builder.HasIndex(u => u.Email).IsUnique(); // [RF-CA-01]
            builder.Property(u => u.NombreCompleto).IsRequired().HasMaxLength(150);
            builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(100);

            builder.HasOne(u => u.Rol)
                .WithMany(r => r.Usuarios)
                .HasForeignKey(u => u.RolId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TokenUsuario>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(64); // hex de SHA-256
            builder.HasIndex(t => t.TokenHash).IsUnique();
            builder.Property(t => t.Tipo).IsRequired().HasMaxLength(50);
            builder.HasIndex(t => new { t.UsuarioId, t.Tipo });

            builder.HasOne(t => t.Usuario)
                .WithMany()
                .HasForeignKey(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SesionUsuario>(builder =>
        {
            builder.HasKey(s => s.Id);

            // Para encontrar rápido las sesiones abiertas de un usuario al cerrarlas todas.
            builder.HasIndex(s => new { s.UsuarioId, s.FechaCierreUtc });

            builder.HasOne(s => s.Usuario)
                .WithMany()
                .HasForeignKey(s => s.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
