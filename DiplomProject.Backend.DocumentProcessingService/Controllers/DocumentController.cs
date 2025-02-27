using Microsoft.AspNetCore.Http;
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

namespace DiplomProject.Backend.DocumentProcessingService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        [HttpPost("pdf")]
        public async Task<IActionResult> CreateNewPdfDocument([FromQuery]string fileDirectory,List<DocMarkup> MarkupItems)
        {
            byte[] pdfBytes;
            string fontPath = "Fonts/TIMES.ttf";
            PdfFont font = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
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
                                //TODO: REMOVE!!!!!!!
                                //FileStream fileStream = new FileStream("test.png", FileMode.Open, FileAccess.Read);
                                //var memoryStream = new MemoryStream();
                                //fileStream.CopyTo(memoryStream);
                                //ENDREMOVE!!!!
                                var memoryStream = (MemoryStream)item.Content;
                                // Добавление изображения
                                memoryStream.Position = 0;
                                ImageData imageData = ImageDataFactory.Create(memoryStream.ToArray());
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
                                    .SetFont(font)
                                    .SetFontSize(14));
                            }
                        }

                        document.Close();
                    }
                }

                // Сохраняем содержимое потока в массив байтов
                pdfBytes = outputStream.ToArray();
            }

            // Возвращаем PDF как файл
            return File(pdfBytes, "application/pdf", "mew.pdf");
        }
    }
}
