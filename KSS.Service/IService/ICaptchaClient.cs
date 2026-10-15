namespace KSS.Service.IService
{
    public interface ICaptchaClient
    {
        Task<bool> VerifyAsync(string payload);
    }
}
