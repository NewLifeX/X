using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer
{
    class SqlCeSession : DbSession<SqlCeSession>
    {

    }

    class SqlCe : DbBase<SqlCe, SqlCeSession>
    {
    }
}
