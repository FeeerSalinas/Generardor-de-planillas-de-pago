namespace ProyectoANF.Models
{
    public class ErrorViewModel
    {
        internal string ErrorMessage;

        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
