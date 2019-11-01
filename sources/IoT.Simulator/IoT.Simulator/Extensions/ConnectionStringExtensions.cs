namespace IoT.Simulator.Extensions
{
    public static class ConnectionStringExtensions
    {
        public static string ExtractValue(this string data, string key)
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;

            int indexOfStartHostname = data.IndexOf($"{key}=");

            if (indexOfStartHostname >= 0)
            {
                string tempData = data.Substring(indexOfStartHostname, data.Length - 1 - indexOfStartHostname);
                int indexOfEndHostname = tempData.IndexOf(";");

                if (indexOfEndHostname >= 0)
                    return tempData.Substring(0, indexOfEndHostname).Remove(0, key.Length + 1);
                else
                    return string.Empty;
            }
            else return string.Empty;
        }
    }
}
