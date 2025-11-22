using Microsoft.EntityFrameworkCore;
using StudentApi.Models;
using System.Collections.Generic;

namespace StudentApi.Data
{
    public class StudentDbContext : DbContext
    {
        public StudentDbContext(DbContextOptions<StudentDbContext> options) : base(options) { }

        public DbSet<Student> Students { get; set; }
    }

}
