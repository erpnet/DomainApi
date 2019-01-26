namespace ErpNet.DomainApi.Samples
{
    public class ErpCredentials
    {
        public ErpCredentials(string app, string user, string pass, string ln)
        {
            this.ApplicationName = app;
            this.UserName = user;
            this.Password = pass;
            this.Language = ln;
        }
        public string ApplicationName { get; }
        public string UserName { get; }
        public string Password { get; }
        public string Language { get; }
    }
}
