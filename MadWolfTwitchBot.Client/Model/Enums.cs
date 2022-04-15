using System;
using System.Collections.Generic;
using System.Text;

namespace MadWolfTwitchBot.Client.Model
{
   public enum OAuthTokenStatus
    {
        None = 0,
        NotValidated = 1,
        Validated = 2,
        NeedsValidating = 3
    }
}
