using System;
using System.Collections.Generic;
using System.Text;

namespace CrmWebAPISample
{
    public interface IEntity
    {
        string GetCrmId();
        string GetEntityLogicalName();
    }
}
