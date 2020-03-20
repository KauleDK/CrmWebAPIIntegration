using System;

namespace CrmWebAPISample
{
    class Program
    {
        static void Main(string[] args)
        {
            string ClientSecret = "";
            string ClientId = "";
            string OrganizationUrl = "https://<company>.crm4.dynamics.com/";
            string AadInstanceUrl = "https://login.microsoftonline.com/";
            string TenantID = "";
            string CrmAppUserName = "name@company.com";
            string CrmAppPassword = "";

            var config = new Configuration()
            {
                ServiceUrl = OrganizationUrl,
                ClientSecret = ClientSecret,
                ClientId = ClientId,
                AuthorityUrl = $"{AadInstanceUrl}{TenantID}",
                UserName = CrmAppUserName,
                Password = CrmAppPassword
            };

            var crmWebApiService = new CrmWebApiService(config, new Authentication(config));

            var accountService = new AccountService(crmWebApiService);

            var accounts = accountService.GetAllAccountEntities();
        }
    }
}
