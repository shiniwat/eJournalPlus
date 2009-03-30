
namespace EjsBridge
{
    internal static class StringValidation
    {
        internal static bool ValidSqlInputVariable(string value)
        {
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
