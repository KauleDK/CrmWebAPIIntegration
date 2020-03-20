using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CrmWebAPISample
{
    public class AccountEntity : IEntity
    {
        public const string EntityLogicalName = "accounts";

        [JsonProperty(PropertyName = "accountid")]
        public string CRMId { get; set; }

        [JsonProperty("accountnumber")]
        public string AccountNumber { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("mcs_name2")]
        public string mcs_Name2 { get; set; }

        [JsonProperty("address1_line1")]
        public string Address1_Line1 { get; set; }

        [JsonProperty("address1_line2")]
        public string Address1_Line2 { get; set; }

        [JsonProperty("address1_postalcode")]
        public string Address1_PostalCode { get; set; }

        [JsonProperty("address1_city")]
        public string Address1_City { get; set; }

        [JsonProperty("telephone1")]
        public string Telephone1 { get; set; }

        [JsonProperty("emailaddress1")]
        public string EMailAddress1 { get; set; }


        public string GetCrmId()
        {
            return CRMId;
        }

        public string GetEntityLogicalName()
        {
            return EntityLogicalName;
        }
    }
}
