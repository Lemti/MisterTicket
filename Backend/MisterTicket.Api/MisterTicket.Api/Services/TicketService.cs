using iTextSharp.text;
using iTextSharp.text.pdf;
using QRCoder;

namespace MisterTicket.Api.Services
{
    public class TicketService : ITicketService
    {
        public byte[] GenerateTicketPdf(int reservationId, string userName, string eventName, List<string> seatNumbers, decimal totalAmount)
        {
            using var memoryStream = new MemoryStream();

            // Créer le document PDF
            var document = new Document(PageSize.A4, 50, 50, 25, 25);
            var writer = PdfWriter.GetInstance(document, memoryStream);

            document.Open();

            // Titre
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 24);
            var title = new Paragraph("BILLET ROLAND GARROS", titleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20
            };
            document.Add(title);

            // Ligne de séparation
            document.Add(new Paragraph("_____________________________________________")
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20
            });

            // Informations du billet
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);

            document.Add(new Paragraph($"Réservation N° : {reservationId}", boldFont) { SpacingAfter = 10 });
            document.Add(new Paragraph($"Nom : {userName}", normalFont) { SpacingAfter = 10 });
            document.Add(new Paragraph($"Événement : {eventName}", normalFont) { SpacingAfter = 10 });
            document.Add(new Paragraph($"Siège(s) : {string.Join(", ", seatNumbers)}", normalFont) { SpacingAfter = 10 });
            document.Add(new Paragraph($"Montant : {totalAmount:C}", normalFont) { SpacingAfter = 20 });

            // Générer le QR Code
            var qrGenerator = new QRCodeGenerator();
            var qrData = $"RESERVATION-{reservationId}-{userName}-{string.Join("-", seatNumbers)}";
            var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);

            // Ajouter le QR Code au PDF
            var qrImage = Image.GetInstance(qrCodeBytes);
            qrImage.ScaleToFit(200f, 200f);
            qrImage.Alignment = Element.ALIGN_CENTER;
            document.Add(qrImage);

            // Footer
            document.Add(new Paragraph("\n\nPrésentez ce billet à l'entrée", normalFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingBefore = 20
            });

            document.Close();
            writer.Close();

            return memoryStream.ToArray();
        }
    }
}