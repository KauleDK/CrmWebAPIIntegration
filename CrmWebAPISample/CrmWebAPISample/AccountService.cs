using System;
using System.Linq;
using System.Collections.Generic;

namespace CrmWebAPISample
{
    public class AccountService
    {
        CrmWebApiService crmWebApi;

        public AccountService(CrmWebApiService crmWebApi)
        {
            this.crmWebApi = crmWebApi;
        }

        public string CreateAccountEntity(AccountEntity accountEntity)
        {
            string json = JsonHelper.ToJson(accountEntity);

            return crmWebApi.CreateQueryAPI(AccountEntity.EntityLogicalName, json);
        }

        public string UpdateAccountEntity(string crmId, AccountEntity accountEntity)
        {
            string json = JsonHelper.ToJson(accountEntity);

            return crmWebApi.UpdateQueryAPI(AccountEntity.EntityLogicalName, crmId, json);
        }

        public IEnumerable<AccountEntity> GetAllAccountEntities()
        {
            int numberOfPages = 0;
            var accounts = new List<AccountEntity>();

            bool hasMorePages = false;

            string url = crmWebApi.BuildQueryRequest(AccountEntity.EntityLogicalName,
                select: JsonHelper.GetFields(typeof(AccountEntity)));

            do
            {
                if (numberOfPages > crmWebApi.MaxNumberOfPages)
                    break;

                var result = crmWebApi.GetQueryAPI(url);
                url = result.NextLinkPageUrl;
                hasMorePages = !string.IsNullOrWhiteSpace(url);
                numberOfPages++;

                var accountEntities = JsonHelper.FromJsonCollection<AccountEntity>(result.Content);
                accounts.AddRange(accountEntities);


            } while (hasMorePages);

            return accounts;
        }

        public AccountEntity GetAccountEntityByCRMId(string CRMId)
        {
            if (CRMId == null) throw new ArgumentNullException("CRM id");

            string url = crmWebApi.BuildQueryRequest(
                AccountEntity.EntityLogicalName,
                entityId: CRMId,
                select: JsonHelper.GetFields(typeof(AccountEntity))
            );

            return JsonHelper.FromJson<AccountEntity>(crmWebApi.GetQueryAPI(url).Content);
        }

        public void DeleteAccountEntityById(string crmId)
        {
            if (crmId == null) throw new ArgumentNullException("CRM id");

            string url = crmWebApi.BuildQueryRequest(
                AccountEntity.EntityLogicalName,
                entityId: crmId
            );

            crmWebApi.DeleteQueryAPI(url);
        }
    }
}
