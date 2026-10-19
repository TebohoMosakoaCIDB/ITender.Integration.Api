using ITender.Integration.Api.Configuration;
using ITender.Integration.Api.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace ITender.Integration.Api.Infrastructure.Crm
{
    public interface ICrmServiceFactory
    {
        IOrganizationServiceAsync2 Service { get; }
    }

    public sealed class CrmServiceFactory : ICrmServiceFactory, IDisposable
    {
        private readonly ServiceClient _client;

        public CrmServiceFactory(
            IOptions<CrmOptions> crmOptions,
            ISecretProtector secretProtector,
            ILogger<CrmServiceFactory> log)
        {
            var crm = crmOptions.Value;
            var secret = secretProtector.Unprotect(crm.ClientSecret);

            var connectionString =
                $"AuthType=ClientSecret;" +
                $"Url={crm.CrmURL};" +
                $"ClientId={crm.ClientId};" +
                $"ClientSecret={secret};" +
                $"RequireNewInstance=true";

            _client = new ServiceClient(connectionString);

            if (!_client.IsReady)
            {
                // Do not include the connection string or secret in the exception.
                log.LogError("Dataverse connection failed for ClientId {ClientId}. LastError: {LastError}",
                    crm.ClientId, _client.LastError);
                throw new InvalidOperationException(
                    $"Dataverse connection failed for ClientId {crm.ClientId}. See logs for detail.");
            }

            log.LogInformation("Dataverse client connected. ClientId {ClientId}, Url {Url}",
                crm.ClientId, crm.CrmURL);
        }

        public IOrganizationServiceAsync2 Service => _client;

        public void Dispose() => _client.Dispose();
    }
}
