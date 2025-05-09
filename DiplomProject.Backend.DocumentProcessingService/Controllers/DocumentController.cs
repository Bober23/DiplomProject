﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Image;
using iText.Bouncycastleconnector;
using iText.Commons.Bouncycastle;
using DiplomProject.DTOLibrary;
using iText.IO.Font;
using iText.Kernel.Font;
using Xceed.Words.NET;
using Org.BouncyCastle.Utilities.Zlib;
using Org.BouncyCastle.Crypto.IO;
using Org.BouncyCastle.Asn1.IsisMtt.X509;

namespace DiplomProject.Backend.DocumentProcessingService.Controllers
{
    [Route("[controller]Processing")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        private readonly S3Loader _loader;
        public DocumentController(S3Loader loader)
        {
            _loader = loader;
        }

        [HttpPost("pdf")]
        public async Task<IActionResult> CreateNewPdfDocument([FromQuery]string fileDirectory,[FromQuery]string fileName,List<DocMarkup> MarkupItems)
        {
            byte[] pdfBytes;
            string linkToFile;
            // Создание PDF-документа
            using (var outputStream = new MemoryStream())
            {
                using (PdfWriter writer = new PdfWriter(outputStream))
                {
                    using (PdfDocument pdf = new PdfDocument(writer))
                    {
                        iText.Layout.Document document = new iText.Layout.Document(pdf);

                        foreach (var item in MarkupItems)
                        {
                            if (item.Type == MarkupType.Image)
                            {
                                string content = item.Content.ToString();
                                byte[] imageBytes = Convert.FromBase64String(content);
                                ImageData imageData = ImageDataFactory.Create(imageBytes);
                                Image image = new Image(imageData);
                                var currentArea = document.GetRenderer().GetCurrentArea().GetBBox();
                                float documentWidth = currentArea.GetWidth() - document.GetLeftMargin() - document.GetRightMargin();
                                // Рассчитываем соотношение сторон
                                float ratio = Math.Min(documentWidth / image.GetImageWidth(), 1);

                                // Масштабируем изображение
                                image.Scale(ratio, ratio);

                                // Добавляем изображение
                                document.Add(new Paragraph()
                                    .Add(image)
                                    .SetMarginBottom(14));
                            }
                            else if (item.Type == MarkupType.Text)
                            {
                                string text = item.Content.ToString();
                                document.Add(new Paragraph(text)
                                    .SetFontSize(14));
                            }
                        }

                        document.Close();
                    }
                }

                // Сохраняем содержимое потока в массив байтов
                pdfBytes = outputStream.ToArray();
            }
            using (var outStream = new MemoryStream(pdfBytes))
            {
                linkToFile = await _loader.LoadToS3Cloud(outStream, $"{fileDirectory}/{fileName}.pdf", "application/pdf");
            }
            // Возвращаем PDF как файл
            return Ok(linkToFile);
        }

        [HttpPost("docx")]
        public async Task<IActionResult> CreateNewDocxDocument([FromQuery] string fileDirectory, [FromQuery] string fileName, List<DocMarkup> MarkupItems)
        {
            byte[] pdfBytes;
            string linkToFile;
            string fontPath = "Fonts/TIMES.ttf";
            // Создаем временный MemoryStream для хранения документа
            using (var memoryStream = new MemoryStream())
            {
                // Создаем новый документ
                using (var doc = DocX.Create(memoryStream))
                {
                    // Обрабатываем каждый элемент в MarkupItems
                    foreach (var item in MarkupItems)
                    {
                        if (item.Type == MarkupType.Text)
                        {
                            string text = item.Content.ToString();
                            // Добавляем текст
                            doc.InsertParagraph(text).FontSize(14).SpacingAfter(14).Font("Times New Roman").Alignment = Xceed.Document.NET.Alignment.both;
                        }
                        else if (item.Type == MarkupType.Image)
                        {
                            string content = item.Content.ToString();
                            byte[] imageBytes = Convert.FromBase64String(content);
                            var imageStream = new MemoryStream(imageBytes);
                            // Добавляем изображение
                            imageStream.Position = 0; // Сбрасываем позицию потока
                            var image = doc.AddImage(imageStream);
                            float pageWidthInPixels = 4.5f * 96;
                            var picture = image.CreatePicture(); // Создаем объект Picture
                            picture.Height = (int)(picture.Height * (pageWidthInPixels / picture.Width));
                            picture.Width = (int)pageWidthInPixels;
                            doc.InsertParagraph("").InsertPicture(picture).SpacingAfter(14).Alignment = Xceed.Document.NET.Alignment.center; // Добавляем изображение с отступом
                        }
                    }

                    // Сохраняем документ
                    doc.Save();
                }
                memoryStream.Position = 0;
                linkToFile = await _loader.LoadToS3Cloud(memoryStream, $"{fileDirectory}/{fileName}.docx", "application/docx");
                // Возвращаем документ как массив байтов
                return Ok(linkToFile);
            }
        }

        [HttpGet("doc")]
        public async Task<IActionResult> GetDocument([FromQuery]string link)
        {
            var doc = await _loader.GetFromS3Cloud(link);
            string extension = link.Substring(link.LastIndexOf('.') + 1).ToLower();
            if (doc == null)
            {
                return BadRequest();
            }
            return File(doc.ToArray(), $"application/{extension}");
        }

        
    }
}
