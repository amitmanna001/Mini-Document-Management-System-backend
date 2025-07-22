namespace DocManagementWebApi.Models
{
    public class DocumentModel
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string Tag1 { get; set; }
        public required string Tag2 { get; set; }
        public required string Tag3 { get; set; }
        public required string UploadDate { get; set; }
        public required string Filename { get; set; }
    }
}
