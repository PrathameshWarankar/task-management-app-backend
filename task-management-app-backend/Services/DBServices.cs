using Azure;
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
                    ErrorCode = null,
                    Message = null
                };
            }
            catch (CosmosException ex)
            {
                return new Status
                {
                    IsError = true,
                    ErrorCode = ((int)ex.StatusCode).ToString(),
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new Status
                {
                    IsError = true,
                    ErrorCode = "500",
                    Message = ex.Message
                };
            }
        }

        public async Task<Status> LoginUser(string email, string password)
        {
            try
            {

                //check if database exists
                var database = await cosmosClient.CreateDatabaseIfNotExistsAsync("task-management-db");

                //check if container exists
                var container = await database.Database.CreateContainerIfNotExistsAsync("Users", "/id");

                //check if email exists
                var query = new QueryDefinition("SELECT * FROM Users u WHERE u.email = @email")
                                .WithParameter("@email", email);

                var iterator = container.Container.GetItemQueryIterator<Models.User>(query);
                if (iterator.HasMoreResults)
                {
                    var result = await iterator.ReadNextAsync();

                    if (result.Any())
                    {
                        List<Models.User> users = new List<Models.User>();

                        users.AddRange(result);

                        string db_password = users[0].Password;

                        var passwordHasher = new PasswordHasher<IdentityUser>();
                        var passResult = passwordHasher.VerifyHashedPassword(null, db_password, password);

                        if (passResult == PasswordVerificationResult.Success)
                            return new Status { IsError = false, ErrorCode = null, Message = users[0].Id };
                        else
                            return new Status { IsError = true, ErrorCode = "400", Message = "Provided password is incorrect" };
                    }
                    else
                        return new Status { IsError = true, ErrorCode = "400", Message = "No user exists for the provided email. Kindly create a new account" };
                }
                else
                    return new Status { IsError = true, ErrorCode = "400", Message = "No user exists for the provided email. Kindly create a new account" };
            }
            catch (CosmosException ex)
            {
                return new Status
                {
                    IsError = true,
                    ErrorCode = ((int)ex.StatusCode).ToString(),
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new Status
                {
                    IsError = true,
                    ErrorCode = "500",
                    Message = ex.Message
                };
            }
        }
    }
}
