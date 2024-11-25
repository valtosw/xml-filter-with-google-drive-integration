using Google.Apis.Drive.v3;
using LAB2.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LAB2.Google_Drive_Services
{
    enum FileTypes
    {
        XML,
        XLSX
    }

    internal class LoadFiles
    {
        public AttributesOptions options = new AttributesOptions();
        public Stream stream;

        public async Task LoadFilesAsync(DriveService service, string[] scopes, FileTypes fileType, Page page)
        {
            var fileList = await service.Files.List().ExecuteAsync();

            List<Google.Apis.Drive.v3.Data.File> typeFiles = fileType switch
            {
                FileTypes.XML => fileList.Files.Where(file => file.MimeType == "text/xml").ToList(),
                FileTypes.XLSX => fileList.Files.Where(file =>
                                    file.MimeType == "application/vnd.ms-excel" ||
                                    file.MimeType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet").ToList(),
                _ => throw new Exception("Unknown type"),
            };

            if (typeFiles.Any())
            {
                if (fileType == FileTypes.XML)
                {
                    var selectedFile = await page.DisplayActionSheet("Select an XML File", "Cancel", null, typeFiles.Select(f => f.Name).ToArray());

                    if (selectedFile != "Cancel")
                    {
                        var fileId = typeFiles.First(f => f.Name == selectedFile).Id;

                        var request = service.Files.Get(fileId);
                        stream = new MemoryStream();
                        await request.DownloadAsync(stream);

                        options = LoadOptionsFromStream(stream);
                    }
                }
                else if (fileType == FileTypes.XLSX)
                {
                    var selectedFile = await page.DisplayActionSheet("Select an XLSX File", "Cancel", null, typeFiles.Select(f => f.Name).ToArray());

                    if (selectedFile != "Cancel")
                    {
                        var fileId = typeFiles.First(f => f.Name == selectedFile).Id;

                        var request = service.Files.Get(fileId);
                        this.stream = new MemoryStream();
                        await request.DownloadAsync(stream);
                    }
                }
            }
            else
            {
                await page.DisplayAlert("No XML Files Found", "There are no XML files in your Google Drive.", "OK");
            }
        }

        public static AttributesOptions LoadOptionsFromStream(Stream xmlStream)
        {
            var options = new AttributesOptions();
            xmlStream.Position = 0;
            var doc = XDocument.Load(xmlStream);

            static List<string?> ExtractValues(XDocument doc, string elementName)
            {
                return doc.Descendants("Student")
                          .SelectMany(student => new[]
                          {
                      student.Attribute(elementName)?.Value,
                      student.Element(elementName)?.Value
                          })
                          .Where(value => !string.IsNullOrEmpty(value))
                          .Distinct()
                          .ToList();
            }

            options.FirstNames = ExtractValues(doc, "FirstName");
            options.LastNames = ExtractValues(doc, "LastName");
            options.Faculties = ExtractValues(doc, "Faculty");
            options.Cathedras = ExtractValues(doc, "Cathedra");
            options.Courses = ExtractValues(doc, "Course");
            options.Addresses = ExtractValues(doc, "Address");
            options.StartDates = ExtractValues(doc, "StartDate");
            options.EndDates = ExtractValues(doc, "EndDate");
            options.RoomNumbers = ExtractValues(doc, "RoomNumber");

            return options;
        }
    }
}
