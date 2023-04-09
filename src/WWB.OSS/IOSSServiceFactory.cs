namespace WWB.OSS
{
    public interface IOSSServiceFactory
    {
        IOSSService Create();

        IOSSService Create(string name);
    }
}