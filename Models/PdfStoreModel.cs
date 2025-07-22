namespace DocManagementWebApi.Models
{
    public class PdfStoreModel
    {
        public required Guid Id { get; set; }
        public required string PdfName { get; set; }
        public required string PdfPath { get; set; }
    }
}
