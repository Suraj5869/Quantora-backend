using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Quantora.Infrastructure.Persistence;

namespace Quantora.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class TestController : ControllerBase
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public TestController(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        [HttpGet("database")]
        public async Task<IActionResult> CheckDatabase(
            CancellationToken cancellationToken)
        {
            await using var connection =
                (Npgsql.NpgsqlConnection)_connectionFactory.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            await using var command =
                new Npgsql.NpgsqlCommand("SELECT 1", connection);

            var result = await command.ExecuteScalarAsync(
                cancellationToken);

            return Ok(new
            {
                success = result is 1,
                database = "PostgreSQL"
            });
        }
    }
}
