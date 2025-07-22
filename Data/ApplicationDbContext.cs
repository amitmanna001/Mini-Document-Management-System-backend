using DocManagementWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DocManagementWebApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options) { }

        public DbSet<DocumentModel> DocumentData { get; set; }
        public DbSet<PdfStoreModel> PdfData { get; set; }
    }
}
