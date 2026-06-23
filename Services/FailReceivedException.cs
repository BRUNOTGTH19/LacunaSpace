namespace LacunaSpace.Services
{
    public class FailReceivedException : Exception
    {
        public FailReceivedException()
            : base("Received 'Fail' response - must restart entire flow")
        {
        }
    }
}
