namespace SDOC.Application.Interfaces.IServices
{
    public interface IErrorNotificationService
    {
        Task NotifySourceErrorAsync( string sourceName, string errorMessage, Exception? exception = null);
    }
}
