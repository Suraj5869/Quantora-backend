using System.Data;
namespace Quantora.Infrastructure.Persistence;
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
