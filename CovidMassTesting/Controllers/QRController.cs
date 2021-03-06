﻿using CovidMassTesting.Model.Settings;
using iText.Barcodes;
using iText.Barcodes.Qrcode;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CovidMassTesting.Controllers
{
    /// <summary>
    /// Manages places
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class QRController : ControllerBase
    {
        /// <summary>
        /// Generates PDF File
        /// 
        /// Default variables: https://github.com/scholtz/covid-sk-api/blob/main/CovidMassTesting/Model/Settings/PDFQRConfiguration.cs
        ///
        /// </summary>
        /// <returns></returns>
        [HttpPost("GeneratePDF")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GeneratePDF([FromBody] PDFQRConfiguration configuration)
        {
            try
            {
                var baseConfig = new PDFQRConfiguration();
                if (configuration.CellPaddingBottom == default)
                {
                    configuration.CellPaddingBottom = baseConfig.CellPaddingBottom;
                }

                if (configuration.CellPaddingLeft == default)
                {
                    configuration.CellPaddingLeft = baseConfig.CellPaddingLeft;
                }

                if (configuration.CellPaddingRight == default)
                {
                    configuration.CellPaddingRight = baseConfig.CellPaddingRight;
                }

                if (configuration.CellPaddingTop == default)
                {
                    configuration.CellPaddingTop = baseConfig.CellPaddingTop;
                }

                if (configuration.Columns == default)
                {
                    configuration.Columns = baseConfig.Columns;
                }

                if (configuration.Count == default)
                {
                    configuration.Count = baseConfig.Count;
                }

                if (configuration.Height == default)
                {
                    configuration.Height = baseConfig.Height;
                }

                if (configuration.OffsetBranch == default)
                {
                    configuration.OffsetBranch = baseConfig.OffsetBranch;
                }

                if (configuration.OffsetIter == default)
                {
                    configuration.OffsetIter = baseConfig.OffsetIter;
                }

                if (configuration.PageMarginBottom == default)
                {
                    configuration.PageMarginBottom = baseConfig.PageMarginBottom;
                }

                if (configuration.PageMarginLeft == default)
                {
                    configuration.PageMarginLeft = baseConfig.PageMarginLeft;
                }

                if (configuration.PageMarginRight == default)
                {
                    configuration.PageMarginRight = baseConfig.PageMarginRight;
                }

                if (configuration.PageMarginTop == default)
                {
                    configuration.PageMarginTop = baseConfig.PageMarginTop;
                }

                if (configuration.Prefix == default || configuration.Prefix == "string")
                {
                    configuration.Prefix = baseConfig.Prefix;
                }

                if (configuration.Scale == default)
                {
                    configuration.Scale = baseConfig.Scale;
                }

                if (configuration.Type == default)
                {
                    configuration.Type = baseConfig.Type;
                }

                if (configuration.Width == default)
                {
                    configuration.Width = baseConfig.Width;
                }

                if (configuration.Increment == default)
                {
                    configuration.Increment = baseConfig.Increment;
                }

                if (configuration.TopTextSize == default)
                {
                    configuration.TopTextSize = baseConfig.Increment;
                }

                if (configuration.BottomTextSize == default)
                {
                    configuration.BottomTextSize = baseConfig.Increment;
                }

                if (configuration.Count > 100000)
                {
                    throw new Exception("Limit has been reached");
                }

                return File(ManipulatePdf(configuration), "application/pdf", $"qr-{configuration.Prefix}-{configuration.OffsetIter}-{configuration.Count}.pdf");
            }
            catch (Exception exc)
            {
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        private byte[] ManipulatePdf(PDFQRConfiguration settings)
        {
            using var writer = new MemoryStream();
            var pdfDoc = new PdfDocument(new PdfWriter(writer));
            var doc = new Document(pdfDoc, iText.Kernel.Geom.PageSize.A4);

            doc.SetTopMargin(settings.PageMarginTop);
            doc.SetLeftMargin(settings.PageMarginLeft);
            doc.SetBottomMargin(settings.PageMarginBottom);
            doc.SetRightMargin(settings.PageMarginRight);
            var table = new Table(UnitValue.CreatePercentArray(settings.Columns)).UseAllAvailableWidth();
            for (long i = settings.OffsetIter; i < settings.OffsetIter + settings.Count; i += settings.Increment)
            {
                if (settings.Type == "BarCode")
                {
                    table.AddCell(CreateBarcode($"{i + settings.OffsetBranch}", pdfDoc, settings));
                }
                else
                {
                    table.AddCell(CreateQRCode($"{i + settings.OffsetBranch}", pdfDoc, settings));
                }
            }

            doc.Add(table);

            doc.SetMargins(settings.PageMarginTop, settings.PageMarginRight, settings.PageMarginBottom, settings.PageMarginLeft);
            doc.Close();
            return writer.ToArray();
        }

        private Cell CreateBarcode(string code, PdfDocument pdfDoc, PDFQRConfiguration settings)
        {
            var barcode = new Barcode39(pdfDoc);
            barcode.SetCodeType(Barcode39.ALIGN_CENTER);
            barcode.SetCode(code);
            barcode.SetBarHeight(settings.Height);
            barcode.FitWidth(settings.Width);

            // Create barcode object to put it to the cell as image
            var barcodeObject = barcode.CreateFormXObject(null, null, pdfDoc);
            var cell = new Cell().Add(new Image(barcodeObject));
            cell.SetPaddingTop(settings.CellPaddingTop);
            cell.SetPaddingRight(settings.CellPaddingRight);
            cell.SetPaddingBottom(settings.CellPaddingBottom);
            cell.SetPaddingLeft(settings.CellPaddingLeft);

            cell.SetBorder(new iText.Layout.Borders.DottedBorder(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY, 1));


            return cell;
        }
        private Cell CreateQRCode(string code, PdfDocument pdfDoc, PDFQRConfiguration settings)
        {
            var ret = new Cell();

            var table = new Table(UnitValue.CreatePercentArray(2)).UseAllAvailableWidth();

            table.SetBorderCollapse(BorderCollapsePropertyValue.COLLAPSE);
            table.AddCell(MakeCell(code, pdfDoc, settings));
            table.AddCell(MakeCell(code, pdfDoc, settings));
            ret.Add(table);
            ret.SetBorder(new iText.Layout.Borders.DottedBorder(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY, 1));

            ret.SetMargin(0);
            ret.SetPadding(0);
            return ret;
        }
        private Cell MakeCell(string code, PdfDocument pdfDoc, PDFQRConfiguration settings)
        {
            var cell = new Cell();
            cell.SetKeepTogether(true);
            cell.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
            cell.SetHorizontalAlignment(HorizontalAlignment.CENTER);
            cell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
            if (settings.PrefixAboveQR)
            {
                var p = new Paragraph(settings.Prefix);
                p.SetFontSize(settings.TopTextSize);
                p.SetTextAlignment(TextAlignment.CENTER);
                p.SetVerticalAlignment(VerticalAlignment.BOTTOM);
                p.SetMargin(0);
                p.SetPadding(0);
                cell.Add(p);
            }
            var qrParam = new Dictionary<EncodeHintType, object>
            {
                [EncodeHintType.ERROR_CORRECTION] = ErrorCorrectionLevel.H,
                [EncodeHintType.CHARACTER_SET] = "ASCII"
            };

            var codeInQR = "";
            if (settings.IncludePrefixInQR)
            {
                codeInQR = $"{settings.Prefix}{code}";
            }
            else
            {
                codeInQR = $"{code}";
            }

            var barcode = new BarcodeQRCode(codeInQR, qrParam);
            var barcodeObject = barcode.CreateFormXObject(pdfDoc);

            // Create barcode object to put it to the cell as image
            var image = new Image(barcodeObject);
            image.SetMargins(0, 0, 0, 0);
            image.Scale(settings.Scale, settings.Scale);
            image.SetPadding(0);
            cell.Add(image);
            cell.SetPaddingTop(settings.CellPaddingTop);
            cell.SetPaddingRight(settings.CellPaddingRight);
            cell.SetPaddingBottom(settings.CellPaddingBottom);
            cell.SetPaddingLeft(settings.CellPaddingLeft);
            cell.SetBorder(iText.Layout.Borders.Border.NO_BORDER);

            if (settings.PrefixAboveQR)
            {
                var p = new Paragraph(code);
                p.SetFontSize(settings.BottomTextSize);
                p.SetTextAlignment(TextAlignment.CENTER);
                p.SetVerticalAlignment(VerticalAlignment.TOP);
                p.SetMargin(0);
                p.SetPadding(0);
                cell.Add(p);
            }
            else
            {
                var p = new Paragraph($"{settings.Prefix}{code}");
                p.SetFontSize(settings.BottomTextSize);
                p.SetTextAlignment(TextAlignment.CENTER);
                p.SetVerticalAlignment(VerticalAlignment.TOP);
                p.SetMargin(0);
                p.SetPadding(0);
                cell.Add(p);
            }
            return cell;
        }
    }
}
