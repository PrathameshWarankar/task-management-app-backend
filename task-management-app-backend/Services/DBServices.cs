using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using task_management_app_backend.Models;

namespace task_management_app_backend.Services
{
    public class DBServices
    {
        private readonly IConfiguration _configuration;
        private readonly CosmosClient cosmosClient;

        public DBServices(IConfiguration configuration, SecretClient secretClient)
        {
            _configuration = configuration;
            cosmosClient = new CosmosClient(secretClient.GetSecret("cosmos-db-connection-string").Value.Value);
        }

        public async Task<Status> CreateUser(Models.User user)
        {
            try
            {
                //check if database exists
                var database = await cosmosClient.CreateDatabaseIfNotExistsAsync("task-management-db");

                //check if container exists
                var container = await database.Database.CreateContainerIfNotExistsAsync("Users", "/id");

                //check if email exists
                var query = new QueryDefinition("SELECT * FROM Users u WHERE u.email = @email")
                                .WithParameter("@email", user.Email);

                var iterator = container.Container.GetItemQueryIterator<Models.User>(query);
                if (iterator.HasMoreResults)
                {
                    var results = await iterator.ReadNextAsync();

                    if (results.Any())
                    {
                        throw new Exception("Email already exists");
                    }
                }

                //hash password before adding to database
                var hasher = new PasswordHasher<Models.User>();
                user.Password = hasher.HashPassword(null, user.Password);

                //add entry to database
                await container.Container.CreateItemAsync(user, new PartitionKey(user.Id));

                return new Status
                {
                    IsError = false,
                    ErrorMessage = null,
                    ErrorCode = null
                };
            }
            catch (CosmosException ex)
            {
                return new Status
                {
                    IsError = true,
                    ErrorMessage = ex.Message,
                    ErrorCode = ((int)ex.StatusCode).ToString()
                };
            }
            catch (Exception ex)
            {
                return new Status
                {
                    IsError = true,
                    ErrorMessage = ex.Message,
                    ErrorCode = "500"
                };
            }
        }
    }
}
