using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Persistence;

public sealed class ErpDbContext(
    DbContextOptions<ErpDbContext> options)
    : DbContext(options)
{
}
