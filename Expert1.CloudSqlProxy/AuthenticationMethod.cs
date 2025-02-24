namespace Expert1.CloudSqlProxy
{
    /// <summary>
    /// Proxy Authentication Method.
    /// </summary>
    public enum AuthenticationMethod
    {
        /// <summary>
        /// Path to google credentials json file.
        /// </summary>
        CredentialFile,
        /// <summary>
        /// Google credentials json file as a string.
        /// </summary>
        JSON
    }
}
