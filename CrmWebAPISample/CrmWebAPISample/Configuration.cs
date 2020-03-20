using System;
using System.Collections.Generic;
using System.Text;

namespace CrmWebAPISample
{
    public class Configuration
    {
        /// <summary>
        /// Address to the Microsoft Azure Active Directory Tenant, used to get an Access token
        /// for use when communicating with Dynamics.
        /// </summary>
        public string AuthorityUrl { get; set; }

        /// <summary>
        /// The root address of the Dynamics CRM service.
        /// </summary>
        /// <example>https://myorg.crm.dynamics.com</example>
        public string ServiceUrl { get; set; }

        /// <summary>
        /// The client ID that was generated when the application was registered in Microsoft Azure
        /// Active Directory or AD FS.
        /// </summary>
        /// <remarks>Required only with a web service configured for OAuth authentication.</remarks>
        public string ClientId { get; set; }

        /// <summary>
        ///  The password generated together with the application user in Microsoft Azure Active Directory.
        ///  This password authenticate the application and must be kept secret.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        ///  The username of the User in Dynamics that the application is impersonating
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        ///  The password of the User in Dynamics that the application is impersonating
        /// </summary>
        public string Password { get; set; }
    }
}
