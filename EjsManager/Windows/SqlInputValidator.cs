using System;
using System.Collections.Generic;
using System.Text;

namespace SiliconStudio.Meet.EjsManager
{
    internal static class StringValidation
    {
        internal static bool ValidSqlInputVariable(string value)
        {
			if (value == "")
				return true;

            if (value.ToLower().Contains("delete")
               || value.ToLower().Contains("update")
               || value.ToLower().Contains("select")
               || value.ToLower().Contains("insert"))
                return true;
            else
                return false;
        }

    }
}
