using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public virtual DbSet<FaceIntruder> FaceIntruders { get; set; } = null!;
}
