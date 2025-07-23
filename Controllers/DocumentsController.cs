using CsvHelper;
using CsvHelper.Configuration;
using DocManagementWebApi.Data;
using DocManagementWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO.Compression;

namespace DocManagementWebApi.Controllers
{
    public class DocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DocumentsController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpPost("api/documents/upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is empty or null.");
            }
            //Strip out any path specifiers (ex: /../)
            var originalFileName = Path.GetFileName(file.FileName);
            var arrSplit = originalFileName.Split('.');
            var foldeName = arrSplit[0];
            string currentDirPath = "D:\\MyProj\\DocManagementWebApi\\Files";
            //Create a unique file path
            //var uniqueFileName = Path.GetRandomFileName();
            var uniqueFilePath = Path.Combine(currentDirPath, originalFileName);
            var uploadFileName = Path.Combine(currentDirPath, foldeName);

            // Save the uploaded file to the specified filepath
            if (System.IO.File.Exists(originalFileName))
            {
                System.IO.File.Delete(originalFileName);
            }
            using (var stream = System.IO.File.Create(uniqueFilePath))
            {
                await file.CopyToAsync(stream);
            };

            if (Directory.Exists(uploadFileName)) Directory.Delete(uploadFileName, true);
            //  System.IO.Directory.Delete(currentDirPath, true);
            ZipFile.ExtractToDirectory(uniqueFilePath, currentDirPath);
            var conf = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null
            };

            using (var zip = ZipFile.OpenRead(uniqueFilePath))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    if (entry.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        var csvFilesPath = Path.Combine(currentDirPath, entry.FullName);
                        using (var reader = new StreamReader(csvFilesPath))
                        using (var csv = new CsvReader(reader, conf))
                        {
                            var records = csv.GetRecords<DocumentModel>().ToList();
                            _context.DocumentData.AddRange(records);
                            foreach (var record in records)
                            {
                                var pdfName = record.Filename;
                                var pdfPath = $"{foldeName}/{pdfName}";

                                var pdfStore = new PdfStoreModel
                                {
                                    Id = Guid.NewGuid(),
                                    PdfName = pdfName,
                                    PdfPath = pdfPath
                                };

                                _context.PdfData.Add(pdfStore);
                            }
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }
            var message = new { Text = "File uploaded successfully." };
            return Ok(message);
        }

        [HttpGet("api/documents")]
        public async Task<List<DocumentModel>> Search(string value)
        {
            IQueryable<DocumentModel> query = _context.DocumentData;
            if (!string.IsNullOrEmpty(value))
            {
                query = query.Where(e => e.Title.Contains(value)
                        || e.Description.Contains(value));
            }
            return await query.ToListAsync();
        }
        [HttpGet("api/documents/getAll")]
        public async Task<List<DocumentModel>> GetAllDocuments()
        {
            return await _context.DocumentData.ToListAsync();
        }

        [HttpGet("api/documents/{id}")]
        public async Task<DocumentModel?> GetDocument(Guid id)
        {
            var result = await _context.DocumentData.FindAsync(id);
            if (result != null)
                return result;

            return null;
        }

        [HttpGet("files/{filename}")]
        public async Task<IActionResult> PdfToBase64(string filename)
        {
            if (filename == null || filename.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }
            try
            {
                IQueryable<PdfStoreModel> query = _context.PdfData;
                var pdf = query.Where(x => x.PdfName.Contains(filename)).FirstOrDefault();
                if (pdf == null)
                {
                    return NotFound($"No PDF found for filename: {filename}");
                }
                var projectPath = Directory.GetCurrentDirectory();
                var pdfFullPath = Path.Combine(projectPath, "files", pdf.PdfPath);

                // Read file directly as bytes (no open lock)
                byte[] pdfBytes = await System.IO.File.ReadAllBytesAsync(pdfFullPath);
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
